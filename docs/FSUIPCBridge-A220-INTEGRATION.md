# Integrating the Synaptic A220 correction into FSUIPCBridge.dll

Technical handoff for the Delta Virtual ACARS maintainer.

## Requested outcome

Add the validated A220 telemetry correction to the existing `Gvagroup.Acars.FSUIPC.FSUIPCBridge` implementation and distribute it through the normal ACARS updater as one updated `FSUIPCBridge.dll`.

Users should not need `A220CompatibilityBridge.dll`, `A220FSUIPCBridge.bin`, a Community package, paid FSUIPC features, or a separate helper application. The existing FSUIPC connection, aircraft support, bridge interfaces, and ACARS recording pipeline should remain intact.

This document describes a source-level integration, not a binary patch recipe. The current bridge source project was not available during the investigation, so the exact internal method names, cache layout and insertion line must be identified by its maintainer. The public interface and working wrapper provide the integration contract.

Reference implementation: https://github.com/Talon42/ACARS-Bridge

## Observed failure and why this correction is needed

In the tested Synaptic A220 configuration, engines were running normally in the cockpit, but the native simulator combustion flags remained false and native fuel flow was near zero. FSUIPC exposed those native values. The stock ACARS bridge gated reported engine RPM on combustion state, producing zero engine RPM and continuous engine-out reporting despite the aircraft operating normally.

Cockpit idle values were approximately 18.5 percent N1, 65 percent N2, and 700-750 pounds per hour per engine. The aircraft's own cockpit telemetry provided the expected values.

Simply changing the native combustion check would not correct fuel flow. Using RPM alone as a running-engine test would also risk reporting a windmilling engine as running. Do not globally remove combustion gating or disable ACARS engine-out warnings. Apply the correction only to this aircraft when its custom telemetry is valid.

The successful path was:

```text
A220 cockpit engine/fuel messages and existing RPM/master L-vars
    -> local Coherent debugger HTTP/WebSocket connection
    -> telemetry cache inside the ACARS process
    -> A220-only correction to the bridge's PositionData
    -> existing ACARS recording/upload/report processing
```

## What the current two-file release actually does

`A220CompatibilityBridge.dll` is a new wrapper implementing ACARS `FlightPlanBridge`. `A220FSUIPCBridge.bin` is the original, unchanged FSUIPCBridge 3.6.6.1 assembly under another filename. The wrapper loads the stock type, delegates bridge operations to it, and corrects its position snapshots.

The original assembly was not rewritten. Its SHA-256 is:

```text
8b0807146481948f8bb68bb6495cea18e4b8c2048946fce1f2014435171acc56
```

The separate filenames exist to avoid ACARS independently discovering both simulator bridge DLLs. Integrating the correction into the original bridge removes that packaging problem entirely.

## Source to reuse and source to leave out

| Reference file | Role in the integrated bridge |
| --- | --- |
| `src/DebuggerTelemetrySource.cs` | Reuse/adapt the local debugger reader, polling, cache, reconnect and cleanup logic. |
| `src/DebugScripts.cs` | Reuse/adapt the embedded temporary cockpit listener and read/cleanup expressions. |
| `src/Adapter.cs` | Reuse the telemetry model, validity rules, aircraft predicate and correction behavior. Implement correction inside the existing bridge. |
| `src/Adapter.Delegation.cs` | Do not port: this is forwarding plumbing needed only by the external wrapper. |
| `tests/BridgeTests.cs` | Adapt correction, fallback and isolation checks to exercise the integrated bridge. Wrapper discovery/dependency checks no longer apply. |
| `tests/RecordingBridge.cs` | Optional test fixture; not a runtime dependency. |

Do not port `Adapter.LoadStock()`, its reflection-based assembly loading, the wrapper's public bridge class, or its requirement for a `.bin` file. Keep one discoverable production simulator bridge class. A helper class should not implement `Bridge` merely to acquire telemetry.

## Required integration changes

### 1. Add aircraft-specific telemetry support inside the existing assembly

Compile the telemetry model, debugger reader and embedded script constants into `FSUIPCBridge.dll`, adjusting namespace/accessibility to the maintainer's conventions. Keep the existing assembly identity, bridge type and supported interfaces unless the official release process intentionally changes them.

The proof was compiled for x64 using the .NET Framework compiler and framework references `System`, `System.Core`, and `System.Web.Extensions`. It uses `ClientWebSocket`, `HttpWebRequest`, `JavaScriptSerializer`, threads and cancellation tokens. Confirm availability against the actual bridge target framework before integrating. No new native `SimConnect.dll` is required by this reader; retain any existing dependencies needed by the stock bridge itself.

Use the project's normal versioning/signing and update packaging. Do not copy a signing identity from the prototype or distribute an unofficial binary as though it were an official signed build.

### 2. Scope acquisition and correction to the A220

The tested predicate is case-insensitive:

```csharp
bool isA220 = aircraft != null &&
    ((aircraft.AircraftPath ?? "").IndexOf("synaptic_a220",
        StringComparison.OrdinalIgnoreCase) >= 0 ||
     (aircraft.AIRPath ?? "").IndexOf("synaptic-aircraft-a220",
        StringComparison.OrdinalIgnoreCase) >= 0);
```

Correction also requires `PositionData.EngineCount == 2`. The predicate identifies the tested Synaptic aircraft paths; it is not a generic correction for every A220 product.

The prototype starts the debugger worker whenever its stock bridge connects, even for another aircraft; the position correction itself is A220-only. For production, start the reader only after the connected aircraft has been identified as the supported A220. Stop it and clear its cache on disconnect or a switch to another aircraft. Clear old samples on an aircraft reload before accepting data from the replacement cockpit target. Aircraft-switch/reload handling is recommended integration work and needs additional testing.

### 3. Obtain actual cockpit engine data

The tested local discovery endpoint is:

```text
GET http://127.0.0.1:19999/pagelist.json
```

Find a page whose title contains both `VCockpit` and `DisplayUnits`, case-insensitively, and whose `id` parses as an integer. Connect to:

```text
ws://127.0.0.1:19999/devtools/page/{id}
```

The proof sends Origin `http://127.0.0.1:19999`, disables proxy use for this loopback connection, and executes the embedded scripts using debugger `Runtime.evaluate` with `returnByValue: true`. Match responses to request IDs and reject protocol errors, thrown script evaluations, invalid payloads and oversized WebSocket messages.

The page-title match alone is not unique aircraft authentication. Use it with the bridge's aircraft identification; consider an additional expected-variable/aircraft check if the target-selection strategy is extended.

The temporary script registers `JS_LISTENER_COMM_BUS`, subscribes to `A22X.Engine Data` and `A22X.Fuel Data`, and requests the existing snapshot with:

```javascript
listener.call('COMM_BUS_WASM_CALLBACK', 'A22X.Resync', '{}');
```

Callbacks receive JSON strings. The script merges each engine/fuel message into its corresponding cached object. Engine-message arrival time is captured with `Date.now()`. It reads existing L-vars through `SimVar.GetSimVarValue`; it does not write L-vars, export new variables, manipulate aircraft controls or change aircraft files.

| Required value | Tested source | Interpretation |
| --- | --- | --- |
| Engine 1/2 N1 | `L:A22X Engine 1 N1`, `L:A22X Engine 2 N1` | Read with unit **`number`**; values already use percent-style magnitude. |
| Engine 1/2 N2 | `L:A22X Engine 1 N2`, `L:A22X Engine 2 N2` | Read with unit **`number`**. |
| Engine 1/2 master | `L:A22X Engine 1 Master`, `L:A22X Engine 2 Master` | Read as `Bool`; greater than 0.5 means enabled. |
| Engine fuel flow | `eng1_fuel_flow`, `eng2_fuel_flow` from engine messages | Convert to pph using the fuel message's units flag. |
| Units | `metric` from fuel messages | Must be a boolean. True means multiply fuel flow by 2.2046226218487757; false means use directly as pph. |
| EGT | `eng1_egt`, `eng2_egt` | Used for engine-running classification; the prototype does not overwrite reported EGT. |
| Windmilling | `eng1_windmill`, `eng2_windmill` | A strict boolean true excludes that engine from running classification. |
| Starting | `eng1_start`, `eng2_start` | Captured by the proof but currently not used in its classification. |

**Do not use the unit `percent` for these custom RPM L-vars.** In the investigation that produced values scaled by 100. An N1 value around 18.5 must remain 18.5, not 1850.

The simulator debugger was reachable with Developer Mode off in the tested session. That is demonstrated behavior for the tested installation, not a guarantee that this endpoint exists on every machine. The bridge must continue functioning if it is unavailable. Another debugger client attached to the same target may interfere; exclusive ownership cannot be assumed.

### 4. Maintain a bounded, thread-safe telemetry cache

The tested reader uses a background thread, not the UI or synchronous position-read path. It polls every 500 ms, retries discovery/connection after approximately 1500 ms, uses 1500 ms HTTP timeouts and 4000 ms WebSocket operation cancellation. The receive path limits a debugger message to 1 MB. These are the reference values, not requirements to change existing FSUIPC sampling frequency.

The cockpit script rejects engine messages older than 4000 ms and waits for a boolean units flag plus numeric fuel-flow values. The managed telemetry model additionally requires:

- Source timestamp age between -1 and +5 seconds relative to UTC now.
- Both engines' N1, N2 and fuel flow finite and nonnegative.
- Each RPM value below 200 and each converted fuel flow below 100,000 pph.

Timestamp is the engine-message arrival time in Unix milliseconds, converted to UTC. Do not stamp each poll as fresh when it is only rereading an old engine message.

On socket loss, parsing failure, stale/invalid values or an unavailable source, clear the correction cache and return normal stock readings. Recovery must reconnect automatically. Never retain the last running-engine state indefinitely or manufacture normal values when the source fails.

For production, consider validating EGT and boolean/message schema more explicitly, bounding script diagnostic error accumulation, and tracking fuel-unit freshness as well as engine freshness. The proof checks EGT through the running-state expression, treats absent optional booleans as false, and retains the last received units flag. Those additional checks are hardening recommendations, not already-completed validation claims.

### 5. Apply correction at the bridge's outgoing position boundary

Find where the stock bridge constructs/returns the `PositionData` consumed by ACARS (public contract: `Bridge.CurrentPosition`). Keep the original FSUIPC read and existing aircraft-specific processing. Apply the A220 override after the stock engine gating that otherwise zeros RPM, before ACARS records or evaluates that snapshot. A later stock normalization must not overwrite the correction.

The tested running-state predicate is evaluated independently for each engine:

```csharp
running = masterEnabled
       && fuelFlowPph > 0
       && n2 >= 14.4
       && egt > 100
       && !windmilling;
```

This is the validated prototype heuristic, not a comprehensive aircraft engine-failure model. Do not infer running solely from RPM. Preserve the per-engine distinction during a single-engine start or shutdown.

Illustrative integration flow (helper names are descriptive, not stock methods):

```csharp
PositionData p = ReadStockPositionUsingExistingCode();

if (p == null || !IsSupportedA220(currentAircraft) || p.EngineCount != 2)
    return p;

Telemetry v;
if (!a220Source.TryGet(out v) || v == null || !v.Valid)
    return p;

// Use the bridge's supported snapshot/copy mechanism if p is shared.
PositionData result = CopyPositionAndItsEngineArrays(p);

result.SetN1(0, v.Running1 ? v.N1a : 0);
result.SetN2(0, v.Running1 ? v.N2a : 0);
result.SetN1(1, v.Running2 ? v.N1b : 0);
result.SetN2(1, v.Running2 ? v.N2b : 0);
result.SetFuelFlow(0, (uint)Math.Round(v.Fuel1Pph));
result.SetFuelFlow(1, (uint)Math.Round(v.Fuel2Pph));
result.EnginesStarted = v.Running1 || v.Running2;
return result;
```

Engine indexes are zero-based. Fuel flow is rounded using the reference `Math.Round` behavior and assigned as unsigned integer pph. The proof supplies valid measured fuel flow even if its running-state heuristic classifies that engine as off; only RPM is zeroed by that heuristic.

These seven values are the only overwritten outputs: N1 for two engines, N2 for two engines, fuel flow for two engines, and the aggregate `EnginesStarted` flag. Fuel quantity, tank contents, position, altitude, heading, speed, weather, payload, controls and unrelated flags remain from the original snapshot. The correction does not integrate fuel flow into a replacement fuel-quantity value.

The wrapper protects the stock object by shallow-cloning the position plus cloning array fields. Inside the real bridge, prefer its existing explicit snapshot/copy mechanism. If modifying an exclusively owned snapshot in place is safe, the maintainer can use that instead. Do not introduce shared-array mutation affecting other consumers or cached samples.

If the real bridge maintains additional internal engine-state caches used independently of `CurrentPosition`, review them for consistency. The wrapper proves correction at the outgoing interface; it does not establish the layout or requirements of private stock caches.

### 6. Tie lifetime and cleanup to the bridge

Start acquisition only for the supported connected aircraft. Stop it on disconnect, dispose, application shutdown, and aircraft switches. Keep HTTP/WebSocket work off the UI and position-read thread. Do not hold the position-cache lock during network waits.

The reference script stores its listener under `window.__dvaAcarsDebugTelemetry` with a unique owner token. Read/cleanup expressions check ownership. A second active owner returns `busy`; an expired owner is cleaned up before replacement. Remove only this reader's own event handlers, not other applications' listeners.

Normal shutdown explicitly runs the cleanup expression. A one-second cockpit watchdog removes the listener after more than ten seconds without a polling lease refresh, covering ACARS crashes or abrupt termination. Keep that independent cleanup path even with managed disposal. The current managed Stop attempts a six-second join, aborts the socket if necessary, and waits one more second; adapt bounded shutdown to the existing bridge lifecycle.

Expose acquisition/fallback status through the bridge's existing logging facilities, preferably on state transitions rather than every sample. The prototype has diagnostic properties, but its forwarding wrapper does not automatically publish them to ACARS logs. Missing-source status should be visible without turning it into a fatal FSUIPC connection error.

### 7. Ship through the normal ACARS update path

Build and sign the integrated `FSUIPCBridge.dll` using the official project. Preserve its existing supported interfaces and simulator support. No server schema or flight-report processing change was needed by the working correction.

An official installer/updater migrating users of the prototype must remove or disable `A220CompatibilityBridge.dll`; otherwise it remains another discoverable bridge and might wrap an obsolete stock assembly. Remove the no-longer-needed `A220FSUIPCBridge.bin` or legacy `StockFSUIPCBridge.bin` as part of that migration. A normal new user should receive just the normal updated stock bridge filename, with no manual rename steps.

Do not use alphabetical DLL naming or filesystem discovery order as a bridge-priority mechanism. Integrating the helper inside the existing bridge avoids this ambiguity.

## Validation already completed

Tested configuration: Synaptic A220 1.0.10; MSFS 2024 v1.22; ACARS 3.5 build 197 beta 3 x64; stock FSUIPCBridge 3.6.6.1; FSUIPC7 7.5.9 Free/unregistered; Developer Mode off.

- Live harness: 24 checks passed, including real cockpit values, socket-loss fallback/reconnect, unavailable endpoint handling, normal cleanup, and interface/delegation behavior.
- Abrupt-exit test: the cockpit listener removed itself after its lease expired.
- Installed ACARS test: engines off, right engine only, both engines running, flight, landing and shutdown were exercised.
- Filed server export: 254 position records, valid powered-flight engine readings, zero Engine Out warnings, blank warning fields throughout, and zero shutdown readings.
- Retained offline suite: 16 checks pass for the current wrapper build, including other-aircraft bypass, stale/NaN fallback, no mutation of the original engine arrays, preservation of unrelated fields and per-engine state.

The filename-only packaging changes were checked offline; the complete live flight validated the earlier installed debugger implementation. The live harness and personal flight exports were removed during cleanup; the repository retains the regression source and a sanitized validation summary. These are bounded results, not proof of compatibility with every aircraft, simulator installation or failure scenario.

## Acceptance tests for the integrated official bridge

| Test | Required result |
| --- | --- |
| Supported A220, both engines off | Correct recorded zero engine state; no fabricated running indication. |
| Right engine only, then left engine | Per-engine N1/N2 and pph track the cockpit; the stopped engine is not reported as combusting. |
| Idle, climb, cruise, descent, shutdown | Correct readings reach saved local records and the filed server report; no continuous false engine-out warnings. |
| Metric and imperial fuel-flow display | Consistent pph output, including a live units change; no double conversion. |
| Windmilling, failed start, engine failure | No false running classification; verify whether the prototype heuristic needs refinement. |
| Missing endpoint, invalid messages, stale data, socket loss | Stock fallback, bounded logging, responsive UI, no stale running values and automatic recovery. |
| Aircraft reload, simulator restart, A220-to-other-aircraft switch | Old cache cleared; correct target reacquired; no A220 override on another aircraft. |
| Representative stock and third-party non-A220 aircraft | Same engine and unrelated telemetry as the unmodified bridge. Include single-, twin- and multi-engine aircraft as supported. |
| Disconnect, normal exit, forced process termination | Reader stops or watchdog expires; no orphan listener. |
| Other existing bridge features | Fuel/payload handling, radios, flight plans, pause/rate, doors, failures and save/restore preserve existing behavior. |
| Clean install and upgrade from prototype | One intended simulator bridge is selected; no separate wrapper or stock-copy binary is required. |

For cross-version support, extend this matrix to each simulator/architecture that the official bridge continues to support. The direct debugger path has only been demonstrated with the tested MSFS 2024/A220 combination; unsupported configurations must keep the original FSUIPC behavior.

## Administrator deliverable

The desired deliverable is an official, versioned `FSUIPCBridge.dll` containing an A220-specific telemetry provider and a narrowly scoped override of outgoing engine data, while retaining the existing bridge implementation for everything else. The supplied source is a working reference for that change; the wrapper assembly and `.bin` packaging are temporary integration scaffolding and should not be carried into the final design.

