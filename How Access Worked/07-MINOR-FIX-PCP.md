# 07 - minor fix pCP

This revision addresses two developer-QA findings.

## Port 443 display rule

The earlier offline loader saved the two PSN rule files before it read the
native custom-filter list. That ordering replaced an existing disabled 443
entry. This revision reads the native list first, preserves every preexisting
entry, removes only stale copies of the two PoC PSN rules, and then appends the
current verified PSN rules. It separately restores a disabled 443 fallback when
that entry is absent and reuses the generated `Auto-hide Port 443` rule or
creates one deterministic enabled fallback when absent. The disabled rule and
the enabled display-only rule can therefore coexist without duplicating either:

- protocol: `BOTH`
- direction: `BOTH`
- minimum and maximum port: `443`
- action: `hide`
- enabled by default

This is display filtering. It does not block or interfere with HTTPS.

## Sticky manual adapter selection

The previous fallback order selected the default physical interface before it
looked for the adapter saved in `selectedInterface`. Every interface refresh
could therefore overwrite a valid manual choice.

The new order is:

1. Exact saved adapter, when it is still enumerated.
2. Default physical adapter.
3. First active physical adapter.
4. First active adapter.
5. First enumerated adapter.

The manual selection continues to be saved through the application's existing
`UpdateConfig("selectedInterface", name)` path.

## ARP interface routing

The same interface list now derives a separate ARP-safe adapter: the saved
adapter when it is physical, otherwise the active default physical adapter, then
another active Wi-Fi/Ethernet adapter. `StartARPScanning` and `StartActiveScan`
receive that physical interface name directly, so a VPN/TUN/TAP monitoring
adapter is not forwarded to the Layer-2 scanner.

Classification checks the complete interface record, including description and
type. This matters because a Microsoft Wi-Fi Direct adapter can report type
`WiFi` while only its description identifies it as virtual.

The classifier deliberately reads `description` and `type` rather than
serializing the entire object: every record contains an `isLoopback` property,
and matching the property name itself would incorrectly reject all adapters.

The native validation remains intact. If no physical IPv4 adapter exists, the
ARP operation still stops with a diagnostic instead of attempting unsupported
Layer-2 activity over a virtual tunnel.
