using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using Gvagroup.Acars;
using A220Compatibility;
using A220Compatibility.Tests;
public static class AdapterProof {
 static List<string> checks=new List<string>();
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks.Add(name);Console.WriteLine("PASS: "+name);}
 sealed class FixedSource:ITelemetrySource {public Telemetry Value;public bool Started,Stopped;public void Start(){Started=true;}public void Stop(){Stopped=true;}public void Dispose(){Stop();}public bool TryGet(out Telemetry v){v=Value;return v!=null;}}
 static Telemetry Valid(){return new Telemetry{CapturedUtc=DateTime.UtcNow,N1a=18.5,N2a=64.56,N1b=18.6,N2b=64.6,Fuel1Pph=717,Fuel2Pph=734,Egt1=554,Egt2=554,Master1=true,Master2=true};}
 static RecordingBridge Stock(){var b=new RecordingBridge();b.Aircraft.AIRPath=@"C:\Proof\synaptic-aircraft-a220\SimObjects\Airplanes\Synaptic_A220\aircraft.cfg";b.Position.EngineCount=2;b.Position.Fuel=5275;b.Position.Heading=123;return b;}
 public static int Main(string[] args){ SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
var report=new Dictionary<string,object>();string root=AppDomain.CurrentDomain.BaseDirectory;
 try{
  Check(!File.Exists(Path.Combine(root,"SimConnect.dll")),"debugger-only proof has no native SimConnect runtime dependency");
  var assembly=Assembly.LoadFrom(Path.Combine(root,"A220CompatibilityBridge.dll"));int count=0;
  foreach(var t in assembly.GetTypes())if(t.GetInterface(typeof(Bridge).FullName)!=null&&!t.IsAbstract){var bridge=(Bridge)assembly.CreateInstance(t.FullName);Check(bridge!=null,"ACARS discovery pattern creates "+t.FullName);var adapter=(Adapter)bridge;Check(adapter.StockBridgeType=="Gvagroup.Acars.FSUIPC.FSUIPCBridge","parameterless adapter loads unchanged stock bridge");report["stockDiscoveryScope"]="Constructor and interface discovery only; unchanged stock Connect was not called.";count++;}
  Check(count==1,"only one concrete simulator bridge in plug-in DLL");
  var stock=Stock();var source=new FixedSource{Value=Valid()};var wrapper=new Adapter(stock,source);
  wrapper.Connect("test");Check(stock.ConnectCalls==1&&source.Started,"connect delegates and starts in-process source");
  var corrected=wrapper.CurrentPosition;
  Check(corrected.GetN1(0)==18.5&&corrected.GetN2(1)==64.6,"fresh A220 telemetry supplies per-engine RPM");
  Check(corrected.GetFuelFlow(0)==717&&corrected.GetFuelFlow(1)==734&&corrected.EnginesStarted,"fresh A220 telemetry supplies fuel flow and started state");
  Check(stock.Position.GetN1(0)==0&&stock.Position.GetFuelFlow(0)==0&&!Object.ReferenceEquals(corrected,stock.Position),"correction does not mutate stock position or its engine arrays");
  Check(corrected.Fuel==5275&&corrected.Heading==123,"unrelated position data preserved");
  source.Value.CapturedUtc=DateTime.UtcNow.AddSeconds(-20);Check(Object.ReferenceEquals(wrapper.CurrentPosition,stock.Position),"stale telemetry falls back to untouched stock data");
  source.Value=Valid();source.Value.Fuel1Pph=Double.NaN;Check(Object.ReferenceEquals(wrapper.CurrentPosition,stock.Position),"invalid telemetry falls back to stock data");
  source.Value=Valid();stock.Aircraft.AIRPath=@"C:\Proof\flybywire_a320_neo\aircraft.cfg";Check(Object.ReferenceEquals(wrapper.CurrentPosition,stock.Position),"other aircraft bypass correction");
  stock.Aircraft.AIRPath=@"C:\Proof\synaptic-aircraft-a220\SimObjects\Airplanes\Synaptic_A220\aircraft.cfg";source.Value=Valid();source.Value.Master1=false;source.Value.Fuel1Pph=0;source.Value.Windmill1=true;
  corrected=wrapper.CurrentPosition;Check(corrected.GetN1(0)==0&&corrected.GetN2(0)==0&&corrected.GetN1(1)>0&&corrected.EnginesStarted,"stopped/windmilling engine not reported as combusting");
  wrapper.LoadPlan("test",null,0);Check(stock.PlanCalls==1,"flight-plan bridge interface preserved");wrapper.Disconnect();Check(stock.DisconnectCalls==1&&source.Stopped,"disconnect delegates and stops telemetry");wrapper.Dispose();Check(stock.WasDisposed,"dispose reaches stock bridge");
  report["passed"]=true;return 0;
 }catch(Exception e){report["passed"]=false;report["error"]=e.ToString();Console.Error.WriteLine(e);return 1;}
 finally{report["checks"]=checks;report["completedUtc"]=DateTime.UtcNow.ToString("o");File.WriteAllText(Path.Combine(root,"test-results.json"),new JavaScriptSerializer().Serialize(report));}
 }
}
