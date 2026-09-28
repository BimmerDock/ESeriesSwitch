namespace ESeriesSwitch.Localization
{
    /// <summary>All UI texts in English and Hungarian. Keys missing from Hungarian fall back to English.</summary>
    static class Strings
    {
        public static readonly Dictionary<string, string> English = new()
        {
            // Header
            ["Subtitle"] = "Switch between the legacy E-series coding tools (INPA, WinKFP, NCS Expert, Tool32) and BMW's official ISTA+ downloaded from AOS.",

            // Status card
            ["CurrentlyActive"] = "CURRENTLY ACTIVE",
            ["Loading"] = "Loading…",
            ["ModeIsta"] = "Factory ISTA+ mode",
            ["ModeIstaDesc"] = "EDIABAS is not in the system environment variables, ISTA+ can be used.",
            ["ModeESeries"] = "E-series mode",
            ["ModeESeriesDesc"] = "INPA / WinKFP / NCS Expert / Tool32 can be used. ISTA+ will not work properly in this mode.",
            ["ModeMixed"] = "Partial state",
            ["ModeMixedDesc"] = "Only one of the two settings is present. Choose a mode below to fix it.",
            ["NotSet"] = "✗  not set",
            ["PathContains"] = "✓  contains: {0}",
            ["PathMissing"] = "✗  does not contain the EDIABAS folder",

            // EDIABAS interface card
            ["InterfaceHeader"] = "EDIABAS INTERFACE",
            ["InterfaceIcomTitle"] = "ICOM",
            ["InterfaceOfflineTitle"] = "Offline (no interface)",
            ["InterfaceOtherTitle"] = "Other interface",
            ["InterfaceUnreadable"] = "Cannot be read",
            ["InterfaceHintIcom"] = "For E-series cars with an ICOM (INPA, Tool32, NCS Expert). Reserve the ICOM in ITool Radar first and enter its IP address below. Without a connected ICOM, INPA / Tool32 show a “NET-0009: TIMEOUT” error on startup. Switch to Offline in that case.",
            ["InterfaceHintOffline"] = "INPA / Tool32 start without an error message, but cannot communicate with the car. Switch to ICOM for diagnostics.",
            ["InterfaceHintOther"] = "EDIABAS.INI contains an interface this app does not manage. Choosing one of the options above replaces it.",
            ["InterfaceEnetTitle"] = "ENET (Ethernet cable)",
            ["InterfaceKDcanTitle"] = "K+DCAN (USB cable)",
            ["InterfaceHintEnet"] = "Ethernet (ENET) connection for F/G-series cars (e.g. INPA64 / Tool64). With an ICOM Next choose “ICOM Next” (ports 50161 / 50160) and reserve the ICOM in ITool Radar first. Leave the address on Autodetect, or enter the ICOM's IP.",
            ["InterfaceHintKDcan"] = "K+DCAN USB cable for E-series cars (STD:OBD, 32-bit EDIABAS: LoadWin64 = 0). Enter the cable's COM port below (obd.ini), and set the port's latency to 1 ms in Device Manager.",
            ["HostLabelIcom"] = "ICOM IP address",
            ["HostLabelEnet"] = "Vehicle / ICOM IP",
            ["HostLabelKDcan"] = "COM port",
            ["NfsMatches"] = "✓  Uses the same interface as EDIABAS (NFS.INI).",
            ["NfsDiffers"] = "⚠  NFS.INI uses a different interface than EDIABAS, so WinKFP / NFS may not start or connect. Click Sync to match it.",
            ["NfsSync"] = "Sync",
            ["EnetVariantLabel"] = "Connection",
            ["EnetCable"] = "ENET cable",
            ["EnetCustomPorts"] = "custom ports",
            ["LoadWin64Mismatch"] = "LoadWin64 = {0}, but {1} needs {2}. Click {1} again to fix it.",
            ["LoadWin64Missing"] = "EDIABAS.INI has no LoadWin64 line, so EDIABAS uses 0. {0} needs LoadWin64 = {1}: add it to the [Configuration] section by hand.",
            ["Save"] = "Save",
            ["Saved"] = "✓  Saved",
            ["HostNotSetIcom"] = "The ICOM IP address is not set yet. Enter it and click Save.",
            ["HostMissingIcom"] = "No RemoteHost line was found in the [ICOM_P] section of Rplus.ini.",
            ["HostMissingEnet"] = "No RemoteHost line was found in the [XEthernet] section of EDIABAS.INI.",
            ["HostMissingKDcan"] = "No Port line was found in the [OBD] section of obd.ini.",
            ["ButtonOffline"] = "Offline",

            // Buttons
            ["SwitchToESeries"] = "Switch to E-series tools  (INPA / WinKFP / EDIABAS)",
            ["SwitchToIsta"] = "Switch to factory ISTA+",
            ["RebootRecommended"] = "A restart is recommended for the change to take full effect.",
            ["RestartNow"] = "Restart now",
            ["Refresh"] = "Refresh",
            ["OpenBackups"] = "Backups folder",
            ["About"] = "About",

            // Update check
            ["UpdateAvailable"] = "A new version is available: v{0} (you have v{1})",
            ["Download"] = "Download",
            ["Hide"] = "Hide",

            // Warnings
            ["WarnConfigDirMissingFolder"] = "{0} points to a folder that does not exist: {1}",
            ["WarnFolderMissing"] = "The EDIABAS folder was not found on this computer ({0}).",
            ["WarnUserVar"] = "{0} is also set among the user variables. The app does not change it; consider removing it manually.",
            ["WarnUserPath"] = "The user PATH also contains the EDIABAS folder. The app does not change it; consider removing it manually.",

            // Messages
            ["ErrReadEnv"] = "Could not read the environment variables:\n\n{0}",
            ["ErrEnvKeyRead"] = "The system environment variables registry key cannot be read.",
            ["ErrEnvKeyWrite"] = "The system environment variables registry key cannot be written.",
            ["ErrNoPermission"] = "No permission to modify the system environment variables.\nRun the app as administrator.",
            ["ErrSwitchFailed"] = "The switch failed:\n\n{0}",
            ["SwitchedESeries"] = "E-series mode (INPA / WinKFP / EDIABAS)",
            ["SwitchedIsta"] = "factory ISTA+ mode",
            ["IcomNote"] = "EDIABAS is set to ICOM: without a connected ICOM, INPA shows a “NET-0009: TIMEOUT” error. If no ICOM is connected, switch to Offline below.\n\n",
            ["SwitchSuccess"] = "Switched successfully: {0}.\n\n{1}Newly started programs already see the new setting, but a restart is recommended for reliable operation (especially because of the ISTA+ services).\n\nRestart the computer now?",
            ["ConfirmRestart"] = "Restart the computer now? Save your open work first!",
            ["ErrRestart"] = "Could not start the restart:\n\n{0}",
            ["ErrIniWrite"] = "Could not modify EDIABAS.INI:\n\n{0}",
            ["ErrIniKeyMissing"] = "No '{0}' line was found in the [{1}] section of {2}.",
            ["ErrInvalidIcomIp"] = "Invalid IP address: “{0}”\n\nExample: 169.254.92.38",
            ["ErrInvalidEnetHost"] = "Invalid address: “{0}”\n\nEnter an IP address (e.g. 169.254.1.1) or Autodetect.",
            ["ErrInvalidComPort"] = "Invalid COM port: “{0}”\n\nExample: COM3",
            ["MadeBy"] = "Made by",
            ["AboutText"] =
                "E-Series ⇄ ISTA+ Switch  v{0}\n" +
                "© 2026 BimmerDock\n\n" +
                "Website:  {1}\n" +
                "Source code:  {2}\n\n" +
                "Free, open-source tool (MIT License).\n\n" +
                "Not affiliated with, endorsed or supported by BMW AG. BMW, MINI, ISTA, INPA, WinKFP, NCS Expert and EDIABAS " +
                "are trademarks of their respective owners.\n\n" +
                "Use at your own risk. The software is provided “as is”, without warranty of any kind. BimmerDock accepts no " +
                "liability for any damage to vehicles, control units, computers or data.\n\n" +
                "Open bimmerdock.com?",
        };

        public static readonly Dictionary<string, string> Hungarian = new()
        {
            // Header
            ["Subtitle"] = "Váltás a régi E-szériás kódoló toolok (INPA, WinKFP, NCS Expert, Tool32) és a gyári, AOS-ból letöltött ISTA+ között.",

            // Status card
            ["CurrentlyActive"] = "JELENLEG AKTÍV",
            ["Loading"] = "Betöltés…",
            ["ModeIsta"] = "Gyári ISTA+ mód",
            ["ModeIstaDesc"] = "Az EDIABAS nincs a rendszer környezeti változói között, az ISTA+ használható.",
            ["ModeESeries"] = "E-szériás mód",
            ["ModeESeriesDesc"] = "INPA / WinKFP / NCS Expert / Tool32 használható. Az ISTA+ ilyenkor nem fog rendesen működni.",
            ["ModeMixed"] = "Részleges állapot",
            ["ModeMixedDesc"] = "Csak az egyik beállítás van jelen. Válassz lent egy módot a rendbetételhez.",
            ["NotSet"] = "✗  nincs beállítva",
            ["PathContains"] = "✓  tartalmazza: {0}",
            ["PathMissing"] = "✗  nem tartalmazza az EDIABAS mappát",

            // EDIABAS interface card
            ["InterfaceHeader"] = "EDIABAS INTERFÉSZ",
            ["InterfaceIcomTitle"] = "ICOM",
            ["InterfaceOfflineTitle"] = "Offline (nincs interfész)",
            ["InterfaceOtherTitle"] = "Egyéb interfész",
            ["InterfaceUnreadable"] = "Nem olvasható",
            ["InterfaceHintIcom"] = "E-szériás autókhoz ICOM-mal (INPA, Tool32, NCS Expert). Előbb foglald le az ICOM-ot az ITool Radarban, és lent add meg az IP-címét. Csatlakoztatott ICOM nélkül az INPA / Tool32 indításkor „NET-0009: TIMEOUT” hibát ad. Ilyenkor válts Offline-ra.",
            ["InterfaceHintOffline"] = "Az INPA / Tool32 hibaüzenet nélkül indul, de az autóval nem tud kommunikálni. Diagnosztikához válts ICOM-ra.",
            ["InterfaceHintOther"] = "Az EDIABAS.INI-ben olyan interfész van, amit az app nem kezel. A fenti gombok bármelyike lecseréli.",
            ["InterfaceEnetTitle"] = "ENET (Ethernet kábel)",
            ["InterfaceKDcanTitle"] = "K+DCAN (USB kábel)",
            ["InterfaceHintEnet"] = "Ethernet (ENET) kapcsolat F/G szériás autókhoz (pl. INPA64 / Tool64). ICOM Next-hez válaszd az „ICOM Next” lehetőséget (50161 / 50160-as portok), és előtte foglald le az ICOM-ot az ITool Radarban. A címet hagyd Autodetect-en, vagy add meg az ICOM IP-címét.",
            ["InterfaceHintKDcan"] = "K+DCAN USB kábel E-szériás autókhoz (STD:OBD, 32 bites EDIABAS: LoadWin64 = 0). Lent add meg a kábel COM portját (obd.ini), az Eszközkezelőben pedig állítsd a port késleltetését (latency) 1 ms-ra.",
            ["HostLabelIcom"] = "ICOM IP-cím",
            ["HostLabelEnet"] = "Autó / ICOM IP",
            ["HostLabelKDcan"] = "COM port",
            ["NfsMatches"] = "✓  Ugyanazt az interfészt használja, mint az EDIABAS (NFS.INI).",
            ["NfsDiffers"] = "⚠  Az NFS.INI más interfészt használ, mint az EDIABAS, ezért a WinKFP / NFS nem biztos, hogy elindul vagy csatlakozik. Kattints a Szinkron gombra.",
            ["NfsSync"] = "Szinkron",
            ["EnetVariantLabel"] = "Kapcsolat",
            ["EnetCable"] = "ENET kábel",
            ["EnetCustomPorts"] = "egyedi portok",
            ["LoadWin64Mismatch"] = "LoadWin64 = {0}, de a(z) {1} interfészhez {2} kell. Kattints újra a(z) {1} gombra a javításhoz.",
            ["LoadWin64Missing"] = "Az EDIABAS.INI-ben nincs LoadWin64 sor, így az EDIABAS 0-t használ. A(z) {0} interfészhez LoadWin64 = {1} kell: add hozzá kézzel a [Configuration] részhez.",
            ["Save"] = "Mentés",
            ["Saved"] = "✓  Mentve",
            ["HostNotSetIcom"] = "Az ICOM IP-címe még nincs beállítva. Írd be, és kattints a Mentés gombra.",
            ["HostMissingIcom"] = "Az Rplus.ini [ICOM_P] részében nem található RemoteHost sor.",
            ["HostMissingEnet"] = "Az EDIABAS.INI [XEthernet] részében nem található RemoteHost sor.",
            ["HostMissingKDcan"] = "Az obd.ini [OBD] részében nem található Port sor.",
            ["ButtonOffline"] = "Offline",

            // Buttons
            ["SwitchToESeries"] = "Váltás E-szériás toolokra  (INPA / WinKFP / EDIABAS)",
            ["SwitchToIsta"] = "Váltás gyári ISTA+ -ra",
            ["RebootRecommended"] = "A váltás teljes érvényesüléséhez újraindítás ajánlott.",
            ["RestartNow"] = "Újraindítás most",
            ["Refresh"] = "Frissítés",
            ["OpenBackups"] = "Mentések mappája",
            ["About"] = "Névjegy",

            // Update check
            ["UpdateAvailable"] = "Új verzió érhető el: v{0} (a tiéd: v{1})",
            ["Download"] = "Letöltés",
            ["Hide"] = "Elrejtés",

            // Warnings
            ["WarnConfigDirMissingFolder"] = "Az {0} egy nem létező mappára mutat: {1}",
            ["WarnFolderMissing"] = "Az EDIABAS mappa nem található ezen a gépen ({0}).",
            ["WarnUserVar"] = "A felhasználói változók között is van {0}. Ezt az app nem módosítja, érdemes kézzel törölni.",
            ["WarnUserPath"] = "A felhasználói PATH is tartalmazza az EDIABAS mappát. Ezt az app nem módosítja, érdemes kézzel törölni.",

            // Messages
            ["ErrReadEnv"] = "Nem sikerült beolvasni a környezeti változókat:\n\n{0}",
            ["ErrEnvKeyRead"] = "A rendszer környezeti változóinak registry kulcsa nem olvasható.",
            ["ErrEnvKeyWrite"] = "A rendszer környezeti változóinak registry kulcsa nem írható.",
            ["ErrNoPermission"] = "Nincs jogosultság a rendszer környezeti változóinak módosításához.\nIndítsd az appot rendszergazdaként.",
            ["ErrSwitchFailed"] = "A váltás nem sikerült:\n\n{0}",
            ["SwitchedESeries"] = "E-szériás mód (INPA / WinKFP / EDIABAS)",
            ["SwitchedIsta"] = "gyári ISTA+ mód",
            ["IcomNote"] = "Az EDIABAS ICOM-ra van állítva: csatlakoztatott ICOM nélkül az INPA „NET-0009: TIMEOUT” hibát ad. Ha nincs ICOM a gépen, válts lent Offline-ra.\n\n",
            ["SwitchSuccess"] = "Sikeres váltás: {0}.\n\n{1}Az újonnan indított programok már az új beállítást látják, de a biztos működéshez (főleg az ISTA+ szolgáltatásai miatt) újraindítás ajánlott.\n\nÚjraindítod most a gépet?",
            ["ConfirmRestart"] = "Biztosan újraindítod a gépet? Előtte mentsd el a nyitott munkáidat!",
            ["ErrRestart"] = "Nem sikerült elindítani az újraindítást:\n\n{0}",
            ["ErrIniWrite"] = "Az EDIABAS.INI módosítása nem sikerült:\n\n{0}",
            ["ErrIniKeyMissing"] = "A(z) {2} [{1}] részében nem található '{0}' sor.",
            ["ErrInvalidIcomIp"] = "Érvénytelen IP-cím: „{0}”\n\nPélda: 169.254.92.38",
            ["ErrInvalidEnetHost"] = "Érvénytelen cím: „{0}”\n\nAdj meg egy IP-címet (pl. 169.254.1.1), vagy azt, hogy Autodetect.",
            ["ErrInvalidComPort"] = "Érvénytelen COM port: „{0}”\n\nPélda: COM3",
            ["MadeBy"] = "Készítette:",
            ["AboutText"] =
                "E-Series ⇄ ISTA+ Switch  v{0}\n" +
                "© 2026 BimmerDock\n\n" +
                "Weboldal:  {1}\n" +
                "Forráskód:  {2}\n\n" +
                "Ingyenes, nyílt forráskódú program (MIT licenc).\n\n" +
                "Nem kapcsolódik a BMW AG-hez, és a BMW AG nem támogatja vagy hagyja jóvá. A BMW, MINI, ISTA, INPA, WinKFP, " +
                "NCS Expert és EDIABAS nevek a jogtulajdonosaik védjegyei.\n\n" +
                "Használata saját felelősségre történik. A program „ahogy van” alapon, mindennemű garancia nélkül érhető el. " +
                "A BimmerDock semmilyen felelősséget nem vállal a járművekben, vezérlőegységekben, számítógépekben vagy adatokban " +
                "keletkező károkért.\n\n" +
                "Megnyitod a bimmerdock.com oldalt?",
        };
    }
}
