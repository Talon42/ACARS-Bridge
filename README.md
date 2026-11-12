# ACARS Bridge for the Synaptic A220

An in-process x64 Delta Virtual ACARS adapter that corrects A220 engine RPM, fuel flow and engine-running state. It needs no Community installation, paid FSUIPC features or separate background application.

## Download and install

Use the files in [release](release/). Follow the [installation, test and rollback instructions](release/README.md). The release includes both the adapter DLL and the unchanged stock FSUIPC bridge, packaged as `A220FSUIPCBridge.bin`. A standard supported ACARS installation and FSUIPC are still required.

## How it works

The A220's native engine combustion flags and fuel-flow values did not match its cockpit values in the tested configuration. FSUIPC exposed those native values, and the stock ACARS bridge suppressed RPM when combustion was false.

The adapter obtains actual cockpit fuel flow from `A22X.Engine Data` and units from `A22X.Fuel Data`, reads existing A220 RPM/master L-vars, converts fuel flow to pounds per hour, and supplies corrected ACARS position snapshots for the A220. Other position fields and other aircraft continue through the stock bridge.

It uses the existing local Coherent debugger HTTP/WebSocket endpoint on port 19999. A temporary in-memory listener requests `A22X.Resync`; it does not write simulator variables or modify aircraft files. Normal shutdown removes its handlers; a ten-second lease watchdog also cleans up after an unexpected host exit. Invalid, missing or stale telemetry falls back to stock data.

## Build and verify

On 64-bit Windows with .NET Framework 4.x and ACARS installed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
# For another ACARS location:
.\build.ps1 -AcarsDirectory 'D:\ACARSv3'
```

The script compiles the adapter and runs offline regression checks before updating `release/A220CompatibilityBridge.dll` and packages `A220FSUIPCBridge.bin` with SHA-256 checksums for both runtime files. Local dependency copies, test executables and test results stay in the ignored `build/` directory. No simulator connection or running ACARS session is required for these checks.

- `src/`: the validated debugger adapter source, including embedded listener scripts.
- `tests/`: offline interface, correction, isolation, fallback and delegation regression checks.
- `docs/validation.md`: completed live checks and compatibility limits.
- `release/`: installable adapter, instructions and checksum.

## Limitations

Endpoint availability has been demonstrated on the tested machine with Developer Mode off, not across every simulator installation. The cockpit message schema is internal and may change. Engine-running detection uses master state, fuel flow, N2, EGT and windmill state; it has not been qualified for every engine-failure scenario. See [validation](docs/validation.md) for the evidence and remaining coverage.
