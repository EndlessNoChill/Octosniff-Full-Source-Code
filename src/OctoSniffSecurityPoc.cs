using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;

internal static class OctoSniffSecurityPoc
{
    private const string ExpectedSha256 = "2CF7F902FEA4E2109210CA36BE8ADE859B0CB7FB1ACEE4794B34AC08D785A317";
    private const uint CreateSuspended = 0x00000004;
    private const uint PageExecuteReadWrite = 0x40;
    private const uint WaitObject0 = 0x00000000;
    private const uint WaitTimeout = 0x00000102;
    private const uint Infinite = 0xFFFFFFFF;
    private const int OfflinePort = 48193;
    private const string WebView2DownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";

    private static readonly string FrontendOriginal =
        "async function Rs(){try{t(14,Zl=await f5()),Zl&&t(16,en=await Za())}catch(ie){console.error(\"Failed to check authentication:\",ie),t(14,Zl=!1),t(16,en=null)}}";

    private static readonly string FrontendReplacement =
        "async function Rs(){t(14,Zl=!0),t(16,en={user:{id:\"local-poc\",username:\"sigh_Divine\",email:\"local-only\",entitlements:[]}})}";

    private static readonly string EntitlementTail =
        "===\"playstation_addon\")}))||!1";

    private static readonly string CapabilityOriginal =
        "function Kc(l,{platform:e=\"\"}={}){const t=new Set,n=Array.isArray(l==null?void 0:l.entitlements)?l.entitlements:[],i=IM();for(const s of n)TM(s,i)&&t.add(kr(\"traffic_control\")),CM(s,t);return t}";

    private static readonly string CapabilityReplacement =
        "function Kc(l,{platform:e=\"\"}={}){return new Set([\"network\",\"filters\",\"arp\",\"traffic_control\",\"hotspot\",\"tools\",\"ipstorage\",\"vpn\"])}";

    private static readonly string TitlebarOriginal =
        "c=(l[3]?l[6](\"inspect.appName\"):\"OctoSniff\")+\"\"";

    private static readonly string TitlebarReplacement =
        "c=\"minor fix pCP\"";

    private static readonly string TitlebarUpdateOriginal =
        "c=(V[3]?V[6](\"inspect.appName\"):\"OctoSniff\")+\"\"";

    private static readonly string TitlebarUpdateReplacement =
        "c=\"minor fix pCP\"";

    private static readonly string SidebarBrandOriginal =
        "pn(i.src,s=X6)||a(i,\"src\",s),a(i,\"alt\",\"OctoSniff\"),a(i,\"class\",r=\"nav-logo-svg \"+(l[24]?\"logo-collapsed\":\"\"))";

    private static readonly string SidebarBrandReplacement =
        "a(i,\"src\",\"http://127.1:48193/l\"),a(i,\"alt\",\"minor fix pCP\"),a(i,\"class\",r=\"nav-logo-svg\")";

    private static readonly string TitlebarIconOriginal =
        "a(n,\"class\",\"titlebar-icon svelte-1xiz6dj\"),pn(n.src,i=DM)||a(n,\"src\",i),a(n,\"alt\",\"\")";

    private static readonly string TitlebarIconReplacement =
        "a(n,\"class\",\"titlebar-icon svelte-1xiz6dj\"),a(n,\"src\",\"http://127.1:48193/i\")";

    private static readonly string LogoutStart =
        "async function ns(){try{await _u(),t(14,Zl=!1)";

    private static readonly string LogoutEnd =
        "async function be(){await Ui()}";

    private static readonly string LogoutReplacement =
        "async function ns(){t(14,Zl=!0),t(16,en=en||{user:{id:\"local-poc\",username:\"sigh_Divine\",email:\"local-only\",entitlements:[]}})}";

    private static readonly string LogoutBridgeOriginal =
        "function _u(){return window.go.main.App.Logout()}";

    private static readonly string LogoutBridgeReplacement =
        "function _u(){return Promise.resolve()}";

    private static readonly string InterfaceRestoreStart =
        "async function So(){try{const ke=(await fi()).selectedInterface;";

    private static readonly string InterfaceRestoreEnd =
        "async function Po(){try{J=await X4()";

    private static readonly string InterfaceRestoreReplacement =
        "async function So(){try{const ke=(await fi()).selectedInterface,pe=tt=>!/tap|tunnel|virtual|bluetooth|loopback/i.test((tt.description||\"\")+\" \"+(tt.type||\"\")),Ae=C.find(tt=>tt.name===ke&&pe(tt))||C.find(tt=>tt.isDefault&&tt.status===\"Up\"&&pe(tt))||C.find(tt=>tt.status===\"Up\"&&pe(tt))||C.find(tt=>pe(tt)),Be=C.find(tt=>tt.name===ke)||C.find(tt=>tt.isDefault&&pe(tt))||C.find(tt=>tt.status===\"Up\"&&pe(tt))||C.find(tt=>tt.status===\"Up\")||C[0];top.ap=Ae?Ae.name:\"\",Be&&await gs(Be)}catch(ie){console.error(\"Interface restore failed:\",ie)}}";

    private static readonly string ArpStartWrappersStart =
        "function ec(){return window.go.main.App.StartARPMonitoring()}";

    private static readonly string ArpStartWrappersEnd =
        "function $5(l){return window.go.main.App.StartCapture(l)}";

    private static readonly string ArpStartWrappersReplacement =
        "var pA7=window.go.main.App;function ec(){return pA7.StartARPMonitoring()}function br(l){return pA7.StartARPScanning(l||top.ap)}function bu(l){return pA7.StartActiveScan(l||top.ap)}";

    private static readonly string InterfaceAccessOriginal =
        "function xl(){return AM({isAuthenticated:Zl,moduleAccess:It,isUpdaterWindow:Ht,isInspectorPopup:yt})}";

    private static readonly string InterfaceAccessReplacement =
        "function xl(){return!0}";

    private static readonly string CustomLoadStart = "async function wl(){";
    private static readonly string CustomLoadEnd = "async function yt()";
    private static readonly string CustomLoadReplacement =
        "async function wl(){try{const i=await fu()||[],n=await fetch(\"http://127.1:48193/r\").then(e=>e.json())||[],p={protocol:\"BOTH\",direction:\"BOTH\",minLength:0,maxLength:65535,minPort:443,maxPort:443,action:\"hide\"};let s=i.find(e=>e.name===\"Auto-hide Port 443\"),d=i.find(e=>!e.enabled&&e.minPort===443);s||(s={...p,id:\"poc-hide-443\",name:\"Auto-hide Port 443\",enabled:!0}),d||(d={...p,id:\"poc-disabled-443\",name:\"Disabled 443\",enabled:!1});const a=i.filter(e=>![s.id,d.id,\"poc-psn-party\",\"poc-psn-tunnel\"].includes(e.id)),r=[d,s,...a,...n];await ro(r),t(121,B=r),qe({customFilters:B})}catch(e){}}";

    private static readonly string UserFilterLoadStart = "async function yt(){";
    private static readonly string UserFilterLoadEnd = "async function Ht()";
    private static readonly string UserFilterLoadReplacement =
        "async function yt(){t(122,q=[]),qe({userFilters:q})}";

    private static readonly string CommunityLoadStart = "async function Ht(){";
    private static readonly string CommunityLoadEnd = "function ll()";
    private static readonly string CommunityLoadReplacement =
        "async function Ht(){try{const e=await fetch(\"http://127.1:48193/c\").then(e=>e.json());t(2,K=e||[]),qe({communityFilters:K})}catch(e){t(2,K=[]),qe({communityFilters:[]})}}";

    private static readonly string CloudToggleStart = "async function Ll(";
    private static readonly string CloudToggleEnd = "async function yl(";
    private static readonly string CloudToggleReplacement =
        "async function Ll(e){try{const n=await $f(e);t(2,K=K.map(l=>l.uuid===e?{...l,enabled:n.enabled}:l));const l=await fetch(\"http://127.1:48193/r\").then(l=>l.json());t(121,B=l||[]),await ro(B),qe({customFilters:B,communityFilters:K})}catch(n){await wl(),await Ht()}}";

    private static readonly string ToggleWrappersStart = "function f9(";
    private static readonly string ToggleWrappersEnd = "function dn(";
    private static readonly string ToggleWrappersReplacement =
        "function f9(l){return window.go.main.App.TestARPInterface(l)}function $f(l){return fetch(\"http://127.1:48193/t/\"+l).then(e=>e.json())}function Z5(){return Promise.resolve()}function Q5(){return Promise.resolve()}function u9(l){return window.go.main.App.UpdateARPConfig(l)}";

    private static readonly string ResolverDescriptionOriginal =
        "Resolve a PSN username to an IP address.";

    private static readonly string ResolverDescriptionReplacement =
        "Unavailable in this offline assessment.";

    private static readonly string XboxResolverDescriptionOriginal =
        "Resolve an Xbox gamertag to an IP address.";

    private static readonly string XboxResolverDescriptionReplacement =
        "Unavailable in this offline assessment.";

    private static readonly string ResolverAuthenticationOriginal =
        "You need to be logged in to use the resolver.";

    private static readonly string ResolverAuthenticationReplacement =
        "Resolver unavailable in this offline build.";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ModuleEntry32
    {
        public uint dwSize;
        public uint th32ModuleID;
        public uint th32ProcessID;
        public uint GlblcntUsage;
        public uint ProccntUsage;
        public IntPtr modBaseAddr;
        public uint modBaseSize;
        public IntPtr hModule;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szModule;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExePath;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(
        string applicationName,
        string commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr process,
        IntPtr address,
        byte[] buffer,
        UIntPtr size,
        out UIntPtr bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr process,
        IntPtr address,
        byte[] buffer,
        UIntPtr size,
        out UIntPtr bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtectEx(
        IntPtr process,
        IntPtr address,
        UIntPtr size,
        uint newProtect,
        out uint oldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetWindowText(IntPtr window, string text);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr window, string text, string caption, uint type);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Module32First(IntPtr snapshot, ref ModuleEntry32 entry);

    [DllImport("ntdll.dll")]
    private static extern int NtSuspendProcess(IntPtr process);

    [DllImport("ntdll.dll")]
    private static extern int NtResumeProcess(IntPtr process);

    private sealed class ModuleInfo
    {
        public long BaseAddress;
        public int Size;
        public string Path;
    }

    private sealed class PatchRecord
    {
        public string Target;
        public long Address;
        public byte[] Before;
        public byte[] After;
    }

    private sealed class OfflineFilterServer
    {
        private const string PartyUuid = "8ad2393d-c09f-4695-82b6-8008d99c7354";
        private const string TunnelUuid = "ffc06e54-7357-4470-9d5d-897a23b492b3";

        private readonly string catalogTemplate;
        private readonly string partyRule;
        private readonly string tunnelRule;
        private readonly byte[] headerLogo;
        private readonly byte[] iconLogo;
        private readonly Dictionary<string, string> filterNames;
        private readonly HashSet<string> enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object stateLock = new object();
        private TcpListener listener;
        private Thread thread;
        private volatile bool stopping;

        public OfflineFilterServer(string baseDir)
        {
            string dataDir = Path.Combine(baseDir, "offline-data");
            string brandingDir = Path.Combine(baseDir, "branding");
            string sourceCatalog = File.ReadAllText(
                Path.Combine(dataDir, "community-filters.json"), Encoding.UTF8);
            filterNames = BuildFilterNames(sourceCatalog);
            catalogTemplate = LocalizeCatalogThumbnails(sourceCatalog);
            partyRule = File.ReadAllText(Path.Combine(dataDir, "psn-party-local-rule.json"), Encoding.UTF8).Trim();
            tunnelRule = File.ReadAllText(Path.Combine(dataDir, "psn-tunnel-local-rule.json"), Encoding.UTF8).Trim();
            headerLogo = File.ReadAllBytes(Path.Combine(brandingDir, "sigh-divine-header-red-slashed-ui.png"));
            iconLogo = File.ReadAllBytes(Path.Combine(brandingDir, "sigh-divine-icon-red-slashed.png"));
            enabled.Add(PartyUuid);
            enabled.Add(TunnelUuid);
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Loopback, OfflinePort);
            listener.Start(8);
            thread = new Thread(ServeLoop);
            thread.IsBackground = true;
            thread.Name = "sigh_Divine offline-filter server";
            thread.Start();
        }

        public void Stop()
        {
            stopping = true;
            try { if (listener != null) listener.Stop(); } catch { }
            try { if (thread != null && thread.IsAlive) thread.Join(1500); } catch { }
        }

        private void ServeLoop()
        {
            while (!stopping)
            {
                try
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = 3000;
                        client.SendTimeout = 3000;
                        ServeClient(client);
                    }
                }
                catch (SocketException)
                {
                    if (!stopping)
                        Thread.Sleep(25);
                }
                catch
                {
                    if (!stopping)
                        Thread.Sleep(25);
                }
            }
        }

        private void ServeClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string requestLine;
            using (StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                requestLine = reader.ReadLine() ?? string.Empty;
                string line;
                do { line = reader.ReadLine(); } while (!string.IsNullOrEmpty(line));
            }

            string[] parts = requestLine.Split(' ');
            string path = parts.Length > 1 ? parts[1] : "/";
            if (path == "/c")
            {
                WriteResponse(stream, Encoding.UTF8.GetBytes(BuildCatalog()), "application/json; charset=utf-8");
                return;
            }
            if (path == "/r")
            {
                WriteResponse(stream, Encoding.UTF8.GetBytes(BuildRules()), "application/json; charset=utf-8");
                return;
            }
            if (path == "/l")
            {
                WriteResponse(stream, headerLogo, "image/png");
                return;
            }
            if (path == "/i")
            {
                WriteResponse(stream, iconLogo, "image/png");
                return;
            }
            if (path.StartsWith("/f/", StringComparison.Ordinal))
            {
                string uuid = Uri.UnescapeDataString(path.Substring(3));
                int queryAt = uuid.IndexOf('?');
                if (queryAt >= 0)
                    uuid = uuid.Substring(0, queryAt);
                if (uuid.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    uuid = uuid.Substring(0, uuid.Length - 4);
                WriteResponse(stream, Encoding.UTF8.GetBytes(BuildFilterSvg(uuid)),
                    "image/svg+xml; charset=utf-8");
                return;
            }
            if (path.StartsWith("/t/", StringComparison.Ordinal))
            {
                string uuid = Uri.UnescapeDataString(path.Substring(3));
                bool value = false;
                bool supported = uuid.Equals(PartyUuid, StringComparison.OrdinalIgnoreCase) ||
                    uuid.Equals(TunnelUuid, StringComparison.OrdinalIgnoreCase);
                if (supported)
                {
                    lock (stateLock)
                    {
                        if (enabled.Contains(uuid))
                            enabled.Remove(uuid);
                        else
                            enabled.Add(uuid);
                        value = enabled.Contains(uuid);
                    }
                }
                string result = "{\"enabled\":" + (value ? "true" : "false") +
                    ",\"supported\":" + (supported ? "true" : "false") + "}";
                WriteResponse(stream, Encoding.UTF8.GetBytes(result), "application/json; charset=utf-8");
                return;
            }
            WriteResponse(stream, Encoding.UTF8.GetBytes("{\"error\":\"not found\"}"),
                "application/json; charset=utf-8", "404 Not Found");
        }

        private string BuildCatalog()
        {
            string json = catalogTemplate;
            lock (stateLock)
            {
                if (enabled.Contains(PartyUuid))
                    json = EnableCatalogEntry(json, PartyUuid);
                if (enabled.Contains(TunnelUuid))
                    json = EnableCatalogEntry(json, TunnelUuid);
            }
            return json;
        }

        private static string LocalizeCatalogThumbnails(string json)
        {
            string normalized = json.Replace(
                "\"user\":{\"username\":\"Offline catalog\"}",
                "\"user\":{\"username\":\"Unavailable - metadata only\"}");
            string[] lines = normalized.Replace("\r\n", "\n").Split('\n');
            const string thumbnailMarker = "\"thumbnail_url\":\"";
            const string offlineMarker = "\"offline_status\":";
            for (int i = 0; i < lines.Length; i++)
            {
                string uuid = ReadJsonString(lines[i], "uuid");
                if (string.IsNullOrEmpty(uuid))
                    continue;

                string url = "http://127.0.0.1:" + OfflinePort + "/f/" + uuid + ".svg";
                int thumbnailAt = lines[i].IndexOf(thumbnailMarker, StringComparison.Ordinal);
                if (thumbnailAt >= 0)
                {
                    int valueStart = thumbnailAt + thumbnailMarker.Length;
                    int valueEnd = lines[i].IndexOf('"', valueStart);
                    if (valueEnd > valueStart)
                        lines[i] = lines[i].Substring(0, valueStart) + url +
                            lines[i].Substring(valueEnd);
                    continue;
                }

                int offlineAt = lines[i].IndexOf(offlineMarker, StringComparison.Ordinal);
                if (offlineAt >= 0)
                    lines[i] = lines[i].Insert(offlineAt,
                        "\"thumbnail_url\":\"" + url + "\",");
            }
            return string.Join(Environment.NewLine, lines);
        }

        private static Dictionary<string, string> BuildFilterNames(string json)
        {
            Dictionary<string, string> names =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = json.Replace("\r\n", "\n").Split('\n');
            foreach (string line in lines)
            {
                string uuid = ReadJsonString(line, "uuid");
                string name = ReadJsonString(line, "name");
                if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(name))
                    names[uuid] = name;
            }
            return names;
        }

        private static string ReadJsonString(string line, string key)
        {
            string marker = "\"" + key + "\":\"";
            int markerAt = line.IndexOf(marker, StringComparison.Ordinal);
            if (markerAt < 0)
                return string.Empty;
            int valueStart = markerAt + marker.Length;
            int valueEnd = line.IndexOf('"', valueStart);
            return valueEnd > valueStart
                ? line.Substring(valueStart, valueEnd - valueStart)
                : string.Empty;
        }

        private string BuildFilterSvg(string uuid)
        {
            string name;
            if (!filterNames.TryGetValue(uuid, out name))
                name = "Community Filter";
            bool verified = uuid.Equals(PartyUuid, StringComparison.OrdinalIgnoreCase) ||
                uuid.Equals(TunnelUuid, StringComparison.OrdinalIgnoreCase);
            string accent = verified ? "#2563eb" : "#475569";
            string status = verified ? "VERIFIED LOCAL RULE" : "METADATA ONLY";
            string initials = BuildInitials(name);
            string safeName = WebUtility.HtmlEncode(
                name.Length > 30 ? name.Substring(0, 30) : name);

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"640\" height=\"360\" viewBox=\"0 0 640 360\">" +
                "<rect width=\"640\" height=\"360\" fill=\"#090b10\"/>" +
                "<rect x=\"24\" y=\"24\" width=\"592\" height=\"312\" rx=\"24\" fill=\"#11151d\" stroke=\"" +
                accent + "\" stroke-width=\"3\"/>" +
                "<circle cx=\"320\" cy=\"145\" r=\"76\" fill=\"" + accent + "\" opacity=\"0.22\"/>" +
                "<circle cx=\"320\" cy=\"145\" r=\"57\" fill=\"" + accent + "\"/>" +
                "<text x=\"320\" y=\"166\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" " +
                "font-size=\"58\" font-weight=\"700\" fill=\"#ffffff\">" +
                WebUtility.HtmlEncode(initials) + "</text>" +
                "<text x=\"320\" y=\"255\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" " +
                "font-size=\"28\" font-weight=\"600\" fill=\"#f8fafc\">" + safeName + "</text>" +
                "<text x=\"320\" y=\"292\" text-anchor=\"middle\" font-family=\"Segoe UI,Arial,sans-serif\" " +
                "font-size=\"17\" letter-spacing=\"2\" fill=\"#94a3b8\">" + status + "</text>" +
                "</svg>";
        }

        private static string BuildInitials(string name)
        {
            StringBuilder result = new StringBuilder(2);
            string[] words = name.Split(
                new char[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (word.Length > 0)
                    result.Append(char.ToUpperInvariant(word[0]));
                if (result.Length == 2)
                    break;
            }
            if (result.Length == 0)
                return "CF";
            if (result.Length == 1 && name.Length > 1)
                result.Append(char.ToUpperInvariant(name[1]));
            return result.ToString();
        }

        private static string EnableCatalogEntry(string json, string uuid)
        {
            string from = "\"uuid\":\"" + uuid + "\",\"name\":";
            int start = json.IndexOf(from, StringComparison.Ordinal);
            if (start < 0)
                return json;
            int enabledAt = json.IndexOf("\"enabled\":false", start, StringComparison.Ordinal);
            if (enabledAt < 0)
                return json;
            return json.Substring(0, enabledAt) + "\"enabled\":true" +
                json.Substring(enabledAt + "\"enabled\":false".Length);
        }

        private string BuildRules()
        {
            List<string> rules = new List<string>();
            lock (stateLock)
            {
                if (enabled.Contains(TunnelUuid))
                    rules.Add(tunnelRule);
                if (enabled.Contains(PartyUuid))
                    rules.Add(partyRule);
            }
            return "[" + string.Join(",", rules.ToArray()) + "]";
        }

        private static void WriteResponse(NetworkStream stream, byte[] body, string contentType,
            string status = "200 OK")
        {
            string headers = "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }
    }

    private static readonly List<string> LogLines = new List<string>();

    private static void Log(string message)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message;
        LogLines.Add(line);
        Console.WriteLine(line);
    }

    private static bool IsAdministrator()
    {
        try
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string FindWebView2Runtime(string baseDir, string originalLocalAppData)
    {
        List<string> roots = new List<string>();
        string configured = Environment.GetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER");
        if (!string.IsNullOrWhiteSpace(configured))
            roots.Add(configured);
        roots.Add(Path.Combine(baseDir, "runtime", "WebView2"));
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            roots.Add(Path.Combine(programFilesX86, "Microsoft", "EdgeWebView", "Application"));
        if (!string.IsNullOrWhiteSpace(programFiles))
            roots.Add(Path.Combine(programFiles, "Microsoft", "EdgeWebView", "Application"));
        if (!string.IsNullOrWhiteSpace(originalLocalAppData))
            roots.Add(Path.Combine(originalLocalAppData, "Microsoft", "EdgeWebView", "Application"));

        foreach (string root in roots.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(Path.Combine(root, "msedgewebview2.exe")))
                    return Path.GetFullPath(root);
                if (!Directory.Exists(root))
                    continue;
                string[] versionFolders = Directory.GetDirectories(root);
                Array.Sort(versionFolders, StringComparer.OrdinalIgnoreCase);
                Array.Reverse(versionFolders);
                foreach (string versionFolder in versionFolders)
                {
                    if (File.Exists(Path.Combine(versionFolder, "msedgewebview2.exe")))
                        return Path.GetFullPath(versionFolder);
                }
            }
            catch
            {
                // Continue through the other supported runtime locations.
            }
        }
        return null;
    }

    private static string ExitCodeDescription(uint exitCode)
    {
        switch (exitCode)
        {
            case 0x00000000: return "The target exited normally before creating a window.";
            case 0xC0000005: return "Access violation in the target process.";
            case 0xC000007B: return "Invalid executable or native dependency architecture.";
            case 0xC0000135: return "A required DLL or runtime was not found.";
            case 0xC0000409: return "The target terminated because a security check failed.";
            case 0xC0000428: return "Windows rejected an image signature.";
            default: return "No built-in description is available; give this code and the report to the developers.";
        }
    }

    private static bool TryGetExitCode(IntPtr processHandle, out uint exitCode)
    {
        uint wait = WaitForSingleObject(processHandle, 0);
        if (wait == WaitTimeout)
        {
            exitCode = 0;
            return false;
        }
        if (wait != WaitObject0)
            throw new InvalidOperationException("WaitForSingleObject failed: " + Marshal.GetLastWin32Error());
        if (!GetExitCodeProcess(processHandle, out exitCode))
            throw new InvalidOperationException("GetExitCodeProcess failed: " + Marshal.GetLastWin32Error());
        return true;
    }

    private static IntPtr WaitForMainWindowOrExit(Process launched, IntPtr processHandle,
        TimeSpan timeout, out bool exited, out uint exitCode)
    {
        Stopwatch wait = Stopwatch.StartNew();
        exited = false;
        exitCode = 0;
        while (wait.Elapsed < timeout)
        {
            if (TryGetExitCode(processHandle, out exitCode))
            {
                exited = true;
                return IntPtr.Zero;
            }
            try
            {
                launched.Refresh();
                IntPtr window = launched.MainWindowHandle;
                if (window != IntPtr.Zero)
                    return window;
            }
            catch (InvalidOperationException)
            {
                if (TryGetExitCode(processHandle, out exitCode))
                {
                    exited = true;
                    return IntPtr.Zero;
                }
            }
            Thread.Sleep(100);
        }
        return IntPtr.Zero;
    }

    private static void WriteCompatibilityReport(string reportPath, string baseDir, string appPath,
        string webView2Runtime, string outcome, uint? exitCode)
    {
        List<string> report = new List<string>();
        report.Add("minor fix pCP - Compatibility Report");
        report.Add("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        report.Add("Outcome: " + outcome);
        report.Add("");
        report.Add("Environment");
        report.Add("- OS: " + Environment.OSVersion);
        report.Add("- 64-bit OS: " + Environment.Is64BitOperatingSystem);
        report.Add("- 64-bit launcher: " + Environment.Is64BitProcess);
        report.Add("- Elevated administrator token: " + IsAdministrator());
        report.Add("- Package path: " + baseDir);
        report.Add("- Package-local profile writable: " + CanWriteDirectory(Path.Combine(baseDir, "profile")));
        report.Add("");
        report.Add("Required files");
        report.Add("- Target executable: " + File.Exists(appPath));
        string appDir = Path.GetDirectoryName(appPath);
        foreach (string fileName in new[] { "WinDivert.dll", "WinDivert64.sys", "wintun.dll" })
            report.Add("- " + fileName + ": " + File.Exists(Path.Combine(appDir, fileName)));
        report.Add("");
        report.Add("WebView2");
        report.Add("- Runtime: " + (string.IsNullOrWhiteSpace(webView2Runtime) ? "NOT FOUND" : webView2Runtime));
        if (string.IsNullOrWhiteSpace(webView2Runtime))
            report.Add("- Official runtime download: " + WebView2DownloadUrl);
        if (exitCode.HasValue)
        {
            report.Add("");
            report.Add("Target exit");
            report.Add(string.Format("- Code: {0} (0x{0:X8})", exitCode.Value));
            report.Add("- Meaning: " + ExitCodeDescription(exitCode.Value));
        }

        string crashDir = Path.Combine(baseDir, "profile", "Roaming", "OctoSniff", "Crashes");
        report.Add("");
        report.Add("Crash artifacts (names only; contents are not copied)");
        try
        {
            string[] crashes = Directory.Exists(crashDir)
                ? Directory.GetFiles(crashDir, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Take(8).ToArray()
                : new string[0];
            if (crashes.Length == 0)
                report.Add("- None found.");
            else
                foreach (string crash in crashes)
                    report.Add("- " + Path.GetFileName(crash) + " | " + File.GetLastWriteTime(crash).ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            report.Add("- Could not enumerate crash filenames: " + ex.Message);
        }
        report.Add("");
        report.Add("Share this file and poc-run.log with the developers. Do not share the profile folder.");
        File.WriteAllLines(reportPath, report.ToArray(), Encoding.UTF8);
    }

    private static bool CanWriteDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string probe = Path.Combine(directory, ".compatibility-write-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(probe, "ok", Encoding.ASCII);
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowLaunchFailure(string message, string reportPath)
    {
        try
        {
            MessageBox(IntPtr.Zero, message + "\r\n\r\nCompatibility report:\r\n" + reportPath,
                "minor fix pCP - Compatibility failure", 0x00000010 | 0x00040000);
        }
        catch { }
    }

    private static string Hex(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    private static string Sha256(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (SHA256 hash = SHA256.Create())
        {
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int startIndex)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
            return -1;
        for (int i = Math.Max(0, startIndex); i <= haystack.Length - needle.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && haystack[i + j] == needle[j])
                j++;
            if (j == needle.Length)
                return i;
        }
        return -1;
    }

    private static ulong ReadUInt64(byte[] data, int offset)
    {
        return BitConverter.ToUInt64(data, offset);
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return BitConverter.ToUInt32(data, offset);
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data, offset);
    }

    private static string ReadCString(byte[] data, int offset)
    {
        if (offset < 0 || offset >= data.Length)
            return string.Empty;
        int end = offset;
        int limit = Math.Min(data.Length, offset + 2048);
        while (end < limit && data[end] != 0)
            end++;
        return Encoding.UTF8.GetString(data, offset, end - offset);
    }

    private static ModuleInfo GetMainModule(uint processId)
    {
        const uint SnapModule = 0x00000008;
        const uint SnapModule32 = 0x00000010;
        IntPtr snapshot = CreateToolhelp32Snapshot(SnapModule | SnapModule32, processId);
        if (snapshot == new IntPtr(-1))
            throw new InvalidOperationException("CreateToolhelp32Snapshot failed: " + Marshal.GetLastWin32Error());
        try
        {
            ModuleEntry32 entry = new ModuleEntry32();
            entry.dwSize = (uint)Marshal.SizeOf(typeof(ModuleEntry32));
            if (!Module32First(snapshot, ref entry))
                throw new InvalidOperationException("Module32First failed: " + Marshal.GetLastWin32Error());
            return new ModuleInfo
            {
                BaseAddress = entry.modBaseAddr.ToInt64(),
                Size = checked((int)entry.modBaseSize),
                Path = entry.szExePath
            };
        }
        finally
        {
            CloseHandle(snapshot);
        }
    }

    private static byte[] ReadMemory(IntPtr process, long address, int size)
    {
        byte[] buffer = new byte[size];
        UIntPtr bytesRead;
        if (!ReadProcessMemory(process, new IntPtr(address), buffer, new UIntPtr((uint)size), out bytesRead) ||
            bytesRead.ToUInt64() != (ulong)size)
        {
            throw new InvalidOperationException(
                string.Format("ReadProcessMemory failed at 0x{0:X}: {1}", address, Marshal.GetLastWin32Error()));
        }
        return buffer;
    }

    private static void WriteMemory(IntPtr process, long address, byte[] bytes)
    {
        uint oldProtect;
        if (!VirtualProtectEx(process, new IntPtr(address), new UIntPtr((uint)bytes.Length), PageExecuteReadWrite, out oldProtect))
            throw new InvalidOperationException("VirtualProtectEx failed: " + Marshal.GetLastWin32Error());
        try
        {
            UIntPtr written;
            if (!WriteProcessMemory(process, new IntPtr(address), bytes, new UIntPtr((uint)bytes.Length), out written) ||
                written.ToUInt64() != (ulong)bytes.Length)
            {
                throw new InvalidOperationException("WriteProcessMemory failed: " + Marshal.GetLastWin32Error());
            }
            FlushInstructionCache(process, new IntPtr(address), new UIntPtr((uint)bytes.Length));
        }
        finally
        {
            uint ignored;
            VirtualProtectEx(process, new IntPtr(address), new UIntPtr((uint)bytes.Length), oldProtect, out ignored);
        }
    }

    private static Dictionary<string, long> ParseGoSymbols(byte[] module, long moduleBase, bool logSuccess)
    {
        byte[] magic = new byte[] { 0xF1, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x01, 0x08 };
        int searchFrom = 0;
        while (searchFrom < module.Length)
        {
            int pcln = IndexOf(module, magic, searchFrom);
            if (pcln < 0)
                break;
            searchFrom = pcln + 1;
            if (pcln + 72 > module.Length)
                continue;

            ulong nfunc = ReadUInt64(module, pcln + 8);
            ulong textStart = ReadUInt64(module, pcln + 24);
            ulong funcNameOffset = ReadUInt64(module, pcln + 32);
            ulong funcTableOffset = ReadUInt64(module, pcln + 64);
            ulong moduleEnd = checked((ulong)moduleBase + (ulong)module.Length);
            if (nfunc == 0 || nfunc > 1000000 || textStart < (ulong)moduleBase || textStart >= moduleEnd)
                continue;

            Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.Ordinal);
            for (ulong i = 0; i < nfunc; i++)
            {
                long tableEntry = checked((long)pcln + (long)funcTableOffset + (long)i * 8L);
                if (tableEntry < 0 || tableEntry + 8 > module.Length)
                    break;
                uint entryOffset = ReadUInt32(module, (int)tableEntry);
                uint funcOffset = ReadUInt32(module, (int)tableEntry + 4);
                long funcData = checked((long)pcln + (long)funcTableOffset + funcOffset);
                if (funcData < 0 || funcData + 8 > module.Length)
                    continue;
                int nameOffset = ReadInt32(module, (int)funcData + 4);
                long nameIndex = checked((long)pcln + (long)funcNameOffset + nameOffset);
                if (nameIndex < 0 || nameIndex >= module.Length)
                    continue;
                string name = ReadCString(module, (int)nameIndex);
                if (!result.ContainsKey(name))
                    result.Add(name, checked((long)textStart + entryOffset));
            }
            if (result.Count > 0)
            {
                if (logSuccess)
                    Log(string.Format("Parsed {0} Go functions from pclntab at module RVA 0x{1:X}.", result.Count, pcln));
                return result;
            }
        }
        throw new InvalidOperationException("A relocation-stable Go pclntab was not found.");
    }

    private static PatchRecord PatchFunction(
        IntPtr process,
        Dictionary<string, long> symbols,
        string name,
        byte[] replacement)
    {
        long address;
        if (!symbols.TryGetValue(name, out address))
            throw new InvalidOperationException("Required Go symbol not found: " + name);
        byte[] before = ReadMemory(process, address, replacement.Length);
        WriteMemory(process, address, replacement);
        byte[] after = ReadMemory(process, address, replacement.Length);
        if (!after.SequenceEqual(replacement))
            throw new InvalidOperationException("Patch verification failed for " + name);
        Log(string.Format("Patched {0} at 0x{1:X}: {2} -> {3}", name, address, Hex(before), Hex(after)));
        return new PatchRecord { Target = name, Address = address, Before = before, After = after };
    }

    private static PatchRecord PatchBytes(
        IntPtr process,
        long moduleBase,
        byte[] module,
        string target,
        byte[] original,
        byte[] replacement)
    {
        if (original.Length != replacement.Length)
            throw new InvalidOperationException("In-place patch length mismatch for " + target);
        int offset = IndexOf(module, original, 0);
        if (offset < 0)
            throw new InvalidOperationException("Frontend patch target not found: " + target);
        int duplicate = IndexOf(module, original, offset + 1);
        if (duplicate >= 0)
            throw new InvalidOperationException("Frontend patch target was not unique: " + target);
        long address = moduleBase + offset;
        byte[] before = ReadMemory(process, address, original.Length);
        WriteMemory(process, address, replacement);
        byte[] after = ReadMemory(process, address, replacement.Length);
        if (!after.SequenceEqual(replacement))
            throw new InvalidOperationException("Frontend patch verification failed: " + target);
        Log(string.Format("Patched {0} at 0x{1:X} ({2} bytes).", target, address, replacement.Length));
        return new PatchRecord { Target = target, Address = address, Before = before, After = after };
    }

    private static byte[] PadAscii(string value, int length)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > length)
            throw new InvalidOperationException("Replacement is longer than original.");
        byte[] padded = Enumerable.Repeat((byte)0x20, length).ToArray();
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        return padded;
    }

    private static void CloseExistingTargets(string appPath)
    {
        IEnumerable<Process> candidates = Process.GetProcessesByName("sigh_Divine sniff");
        foreach (Process process in candidates)
        {
            try
            {
                string runningPath = process.MainModule == null ? "" : process.MainModule.FileName;
                if (!string.Equals(Path.GetFullPath(runningPath), Path.GetFullPath(appPath),
                    StringComparison.OrdinalIgnoreCase))
                    continue;
                Log("Closing existing assessment target process " + process.Id + ".");
                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                Log("Could not close process " + process.Id + ": " + ex.Message);
            }
        }
    }

    private static int Main(string[] args)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string appPath = Path.Combine(baseDir, "app", "sigh_Divine sniff.exe");
        string logPath = Path.Combine(baseDir, "poc-run.log");
        string compatibilityPath = Path.Combine(baseDir, "compatibility-report.txt");
        string profileRoot = Path.Combine(baseDir, "profile");
        string profileRoaming = Path.Combine(profileRoot, "Roaming");
        string profileLocal = Path.Combine(profileRoot, "Local");
        string originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        string webView2Runtime = null;
        ProcessInformation pi = new ProcessInformation();
        OfflineFilterServer offlineServer = null;
        bool created = false;
        bool processSuspended = false;

        try
        {
            Log("minor fix pCP local authorization bypass PoC - assessment use only.");
            if (!File.Exists(appPath))
                throw new FileNotFoundException("The isolated OctoSniff copy is missing.", appPath);
            string actualHash = Sha256(appPath);
            Log("Target SHA-256: " + actualHash);
            if (!actualHash.Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to patch an unrecognized OctoSniff build.");

            if (!Environment.Is64BitOperatingSystem || !Environment.Is64BitProcess)
                throw new PlatformNotSupportedException("This assessment package requires 64-bit Windows and a 64-bit launcher.");
            if (!IsAdministrator())
                throw new UnauthorizedAccessException("Administrator elevation is required for the local packet-capture driver.");

            string appDir = Path.GetDirectoryName(appPath);
            foreach (string dependency in new[] { "WinDivert.dll", "WinDivert64.sys", "wintun.dll" })
            {
                if (!File.Exists(Path.Combine(appDir, dependency)))
                    throw new FileNotFoundException("A required local capture dependency is missing: " + dependency);
            }
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!currentPath.Split(';').Any(entry => string.Equals(entry.Trim(), appDir, StringComparison.OrdinalIgnoreCase)))
                Environment.SetEnvironmentVariable("PATH", appDir + ";" + currentPath, EnvironmentVariableTarget.Process);

            webView2Runtime = FindWebView2Runtime(baseDir, originalLocalAppData);
            if (string.IsNullOrWhiteSpace(webView2Runtime))
            {
                WriteCompatibilityReport(compatibilityPath, baseDir, appPath, null,
                    "FAILED - Microsoft Edge WebView2 Runtime was not found.", null);
                ShowLaunchFailure("Microsoft Edge WebView2 Runtime is required. Install it from Microsoft's official WebView2 page, then run the PoC again.", compatibilityPath);
                return 3;
            }
            Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", webView2Runtime, EnvironmentVariableTarget.Process);
            Log("Using WebView2 runtime at " + webView2Runtime + ".");

            Directory.CreateDirectory(profileRoaming);
            Directory.CreateDirectory(profileLocal);
            Environment.SetEnvironmentVariable("APPDATA", profileRoaming, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", profileLocal, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Combine(profileRoaming, "OctoSniff", "WebView2"), EnvironmentVariableTarget.Process);
            Log("Redirected the isolated target to a package-local application profile.");

            offlineServer = new OfflineFilterServer(baseDir);
            offlineServer.Start();
            Log("Started the loopback-only offline catalog at 127.0.0.1:" + OfflinePort + ".");

            CloseExistingTargets(appPath);

            StartupInfo si = new StartupInfo();
            si.cb = (uint)Marshal.SizeOf(typeof(StartupInfo));
            string commandLine = "\"" + appPath + "\"";
            if (!CreateProcess(appPath, commandLine, IntPtr.Zero, IntPtr.Zero, false, CreateSuspended,
                IntPtr.Zero, Path.GetDirectoryName(appPath), ref si, out pi))
            {
                throw new InvalidOperationException("CreateProcess failed: " + Marshal.GetLastWin32Error());
            }
            created = true;
            Log("Created isolated target as PID " + pi.dwProcessId + " in suspended state.");

            if (ResumeThread(pi.hThread) == 0xFFFFFFFF)
                throw new InvalidOperationException("ResumeThread failed: " + Marshal.GetLastWin32Error());
            Log("Resumed target and waiting for the protected image to unpack in memory.");

            ModuleInfo moduleInfo = null;
            byte[] moduleBytes = null;
            byte[] frontendMarker = Encoding.ASCII.GetBytes("async function Rs(){try{t(14,Zl=await f5())");
            Stopwatch wait = Stopwatch.StartNew();
            while (wait.Elapsed < TimeSpan.FromSeconds(20))
            {
                try
                {
                    moduleInfo = GetMainModule(pi.dwProcessId);
                    moduleBytes = ReadMemory(pi.hProcess, moduleInfo.BaseAddress, moduleInfo.Size);
                    if (IndexOf(moduleBytes, frontendMarker, 0) >= 0)
                        break;
                }
                catch
                {
                    moduleBytes = null;
                }
                Thread.Sleep(10);
            }
            if (moduleInfo == null || moduleBytes == null || IndexOf(moduleBytes, frontendMarker, 0) < 0)
                throw new TimeoutException("The unpacked frontend was not found within 20 seconds.");

            int suspendStatus = NtSuspendProcess(pi.hProcess);
            if (suspendStatus != 0)
                throw new InvalidOperationException(string.Format("NtSuspendProcess failed: 0x{0:X8}", suspendStatus));
            processSuspended = true;
            Log(string.Format("Suspended unpacked target at base 0x{0:X}, size {1} bytes.", moduleInfo.BaseAddress, moduleInfo.Size));

            moduleBytes = ReadMemory(pi.hProcess, moduleInfo.BaseAddress, moduleInfo.Size);
            List<PatchRecord> patches = new List<PatchRecord>();

            byte[] originalFrontend = Encoding.ASCII.GetBytes(FrontendOriginal);
            byte[] replacementFrontend = PadAscii(FrontendReplacement, originalFrontend.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend authentication/session bootstrap", originalFrontend, replacementFrontend));

            byte[] entitlementOriginal = Encoding.ASCII.GetBytes(EntitlementTail);
            byte[] entitlementReplacement = (byte[])entitlementOriginal.Clone();
            entitlementReplacement[entitlementReplacement.Length - 1] = (byte)'0';
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend Party Chat entitlement fallback", entitlementOriginal, entitlementReplacement));

            byte[] capabilityOriginal = Encoding.ASCII.GetBytes(CapabilityOriginal);
            byte[] capabilityReplacement = PadAscii(CapabilityReplacement, capabilityOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend capability/paywall map", capabilityOriginal, capabilityReplacement));

            byte[] titlebarOriginal = Encoding.ASCII.GetBytes(TitlebarOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend titlebar branding", titlebarOriginal,
                PadAscii(TitlebarReplacement, titlebarOriginal.Length)));

            byte[] titlebarUpdateOriginal = Encoding.ASCII.GetBytes(TitlebarUpdateOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend reactive titlebar branding", titlebarUpdateOriginal,
                PadAscii(TitlebarUpdateReplacement, titlebarUpdateOriginal.Length)));

            byte[] titlebarIconOriginal = Encoding.ASCII.GetBytes(TitlebarIconOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend red-slashed titlebar icon", titlebarIconOriginal,
                PadAscii(TitlebarIconReplacement, titlebarIconOriginal.Length)));

            byte[] sidebarBrandOriginal = Encoding.ASCII.GetBytes(SidebarBrandOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend sidebar branding", sidebarBrandOriginal,
                PadAscii(SidebarBrandReplacement, sidebarBrandOriginal.Length)));

            byte[] logoutBridgeOriginal = Encoding.ASCII.GetBytes(LogoutBridgeOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend logout bridge", logoutBridgeOriginal,
                PadAscii(LogoutBridgeReplacement, logoutBridgeOriginal.Length)));

            byte[] logoutStartMarker = Encoding.ASCII.GetBytes(LogoutStart);
            byte[] logoutEndMarker = Encoding.ASCII.GetBytes(LogoutEnd);
            int logoutStart = IndexOf(moduleBytes, logoutStartMarker, 0);
            int logoutEnd = logoutStart < 0 ? -1 : IndexOf(moduleBytes, logoutEndMarker, logoutStart + logoutStartMarker.Length);
            if (logoutStart < 0 || logoutEnd <= logoutStart)
                throw new InvalidOperationException("Could not locate the frontend logout-state transition.");
            byte[] logoutOriginal = new byte[logoutEnd - logoutStart];
            Buffer.BlockCopy(moduleBytes, logoutStart, logoutOriginal, 0, logoutOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend login-wall reentry/logout transition", logoutOriginal,
                PadAscii(LogoutReplacement, logoutOriginal.Length)));

            byte[] interfaceRestoreStartMarker = Encoding.ASCII.GetBytes(InterfaceRestoreStart);
            byte[] interfaceRestoreEndMarker = Encoding.ASCII.GetBytes(InterfaceRestoreEnd);
            int interfaceRestoreStart = IndexOf(moduleBytes, interfaceRestoreStartMarker, 0);
            int interfaceRestoreEnd = interfaceRestoreStart < 0 ? -1 : IndexOf(moduleBytes,
                interfaceRestoreEndMarker, interfaceRestoreStart + interfaceRestoreStartMarker.Length);
            if (interfaceRestoreStart < 0 || interfaceRestoreEnd <= interfaceRestoreStart)
                throw new InvalidOperationException("Could not locate the frontend interface restore routine.");
            byte[] interfaceRestoreOriginal = new byte[interfaceRestoreEnd - interfaceRestoreStart];
            Buffer.BlockCopy(moduleBytes, interfaceRestoreStart, interfaceRestoreOriginal, 0,
                interfaceRestoreOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend network-interface auto-selection", interfaceRestoreOriginal,
                PadAscii(InterfaceRestoreReplacement, interfaceRestoreOriginal.Length)));

            byte[] interfaceAccessOriginal = Encoding.ASCII.GetBytes(InterfaceAccessOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend network-interface initialization access", interfaceAccessOriginal,
                PadAscii(InterfaceAccessReplacement, interfaceAccessOriginal.Length)));

            byte[] arpStartWrappersStartMarker = Encoding.ASCII.GetBytes(ArpStartWrappersStart);
            byte[] arpStartWrappersEndMarker = Encoding.ASCII.GetBytes(ArpStartWrappersEnd);
            int arpStartWrappersStart = IndexOf(moduleBytes, arpStartWrappersStartMarker, 0);
            int arpStartWrappersEnd = arpStartWrappersStart < 0 ? -1 : IndexOf(moduleBytes,
                arpStartWrappersEndMarker, arpStartWrappersStart + arpStartWrappersStartMarker.Length);
            if (arpStartWrappersStart < 0 || arpStartWrappersEnd <= arpStartWrappersStart)
                throw new InvalidOperationException("Could not locate the frontend ARP start wrappers.");
            byte[] arpStartWrappersOriginal = new byte[arpStartWrappersEnd - arpStartWrappersStart];
            Buffer.BlockCopy(moduleBytes, arpStartWrappersStart, arpStartWrappersOriginal, 0,
                arpStartWrappersOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend physical-interface routing for ARP operations", arpStartWrappersOriginal,
                PadAscii(ArpStartWrappersReplacement, arpStartWrappersOriginal.Length)));

            byte[] customLoadStartMarker = Encoding.ASCII.GetBytes(CustomLoadStart);
            byte[] customLoadEndMarker = Encoding.ASCII.GetBytes(CustomLoadEnd);
            int customLoadStart = IndexOf(moduleBytes, customLoadStartMarker, 0);
            int customLoadEnd = customLoadStart < 0 ? -1 : IndexOf(moduleBytes,
                customLoadEndMarker, customLoadStart + customLoadStartMarker.Length);
            if (customLoadStart < 0 || customLoadEnd <= customLoadStart)
                throw new InvalidOperationException("Could not locate custom filter loading.");
            byte[] customLoadOriginal = new byte[customLoadEnd - customLoadStart];
            Buffer.BlockCopy(moduleBytes, customLoadStart, customLoadOriginal, 0, customLoadOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline exact PSN rule loading", customLoadOriginal,
                PadAscii(CustomLoadReplacement, customLoadOriginal.Length)));

            byte[] userFilterLoadStartMarker = Encoding.ASCII.GetBytes(UserFilterLoadStart);
            byte[] userFilterLoadEndMarker = Encoding.ASCII.GetBytes(UserFilterLoadEnd);
            int userFilterLoadStart = IndexOf(moduleBytes, userFilterLoadStartMarker, 0);
            int userFilterLoadEnd = userFilterLoadStart < 0 ? -1 : IndexOf(moduleBytes,
                userFilterLoadEndMarker, userFilterLoadStart + userFilterLoadStartMarker.Length);
            if (userFilterLoadStart < 0 || userFilterLoadEnd <= userFilterLoadStart)
                throw new InvalidOperationException("Could not locate account-backed user-filter loading.");
            byte[] userFilterLoadOriginal = new byte[userFilterLoadEnd - userFilterLoadStart];
            Buffer.BlockCopy(moduleBytes, userFilterLoadStart, userFilterLoadOriginal, 0,
                userFilterLoadOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend account-free user-filter loading", userFilterLoadOriginal,
                PadAscii(UserFilterLoadReplacement, userFilterLoadOriginal.Length)));

            byte[] communityLoadStartMarker = Encoding.ASCII.GetBytes(CommunityLoadStart);
            byte[] communityLoadEndMarker = Encoding.ASCII.GetBytes(CommunityLoadEnd);
            int communityLoadStart = IndexOf(moduleBytes, communityLoadStartMarker, 0);
            int communityLoadEnd = communityLoadStart < 0 ? -1 : IndexOf(moduleBytes,
                communityLoadEndMarker, communityLoadStart + communityLoadStartMarker.Length);
            if (communityLoadStart < 0 || communityLoadEnd <= communityLoadStart)
                throw new InvalidOperationException("Could not locate Community Filters loading.");
            byte[] communityLoadOriginal = new byte[communityLoadEnd - communityLoadStart];
            Buffer.BlockCopy(moduleBytes, communityLoadStart, communityLoadOriginal, 0,
                communityLoadOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline 37-entry Community Filters catalog", communityLoadOriginal,
                PadAscii(CommunityLoadReplacement, communityLoadOriginal.Length)));

            byte[] cloudToggleStartMarker = Encoding.ASCII.GetBytes(CloudToggleStart);
            byte[] cloudToggleEndMarker = Encoding.ASCII.GetBytes(CloudToggleEnd);
            int cloudToggleStart = IndexOf(moduleBytes, cloudToggleStartMarker, 0);
            int cloudToggleEnd = cloudToggleStart < 0 ? -1 : IndexOf(moduleBytes,
                cloudToggleEndMarker, cloudToggleStart + cloudToggleStartMarker.Length);
            if (cloudToggleStart < 0 || cloudToggleEnd <= cloudToggleStart)
                throw new InvalidOperationException("Could not locate Community Filters toggle logic.");
            byte[] cloudToggleOriginal = new byte[cloudToggleEnd - cloudToggleStart];
            Buffer.BlockCopy(moduleBytes, cloudToggleStart, cloudToggleOriginal, 0,
                cloudToggleOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline PSN Party and Tunnel toggles", cloudToggleOriginal,
                PadAscii(CloudToggleReplacement, cloudToggleOriginal.Length)));

            byte[] toggleWrappersStartMarker = Encoding.ASCII.GetBytes(ToggleWrappersStart);
            byte[] toggleWrappersEndMarker = Encoding.ASCII.GetBytes(ToggleWrappersEnd);
            int toggleWrappersStart = IndexOf(moduleBytes, toggleWrappersStartMarker, 0);
            int toggleWrappersEnd = toggleWrappersStart < 0 ? -1 : IndexOf(moduleBytes,
                toggleWrappersEndMarker, toggleWrappersStart + toggleWrappersStartMarker.Length);
            if (toggleWrappersStart < 0 || toggleWrappersEnd <= toggleWrappersStart)
                throw new InvalidOperationException("Could not locate cloud-filter bridge wrappers.");
            byte[] toggleWrappersOriginal = new byte[toggleWrappersEnd - toggleWrappersStart];
            Buffer.BlockCopy(moduleBytes, toggleWrappersStart, toggleWrappersOriginal, 0,
                toggleWrappersOriginal.Length);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend loopback-only Community Filters bridge", toggleWrappersOriginal,
                PadAscii(ToggleWrappersReplacement, toggleWrappersOriginal.Length)));

            // Preserve the application's native PlayStation status and Party session bridges.
            // The previous compatibility build replaced these calls with psnLinked:false and
            // an empty list, which prevented a legitimately linked account from propagating
            // from Settings into Party Chat.
            Log("Preserving native PlayStation link status and Party session bridge.");

            byte[] resolverDescriptionOriginal = Encoding.ASCII.GetBytes(
                ResolverDescriptionOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline PSN Resolver description", resolverDescriptionOriginal,
                PadAscii(ResolverDescriptionReplacement, resolverDescriptionOriginal.Length)));

            byte[] xboxResolverDescriptionOriginal = Encoding.ASCII.GetBytes(
                XboxResolverDescriptionOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline Xbox Resolver description", xboxResolverDescriptionOriginal,
                PadAscii(XboxResolverDescriptionReplacement,
                    xboxResolverDescriptionOriginal.Length)));

            byte[] resolverAuthenticationOriginal = Encoding.ASCII.GetBytes(
                ResolverAuthenticationOriginal);
            patches.Add(PatchBytes(pi.hProcess, moduleInfo.BaseAddress, moduleBytes,
                "frontend offline PSN Resolver status", resolverAuthenticationOriginal,
                PadAscii(ResolverAuthenticationReplacement, resolverAuthenticationOriginal.Length)));

            int firstResumeStatus = NtResumeProcess(pi.hProcess);
            processSuspended = false;
            if (firstResumeStatus != 0)
                throw new InvalidOperationException(string.Format("NtResumeProcess failed: 0x{0:X8}", firstResumeStatus));
            Log("Frontend gates patched; waiting for the native Go image to become relocation-stable.");

            string[] requiredSymbols = new string[]
            {
                "OctoSniff-Go/internal/apiclient.(*Client).IsAuthenticated",
                "OctoSniff-Go/internal/app.(*App).IsAuthenticated",
                "OctoSniff-Go/internal/app.(*App).hasSubEntitlement",
                "OctoSniff-Go/internal/app.(*App).hasPlayStationAddon",
                "OctoSniff-Go/internal/app.(*App).requirePlayStationAddon"
            };
            Dictionary<string, long> symbols = null;
            Stopwatch nativeWait = Stopwatch.StartNew();
            while (nativeWait.Elapsed < TimeSpan.FromSeconds(20))
            {
                try
                {
                    moduleBytes = ReadMemory(pi.hProcess, moduleInfo.BaseAddress, moduleInfo.Size);
                    Dictionary<string, long> candidate = ParseGoSymbols(moduleBytes, moduleInfo.BaseAddress, false);
                    if (requiredSymbols.All(candidate.ContainsKey))
                    {
                        symbols = candidate;
                        break;
                    }
                }
                catch { }
                Thread.Sleep(20);
            }
            if (symbols == null)
                throw new TimeoutException("The native Go symbols did not become relocation-stable within 20 seconds.");

            int secondSuspendStatus = NtSuspendProcess(pi.hProcess);
            if (secondSuspendStatus != 0)
                throw new InvalidOperationException(string.Format("NtSuspendProcess failed: 0x{0:X8}", secondSuspendStatus));
            processSuspended = true;
            moduleBytes = ReadMemory(pi.hProcess, moduleInfo.BaseAddress, moduleInfo.Size);
            symbols = ParseGoSymbols(moduleBytes, moduleInfo.BaseAddress, true);
            byte[] returnTrue = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };
            byte[] returnNilError = new byte[] { 0x31, 0xC0, 0x31, 0xDB, 0xC3 };

            patches.Add(PatchFunction(pi.hProcess, symbols,
                "OctoSniff-Go/internal/apiclient.(*Client).IsAuthenticated", returnTrue));
            patches.Add(PatchFunction(pi.hProcess, symbols,
                "OctoSniff-Go/internal/app.(*App).IsAuthenticated", returnTrue));
            patches.Add(PatchFunction(pi.hProcess, symbols,
                "OctoSniff-Go/internal/app.(*App).hasSubEntitlement", returnTrue));
            patches.Add(PatchFunction(pi.hProcess, symbols,
                "OctoSniff-Go/internal/app.(*App).hasPlayStationAddon", returnTrue));
            patches.Add(PatchFunction(pi.hProcess, symbols,
                "OctoSniff-Go/internal/app.(*App).requirePlayStationAddon", returnNilError));

            int resumeStatus = NtResumeProcess(pi.hProcess);
            processSuspended = false;
            if (resumeStatus != 0)
                throw new InvalidOperationException(string.Format("NtResumeProcess failed: 0x{0:X8}", resumeStatus));
            Log("All in-memory patches verified; resumed the PoC application.");

            Process launched = Process.GetProcessById((int)pi.dwProcessId);
            bool exitedBeforeWindow;
            uint earlyExitCode;
            IntPtr mainWindow = WaitForMainWindowOrExit(launched, pi.hProcess, TimeSpan.FromSeconds(30),
                out exitedBeforeWindow, out earlyExitCode);
            if (exitedBeforeWindow)
            {
                string outcome = string.Format("FAILED - target exited before creating a window with code {0} (0x{0:X8}).", earlyExitCode);
                Log(outcome);
                Log(ExitCodeDescription(earlyExitCode));
                WriteCompatibilityReport(compatibilityPath, baseDir, appPath, webView2Runtime, outcome, earlyExitCode);
                File.WriteAllLines(logPath, LogLines.ToArray(), Encoding.UTF8);
                ShowLaunchFailure("The target exited before its window opened. The launcher captured the real exit code instead of throwing MainWindowHandle.", compatibilityPath);
                return 2;
            }
            if (mainWindow != IntPtr.Zero)
            {
                SetWindowText(mainWindow, "minor fix pCP - Security PoC");
                Log("Main window handle: 0x" + mainWindow.ToInt64().ToString("X"));
            }
            else
            {
                Log("WARNING: Target is still running but no main window appeared within 30 seconds.");
            }
            Log("minor fix pCP is running. Close the assessment window to end the test; no installed executable bytes were changed.");
            WriteCompatibilityReport(compatibilityPath, baseDir, appPath, webView2Runtime,
                mainWindow == IntPtr.Zero ? "RUNNING - no main window detected within 30 seconds." : "RUNNING - main window detected.", null);
            File.WriteAllLines(logPath, LogLines.ToArray(), Encoding.UTF8);
            uint finalWait = WaitForSingleObject(pi.hProcess, Infinite);
            if (finalWait != WaitObject0)
                throw new InvalidOperationException("WaitForSingleObject failed while waiting for target shutdown: " + Marshal.GetLastWin32Error());
            uint finalExitCode;
            if (!GetExitCodeProcess(pi.hProcess, out finalExitCode))
                throw new InvalidOperationException("GetExitCodeProcess failed after target shutdown: " + Marshal.GetLastWin32Error());
            Log(string.Format("PoC target closed with code {0} (0x{0:X8}); stopping the loopback-only offline catalog.", finalExitCode));
            WriteCompatibilityReport(compatibilityPath, baseDir, appPath, webView2Runtime,
                "CLOSED - target window was opened and later exited.", finalExitCode);
            File.WriteAllLines(logPath, LogLines.ToArray(), Encoding.UTF8);
            return 0;
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex);
            try { File.WriteAllLines(logPath, LogLines.ToArray(), Encoding.UTF8); } catch { }
            try
            {
                WriteCompatibilityReport(compatibilityPath, baseDir, appPath, webView2Runtime,
                    "FAILED - launcher exception: " + ex.GetType().Name + ": " + ex.Message, null);
            }
            catch { }
            ShowLaunchFailure(ex.Message, compatibilityPath);
            if (created)
            {
                try
                {
                    if (processSuspended)
                        NtResumeProcess(pi.hProcess);
                    Process.GetProcessById((int)pi.dwProcessId).Kill();
                }
                catch { }
            }
            return 1;
        }
        finally
        {
            if (offlineServer != null)
                offlineServer.Stop();
            if (pi.hThread != IntPtr.Zero)
                CloseHandle(pi.hThread);
            if (pi.hProcess != IntPtr.Zero)
                CloseHandle(pi.hProcess);
        }
    }
}
