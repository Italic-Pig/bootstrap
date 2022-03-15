using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Serialization;

namespace ItalicPig.Bootstrap
{
    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    // POINT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // WINDOWPLACEMENT stores the position, size, and state of a window
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
    }

    public static class WindowPlacement
    {
        public static void SetPlacement(this Window window, string placementXml) => SetPlacementInternal(new WindowInteropHelper(window).Handle, placementXml);

        public static string GetPlacement(this Window window) => GetPlacementInternal(new WindowInteropHelper(window).Handle);

        #region Private
        [DllImport("user32.dll")]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        private static void SetPlacementInternal(IntPtr windowHandle, string placementXml)
        {
            if (string.IsNullOrEmpty(placementXml))
            {
                return;
            }

            WINDOWPLACEMENT Placement;
            var XmlBytes = _Encoding.GetBytes(placementXml);

            try
            {
                using (var MemoryStream = new MemoryStream(XmlBytes))
                {
                    var Deserialized = _Serializer.Deserialize(MemoryStream);
                    Placement = (Deserialized is null) ? new WINDOWPLACEMENT() : (WINDOWPLACEMENT)Deserialized;
                }

                Placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                Placement.flags = 0;
                Placement.showCmd = (Placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : Placement.showCmd);
                SetWindowPlacement(windowHandle, ref Placement);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
            }
        }

        private static string GetPlacementInternal(IntPtr windowHandle)
        {
            GetWindowPlacement(windowHandle, out var Placement);

            using var MemoryStream = new MemoryStream();
            using var XmlTextWriter = new XmlTextWriter(MemoryStream, Encoding.UTF8);
            _Serializer.Serialize(XmlTextWriter, Placement);
            var XmlBytes = MemoryStream.ToArray();
            return _Encoding.GetString(XmlBytes);
        }

        private static readonly Encoding _Encoding = new UTF8Encoding();
        private static readonly XmlSerializer _Serializer = new(typeof(WINDOWPLACEMENT));
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matches Win32 API")]
        private const int SW_SHOWNORMAL = 1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matches Win32 API")]
        private const int SW_SHOWMINIMIZED = 2;
        #endregion
    }
}
