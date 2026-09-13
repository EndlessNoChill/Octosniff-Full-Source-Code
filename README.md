# minor fix pCP - Developer Security PoC

This is a separate, isolated proof-of-concept for the assessed OctoSniff 5.1.1
Windows build. The installed application is not modified.

- Assessed target SHA-256: `2CF7F902FEA4E2109210CA36BE8ADE859B0CB7FB1ACEE4794B34AC08D785A317`
- Isolated target: `app\sigh_Divine sniff.exe`
- Launcher: `minor fix pCP.exe`
- Local-only catalog service: `127.0.0.1:48193`

## Run

1. Extract the complete folder from the ZIP before running it.
2. Double-click `Start-PoC.cmd` or run the launcher directly, then accept the
   Windows administrator prompt required by the local packet-capture driver.
3. If startup fails, send `compatibility-report.txt` and `poc-run.log` to the
   developers. The new launcher records the target's real exit code instead of
   failing while it reads a closed window handle.
4. Confirm a network interface appears in the lower-left status card.
5. Open **Filters > Community Filters**. The recovered 37-entry catalog is
   available without an OctoSniff account, and every card uses package-local
   neutral artwork rather than a missing or remote image.
6. Search for `PSN`. PSN Party and PSN Tunnel start enabled and are backed by
   reconstructed local packet rules.
7. Link an authorized PlayStation account, then open **Party Chat**. The updated
   build preserves the native account-status and party-session bridge. Without
   a valid link, it still shows **Connect PlayStation Network** and does not
   fabricate a party or user.

The Custom Filters loader reads and preserves the existing list before adding
the PoC rules. This keeps the application's original disabled port-443 entry
and any other developer filters; if the disabled 443 entry is already missing,
the loader recreates one disabled fallback. It also retains exactly one enabled
`Auto-hide Port 443` rule, creating a local fallback only when that enabled rule
is absent. The auto-hide rule matches TCP or UDP with port 443 in either
direction and hides those rows from the monitor display. It does not block,
reroute, or interrupt HTTPS.

The launcher stays alive while the PoC runs because it owns a loopback-only
catalog/rule service. Closing the PoC also stops that service.

## Windows compatibility

- Supported assessment targets: 64-bit Windows 10 and Windows 11.
- Microsoft Edge WebView2 Runtime is required. If it is missing, install it
  from Microsoft's official page: https://developer.microsoft.com/microsoft-edge/webview2/
- `WinDivert.dll`, `WinDivert64.sys`, and `wintun.dll` must remain beside the
  isolated app copy. The launcher checks all three before startup.
- The build requests administrator elevation because WinDivert packet capture
  requires it.
- No unsigned runtime-patching PoC can honestly be guaranteed on every managed
  PC. Endpoint policy may block it; developers should use a signed,
  source-built `SECURITY_TEST_BUILD` for broad internal distribution.

## What works without an account

- The OctoSniff login page is skipped and the local logout-to-login transition
  is disabled.
- Locally gated navigation remains available for developer inspection.
- Party Chat uses the native PlayStation link status and session-list calls.
  Loading real sessions still requires an authorized PlayStation account and
  available service state.
- All 37 recovered Community Filter names and neutral local thumbnails are
  visible offline.
- PSN Party and PSN Tunnel use exact recovered UDP port, packet-length, and
  payload-signature rules through the existing local custom-filter engine.
- Removing or applying either PSN filter updates both the visible Community
  Filter state and the local rule list.
- Network-interface initialization restores the saved adapter first, then uses
  the default physical adapter, another active physical adapter, and finally an
  active virtual adapter only as a fallback.
- After a manual adapter is saved, that exact adapter is therefore restored and is
  not replaced by the current default adapter during interface refresh.
- ARP scan/start calls use the saved physical adapter when possible, otherwise
  the active default Wi-Fi/Ethernet adapter. A VPN/TUN/TAP adapter may remain the
  monitoring choice, but it is never passed to the Layer-2 ARP scanner.

The remaining 35 catalog entries are metadata-only because their complete rule
definitions were not recovered. They are intentionally not activatable; this
prevents an incomplete placeholder from silently matching or discarding real
traffic. Their cards are labeled `Unavailable - metadata only`, and their local
artwork says `METADATA ONLY`.

The PSN and Xbox resolvers are intentionally unavailable in this offline
assessment. The package does not bypass their account/database authorization
and does not fabricate username-to-IP results.

ARP still requires a real Layer-2 Wi-Fi/Ethernet adapter with IPv4 and
administrator access. The build does not bypass this platform requirement and
does not make ARP operations available on a VPN tunnel or virtual interface.

## Stop and restore

Run `Stop-PoC.cmd`, or close the PoC window. All executable patches are
process-only. The copied app uses package-local `APPDATA` and `LOCALAPPDATA`
under `profile\`; that folder is excluded from the shareable package.

## Important limitation

The PSN Party and Tunnel definitions are packet matchers, not username
decryption algorithms. The PoC proves local authorization and entitlement
checks can be bypassed and proves the two packet filters can be reconstructed.
It does not create a PSN token, fabricate a party, decrypt a username, or prove
that Sony party identifiers can be resolved without valid authorized service
access.

See `How Access Worked\` for the developer handoff and exact remediation map.
