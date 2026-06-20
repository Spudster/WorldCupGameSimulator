namespace WorldCup.Cli;

/// <summary>
/// Every generated artifact (HTML, JSON, CSV, commentary transcripts, snapshots, report bundles) is
/// written under a single <c>output/</c> folder, so the whole lot is trivial to find, move or delete.
/// <para>
/// The folder is anchored at the <em>project root</em> (the nearest ancestor containing a
/// <c>.sln</c> file) rather than the raw working directory — otherwise, when the app is launched from
/// its <c>bin/</c> build folder, outputs would be buried there and wiped on the next rebuild. If no
/// solution is found (e.g. a published stand-alone build), it falls back to the working directory.
/// </para>
/// </summary>
public static class OutputFolder
{
    public const string Name = "output";

    private static readonly Lazy<string> RootPath = new(() =>
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (dir.GetFiles("*.sln").Length > 0)
                {
                    return Path.Combine(dir.FullName, Name);
                }
            }
        }

        return Path.Combine(Directory.GetCurrentDirectory(), Name);
    });

    /// <summary>The output folder (absolute). Not guaranteed to exist until something is written to it.</summary>
    public static string Root => RootPath.Value;

    /// <summary>Full path for an output file inside the output folder (the folder is created if needed).</summary>
    public static string Resolve(string fileName)
    {
        Directory.CreateDirectory(Root);
        return Path.Combine(Root, fileName);
    }

    /// <summary>Full path for an output SUBFOLDER (created if needed) — used for multi-file bundles.</summary>
    public static string Subdir(string name)
    {
        string dir = Path.Combine(Root, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Locate a file for loading: prefer the output folder, then fall back to the working directory.</summary>
    public static string Find(string fileName)
    {
        string inOutput = Path.Combine(Root, fileName);
        if (File.Exists(inOutput))
        {
            return inOutput;
        }

        string inCwd = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        return File.Exists(inCwd) ? inCwd : inOutput;
    }
}
