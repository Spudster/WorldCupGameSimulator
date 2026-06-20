using System.Diagnostics;

namespace WorldCup.Cli;

/// <summary>
/// Renders a generated HTML report to a PNG image by driving a headless Chromium browser (the Microsoft
/// Edge that ships with Windows, or Chrome/Chromium) — no extra dependencies, full-fidelity rendering of
/// the report's CSS and the bracket fit-script. Returns false (gracefully) when no browser is found.
/// </summary>
public static class BrowserShot
{
    /// <summary>True when a Chromium browser is available to render images.</summary>
    public static bool Available => FindBrowser() is not null;

    /// <summary>
    /// Screenshot <paramref name="htmlPath"/> to <paramref name="pngPath"/>. The capture is the size of the
    /// window, so <paramref name="height"/> should be tall enough to cover the report's content.
    /// </summary>
    public static bool TrySave(string htmlPath, string pngPath, int width, int height, out string diagnostic)
    {
        string? browser = FindBrowser();
        if (browser is null)
        {
            diagnostic = "no Chromium browser (Edge/Chrome) found to render the image";
            return false;
        }

        try
        {
            string url = "file:///" + Path.GetFullPath(htmlPath).Replace('\\', '/');
            var psi = new ProcessStartInfo(browser)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("--headless=new");
            psi.ArgumentList.Add("--disable-gpu");
            psi.ArgumentList.Add("--hide-scrollbars");
            psi.ArgumentList.Add("--force-device-scale-factor=1");
            psi.ArgumentList.Add($"--screenshot={pngPath}");
            psi.ArgumentList.Add($"--window-size={width},{height}");
            psi.ArgumentList.Add(url);

            using var p = Process.Start(psi)!;
            // Drain the (verbose) browser output so the pipes never fill and deadlock.
            p.OutputDataReceived += (_, _) => { };
            p.ErrorDataReceived += (_, _) => { };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (!p.WaitForExit(30000))
            {
                try { p.Kill(true); } catch { /* best effort */ }
                diagnostic = "the browser took too long to render the image";
                return false;
            }

            if (File.Exists(pngPath))
            {
                diagnostic = Path.GetFileName(browser);
                return true;
            }

            diagnostic = "the browser ran but no image was produced";
            return false;
        }
        catch (Exception ex)
        {
            diagnostic = ex.Message;
            return false;
        }
    }

    private static string? FindBrowser()
    {
        var candidates = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            candidates.Add(Path.Combine(pfx86, "Microsoft", "Edge", "Application", "msedge.exe"));
            candidates.Add(Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe"));
            candidates.Add(Path.Combine(pf, "Google", "Chrome", "Application", "chrome.exe"));
            candidates.Add(Path.Combine(pfx86, "Google", "Chrome", "Application", "chrome.exe"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.Add("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome");
            candidates.Add("/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge");
            candidates.Add("/Applications/Chromium.app/Contents/MacOS/Chromium");
        }
        else
        {
            candidates.Add("/usr/bin/google-chrome");
            candidates.Add("/usr/bin/google-chrome-stable");
            candidates.Add("/usr/bin/chromium");
            candidates.Add("/usr/bin/chromium-browser");
            candidates.Add("/usr/bin/microsoft-edge");
        }

        return candidates.FirstOrDefault(File.Exists);
    }
}
