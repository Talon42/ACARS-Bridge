# Installation and testing

This release contains the debugger-only x64 ACARS adapter. No Community package or separate helper application is needed. FSUIPC7 Free is sufficient. Both required runtime files are included: `A220CompatibilityBridge.dll` (the adapter) and `A220FSUIPCBridge.bin` (the unchanged stock FSUIPCBridge 3.6.6.1). You still need a standard supported ACARS installation and FSUIPC.

## Install

1. Close ACARS. Open its installation directory (normally `C:\Program Files\Delta Virtual\ACARSv3`). Windows may require administrator permission. Ensure file extensions are visible.
2. Rename the existing `FSUIPCBridge.dll` to `FSUIPCBridge.dll.disabled` to keep it as a backup and prevent ACARS loading it alongside the adapter. If that backup name already exists, keep a separate backup outside this directory before disabling the active DLL.
3. Copy **both** `A220CompatibilityBridge.dll` and `A220FSUIPCBridge.bin` from this release into the ACARS installation directory. Keep both files together. For an adapter upgrade, replace both files while ACARS is closed; the earlier `StockFSUIPCBridge.bin` is no longer used and can be removed.
4. Start MSFS, load the Synaptic A220, start FSUIPC, then start ACARS and connect normally. Developer Mode was OFF in the successful test; enabling it is not an installation step.

The final directory should contain these two active adapter files and the disabled original bridge:

```text
A220CompatibilityBridge.dll
A220FSUIPCBridge.bin
FSUIPCBridge.dll.disabled
```

Do not leave an active `FSUIPCBridge.dll` alongside the adapter. Other ACARS application files stay in place.

No aircraft files are changed. The adapter runs inside ACARS and temporarily subscribes to cockpit telemetry through the simulator's local debugger endpoint.

## Test

1. At the gate with both engines off, start an ACARS flight and check that the recorded engine values are zero.
2. Start the right engine and let it stabilize. Check the next ACARS position reports: right-engine N1, N2 and fuel flow should match the cockpit; the left engine should remain off. Typical observed idle was about 18.5% N1, 65% N2 and 700-750 pph per engine, but compare with your actual cockpit.
3. Start the left engine. Check both engines in the reports, then complete a flight and check that the filed report has valid engine readings and no continuous Engine Out warnings.
4. Shut down both engines and verify that the final recorded RPM and fuel flow return to zero.

The filed flight report, rather than the ACARS connection status alone, verifies the full recording path. An unavailable debugger endpoint falls back to stock telemetry, so the original A220 reporting problem can return. Close other clients attached to the same cockpit debugger target when troubleshooting.

## Roll back

Close ACARS. Remove `A220CompatibilityBridge.dll` and `A220FSUIPCBridge.bin`, then rename your backed-up `FSUIPCBridge.dll.disabled` to `FSUIPCBridge.dll` (or restore your original bridge backup). Restart ACARS. Do not keep two active simulator bridge DLLs.

## Compatibility

Validated with Synaptic A220 1.0.10, MSFS 2024 v1.22, ACARS 3.5 build 197 beta 3 x64, stock FSUIPCBridge 3.6.6.1 and FSUIPC7 7.5.9 Free. This is an independently tested adapter, not an official ACARS release. Updates to the cockpit's internal messages, debugger interface or ACARS bridge API may require changes. Recheck the installation after ACARS updates.
