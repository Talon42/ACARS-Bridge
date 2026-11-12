using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gvagroup.Acars;
namespace A220Compatibility {
 public sealed class Telemetry {
  public DateTime CapturedUtc;
  public double N1a,N2a,N1b,N2b,Fuel1Pph,Fuel2Pph,Egt1,Egt2;
  public bool Master1,Master2,Starting1,Starting2,Windmill1,Windmill2;
  public bool Fresh { get { double age=(DateTime.UtcNow-CapturedUtc).TotalSeconds; return age>=-1 && age<=5; } }
  public bool Valid { get { return Fresh && Good(N1a)&&Good(N2a)&&Good(N1b)&&Good(N2b)&&Good(Fuel1Pph)&&Good(Fuel2Pph)&&Fuel1Pph<100000&&Fuel2Pph<100000&&N1a<200&&N1b<200&&N2a<200&&N2b<200; } }
  static bool Good(double d){return !Double.IsNaN(d)&&!Double.IsInfinity(d)&&d>=0;}
  public bool Running1 { get {return Master1&&Fuel1Pph>0&&N2a>=14.4&&Egt1>100&&!Windmill1;} }
  public bool Running2 { get {return Master2&&Fuel2Pph>0&&N2b>=14.4&&Egt2>100&&!Windmill2;} }
 }
 public interface ITelemetrySource : IDisposable { void Start(); void Stop(); bool TryGet(out Telemetry value); }
 public sealed partial class Adapter {
  readonly Bridge inner; readonly ITelemetrySource source;
  public Adapter() : this(LoadStock(),new DebuggerTelemetrySource()) { }
  public Adapter(Bridge stock,ITelemetrySource telemetry){if(stock==null||telemetry==null)throw new ArgumentNullException();inner=stock;source=telemetry;}
  public string StockBridgeType {get{return inner.GetType().FullName;}}
  static Bridge LoadStock(){
   string path=Path.Combine(Path.GetDirectoryName(typeof(Adapter).Assembly.Location),"A220FSUIPCBridge.bin");
   if(!File.Exists(path))throw new FileNotFoundException("The unchanged stock FSUIPC bridge is required as A220FSUIPCBridge.bin beside this plug-in.",path);
   return (Bridge)Assembly.LoadFrom(path).CreateInstance("Gvagroup.Acars.FSUIPC.FSUIPCBridge");
  }
  public static bool IsA220(AircraftInfo a){return a!=null&&((a.AircraftPath??"").IndexOf("synaptic_a220",StringComparison.OrdinalIgnoreCase)>=0||(a.AIRPath??"").IndexOf("synaptic-aircraft-a220",StringComparison.OrdinalIgnoreCase)>=0);}
  PositionData CorrectedPosition(){
   PositionData p=inner.CurrentPosition;
   if(p==null||!IsA220(inner.AircraftInformation)||p.EngineCount!=2)return p;
   Telemetry v;if(!source.TryGet(out v)||v==null||!v.Valid)return p;
   PositionData copy=Clone(p);
   copy.SetN1(0,v.Running1?v.N1a:0);copy.SetN2(0,v.Running1?v.N2a:0);
   copy.SetN1(1,v.Running2?v.N1b:0);copy.SetN2(1,v.Running2?v.N2b:0);
   copy.SetFuelFlow(0,(uint)Math.Round(v.Fuel1Pph));copy.SetFuelFlow(1,(uint)Math.Round(v.Fuel2Pph));
   copy.EnginesStarted=v.Running1||v.Running2;
   return copy;
  }
  static readonly MethodInfo CloneMethod=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic);
  static readonly Dictionary<Type,FieldInfo[]> ArrayFields=new Dictionary<Type,FieldInfo[]>();
  static PositionData Clone(PositionData p){
   PositionData c=(PositionData)CloneMethod.Invoke(p,null);FieldInfo[] fields;
   lock(ArrayFields){if(!ArrayFields.TryGetValue(p.GetType(),out fields)){var list=new List<FieldInfo>();for(Type t=p.GetType();t!=null;t=t.BaseType)foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly))if(f.FieldType.IsArray)list.Add(f);fields=list.ToArray();ArrayFields[p.GetType()]=fields;}}
   foreach(var f in fields){Array value=f.GetValue(p) as Array;if(value!=null)f.SetValue(c,value.Clone());}
   return c;
  }
 }
}
