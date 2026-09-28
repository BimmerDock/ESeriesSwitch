using System.IO;

namespace ESeriesSwitch.Services
{
    public sealed record NfsStatus(string IniPath, string? Interface, string? MultiChannel, IReadOnlyList<string> ChannelInterfaces)
    {
        /// <summary>WinKFP channel entries (Interface_1..3) that are not empty.</summary>
        public bool HasChannelInterfaces => ChannelInterfaces.Any(c => c.Length > 0);
    }

    /// <summary>
    /// NFS (WinKFP's "Nachflash-System", NFS\BIN\nfs.exe) has its own interface setting in NFS.INI [INSTANZ].
    /// Its default points to an OPPS (remote:OBD_ab625 ...), which makes NFS fail to start without one.
    /// Per the NFS help, a single-instance NFS should simply use the same channel as EDIABAS,
    /// with the WinKFP channel entries (Interface_1..3) left empty.
    /// </summary>
    public static class NfsConfig
    {
        const string Section = "INSTANZ";
        static readonly string[] ChannelKeys = ["Interface_1", "Interface_2", "Interface_3"];

        /// <summary>NFS.INI next to the EDIABAS install (…\EC-APPS\NFS\BIN), or the classic C:\NFS\BIN. Null if not found.</summary>
        public static string? FindIniPath()
        {
            var ecApps = Path.GetFullPath(Path.Combine(Environment.ExpandEnvironmentVariables(EnvironmentSwitcher.EdiabasBin), "..", ".."));
            string[] candidates =
            [
                Path.Combine(ecApps, "NFS", "BIN", "NFS.INI"),
                @"C:\EC-APPS\NFS\BIN\NFS.INI",
                @"C:\NFS\BIN\NFS.INI",
            ];
            return candidates.FirstOrDefault(File.Exists);
        }

        /// <summary>Null if NFS is not installed or NFS.INI has no [INSTANZ] Interface line.</summary>
        public static NfsStatus? Read()
        {
            var path = FindIniPath();
            if (path == null)
                return null;

            var iface = IniFile.Read(path, Section, "Interface");
            if (iface == null)
                return null;

            var channels = ChannelKeys.Select(k => IniFile.Read(path, Section, k) ?? "").ToList();
            return new NfsStatus(path, iface, IniFile.Read(path, Section, "MEHRKANAL"), channels);
        }

        public static bool MatchesEdiabas(NfsStatus nfs, string? ediabasInterface) =>
            ediabasInterface != null
            && string.Equals(nfs.Interface, ediabasInterface, StringComparison.OrdinalIgnoreCase)
            && !nfs.HasChannelInterfaces;

        /// <summary>
        /// Sets NFS to the given EDIABAS interface and empties the WinKFP channel entries.
        /// Only lines that actually differ are written; MEHRKANAL and everything else is left as it is.
        /// </summary>
        public static void SyncWith(string ediabasInterface)
        {
            var nfs = Read();
            if (nfs == null)
                return;

            if (!string.Equals(nfs.Interface, ediabasInterface, StringComparison.OrdinalIgnoreCase))
                IniFile.Write(nfs.IniPath, Section, "Interface", ediabasInterface);

            for (int i = 0; i < ChannelKeys.Length; i++)
                if (nfs.ChannelInterfaces[i].Length > 0)
                    IniFile.Write(nfs.IniPath, Section, ChannelKeys[i], "");
        }
    }
}
