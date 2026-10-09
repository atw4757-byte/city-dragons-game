// City Dragons one-click installer. Built with the .NET Framework csc that ships in Windows (no SDK needed):
//   csc /nologo /optimize /target:exe /out:CityDragons-Setup.exe /r:System.Net.Http.dll Setup.cs
// Downloads SHA256SUMS.txt and every CityDragons-win64.tar.partNNN it lists from the latest GitHub release, verifies each
// part, streams the parts in order into Windows' tar.exe (extracting to <dir>\CityDragons), then adds a desktop shortcut.
// Flags (for testing): --base <url/>  --dir <path>  --yes  --no-shortcut  --no-launch  --verify-only (hash + tar -t, writes nothing)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;

static class Setup
{
    const string DefaultBase = "https://github.com/atw4757-byte/city-dragons-game/releases/latest/download/";

    static int Main(string[] args)
    {
        string baseUrl = DefaultBase, dir = null;
        bool yes = false, shortcut = true, launch = true, verifyOnly = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--base": baseUrl = args[++i]; if (!baseUrl.EndsWith("/")) baseUrl += "/"; break;
                case "--dir": dir = args[++i]; break;
                case "--yes": yes = true; break;
                case "--no-shortcut": shortcut = false; break;
                case "--no-launch": launch = false; break;
                case "--verify-only": verifyOnly = true; break;
                default: Console.WriteLine("Unknown option " + args[i]); return 2;
            }
        }
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Console.Title = "City Dragons Setup";
        Console.WriteLine("City Dragons Setup\n==================\n");
        try { return Run(baseUrl, dir, yes, shortcut, launch, verifyOnly); }
        catch (Exception e)
        {
            Console.WriteLine("\nSetup failed: " + e.Message);
            if (!yes) { Console.WriteLine("Press Enter to close."); Console.ReadLine(); }
            return 1;
        }
    }

    static int Run(string baseUrl, string dir, bool yes, bool shortcut, bool launch, bool verifyOnly)
    {
        string tarExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe");
        if (!File.Exists(tarExe)) throw new Exception("Windows tar.exe was not found (Windows 10 1803 or newer is required).");

        if (dir == null)
        {
            dir = @"C:\Games";
            if (!yes)
            {
                Console.Write("Install folder [" + dir + "]: ");
                string typed = (Console.ReadLine() ?? "").Trim().Trim('"');
                if (typed.Length > 0) dir = typed;
            }
        }
        dir = Path.GetFullPath(dir);
        string gameDir = Path.Combine(dir, "CityDragons");

        using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CityDragons-Setup/1.0");
            var parts = ReadSums(http.GetStringAsync(baseUrl + "SHA256SUMS.txt").Result);
            long total = 0, largest = 0;
            foreach (var p in parts)
            {
                using (var head = http.SendAsync(new HttpRequestMessage(HttpMethod.Head, baseUrl + p.Key)).Result)
                {
                    head.EnsureSuccessStatusCode();
                    long len = head.Content.Headers.ContentLength ?? 0;
                    total += len; largest = Math.Max(largest, len);
                }
            }
            if (!verifyOnly)
            {
                Directory.CreateDirectory(dir);
                long need = total + largest + (512L << 20), free = new DriveInfo(Path.GetPathRoot(dir)).AvailableFreeSpace;
                if (free < need)
                    throw new Exception(string.Format("Not enough disk space on {0}: {1:N1} GB free, {2:N1} GB needed.", Path.GetPathRoot(dir), free / 1073741824.0, need / 1073741824.0));
                if (Directory.Exists(gameDir) && !yes)
                {
                    Console.Write(gameDir + " already exists and will be overwritten. Continue? [Y/n]: ");
                    if ((Console.ReadLine() ?? "").Trim().ToLowerInvariant().StartsWith("n")) return 0;
                }
            }
            Console.WriteLine("Downloading {0} parts ({1:N1} GB) to {2}\n", parts.Count, total / 1073741824.0, verifyOnly ? "(verify only)" : gameDir);

            var psi = new ProcessStartInfo(tarExe, verifyOnly ? "-tf -" : "-xf - -C \"" + dir + "\"")
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = verifyOnly, CreateNoWindow = true };
            using (var tar = Process.Start(psi))
            {
                if (verifyOnly) tar.OutputDataReceived += (s, e) => { if (e.Data != null) Interlocked.Increment(ref listed); };
                if (verifyOnly) tar.BeginOutputReadLine();
                Stream tarIn = tar.StandardInput.BaseStream;
                string tmp = Path.Combine(verifyOnly ? Path.GetTempPath() : dir, "CityDragons-part.tmp");
                try
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        DownloadVerified(http, baseUrl + parts[i].Key, tmp, parts[i].Value, i + 1, parts.Count);
                        using (var f = File.OpenRead(tmp)) f.CopyTo(tarIn, 1 << 20);
                        File.Delete(tmp);
                    }
                }
                catch
                {
                    // Never leave tar waiting on a half-fed archive, or a stray part file behind.
                    try { if (!tar.HasExited) tar.Kill(); } catch { }
                    try { File.Delete(tmp); } catch { }
                    throw;
                }
                tarIn.Close();
                tar.WaitForExit();
                if (tar.ExitCode != 0) throw new Exception("tar exited with code " + tar.ExitCode + " while extracting.");
            }
        }
        if (verifyOnly) { Console.WriteLine("\nVERIFY OK: every part matched SHA256SUMS; tar listed {0} entries.", listed); return 0; }

        string bat = Path.Combine(gameDir, "Play City Dragons.bat");
        if (!File.Exists(bat)) throw new Exception("Extraction finished but " + bat + " is missing.");
        if (shortcut) MakeShortcut(bat, Path.Combine(gameDir, "CitySample.exe"));
        Console.WriteLine("\nInstalled to " + gameDir + (shortcut ? "\nA \"City Dragons\" shortcut is on your desktop." : ""));
        if (launch && !yes)
        {
            Console.Write("\nPlay now? [Y/n]: ");
            if (!(Console.ReadLine() ?? "").Trim().ToLowerInvariant().StartsWith("n"))
                Process.Start(new ProcessStartInfo(bat) { WorkingDirectory = gameDir, UseShellExecute = true });
        }
        return 0;
    }

    static int listed;

    // "hash  name" lines (sha256sum format), in archive order.
    static List<KeyValuePair<string, string>> ReadSums(string text)
    {
        var parts = new List<KeyValuePair<string, string>>();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length < 66) continue;
            string hash = line.Substring(0, 64).ToLowerInvariant(), name = line.Substring(64).Trim().TrimStart('*');
            if (name.Contains("/") || name.Contains("\\") || name.Contains("..")) throw new Exception("Bad file name in SHA256SUMS: " + name);
            parts.Add(new KeyValuePair<string, string>(name, hash));
        }
        parts.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        if (parts.Count == 0) throw new Exception("SHA256SUMS.txt lists no parts.");
        return parts;
    }

    static void DownloadVerified(HttpClient http, string url, string tmp, string want, int n, int total)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                string got = Download(http, url, tmp, n, total);
                if (got == want) { Console.WriteLine(); return; }
                throw new Exception("checksum mismatch");
            }
            catch (Exception e)
            {
                Exception root = e is AggregateException ? e.GetBaseException() : e;
                if (attempt >= 4) throw new Exception(string.Format("part {0}/{1} failed after {2} tries: {3}", n, total, attempt, root.Message));
                Console.WriteLine("\n  part {0}/{1}: {2}; retrying ({3}/3)...", n, total, root.Message, attempt);
                Thread.Sleep(3000 * attempt);
            }
        }
    }

    static string Download(HttpClient http, string url, string tmp, int n, int total)
    {
        using (var resp = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result)
        {
            resp.EnsureSuccessStatusCode();
            long len = resp.Content.Headers.ContentLength ?? -1, done = 0;
            var sw = Stopwatch.StartNew();
            using (var src = resp.Content.ReadAsStreamAsync().Result)
            using (var dst = File.Create(tmp))
            using (var sha = SHA256.Create())
            {
                var buf = new byte[1 << 20];
                int r; long lastTick = -1;
                while ((r = src.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, r);
                    sha.TransformBlock(buf, 0, r, null, 0);
                    done += r;
                    long t = sw.ElapsedMilliseconds / 500;
                    if (t != lastTick)
                    {
                        lastTick = t;
                        double mbps = done / 1048576.0 / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
                        Console.Write("\r  part {0}/{1}: {2,5:N0} / {3:N0} MB  {4,6:N1} MB/s   ", n, total, done / 1048576, len > 0 ? len / 1048576 : 0, mbps);
                    }
                }
                sha.TransformFinalBlock(buf, 0, 0);
                if (len > 0 && done != len) throw new Exception("download ended early");
                Console.Write("\r  part {0}/{1}: {2,5:N0} MB done, verified ", n, total, done / 1048576);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    static void MakeShortcut(string target, string icon)
    {
        string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "City Dragons.lnk");
        Type t = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(t);
        object sc = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
        Type st = sc.GetType();
        st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { target });
        st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Path.GetDirectoryName(target) });
        if (File.Exists(icon)) st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { icon + ",0" });
        st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, null);
    }
}
