using System.Runtime.InteropServices;

namespace ESeriesSwitch.Services
{
    /// <summary>
    /// Reads and sets the position of icons on the desktop through the shell's own folder view
    /// (ShellWindows → desktop → IFolderView), so restored shortcuts land exactly where they were.
    /// Everything is best effort: if the desktop view is not available, Windows simply places the icons itself.
    /// Must be called on an STA thread (the UI thread).
    /// </summary>
    static class DesktopIconPositions
    {
        /// <summary>File name (e.g. "INPA.lnk") → icon position, or null if the desktop view is not available.</summary>
        public static Dictionary<string, (int X, int Y)>? TryGetPositions()
        {
            try
            {
                var view = GetDesktopFolderView();
                if (view == null)
                    return null;

                var folder = GetFolder(view);
                var positions = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
                view.ItemCount(SVGIO_ALLVIEW, out int count);
                for (int i = 0; i < count; i++)
                {
                    view.Item(i, out var pidl);
                    try
                    {
                        var name = GetName(folder, pidl);
                        view.GetItemPosition(pidl, out var point);
                        if (name != null)
                            positions[name] = (point.X, point.Y);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pidl);
                    }
                }
                return positions;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Moves the given icons to their positions. Restored files show up on the desktop with a short delay,
        /// so it waits (up to <paramref name="timeout"/>) until each one is in the view.
        /// </summary>
        public static void TrySetPositions(IReadOnlyDictionary<string, (int X, int Y)> wanted, TimeSpan timeout)
        {
            if (wanted.Count == 0)
                return;
            try
            {
                var view = GetDesktopFolderView();
                if (view == null)
                    return;
                var folder = GetFolder(view);

                var pending = new Dictionary<string, (int X, int Y)>(wanted, StringComparer.OrdinalIgnoreCase);
                var deadline = DateTime.UtcNow + timeout;
                while (pending.Count > 0)
                {
                    view.ItemCount(SVGIO_ALLVIEW, out int count);
                    for (int i = 0; i < count && pending.Count > 0; i++)
                    {
                        view.Item(i, out var pidl);
                        try
                        {
                            var name = GetName(folder, pidl);
                            if (name != null && pending.Remove(name, out var position))
                            {
                                view.SelectAndPositionItems(1, [pidl], [new POINT { X = position.X, Y = position.Y }], SVSI_POSITIONITEM);
                            }
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pidl);
                        }
                    }

                    if (pending.Count == 0 || DateTime.UtcNow > deadline)
                        break;
                    Thread.Sleep(150);
                }
            }
            catch
            {
                // Best effort: the icons are on the desktop anyway, only maybe not at their old place
            }
        }

        static IFolderView? GetDesktopFolderView()
        {
            var shellWindowsType = Type.GetTypeFromCLSID(CLSID_ShellWindows);
            if (shellWindowsType == null)
                return null;
            var shellWindows = (IShellWindows)Activator.CreateInstance(shellWindowsType)!;

            object location = CSIDL_DESKTOP;
            object? root = null; // VT_EMPTY
            var dispatch = shellWindows.FindWindowSW(ref location, ref root!, SWC_DESKTOP, out _, SWFO_NEEDDISPATCH);
            if (dispatch is not IServiceProvider services)
                return null;

            var sid = SID_STopLevelBrowser;
            var iid = typeof(IShellBrowser).GUID;
            services.QueryService(ref sid, ref iid, out var browserObject);
            var browser = (IShellBrowser)browserObject;
            browser.QueryActiveShellView(out var shellView);
            return shellView as IFolderView;
        }

        static IShellFolder GetFolder(IFolderView view)
        {
            var iid = typeof(IShellFolder).GUID;
            view.GetFolder(ref iid, out var folder);
            return (IShellFolder)folder;
        }

        // Parsing name inside the folder = the file name with extension (e.g. "INPA.lnk")
        static string? GetName(IShellFolder folder, IntPtr pidl)
        {
            folder.GetDisplayNameOf(pidl, SHGDN_INFOLDER | SHGDN_FORPARSING, out var strret);
            return StrRetToBSTR(ref strret, pidl, out var name) == 0 ? name : null;
        }

        const int CSIDL_DESKTOP = 0;
        const int SWC_DESKTOP = 8;
        const int SWFO_NEEDDISPATCH = 1;
        const uint SVGIO_ALLVIEW = 2;
        const uint SVSI_POSITIONITEM = 0x80;
        const uint SHGDN_INFOLDER = 0x1;
        const uint SHGDN_FORPARSING = 0x8000;

        static readonly Guid CLSID_ShellWindows = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
        static readonly Guid SID_STopLevelBrowser = new("4C96BE40-915C-11CF-99D3-00AA004AE837");

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        // uType + union { LPWSTR; UINT; char[260] }: 272 bytes on x64 (pointer-aligned union), 264 on x86
        [StructLayout(LayoutKind.Explicit, Size = 272)]
        struct STRRET { [FieldOffset(0)] public uint uType; }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int StrRetToBSTR(ref STRRET pstr, IntPtr pidl, [MarshalAs(UnmanagedType.BStr)] out string pbstr);

        [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        interface IShellWindows
        {
            int Count { get; }
            [return: MarshalAs(UnmanagedType.IDispatch)] object Item(object index);
            [return: MarshalAs(UnmanagedType.IUnknown)] object _NewEnum();
            int Register([MarshalAs(UnmanagedType.IDispatch)] object pid, int hwnd, int swClass);
            int RegisterPending(int threadId, ref object loc, ref object locRoot, int swClass);
            void Revoke(int cookie);
            void OnNavigate(int cookie, ref object loc);
            void OnActivated(int cookie, [MarshalAs(UnmanagedType.VariantBool)] bool active);
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW(ref object loc, ref object locRoot, int swClass, out int hwnd, int swfwOptions);
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IServiceProvider
        {
            void QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
        }

        [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellBrowser
        {
            void GetWindow(out IntPtr hwnd);
            void ContextSensitiveHelp(bool enterMode);
            void InsertMenusSB(IntPtr hmenuShared, IntPtr lpMenuWidths);
            void SetMenuSB(IntPtr hmenuShared, IntPtr holemenuRes, IntPtr hwndActiveObject);
            void RemoveMenusSB(IntPtr hmenuShared);
            void SetStatusTextSB(IntPtr text);
            void EnableModelessSB(bool enable);
            void TranslateAcceleratorSB(IntPtr msg, ushort id);
            void BrowseObject(IntPtr pidl, uint flags);
            void GetViewStateStream(uint mode, out IntPtr stream);
            void GetControlWindow(uint id, out IntPtr hwnd);
            void SendControlMsg(uint id, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr result);
            void QueryActiveShellView([MarshalAs(UnmanagedType.IUnknown)] out object shellView);
        }

        [ComImport, Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IFolderView
        {
            void GetCurrentViewMode(out uint mode);
            void SetCurrentViewMode(uint mode);
            void GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object folder);
            void Item(int index, out IntPtr pidl);
            void ItemCount(uint flags, out int count);
            void Items(uint flags, ref Guid riid, out IntPtr items);
            void GetSelectionMarkedItem(out int item);
            void GetFocusedItem(out int item);
            void GetItemPosition(IntPtr pidl, out POINT point);
            void GetSpacing(ref POINT point);
            void GetDefaultSpacing(out POINT point);
            [PreserveSig] int GetAutoArrange();
            void SelectItem(int item, uint flags);
            void SelectAndPositionItems(uint count,
                [MarshalAs(UnmanagedType.LPArray)] IntPtr[] pidls,
                [MarshalAs(UnmanagedType.LPArray)] POINT[] points, uint flags);
        }

        [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellFolder
        {
            void ParseDisplayName(IntPtr hwnd, IntPtr bindCtx, [MarshalAs(UnmanagedType.LPWStr)] string name, out uint eaten, out IntPtr pidl, ref uint attributes);
            void EnumObjects(IntPtr hwnd, uint flags, out IntPtr enumIdList);
            void BindToObject(IntPtr pidl, IntPtr bindCtx, ref Guid riid, out IntPtr ppv);
            void BindToStorage(IntPtr pidl, IntPtr bindCtx, ref Guid riid, out IntPtr ppv);
            [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);
            void CreateViewObject(IntPtr hwndOwner, ref Guid riid, out IntPtr ppv);
            void GetAttributesOf(uint count, IntPtr pidls, ref uint attributes);
            void GetUIObjectOf(IntPtr hwndOwner, uint count, IntPtr pidls, ref Guid riid, IntPtr reserved, out IntPtr ppv);
            void GetDisplayNameOf(IntPtr pidl, uint flags, out STRRET name);
            void SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string name, uint flags, out IntPtr pidlOut);
        }
    }
}
