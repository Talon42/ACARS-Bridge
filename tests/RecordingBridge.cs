using System; using Gvagroup.Acars; namespace A220Compatibility.Tests { public sealed class RecordingBridge : FlightPlanBridge { public PositionData Position = new PositionData(0,0,0); public AircraftInfo Aircraft = new AircraftInfo(); public int ConnectCalls, DisconnectCalls, PlanCalls; public bool WasDisposed;
public global::Gvagroup.Acars.AircraftInfo AircraftInformation { get { return Aircraft; } }
public global::System.Boolean Connected { get { return default(global::System.Boolean); } }
public global::System.Boolean CrashDetection { set { } }
public global::Gvagroup.Acars.PositionData CurrentPosition { get { return Position; } }
public global::System.Boolean FailureSupport { get { return default(global::System.Boolean); } }
public global::System.Collections.Generic.IDictionary<global::Gvagroup.Acars.Tank, global::System.UInt32> FuelTanks { get { return default(global::System.Collections.Generic.IDictionary<global::Gvagroup.Acars.Tank, global::System.UInt32>); } set { } }
public global::Gvagroup.Acars.FlightPhase GaugePhase { get { return default(global::Gvagroup.Acars.FlightPhase); } }
public global::System.Boolean GaugeSupport { get { return default(global::System.Boolean); } }
public global::System.Boolean IsAsyncConnect { get { return default(global::System.Boolean); } }
public global::System.String Name { get { return "recording stock bridge"; } }
public global::System.Boolean ParkingBrake { set { } }
public global::System.Boolean Paused { set { } }
public global::System.Collections.Generic.IList<global::Gvagroup.Acars.PayloadStation> PayloadStations { get { return default(global::System.Collections.Generic.IList<global::Gvagroup.Acars.PayloadStation>); } set { } }
public global::System.Boolean Ready { get { return default(global::System.Boolean); } }
public global::System.Boolean Remote { get { return default(global::System.Boolean); } }
public global::System.Boolean Running { get { return default(global::System.Boolean); } }
public global::Gvagroup.Acars.SimPlatform SimPlatformInfo { get { return default(global::Gvagroup.Acars.SimPlatform); } }
public global::System.Int32 SimRate { get { return default(global::System.Int32); } set { } }
public global::System.String SimulatorName { get { return default(global::System.String); } }
public global::System.Collections.Generic.ICollection<global::Gvagroup.Acars.SimVersion> Simulators { get { return default(global::System.Collections.Generic.ICollection<global::Gvagroup.Acars.SimVersion>); } }
public global::Gvagroup.Acars.SimVersion SimulatorVersion { get { return default(global::Gvagroup.Acars.SimVersion); } set { } }
public global::Gvagroup.Acars.BridgeStatistics Statistics { get { return default(global::Gvagroup.Acars.BridgeStatistics); } }
public global::Gvagroup.Acars.TransponderState TX { get { return default(global::Gvagroup.Acars.TransponderState); } set { } }
public event global::Gvagroup.Acars.LogEntryEventHandler LogEntry { add { } remove { } }
public global::System.Boolean IsDoorOpen(global::System.Int16 p0) { return default(global::System.Boolean); }
public global::System.Boolean RestoreFlight() { return default(global::System.Boolean); }
public void Connect(global::System.String p0) { ConnectCalls++; }
public void Disconnect() { DisconnectCalls++; }
public void Dispose() { WasDisposed=true; }
public void Failure(global::System.String p0) {  }
public void Ident() {  }
public void LoadPlan(global::System.String p0, global::Gvagroup.Acars.Airline p1, global::System.UInt32 p2) { PlanCalls++; }
public void SaveFlight() {  }
public void SetADF1(global::System.String p0) {  }
public void SetCOM1(global::System.String p0) {  }
public void SetCOM2(global::System.String p0) {  }
public void SetGaugePhase(global::Gvagroup.Acars.FlightPhase p0, global::System.Boolean p1) {  }
public void SetGaugeStatus(global::Gvagroup.Acars.GaugeStatus p0) {  }
public void SetNAV1(global::System.String p0, global::System.Int16 p1) {  }
public void SetNAV2(global::System.String p0, global::System.Int16 p1) {  }
public void SetUTCTime(global::System.DateTime p0) {  }
public void SetXPDR(global::System.String p0) {  }
public void ShowMessage(global::System.String p0, global::System.Boolean p1, global::System.Int32 p2) {  }
public void ToggleExit(global::System.Int16 p0) {  }
public void ToggleExits() {  }
} }
