# Windows 10/11 compatibility fix

The developer's second-PC log proved that all hash-locked frontend and native
patches were found, written, and verified. The launcher then failed because it
read `Process.MainWindowHandle` after the target had already exited. That race
replaced the useful target failure with a launcher exception.

The compatibility build changes the launcher as follows:

1. It watches the native process handle with `WaitForSingleObject` before every
   window-handle query.
2. If the child exits early, it records the real unsigned exit code in decimal
   and hexadecimal and does not attempt another `MainWindowHandle` read.
3. It waits up to 30 seconds for slower Windows 10/11 systems.
4. It detects Microsoft Edge WebView2 before changing the package-local profile
   paths, then pins that runtime location for the child process.
5. It verifies the WinDivert/Wintun files and prepends the isolated `app` folder
   to the child process PATH.
6. It requests administrator elevation for the local capture driver.
7. It writes `compatibility-report.txt`, including environment, prerequisite,
   and crash-filename information without copying crash contents or credentials.

This is still a separate, unsigned, hash-locked, loopback-only assessment PoC.
It is not an official vendor build and is not guaranteed to pass every
organization's endpoint-control policy. A broadly deployable developer build
should be compiled and signed from the application's source with an explicit
`SECURITY_TEST_BUILD` flag and production API access disabled.
