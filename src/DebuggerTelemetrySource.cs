using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
namespace A220Compatibility {
 public sealed class DebuggerTelemetrySource : ITelemetrySource {
  readonly object gate=new object();readonly ManualResetEvent stopping=new ManualResetEvent(false);readonly string owner=Guid.NewGuid().ToString("N");readonly int port;
  Thread worker;ClientWebSocket socket;Telemetry latest;int requestId;
  public string LastError {get;private set;}public string LastPayload {get;private set;}public string LastState {get;private set;}public int Connections {get;private set;}public int SamplesReceived {get;private set;}public bool CleanupConfirmed {get;private set;}
  readonly JavaScriptSerializer json=new JavaScriptSerializer();
  public DebuggerTelemetrySource():this(19999){}public DebuggerTelemetrySource(int debugPort){port=debugPort;}
  public void Start(){lock(gate){if(worker!=null)return;stopping.Reset();CleanupConfirmed=false;worker=new Thread(Run);worker.IsBackground=true;worker.Name="A220 debugger telemetry inside ACARS";worker.Start();}}
  public void Stop(){Thread t;lock(gate){stopping.Set();t=worker;}if(t!=null&&!t.Join(6000)){lock(gate){if(socket!=null)socket.Abort();}t.Join(1000);}lock(gate){latest=null;if(t==null||!t.IsAlive)worker=null;}}
  public void Dispose(){Stop();}
  public bool TryGet(out Telemetry value){lock(gate){value=latest;return value!=null&&value.Valid;}}
  public void DisconnectForTest(){lock(gate){if(socket!=null)socket.Abort();}}
  void Clear(){lock(gate){latest=null;}}
  string Owned(string script){return script.Replace("OWNER",json.Serialize(owner));}
  string FindPage(){
   var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+"/pagelist.json");request.Proxy=null;request.Timeout=1500;request.ReadWriteTimeout=1500;
   using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){
    string body=reader.ReadToEnd();if(body.Length>1000000)throw new IOException("Unexpected debug page list size");
    object[] pages=json.DeserializeObject(body) as object[];if(pages==null)throw new IOException("Invalid debug page list");
    foreach(object page in pages){var d=page as Dictionary<string,object>;if(d==null||!d.ContainsKey("title"))continue;
     string title=Convert.ToString(d["title"]);int id;if(title.IndexOf("VCockpit",StringComparison.OrdinalIgnoreCase)>=0&&title.IndexOf("DisplayUnits",StringComparison.OrdinalIgnoreCase)>=0&&d.ContainsKey("id")&&Int32.TryParse(Convert.ToString(d["id"]),out id))return id.ToString();
    }
   }throw new IOException("A220 display debug target not found");
  }
  void Run(){
   while(!stopping.WaitOne(0)){
    ClientWebSocket ws=null;bool attached=false;
    try{
     LastState="discovering";string page=FindPage();if(stopping.WaitOne(0))break;
     ws=new ClientWebSocket();ws.Options.Proxy=null;ws.Options.SetRequestHeader("Origin","http://127.0.0.1:"+port);
     lock(gate){socket=ws;}using(var timeout=new CancellationTokenSource(4000)){ws.ConnectAsync(new Uri("ws://127.0.0.1:"+port+"/devtools/page/"+page),timeout.Token).GetAwaiter().GetResult();}
     Connections++;string status=Evaluate(ws,Owned(DebugScripts.Attach));if(status!="attached")throw new IOException("Debug telemetry listener: "+status);attached=true;LastState="connected";LastError=null;
     while(!stopping.WaitOne(0)){
      LastPayload=Evaluate(ws,Owned(DebugScripts.Read));var d=json.DeserializeObject(LastPayload) as Dictionary<string,object>;
      if(d==null)throw new IOException("Invalid telemetry payload");
      if(B(d,"valid")){
       var v=new Telemetry{CapturedUtc=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(N(d,"epochMs")),N1a=N(d,"n1a"),N2a=N(d,"n2a"),N1b=N(d,"n1b"),N2b=N(d,"n2b"),Fuel1Pph=N(d,"fuel1"),Fuel2Pph=N(d,"fuel2"),Egt1=N(d,"egt1"),Egt2=N(d,"egt2"),Master1=B(d,"master1"),Master2=B(d,"master2"),Starting1=B(d,"starting1"),Starting2=B(d,"starting2"),Windmill1=B(d,"windmill1"),Windmill2=B(d,"windmill2")};
       if(v.Valid){lock(gate){latest=v;}SamplesReceived++;LastState="live";}else{Clear();LastState="invalid telemetry";}
      }else{Clear();LastState=d.ContainsKey("reason")?Convert.ToString(d["reason"]):"waiting";}
      if(stopping.WaitOne(500))break;
     }
    }catch(Exception e){LastError=e.GetBaseException().Message;LastState="unavailable";Clear();}
    finally{
     if(ws!=null){if(attached&&ws.State==WebSocketState.Open){try{CleanupConfirmed=Evaluate(ws,Owned(DebugScripts.Cleanup))=="removed";}catch{}}ws.Dispose();}
     lock(gate){socket=null;latest=null;}
    }
    if(!stopping.WaitOne(0))stopping.WaitOne(1500);
   }
   LastState="stopped";
  }
  static double N(Dictionary<string,object>d,string key){if(!d.ContainsKey(key)||d[key]==null)throw new IOException("Missing "+key);return Convert.ToDouble(d[key]);}
  static bool B(Dictionary<string,object>d,string key){return d.ContainsKey(key)&&d[key]!=null&&Convert.ToBoolean(d[key]);}
  string Evaluate(ClientWebSocket ws,string expression){
   int id=++requestId;var payload=new Dictionary<string,object>{{"id",id},{"method","Runtime.evaluate"},{"params",new Dictionary<string,object>{{"expression",expression},{"returnByValue",true}}}};
   byte[] bytes=Encoding.UTF8.GetBytes(json.Serialize(payload));
   using(var timeout=new CancellationTokenSource(4000)){
    ws.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,timeout.Token).GetAwaiter().GetResult();
    while(true){using(var buffer=new MemoryStream()){
     var chunk=new byte[8192];WebSocketReceiveResult r;
     do{r=ws.ReceiveAsync(new ArraySegment<byte>(chunk),timeout.Token).GetAwaiter().GetResult();if(r.MessageType==WebSocketMessageType.Close)throw new IOException("Cockpit debugger disconnected");buffer.Write(chunk,0,r.Count);if(buffer.Length>1000000)throw new IOException("Oversized debugger message");}while(!r.EndOfMessage);
     var response=json.DeserializeObject(Encoding.UTF8.GetString(buffer.ToArray())) as Dictionary<string,object>;
     if(response==null||!response.ContainsKey("id")||Convert.ToInt32(response["id"])!=id)continue;
     if(response.ContainsKey("error"))throw new IOException(json.Serialize(response["error"]));
     var result=(Dictionary<string,object>)response["result"];if(result.ContainsKey("wasThrown")&&Convert.ToBoolean(result["wasThrown"]))throw new IOException("Cockpit evaluation failed: "+json.Serialize(result));
     var value=(Dictionary<string,object>)result["result"];return value.ContainsKey("value")?Convert.ToString(value["value"]):"";
    }}
   }
  }
 }
}
