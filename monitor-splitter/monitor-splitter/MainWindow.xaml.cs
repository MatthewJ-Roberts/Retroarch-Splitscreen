using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace monitor_splitter
{
    public partial class MainWindow : Window
    {
        private Process retroarchProcessLeft;
        private Process retroarchProcessRight;

        private double panelWidth;
        private double panelHeight;

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
                // Get the dimensions of the left and right panels
                panelWidth = LeftPanel.ActualWidth;
                panelHeight = LeftPanel.ActualHeight;
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
                            if (closedWindows == 2)
                            {
                                Dispatcher.Invoke(() => Environment.Exit(0));  // Exit with code 0
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
                                    // Make the new game window a child of the main window
                                    SetParent(window, new WindowInteropHelper(this).Handle);

                                    // Resize and position the window to fit its side
                                    if (process == retroarchProcessLeft)
                                    {
                                        SetWindowPos(window, IntPtr.Zero, 0, 0, (int)panelWidth - 1, (int)panelHeight, SWP_NOZORDER);
                                    }
                                    else if (process == retroarchProcessRight)
                                    {
                                        SetWindowPos(window, IntPtr.Zero, (int)(SystemParameters.PrimaryScreenWidth - panelWidth + 1), 0, (int)panelWidth - 1, (int)panelHeight, SWP_NOZORDER);
                                    }

                                    // Modify the window styles to remove borders
                                    long windowStyles = GetWindowLong(window, GWL_STYLE);
                                    SetWindowLong(window, GWL_STYLE, windowStyles & ~WS_CAPTION & ~WS_THICKFRAME);
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it accordingly
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
