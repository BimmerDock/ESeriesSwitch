using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ESeriesSwitch.Services
{
    public enum InterfaceKind
    {
        Icom,
        Enet,
        KDcan,
        Offline,
        Other
    }

    public enum EnetVariant
    {
        Cable,
        Icom,
        Custom
    }

    /// <summary>
    /// The diagnostic interface settings of EDIABAS:
    /// - EDIABAS.INI [Configuration] Interface: which interface EDIABAS uses
    /// - EDIABAS.INI [Configuration] LoadWin64: 64-bit (1) or 32-bit (0) EDIABAS, depends on the interface
    /// - ENET ports: EDIABAS.INI [XEthernet] ControlPort / DiagnosticPort (ENET cable or ICOM Next)
    /// - ICOM address: Rplus.ini [ICOM_P] RemoteHost
    /// - ENET address: EDIABAS.INI [XEthernet] RemoteHost
    /// - K+DCAN COM port: obd.ini [OBD] Port
    /// Everything is read from the INI files each time, so the app always shows the current configuration.
    /// </summary>
    public static class EdiabasConfig
    {
        public const string IcomInterface = "RPLUS:ICOM_P";
        public const string EnetInterface = "ENET";
        public const string KDcanInterface = "STD:OBD";
        public const string OfflineInterface = "NUL";
        public const string Autodetect = "Autodetect";

        public static string IniPath => Path.Combine(EnvironmentSwitcher.EdiabasBin, "EDIABAS.INI");
        public static string RplusIniPath => Path.Combine(EnvironmentSwitcher.EdiabasBin, "Rplus.ini");
        public static string ObdIniPath => Path.Combine(EnvironmentSwitcher.EdiabasBin, "obd.ini");

        public static string InterfaceValue(InterfaceKind kind) => kind switch
        {
            InterfaceKind.Icom => IcomInterface,
            InterfaceKind.Enet => EnetInterface,
            InterfaceKind.KDcan => KDcanInterface,
            InterfaceKind.Offline => OfflineInterface,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public static InterfaceKind Classify(string? value) => value?.Trim().ToUpperInvariant() switch
        {
            "RPLUS:ICOM_P" => InterfaceKind.Icom,
            "ENET" => InterfaceKind.Enet,
            "STD:OBD" => InterfaceKind.KDcan,
            "NUL" => InterfaceKind.Offline,
            _ => InterfaceKind.Other
        };

        /// <summary>The current Interface value, or null if EDIABAS.INI or the line does not exist.</summary>
        public static string? ReadInterface() => IniFile.Read(IniPath, "Configuration", "Interface");

        /// <summary>
        /// Sets the interface and the LoadWin64 value it needs (see <see cref="RequiredLoadWin64"/>).
        /// A missing LoadWin64 line is not added; the UI reports it instead.
        /// </summary>
        public static void SetInterface(InterfaceKind kind)
        {
            IniFile.Write(IniPath, "Configuration", "Interface", InterfaceValue(kind));

            var required = RequiredLoadWin64(kind);
            var current = ReadLoadWin64();
            if (required != null && current != null && current != required)
                IniFile.Write(IniPath, "Configuration", "LoadWin64", required);
        }

        /// <summary>
        /// Per the EDIABAS 7.7 configuration guide: ICOM and ENET run with the 64-bit EDIABAS (1),
        /// the K+DCAN OBD driver with the 32-bit one (0). Offline / other: no requirement.
        /// </summary>
        public static string? RequiredLoadWin64(InterfaceKind kind) => kind switch
        {
            InterfaceKind.Icom or InterfaceKind.Enet => "1",
            InterfaceKind.KDcan => "0",
            _ => null
        };

        /// <summary>Null if the line is missing (EDIABAS then uses the default, 0).</summary>
        public static string? ReadLoadWin64() => IniFile.Read(IniPath, "Configuration", "LoadWin64");

        /// <summary>
        /// ENET through an ENET cable uses the standard gateway ports (6811 / 6801),
        /// ENET through an ICOM Next uses the ICOM's ports (50161 / 50160).
        /// </summary>
        public static EnetVariant ReadEnetVariant(out string? controlPort, out string? diagnosticPort)
        {
            controlPort = IniFile.Read(IniPath, "XEthernet", "ControlPort");
            diagnosticPort = IniFile.Read(IniPath, "XEthernet", "DiagnosticPort");
            return (controlPort ?? EnetCablePorts.Control, diagnosticPort ?? EnetCablePorts.Diagnostic) switch
            {
                var (c, d) when c == EnetCablePorts.Control && d == EnetCablePorts.Diagnostic => EnetVariant.Cable,
                var (c, d) when c == EnetIcomPorts.Control && d == EnetIcomPorts.Diagnostic => EnetVariant.Icom,
                _ => EnetVariant.Custom
            };
        }

        public static void SetEnetVariant(EnetVariant variant)
        {
            var (control, diagnostic) = variant == EnetVariant.Icom ? EnetIcomPorts : EnetCablePorts;
            IniFile.Write(IniPath, "XEthernet", "ControlPort", control);
            IniFile.Write(IniPath, "XEthernet", "DiagnosticPort", diagnostic);
        }

        static readonly (string Control, string Diagnostic) EnetCablePorts = ("6811", "6801");
        static readonly (string Control, string Diagnostic) EnetIcomPorts = ("50161", "50160");

        /// <summary>The ICOM address from Rplus.ini [ICOM_P] RemoteHost. 0.0.0.0 means "not set".</summary>
        public static string? ReadIcomHost() => IniFile.Read(RplusIniPath, "ICOM_P", "RemoteHost");

        public static void SetIcomHost(string ip) => IniFile.Write(RplusIniPath, "ICOM_P", "RemoteHost", ip);

        /// <summary>ENET address: Autodetect, the vehicle's IP, or the ICOM's IP when an ICOM Next is used.</summary>
        public static string? ReadEnetHost() => IniFile.Read(IniPath, "XEthernet", "RemoteHost");

        public static void SetEnetHost(string host) => IniFile.Write(IniPath, "XEthernet", "RemoteHost", host);

        /// <summary>COM port of the K+DCAN cable: obd.ini [OBD] Port, next to EDIABAS.INI.</summary>
        public static string? ReadKDcanPort() => IniFile.Read(ObdIniPath, "OBD", "Port");

        public static void SetKDcanPort(string port) => IniFile.Write(ObdIniPath, "OBD", "Port", port);

        /// <summary>"com3", "COM 3" or "3" → "Com3" (the spelling used in obd.ini). Null if invalid.</summary>
        public static string? NormalizeComPort(string input)
        {
            var match = Regex.Match(input.Trim(), @"^(?:com\s*)?(\d{1,3})$", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var n) && n is >= 1 and <= 256
                ? "Com" + n
                : null;
        }

        public static bool IsIPv4(string? value) =>
            value != null
            && value.Split('.').Length == 4
            && IPAddress.TryParse(value, out var ip)
            && ip.AddressFamily == AddressFamily.InterNetwork;
    }
}
