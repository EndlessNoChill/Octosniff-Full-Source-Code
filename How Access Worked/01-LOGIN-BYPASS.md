# 01 - Login bypass

## What was changed

The embedded frontend authentication bootstrap was replaced in memory with a
minimal local assessment profile. The frontend logout bridge and the state
transition that normally returns to the login page were also neutralized.

Two native authentication helpers were patched to return true:

- `OctoSniff-Go/internal/apiclient.(*Client).IsAuthenticated`
- `OctoSniff-Go/internal/app.(*App).IsAuthenticated`

The patch is applied after the packed image expands in memory and is verified
byte-for-byte before the process resumes. Nothing is written back to the copied
target or installed executable.

## Result

The application cold-starts directly into its main interface with a local PoC
identity. Settings still reports no real active server session. This proves the
desktop client treats locally mutable state as sufficient for major UI access.

## Fix

Do not treat a local boolean, cached profile, or native helper return value as
authorization. Require a short-lived, server-signed capability for every paid
operation. Bind it to the authenticated user, device, feature, request nonce,
and expiry, and verify it at the server that performs the operation.
