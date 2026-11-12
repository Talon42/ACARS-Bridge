# Installation and testing

This release contains the debugger-only x64 ACARS adapter. No Community package or separate helper application is needed. FSUIPC7 Free is sufficient. It uses your installed, unchanged ACARS FSUIPC bridge; that third-party binary is not included.

## Install

1. Close ACARS. Open its installation directory (normally `C:\Program Files\Delta Virtual\ACARSv3`).
2. For a first installation, rename `FSUIPCBridge.dll` to `A220FSUIPCBridge.bin` in that same directory. Keep a backup of the original outside the ACARS DLL search directory. Windows may require administrator permission. Ensure file extensions are visible.
3. Copy `A220CompatibilityBridge.dll` from this release into that directory. If updating the earlier tested adapter, rename its `StockFSUIPCBridge.bin` to `A220FSUIPCBridge.bin` before installing the new DLL. If already using the new name, keep that file. Do not replace the stock binary with the adapter. Do not leave another stock `FSUIPCBridge.dll` active alongside the adapter.
4. Start MSFS, load the Synaptic A220, start FSUIPC, then start ACARS and connect normally. Developer Mode was OFF in the successful test; enabling it is not an installation step.

No aircraft files are changed. The adapter runs inside ACARS and temporarily subscribes to cockpit telemetry through the simulator's local debugger endpoint.

## Test

1. At the gate with both engines off, start an ACARS flight and check that the recorded engine values are zero.
2. Start the right engine and let it stabilize. Check the next ACARS position reports: right-engine N1, N2 and fuel flow should match the cockpit; the left engine should remain off. Typical observed idle was about 18.5% N1, 65% N2 and 700-750 pph per engine, but compare with your actual cockpit.
3. Start the left engine. Check both engines in the reports, then complete a flight and check that the filed report has valid engine readings and no continuous Engine Out warnings.
4. Shut down both engines and verify that the final recorded RPM and fuel flow return to zero.

The filed flight report, rather than the ACARS connection status alone, verifies the full recording path. An unavailable debugger endpoint falls back to stock telemetry, so the original A220 reporting problem can return. Close other clients attached to the same cockpit debugger target when troubleshooting.

## Roll back

Close ACARS. Remove `A220CompatibilityBridge.dll` and rename `A220FSUIPCBridge.bin` back to `FSUIPCBridge.dll`. If you kept an original backup instead, restore it. Restart ACARS. Do not keep two active simulator bridge DLLs.

## Compatibility

Validated with Synaptic A220 1.0.10, MSFS 2024 v1.22, ACARS 3.5 build 197 beta 3 x64, stock FSUIPCBridge 3.6.6.1 and FSUIPC7 7.5.9 Free. This is an independently tested adapter, not an official ACARS release. Updates to the cockpit's internal messages, debugger interface or ACARS bridge API may require changes. Recheck the installation after ACARS updates.
