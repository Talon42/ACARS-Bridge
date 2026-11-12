# Validation record

## Failure path

In the observed A220 session, native simulator combustion flags remained false and native fuel flow was near zero while cockpit RPM and fuel flow were normal. FSUIPC mirrored those values. The stock ACARS bridge gated engine RPM on combustion state. The adapter addresses the data mismatch at the ACARS bridge boundary using the cockpit's actual engine telemetry.

## Completed checks

The debugger-only live harness passed 24 checks covering plug-in discovery, stock bridge loading, corrected RPM/fuel flow, unchanged original snapshots, preservation of unrelated fields, stale/invalid fallback, other-aircraft bypass, per-engine running state, flight-plan delegation, lifecycle forwarding, continuous live updates, socket-loss fallback and reconnection, an unavailable endpoint, and normal listener cleanup.

An additional abrupt-exit test confirmed that the temporary cockpit listener removed itself after its ten-second lease expired. The user confirmed Developer Mode was off during endpoint and live-telemetry checks. No external debugger application was required.

The adapter was then installed in ACARS and exercised through engines off, right engine only, both engines running, flight, landing and shutdown. The cockpit showed approximately 18.5% N1, 65% N2 and 700-750 pph per engine at idle. ACARS recorded corrected engine values.

The completed server-side flight export contained 254 position records, valid engine readings during powered flight, zero Engine Out warnings and blank warning fields throughout. Shutdown readings returned to zero. The local recorded-flight check also found no airborne engine-out threshold hits. This establishes the full installed adapter-to-filed-report path for that flight. Personal flight exports and session logs are deliberately not included in this repository.

## Tested configuration

| Component | Version |
| --- | --- |
| Synaptic A220 | 1.0.10 |
| MSFS 2024 | v1.22 |
| Delta Virtual ACARS | 3.5 build 197 beta 3, x64 |
| Stock FSUIPCBridge | 3.6.6.1 |
| FSUIPC7 | 7.5.9, Free/unregistered |
| Developer Mode | Off |

The packaged source is the tested debugger-only implementation with the stock dependency filename changed to `A220FSUIPCBridge.bin` and the offline suite rerun; cleanup removed superseded Community-based prototypes and live diagnostic harnesses. The retained offline suite can be rerun with `build.ps1`. Building does not repeat live tests or connect to the simulator.

## Remaining coverage

Cross-machine endpoint availability, simulator/aircraft reload combinations, future versions, live unit switching and all abnormal engine/failure scenarios have not been comprehensively tested. The existing running-state heuristic and five-second telemetry freshness limit remain unchanged. A missing or incompatible debugger source returns ACARS to the stock data path rather than manufacturing valid readings.
