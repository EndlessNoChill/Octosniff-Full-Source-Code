# How Access Worked

This folder is the developer handoff for the isolated `sigh_Divine sniff`
proof-of-concept. It documents the trust boundaries that were changed, the
evidence those changes produced, and the server-side controls needed to prevent
the same local bypass in a production build.

## Assessment boundary

- Tested only against the copied OctoSniff 5.1.1 binary identified in
  `PATCH-MAP.md`.
- Installed application bytes were not modified.
- Runtime state was redirected to a package-local profile.
- The offline catalog service binds only to `127.0.0.1`.
- Temporary credentials and PSN authorization material are not included.
- No party, username, address, or successful remote decryption is fabricated.

Read the numbered notes in order. The report in the parent package gives the
executive summary, evidence, severity, and complete remediation plan.

`06-WINDOWS-COMPATIBILITY.md` documents the second-PC startup race, prerequisite
checks, and supported Windows 10/11 developer-QA boundary.

`07-MINOR-FIX-PCP.md` documents the visible port 443 display rule and sticky
manual network-adapter selection.
