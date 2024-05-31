using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace monitor_splitter
{
    public partial class MainWindow : Window
    {
        private Process retroarchProcessLeft;
        private Process retroarchProcessRight;

        private int panelWidth;
        private int panelHeight;
        private const int borderSize = 1;  // Adjust this value to make the middle border more prominent

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized; // Open the window in fullscreen mode
            WindowStyle = WindowStyle.None; // Remove window border and title bar
            retroarchProcessLeft = new Process();
            retroarchProcessRight = new Process();
            ContentRendered += MainWindow_ContentRendered; // Subscribe to the ContentRendered event
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                retroarchProcessLeft = Process.Start("C:\\RetroArch-Win64\\retroarch.exe");
                retroarchProcessRight = Process.Start("C:\\RetroArch-Win64\\retroarch.exe");

                Task.Run(() => ListenForWindows());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListenForWindows()
        {
            try
            {
                int closedWindows = 0;
                var dpiScale = VisualTreeHelper.GetDpi(this);

                // Panels are symmetrical
                panelWidth = (int)(LeftPanel.ActualWidth * dpiScale.DpiScaleX);
                panelHeight = (int)((LeftPanel.ActualHeight * dpiScale.DpiScaleY) - borderSize / 2);

                while (true)
                {
                    Thread.Sleep(500);
                    foreach (Process process in new[] { retroarchProcessLeft, retroarchProcessRight })
                    {
                        if (process == null) continue;
                        var windows = GetProcessWindows(process.Id);
                        if (!windows.Any())
                        {
                            closedWindows++;
                            // Higher number = more time for slower systems
                            if (closedWindows == 5)
                            {
                                Dispatcher.Invoke(() => Environment.Exit(0));
                            }
                        }
                        else
                        {
                            closedWindows = 0;
                        }
                        foreach (var window in windows)
                        {
                            if (IsWindowVisible(window) && GetParent(window) == IntPtr.Zero)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    positionWindows(process, window);
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private static List<IntPtr> GetProcessWindows(int processId)
        {
            var windowHandles = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                if (windowProcessId == processId)
                {
                    windowHandles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return windowHandles;
        }

        private void positionWindows(Process process, nint window)
        {
            SetParent(window, new WindowInteropHelper(this).Handle);

            if (process == retroarchProcessLeft)
            {
                SetWindowPos(window, IntPtr.Zero, 0, 0, panelWidth - (borderSize / 2), panelHeight, SWP_NOZORDER);
            }
            else if (process == retroarchProcessRight)
            {
                SetWindowPos(window, IntPtr.Zero, panelWidth + borderSize, 0, panelWidth - (borderSize / 2), panelHeight, SWP_NOZORDER);
            }

            long windowStyles = GetWindowLong(window, GWL_STYLE);
            SetWindowLong(window, GWL_STYLE, windowStyles & ~WS_CAPTION & ~WS_THICKFRAME);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const uint SWP_NOZORDER = 0x0004;
    }
}
