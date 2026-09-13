# 02 - Entitlement and Party Chat

## What was changed

The frontend capability map and `playstation_addon` fallback were forced open.
The following native methods were patched in memory:

- `OctoSniff-Go/internal/app.(*App).hasSubEntitlement` returns true.
- `OctoSniff-Go/internal/app.(*App).hasPlayStationAddon` returns true.
- `OctoSniff-Go/internal/app.(*App).requirePlayStationAddon` returns a nil error.

Party Chat has a second PSN-link state check after the subscription gate. The
updated compatibility build preserves the application's native PlayStation
status and `ListCommunicationSessions` bridges. A legitimately linked account
can therefore propagate from Settings into Party Chat, while an unlinked or
expired account still displays `Connect PlayStation Network`.

## Result

The Party Chat navigation is locally reachable for developer inspection. Real
party discovery still requires a legitimate authorized PlayStation link and
available external service state; no username, party, or credential is
fabricated by the compatibility layer.

## Fix

Enforce the subscription at the Party Chat service boundary, not only in the
desktop process. Issue narrow, short-lived feature capabilities after the
backend independently validates the user and subscription. Reject replay,
expired capabilities, device mismatch, and requests lacking a fresh nonce.
