# 05 - Remediation priorities

## Priority 0 - server enforcement

- Validate authentication and subscription on every paid backend operation.
- Use a short-lived signed feature capability with user, device, feature,
  audience, expiry, and nonce claims.
- Enforce PSN ownership/link state at the service boundary.
- Reject replay and log capability failures with privacy-safe identifiers.

## Priority 1 - remove client authority

- Treat frontend route guards as presentation only.
- Do not let `IsAuthenticated`, `hasSubEntitlement`, or
  `hasPlayStationAddon` authorize a remote paid action by themselves.
- Return structured denial reasons from the backend and fail closed.
- Separate catalog visibility from activation and service execution.

## Priority 2 - hardening and detection

- Sign the desktop binary and verify integrity of embedded frontend resources.
- Add tamper telemetry and alert on impossible combinations such as a paid
  request without a recently issued capability.
- Rotate or invalidate feature capabilities quickly after subscription change.
- Keep sensitive tokens out of renderer memory and redact authorization data
  from crash logs.

Obfuscation and a C++ rewrite can increase analysis cost, but neither repairs a
trust-boundary error. The decisive fix is server-side authorization that does
not accept a locally mutable client decision.
