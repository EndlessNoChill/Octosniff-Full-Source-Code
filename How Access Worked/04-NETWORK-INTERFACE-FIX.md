# 04 - Network interface initialization

The original startup sequence could leave the UI at `NO INTERFACE SELECTED`
when the saved adapter was missing, renamed, virtual, or no longer active.

The PoC startup order is:

1. Current default adapter, if it is physical.
2. First active physical adapter.
3. Previously selected physical adapter, if still present.
4. First active adapter, including a virtual adapter only as a fallback.
5. First enumerated adapter.

Manual selection remains available. This fixes the empty-selection state; it
does not guarantee traffic if the chosen adapter is not on the packet path,
Npcap/WinDivert cannot initialize, or the user starts capture on the wrong
physical network.

Production should persist a stable adapter identifier, reject loopback and
inactive virtual adapters unless deliberately chosen, show capture-driver
health, and explain whether zero traffic means no packets, a bad adapter, or a
driver failure.
