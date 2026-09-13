# 03 - Community Filters

## What was recovered

An authenticated owner-authorized run returned 37 Community Filter summaries.
PSN Party and PSN Tunnel were confirmed in that catalog and their complete
packet-rule definitions were recovered.

PSN Party:

- UDP ports 1024-65535
- Packet length 96-140 bytes
- Payload signatures `0001004C2112A442` and `000100502112A442`
- Platforms: PS3, PS5, mobile
- Connection type: P2P

PSN Tunnel:

- UDP ports 50000-65535
- Packet length 96-158 bytes
- Payload signatures `00011053`, `00011054`, `00011055`, `00011056`,
  `00011057`, and `00011060`
- Platforms: PS4, PS5, mobile
- Connection type: server

## Offline reconstruction

The launcher exposes the 37-name catalog from a loopback-only service. PSN
Party and PSN Tunnel are converted to the app's existing local custom-filter
schema and persisted through `SaveCustomFilters`. Their Community Filter cards
and local rules toggle together.

The other 35 entries are marked `metadata-only`. Their unknown rule bodies are
not guessed and cannot be activated in this PoC. All 37 cards receive neutral
SVG artwork from the same loopback service. The 35 incomplete cards are labeled
`Unavailable - metadata only`; the two reconstructed entries are labeled
`VERIFIED LOCAL RULE`. These images are developer placeholders, not official
game artwork or evidence of a recovered rule.

## Important distinction

These rules identify candidate packets by protocol, ports, length, and fixed
payload prefixes. They do not decrypt a PlayStation username. Username
resolution would require a separate authorized mapping or service response.

## Fix

Treat paid filter definitions as recoverable client data once delivered. If
their secrecy has business value, send only the minimum per-session rule set,
use short-lived signed rule manifests, bind them to an active entitlement, and
make the server reject requests that rely on locally modified enablement state.
