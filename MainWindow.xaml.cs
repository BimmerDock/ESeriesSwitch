using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ESeriesSwitch.Localization;
using ESeriesSwitch.Services;

namespace ESeriesSwitch
{
    public partial class MainWindow : Window
    {
        UpdateInfo? _update;

        public MainWindow()
        {
            Loc.Instance.SetLanguage(AppSettings.LoadLanguage());
            InitializeComponent();
            VersionText.Text = "v" + AppInfo.VersionText;
            SizeChanged += (_, _) => FitToScreen();
            Loaded += async (_, _) =>
            {
                FitToScreen();
                RefreshStatus();
                _update = await UpdateChecker.CheckAsync();
                ShowUpdate();
            };
        }

        // Small laptop screens: the window is sized to its content, which can be taller than the screen.
        // Cap it to the monitor's work area (the content then scrolls) and keep the title bar on screen.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            FitToScreen();
        }

        void FitToScreen()
        {
            var area = GetWorkArea();
            if (MaxHeight != area.Height)
                MaxHeight = area.Height;

            if (ActualHeight > 0)
            {
                if (Top + ActualHeight > area.Bottom)
                    Top = area.Bottom - ActualHeight;
                if (Top < area.Top)
                    Top = area.Top;
            }
        }

        /// <summary>Work area (screen minus taskbar) of the monitor the window is on, in WPF units.</summary>
        Rect GetWorkArea()
        {
            var area = SystemParameters.WorkArea;
            var hwnd = new WindowInteropHelper(this).Handle;
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            var source = PresentationSource.FromVisual(this);
            if (hwnd != IntPtr.Zero && source?.CompositionTarget != null
                && GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref info))
            {
                var toWpf = source.CompositionTarget.TransformFromDevice;
                area = new Rect(
                    toWpf.Transform(new Point(info.rcWork.Left, info.rcWork.Top)),
                    toWpf.Transform(new Point(info.rcWork.Right, info.rcWork.Bottom)));
            }
#if DEBUG
            // Test hook: simulate a small screen, e.g. set ESS_TEST_SCREEN_HEIGHT=700
            if (double.TryParse(Environment.GetEnvironmentVariable("ESS_TEST_SCREEN_HEIGHT"), out var testHeight))
                area = new Rect(area.X, area.Y, area.Width, Math.Min(area.Height, testHeight));
#endif
            return area;
        }

        const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        void ShowUpdate()
        {
            UpdateBanner.Visibility = _update != null ? Visibility.Visible : Visibility.Collapsed;
            if (_update != null)
                UpdateText.Text = Loc.T("UpdateAvailable", _update.Version.ToString(3), AppInfo.VersionText);
        }

        void DownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_update != null)
                OpenUrl(_update.Url);
        }

        void DismissUpdate_Click(object sender, RoutedEventArgs e)
        {
            _update = null;
            ShowUpdate();
        }

        // Via explorer.exe, so the browser does not inherit the app's administrator rights
        static void OpenUrl(string url) =>
            Process.Start(new ProcessStartInfo("explorer.exe", url) { UseShellExecute = true });

        void RefreshStatus()
        {
            UpdateLanguageButtons();
            ShowUpdate();

            EnvStatus status;
            try
            {
                status = EnvironmentSwitcher.GetStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrReadEnv", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (title, subtitle, brushKey) = status.Mode switch
            {
                ToolMode.Ista => (Loc.T("ModeIsta"), Loc.T("ModeIstaDesc"), "IstaBrush"),
                ToolMode.ESeries => (Loc.T("ModeESeries"), Loc.T("ModeESeriesDesc"), "ESeriesBrush"),
                _ => (Loc.T("ModeMixed"), Loc.T("ModeMixedDesc"), "MixedBrush")
            };

            var brush = (Brush)FindResource(brushKey);
            StatusTitle.Text = title;
            StatusSubtitle.Text = subtitle;
            StatusStripe.Background = brush;
            StatusDot.Fill = brush;

            ConfigDirText.Text = string.IsNullOrWhiteSpace(status.ConfigDirValue)
                ? Loc.T("NotSet")
                : "✓  " + status.ConfigDirValue;
            PathText.Text = status.PathContainsEdiabas
                ? Loc.T("PathContains", status.EdiabasBin)
                : Loc.T("PathMissing");

            ToESeriesButton.Visibility = status.Mode == ToolMode.ESeries ? Visibility.Collapsed : Visibility.Visible;
            ToIstaButton.Visibility = status.Mode == ToolMode.Ista ? Visibility.Collapsed : Visibility.Visible;

            WarningText.Text = string.Join("\n", status.Warnings.Select(w => "⚠  " + w));
            WarningBox.Visibility = status.Warnings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            RefreshInterface();
        }

        InterfaceKind _interfaceKind = InterfaceKind.Other;
        string _loadedHost = "";

        void RefreshInterface()
        {
            string? value;
            try
            {
                value = EdiabasConfig.ReadInterface();
            }
            catch (Exception ex)
            {
                InterfaceCard.Visibility = Visibility.Visible;
                InterfaceTitle.Text = Loc.T("InterfaceUnreadable");
                InterfaceValue.Text = EdiabasConfig.IniPath;
                InterfaceHint.Text = ex.Message;
                SetInterfaceButtonsEnabled(false);
                HostRow.Visibility = HostStatus.Visibility = Visibility.Collapsed;
                return;
            }

            if (value == null)
            {
                InterfaceCard.Visibility = Visibility.Collapsed;
                return;
            }

            InterfaceCard.Visibility = Visibility.Visible;
            SetInterfaceButtonsEnabled(true);
            InterfaceValue.Text = "Interface = " + value;
            _interfaceKind = EdiabasConfig.Classify(value);

            (InterfaceTitle.Text, InterfaceHint.Text) = _interfaceKind switch
            {
                InterfaceKind.Icom => (Loc.T("InterfaceIcomTitle"), Loc.T("InterfaceHintIcom")),
                InterfaceKind.Enet => (Loc.T("InterfaceEnetTitle"), Loc.T("InterfaceHintEnet")),
                InterfaceKind.KDcan => (Loc.T("InterfaceKDcanTitle"), Loc.T("InterfaceHintKDcan")),
                InterfaceKind.Offline => (Loc.T("InterfaceOfflineTitle"), Loc.T("InterfaceHintOffline")),
                _ => (Loc.T("InterfaceOtherTitle"), Loc.T("InterfaceHintOther")),
            };

            SetToggle(IcomButton, _interfaceKind == InterfaceKind.Icom);
            SetToggle(EnetButton, _interfaceKind == InterfaceKind.Enet);
            SetToggle(KDcanButton, _interfaceKind == InterfaceKind.KDcan);
            SetToggle(OfflineButton, _interfaceKind == InterfaceKind.Offline);

            RefreshLoadWin64();
            RefreshEnetVariant();
            RefreshHost();
            RefreshNfs(value);
        }

        // WinKFP / NFS: NFS.INI [INSTANZ] should use the same interface as EDIABAS
        void RefreshNfs(string ediabasInterface)
        {
            NfsStatus? nfs;
            try
            {
                nfs = NfsConfig.Read();
            }
            catch
            {
                nfs = null;
            }

            NfsPanel.Visibility = nfs != null ? Visibility.Visible : Visibility.Collapsed;
            if (nfs == null)
                return;

            bool matches = NfsConfig.MatchesEdiabas(nfs, ediabasInterface);
            NfsStatusText.Text = matches ? Loc.T("NfsMatches") : Loc.T("NfsDiffers");
            NfsStatusText.Foreground = (Brush)FindResource(matches ? "IstaBrush" : "ESeriesBrush");

            var channels = string.Join("  ", nfs.ChannelInterfaces.Select((c, i) => $"Interface_{i + 1}={c}"));
            NfsValueText.Text = $"Interface = {nfs.Interface}   MEHRKANAL = {nfs.MultiChannel ?? "–"}\n{channels}";
            NfsSyncButton.Visibility = matches ? Visibility.Collapsed : Visibility.Visible;
        }

        void NfsSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ediabasInterface = EdiabasConfig.ReadInterface();
                if (ediabasInterface != null)
                    NfsConfig.SyncWith(ediabasInterface);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrIniWrite", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshInterface();
        }

        bool _loadWin64Mismatch;

        // ICOM / ENET need the 64-bit EDIABAS (LoadWin64 = 1), K+DCAN the 32-bit one (0)
        void RefreshLoadWin64()
        {
            _loadWin64Mismatch = false;
            LoadWin64Warning.Visibility = Visibility.Collapsed;

            var current = EdiabasConfig.ReadLoadWin64();
            var required = EdiabasConfig.RequiredLoadWin64(_interfaceKind);
            InterfaceValue.Text += $"   LoadWin64 = {current ?? "–"}";
            if (required == null)
                return;

            var name = InterfaceName(_interfaceKind);
            if (current == null)
            {
                // Missing line = EDIABAS default 0; only a problem if 1 is needed
                if (required != "0")
                    ShowLoadWin64Warning(Loc.T("LoadWin64Missing", name, required));
            }
            else if (current != required)
            {
                _loadWin64Mismatch = true;
                ShowLoadWin64Warning(Loc.T("LoadWin64Mismatch", current, name, required));
            }
        }

        void ShowLoadWin64Warning(string text)
        {
            LoadWin64Warning.Text = "⚠  " + text;
            LoadWin64Warning.Visibility = Visibility.Visible;
        }

        static string InterfaceName(InterfaceKind kind) => kind switch
        {
            InterfaceKind.Icom => "ICOM",
            InterfaceKind.Enet => "ENET",
            InterfaceKind.KDcan => "K+DCAN",
            _ => kind.ToString()
        };

        void RefreshEnetVariant()
        {
            bool isEnet = _interfaceKind == InterfaceKind.Enet;
            EnetVariantRow.Visibility = isEnet ? Visibility.Visible : Visibility.Collapsed;
            EnetPortsText.Visibility = EnetVariantRow.Visibility;
            if (!isEnet)
                return;

            var variant = EdiabasConfig.ReadEnetVariant(out var control, out var diagnostic);
            SetToggle(EnetCableButton, variant == EnetVariant.Cable);
            SetToggle(EnetIcomButton, variant == EnetVariant.Icom);
            EnetPortsText.Text = $"ControlPort = {control ?? "–"}   DiagnosticPort = {diagnostic ?? "–"}"
                + (variant == EnetVariant.Custom ? "   (" + Loc.T("EnetCustomPorts") + ")" : "");
        }

        void EnetVariant_Click(object sender, RoutedEventArgs e)
        {
            var variant = Enum.Parse<EnetVariant>((string)((Button)sender).Tag);
            if (EdiabasConfig.ReadEnetVariant(out _, out _) == variant)
                return;

            try
            {
                EdiabasConfig.SetEnetVariant(variant);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrIniWrite", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshInterface();
        }

        // The address-like setting of the selected interface, always read fresh from its INI file:
        // ICOM IP (Rplus.ini), ENET RemoteHost (EDIABAS.INI) or K+DCAN COM port (obd.ini)
        void RefreshHost()
        {
            HostStatus.Visibility = Visibility.Collapsed;
            if (_interfaceKind is not (InterfaceKind.Icom or InterfaceKind.Enet or InterfaceKind.KDcan))
            {
                HostRow.Visibility = Visibility.Collapsed;
                return;
            }

            HostRow.Visibility = Visibility.Visible;
            var (labelKey, missingKey) = _interfaceKind switch
            {
                InterfaceKind.Icom => ("HostLabelIcom", "HostMissingIcom"),
                InterfaceKind.Enet => ("HostLabelEnet", "HostMissingEnet"),
                _ => ("HostLabelKDcan", "HostMissingKDcan"),
            };
            HostLabel.Text = Loc.T(labelKey);

            string? host = null;
            try
            {
                host = _interfaceKind switch
                {
                    InterfaceKind.Icom => EdiabasConfig.ReadIcomHost(),
                    InterfaceKind.Enet => EdiabasConfig.ReadEnetHost(),
                    _ => EdiabasConfig.ReadKDcanPort(),
                };
            }
            catch (Exception ex)
            {
                ShowHostStatus(ex.Message, "MixedBrush");
            }

            _loadedHost = host ?? "";
            HostBox.IsEnabled = host != null;
            HostBox.Text = _loadedHost;

            if (host == null && HostStatus.Visibility != Visibility.Visible)
                ShowHostStatus(Loc.T(missingKey), "MixedBrush");
            else if (_interfaceKind == InterfaceKind.Icom && host != null && (host == "0.0.0.0" || !EdiabasConfig.IsIPv4(host)))
                ShowHostStatus(Loc.T("HostNotSetIcom"), "ESeriesBrush");

            UpdateSaveButton();
        }
        void ShowHostStatus(string text, string brushKey)
        {
            HostStatus.Text = text;
            HostStatus.Foreground = (Brush)FindResource(brushKey);
            HostStatus.Visibility = Visibility.Visible;
        }

        void UpdateSaveButton() =>
            SaveHostButton.IsEnabled = HostBox.IsEnabled && HostBox.Text.Trim() != _loadedHost;

        void HostBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateSaveButton();

        void HostBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SaveHostButton.IsEnabled)
                SaveHost();
        }

        void SaveHost_Click(object sender, RoutedEventArgs e) => SaveHost();

        void SaveHost()
        {
            var text = HostBox.Text.Trim();

            string? value = _interfaceKind switch
            {
                InterfaceKind.Icom => EdiabasConfig.IsIPv4(text) ? text : null,
                InterfaceKind.Enet => text.Equals(EdiabasConfig.Autodetect, StringComparison.OrdinalIgnoreCase)
                    ? EdiabasConfig.Autodetect
                    : EdiabasConfig.IsIPv4(text) ? text : null,
                InterfaceKind.KDcan => EdiabasConfig.NormalizeComPort(text),
                _ => null
            };

            if (value == null)
            {
                var errorKey = _interfaceKind switch
                {
                    InterfaceKind.Icom => "ErrInvalidIcomIp",
                    InterfaceKind.Enet => "ErrInvalidEnetHost",
                    _ => "ErrInvalidComPort",
                };
                MessageBox.Show(this, Loc.T(errorKey, text), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                switch (_interfaceKind)
                {
                    case InterfaceKind.Icom: EdiabasConfig.SetIcomHost(value); break;
                    case InterfaceKind.Enet: EdiabasConfig.SetEnetHost(value); break;
                    case InterfaceKind.KDcan: EdiabasConfig.SetKDcanPort(value); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrIniWrite", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshInterface();
            ShowHostStatus(Loc.T("Saved"), "IstaBrush");
        }
        void Interface_Click(object sender, RoutedEventArgs e)
        {
            var kind = Enum.Parse<InterfaceKind>((string)((Button)sender).Tag);
            // Clicking the active interface again only makes sense to fix its LoadWin64 value
            if (kind == _interfaceKind && !_loadWin64Mismatch)
                return;

            try
            {
                EdiabasConfig.SetInterface(kind);
                // WinKFP / NFS follows the EDIABAS interface (no-op if NFS is not installed)
                NfsConfig.SyncWith(EdiabasConfig.InterfaceValue(kind));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrIniWrite", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshInterface();
        }

        void SetInterfaceButtonsEnabled(bool enabled)
        {
            foreach (var button in new[] { IcomButton, EnetButton, KDcanButton, OfflineButton })
                button.IsEnabled = enabled;
        }

        // Active option: blue accent button. Inactive: the theme's normal (rounded) button.
        // ClearValue instead of Style = null, otherwise WPF falls back to the old square system style.
        void SetToggle(Button button, bool active)
        {
            if (active)
                button.Style = (Style)FindResource("AccentButtonStyle");
            else
                button.ClearValue(StyleProperty);
        }

        void UpdateLanguageButtons()
        {
            bool hungarian = Loc.Instance.Language == AppLanguage.Hungarian;
            SetToggle(EnglishButton, !hungarian);
            SetToggle(HungarianButton, hungarian);
        }

        void English_Click(object sender, RoutedEventArgs e) => ChangeLanguage(AppLanguage.English);

        void Hungarian_Click(object sender, RoutedEventArgs e) => ChangeLanguage(AppLanguage.Hungarian);

        void ChangeLanguage(AppLanguage language)
        {
            Loc.Instance.SetLanguage(language);
            AppSettings.SaveLanguage(language);
            RefreshStatus();
        }

        async void ToESeries_Click(object sender, RoutedEventArgs e) => await SwitchTo(ToolMode.ESeries);

        async void ToIsta_Click(object sender, RoutedEventArgs e) => await SwitchTo(ToolMode.Ista);

        async Task SwitchTo(ToolMode target)
        {
            SetBusy(true);
            try
            {
                await Task.Run(() => EnvironmentSwitcher.SwitchTo(target));
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(this, Loc.T("ErrNoPermission"), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrSwitchFailed", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                SetBusy(false);
                RefreshStatus();
            }

            RebootBanner.Visibility = Visibility.Visible;

            var modeName = Loc.T(target == ToolMode.ESeries ? "SwitchedESeries" : "SwitchedIsta");
            var icomNote = target == ToolMode.ESeries && IsIcomInterface() ? Loc.T("IcomNote") : "";
            var answer = MessageBox.Show(this, Loc.T("SwitchSuccess", modeName, icomNote),
                Title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                Restart();
        }

        static bool IsIcomInterface()
        {
            try
            {
                return string.Equals(EdiabasConfig.ReadInterface(), EdiabasConfig.IcomInterface, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        void Reboot_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(this, Loc.T("ConfirmRestart"), Title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes)
                Restart();
        }

        void Restart()
        {
            try
            {
                EnvironmentSwitcher.RestartComputer();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("ErrRestart", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void Refresh_Click(object sender, RoutedEventArgs e) => RefreshStatus();

        void OpenBackups_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            Process.Start(new ProcessStartInfo("explorer.exe", EnvironmentSwitcher.BackupDir) { UseShellExecute = true });
        }

        void About_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(this, Loc.T("AboutText", AppInfo.VersionText, AppInfo.WebsiteUrl, AppInfo.RepositoryUrl),
                Title, MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
                OpenUrl(AppInfo.WebsiteUrl);
        }

        void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        void SetBusy(bool busy)
        {
            ToESeriesButton.IsEnabled = !busy;
            ToIstaButton.IsEnabled = !busy;
            Cursor = busy ? Cursors.Wait : null;
        }
    }
}
