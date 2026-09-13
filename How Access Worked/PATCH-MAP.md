# Patch map

## Assessed target

- Product: OctoSniff 5.1.1 for Windows
- Required target SHA-256:
  `2CF7F902FEA4E2109210CA36BE8ADE859B0CB7FB1ACEE4794B34AC08D785A317`
- Patch lifetime: process memory only
- Profile boundary: package-local `profile\`

## Frontend spans

| Purpose | Located symbol or marker |
|---|---|
| Authentication bootstrap | `async function Rs()` |
| Logout bridge | `function _u()` |
| Logout-to-login state | `async function ns()` span |
| Capability map | `function Kc()` |
| Party add-on fallback | `playstation_addon` expression tail |
| Interface restore | `async function So()` span |
| Community catalog load | `async function Ht()` span |
| Local custom-rule load | `async function wl()` span |
| Cloud-filter toggle | `async function Ll()` span |
| Party status/list wrappers | `U4`, `Pf`, `V4`, `m5` wrapper spans |
| Visible branding | titlebar text/icon and sidebar logo spans |

## Native Go symbols

| Symbol | PoC return behavior |
|---|---|
| `apiclient.(*Client).IsAuthenticated` | true |
| `app.(*App).IsAuthenticated` | true |
| `app.(*App).hasSubEntitlement` | true |
| `app.(*App).hasPlayStationAddon` | true |
| `app.(*App).requirePlayStationAddon` | nil error |

Addresses are resolved from the unpacked Go `pclntab` at runtime and change
under ASLR. The launcher verifies every write and refuses an unrecognized
binary hash.

## Loopback endpoints

| Path | Purpose |
|---|---|
| `/c` | Offline 37-entry Community Filter catalog |
| `/r` | Enabled exact PSN local-rule array |
| `/t/{uuid}` | Toggle supported PSN rule state |
| `/l` and `/i` | Red-slashed header and icon assets |

The listener binds to IPv4 loopback only and stops when the PoC target closes.
