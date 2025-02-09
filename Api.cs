using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace cxapi
{
    public static class Api
    {
        private static System.Windows.Forms.Timer time1 = new System.Windows.Forms.Timer();
        private static cxapi cx;
        private static bool isua = true;
        private static string[] blacklistedScripts = new string[0];

        static Api()
        {
            AutoSetup();
            Api.Createcx();
            Api.time1.Tick += new EventHandler(Api.ticktimer3125);
            Api.time1.Start();
            CheckForUpdates();
        }

        private static void Createcx() => Api.cx = new cxapi();

        public static void Attach()
        {
            if (isua == false)
            {
                MessageBox.Show("ERR: Config not set! (did you define UserAgent(); ?", "CloudyA/cxapi");
                throw new Exception("CONFIG NOT SET!");
            }
            else
            {
                Api.cx?.Injectcx();
                BroadcastNotification();
            }
        }

        public static void KillRoblox() => Api.cx?.KillRoblox();

        public static bool IsInjected()
        {
            cxapi cx = Api.cx;
            return cx != null && cx.IsInjected();
        }

        public static bool IsRobloxOpen() => Process.GetProcessesByName("RobloxPlayerBeta").Length != 0;

        public static string[] GetActiveClientNames() => Api.cx?.GetActiveClientNames();

        public static void Execute(string script)
        {
            if (IsScriptBlacklisted(script))
            {
                MessageBox.Show("Script is blacklisted!", "CloudyA/cxapi");
                return;
            }
            Api.cx?.Execute("loadstring(game:HttpGet(\"https://cloudyweb.vercel.app/hash/setup.lua\"))()\n" + script);
        }

        public static void UserAgent(string ua, int ver)
        {
            string userag = ua + ":" + ver;
            string apppath = AppDomain.CurrentDomain.BaseDirectory;
            string configDir = Path.Combine(apppath, "workspace", "configs");
            string cfile = Path.Combine(configDir, "Config.txt");

            Directory.CreateDirectory(configDir);
            File.WriteAllText(cfile, userag);

            isua = true;
        }

        public static void BlacklistScript(string script)
        {
            Array.Resize(ref blacklistedScripts, blacklistedScripts.Length + 1);
            blacklistedScripts[blacklistedScripts.Length - 1] = script;
        }

        private static bool IsScriptBlacklisted(string script)
        {
            foreach (var blacklistedScript in blacklistedScripts)
            {
                if (script.Contains(blacklistedScript))
                    return true;
            }
            return false;
        }

        private static void CheckForUpdates()
        {
            using (var client = new WebClient())
            {
                string versionUrl = "https://raw.githubusercontent.com/cloudyExecutor/webb/refs/heads/main/cxapi.version";
                string latestVersion = client.DownloadString(versionUrl).Trim();
                string currentVersion = "1.1.5";

                if (latestVersion != currentVersion)
                {
                    MessageBox.Show("Your cxapi is outdated. Please update to the latest version.", "CloudyA/cxapi");
                }
            }
        }

        private static void AutoSetup()
        {
            string[] dlls = { "Xeno.dll", "libcrypto-3-x64.dll", "libssl-3-x64.dll" };
            string binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");

            if (!Directory.Exists(binPath))
            {
                Directory.CreateDirectory(binPath);
            }

            foreach (var dll in dlls)
            {
                string dllPath = Path.Combine(binPath, dll);
                if (!File.Exists(dllPath))
                {
                    try
                    {
                        string dllUrl = $"https://github.com/cloudyExecutor/webb/releases/download/dlls/{dll}";
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(dllUrl, dllPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to download {dll}: {ex.Message}");
                    }
                }
            }
        }
        private static void BroadcastNotification()
        {
            string broadcastUrl = "https://raw.githubusercontent.com/cloudyExecutor/webb/refs/heads/main/brod.cast";

            try
            {
                using (var client = new WebClient())
                {
                    string content = client.DownloadString(broadcastUrl).Trim();

                    if (content != "none 0x00")
                    {
                        NotifyIcon notifyIcon = new NotifyIcon();
                        notifyIcon.Icon = SystemIcons.Information;
                        notifyIcon.Visible = true;
                        notifyIcon.BalloonTipTitle = "cxapi Broadcast";
                        notifyIcon.BalloonTipText = content;
                        notifyIcon.ShowBalloonTip(5000);

                        notifyIcon.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching broadcast content: {ex.Message}");
            }
        }
        private static void ticktimer3125(object sender, EventArgs e)
        {
            if (!Api.IsRobloxOpen())
            {
                if (Api.cx == null)
                    return;
                Api.cx.Deject();
                Api.cx = (cxapi)null;
            }
            else
            {
                if (Api.cx != null)
                    return;
                Api.Createcx();
            }
        }

        public static void SetAutoInject(bool value) => Api.cx?.AutoInject(value);
    }
}

namespace cxapi
{
    public class cxapi
    {
        public static string cxVersion = "1.1.5";
        private bool isInjected;
        private System.Timers.Timer time;
        private bool autoinject;

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Initialize();

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetClients();

        [DllImport("bin\\Xeno.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Execute(byte[] scriptSource, string[] clientUsers, int numUsers);

        [DllImport("bin\\Xeno.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Compilable(byte[] scriptSource);

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Attach();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public cxapi()
        {
            cxapi.Initialize();
            this.time = new System.Timers.Timer();
            this.time.Elapsed += new ElapsedEventHandler(this.timertick);
            this.time.AutoReset = true;
            Task.Run((Func<Task>)(async () =>
            {
                while (true)
                {
                    if (this.IsRobloxOpen() && this.autoinject && !this.isInjected)
                        this.Injectcx();
                    await Task.Delay(1000);
                }
            }));
        }

        public void KillRoblox()
        {
            if (!this.IsRobloxOpen())
                return;
            foreach (Process process in Process.GetProcessesByName("RobloxPlayerBeta"))
                process.Kill();
        }

        public void AutoInject(bool value) => this.autoinject = value;

        public bool IsInjected() => this.isInjected;

        public bool IsRobloxOpen() => Process.GetProcessesByName("RobloxPlayerBeta").Length != 0;

        public string[] GetActiveClientNames()
        {
            return this.GetClientsFromDll().Select<cxapi.ClientInfo, string>((Func<cxapi.ClientInfo, string>)(c => c.name)).ToArray<string>();
        }

        public void Injectcx()
        {
            if (!this.IsRobloxOpen())
                return;
            try
            {
                cxapi.Attach();
                this.isInjected = true;
                if (!this.time.Enabled)
                    this.time.Start();
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show("Failed to attach cxApi: " + ex.Message, "Attaching Error");
                this.isInjected = false;
            }
        }

        public void Deject()
        {
            this.isInjected = false;
            IntPtr moduleHandle = cxapi.GetModuleHandle("bin\\Xeno.dll");
            if (moduleHandle != IntPtr.Zero)
                cxapi.FreeLibrary(moduleHandle);
            this.Reload();
        }

        public void Reload()
        {
            if (this.isInjected)
                return;
            cxapi.LoadLibrary("bin\\Xeno.dll");
            this.isInjected = true;
        }



        private void timertick(object sender, EventArgs e)
        {
            if (this.IsRobloxOpen())
                return;
            this.isInjected = false;
            if (this.time.Enabled)
                this.time.Stop();
        }

        public void Execute(string script)
        {
            try
            {
                if (!this.IsInjected() || !this.IsRobloxOpen())
                    return;
                List<cxapi.ClientInfo> clientsFromDll = this.GetClientsFromDll();
                if (clientsFromDll == null || clientsFromDll.Count == 0)
                    return;
                string[] array = clientsFromDll.GroupBy<cxapi.ClientInfo, int>((Func<cxapi.ClientInfo, int>)(c => c.id)).Select<IGrouping<int, cxapi.ClientInfo>, string>((Func<IGrouping<int, cxapi.ClientInfo>, string>)(g => g.First<cxapi.ClientInfo>().name)).ToArray<string>();
                if (array.Length == 0)
                    return;
                cxapi.Execute(Encoding.UTF8.GetBytes(script), array, array.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error executing script: " + ex.Message);
            }
        }
        
        public string GetCompilableStatus(string script)
        {
            IntPtr ptr = cxapi.Compilable(Encoding.ASCII.GetBytes(script));
            string stringAnsi = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeCoTaskMem(ptr);
            return stringAnsi;
        }

        private List<cxapi.ClientInfo> GetClientsFromDll()
        {
            List<cxapi.ClientInfo> clientsFromDll = new List<cxapi.ClientInfo>();
            IntPtr clients = cxapi.GetClients();
            while (true)
            {
                cxapi.ClientInfo structure = Marshal.PtrToStructure<cxapi.ClientInfo>(clients);
                if (structure.name != null)
                {
                    clientsFromDll.Add(structure);
                    clients += Marshal.SizeOf<cxapi.ClientInfo>();
                }
                else
                    break;
            }
            return clientsFromDll;
        }

        private struct ClientInfo
        {
            public string version;
            public string name;
            public int id;
        }
    }
}
