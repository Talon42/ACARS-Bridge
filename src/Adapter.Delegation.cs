using System; using Gvagroup.Acars; namespace A220Compatibility { public sealed partial class Adapter : FlightPlanBridge {
public global::Gvagroup.Acars.AircraftInfo AircraftInformation { get { return inner.AircraftInformation; } }
public global::System.Boolean Connected { get { return inner.Connected; } }
public global::System.Boolean CrashDetection { set { inner.CrashDetection = value; } }
public global::Gvagroup.Acars.PositionData CurrentPosition { get { return CorrectedPosition(); } }
public global::System.Boolean FailureSupport { get { return inner.FailureSupport; } }
public global::System.Collections.Generic.IDictionary<global::Gvagroup.Acars.Tank, global::System.UInt32> FuelTanks { get { return inner.FuelTanks; } set { inner.FuelTanks = value; } }
public global::Gvagroup.Acars.FlightPhase GaugePhase { get { return inner.GaugePhase; } }
public global::System.Boolean GaugeSupport { get { return inner.GaugeSupport; } }
public global::System.Boolean IsAsyncConnect { get { return inner.IsAsyncConnect; } }
public global::System.String Name { get { return inner.Name; } }
public global::System.Boolean ParkingBrake { set { inner.ParkingBrake = value; } }
public global::System.Boolean Paused { set { inner.Paused = value; } }
public global::System.Collections.Generic.IList<global::Gvagroup.Acars.PayloadStation> PayloadStations { get { return inner.PayloadStations; } set { inner.PayloadStations = value; } }
public global::System.Boolean Ready { get { return inner.Ready; } }
public global::System.Boolean Remote { get { return inner.Remote; } }
public global::System.Boolean Running { get { return inner.Running; } }
public global::Gvagroup.Acars.SimPlatform SimPlatformInfo { get { return inner.SimPlatformInfo; } }
public global::System.Int32 SimRate { get { return inner.SimRate; } set { inner.SimRate = value; } }
public global::System.String SimulatorName { get { return inner.SimulatorName; } }
public global::System.Collections.Generic.ICollection<global::Gvagroup.Acars.SimVersion> Simulators { get { return inner.Simulators; } }
public global::Gvagroup.Acars.SimVersion SimulatorVersion { get { return inner.SimulatorVersion; } set { inner.SimulatorVersion = value; } }
public global::Gvagroup.Acars.BridgeStatistics Statistics { get { return inner.Statistics; } }
public global::Gvagroup.Acars.TransponderState TX { get { return inner.TX; } set { inner.TX = value; } }
public event global::Gvagroup.Acars.LogEntryEventHandler LogEntry { add { inner.LogEntry += value; } remove { inner.LogEntry -= value; } }
public global::System.Boolean IsDoorOpen(global::System.Int16 p0) { return inner.IsDoorOpen(p0); }
public global::System.Boolean RestoreFlight() { return inner.RestoreFlight(); }
public void Connect(global::System.String p0) { inner.Connect(p0); source.Start(); }
public void Disconnect() { source.Stop(); inner.Disconnect(); }
public void Dispose() { source.Dispose(); inner.Dispose(); }
public void Failure(global::System.String p0) { inner.Failure(p0); }
public void Ident() { inner.Ident(); }
public void LoadPlan(global::System.String p0, global::Gvagroup.Acars.Airline p1, global::System.UInt32 p2) { ((FlightPlanBridge)inner).LoadPlan(p0,p1,p2); }
public void SaveFlight() { inner.SaveFlight(); }
public void SetADF1(global::System.String p0) { inner.SetADF1(p0); }
public void SetCOM1(global::System.String p0) { inner.SetCOM1(p0); }
public void SetCOM2(global::System.String p0) { inner.SetCOM2(p0); }
public void SetGaugePhase(global::Gvagroup.Acars.FlightPhase p0, global::System.Boolean p1) { inner.SetGaugePhase(p0, p1); }
public void SetGaugeStatus(global::Gvagroup.Acars.GaugeStatus p0) { inner.SetGaugeStatus(p0); }
public void SetNAV1(global::System.String p0, global::System.Int16 p1) { inner.SetNAV1(p0, p1); }
public void SetNAV2(global::System.String p0, global::System.Int16 p1) { inner.SetNAV2(p0, p1); }
public void SetUTCTime(global::System.DateTime p0) { inner.SetUTCTime(p0); }
public void SetXPDR(global::System.String p0) { inner.SetXPDR(p0); }
public void ShowMessage(global::System.String p0, global::System.Boolean p1, global::System.Int32 p2) { inner.ShowMessage(p0, p1, p2); }
public void ToggleExit(global::System.Int16 p0) { inner.ToggleExit(p0); }
public void ToggleExits() { inner.ToggleExits(); }
} }
