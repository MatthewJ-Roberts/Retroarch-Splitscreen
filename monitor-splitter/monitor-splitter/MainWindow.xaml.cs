using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

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

        private (int widthInPixels, int heightInPixels) GetPhysicalDimensions(UIElement element, double actualWidth, double actualHeight)
        {
            var dpiScale = VisualTreeHelper.GetDpi(element);
            double dpiX = dpiScale.DpiScaleX;
            double dpiY = dpiScale.DpiScaleY;

            int widthInPixels = (int)(actualWidth * dpiX);
            int heightInPixels = (int)(actualHeight * dpiY);

            return (widthInPixels, heightInPixels);
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                // Force layout update
                LeftPanel.UpdateLayout();
                RightPanel.UpdateLayout();

                var leftPanelDimensions = GetPhysicalDimensions(LeftPanel, LeftPanel.ActualWidth, LeftPanel.ActualHeight);
                var rightPanelDimensions = GetPhysicalDimensions(RightPanel, RightPanel.ActualWidth, RightPanel.ActualHeight);

                Debug.WriteLine($"Left Panel - Width: {leftPanelDimensions.widthInPixels}, Height: {leftPanelDimensions.heightInPixels}");
                Debug.WriteLine($"Right Panel - Width: {rightPanelDimensions.widthInPixels}, Height: {rightPanelDimensions.heightInPixels}");

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
                            if (closedWindows == 3)
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
                                    SetParent(window, new WindowInteropHelper(this).Handle);

                                    var screen = SystemParameters.WorkArea;
                                    var dpiScale = VisualTreeHelper.GetDpi(this);

                                    int leftPanelWidth = (int)(LeftPanel.ActualWidth * dpiScale.DpiScaleX);
                                    int leftPanelHeight = (int)(LeftPanel.ActualHeight * dpiScale.DpiScaleY);
                                    int rightPanelWidth = (int)(RightPanel.ActualWidth * dpiScale.DpiScaleX);
                                    int rightPanelHeight = (int)(RightPanel.ActualHeight * dpiScale.DpiScaleY);

                                    Debug.WriteLine($"Screen Width: {screen.Width}, Screen Height: {screen.Height}");
                                    Debug.WriteLine($"Left Panel Width: {leftPanelWidth}, Left Panel Height: {leftPanelHeight}");
                                    Debug.WriteLine($"Right Panel Width: {rightPanelWidth}, Right Panel Height: {rightPanelHeight}");

                                    if (process == retroarchProcessLeft)
                                    {
                                        SetWindowPos(window, IntPtr.Zero, 0, 0, leftPanelWidth - 1, leftPanelHeight, SWP_NOZORDER);
                                    }
                                    else if (process == retroarchProcessRight)
                                    {
                                        SetWindowPos(window, IntPtr.Zero, leftPanelWidth + 1, 0, rightPanelWidth - 1, rightPanelHeight, SWP_NOZORDER);
                                    }

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
