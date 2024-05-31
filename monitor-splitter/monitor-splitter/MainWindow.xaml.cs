using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
        private Process[] retroarchProcesses;

        private int panelWidth;
        private int panelHeight;
        private const int borderSize = 1;

        public MainWindow()
        {
            InitializeComponent();
            ShowOptionsDialog();
            WindowState = WindowState.Maximized; // Open the window in fullscreen mode
            WindowStyle = WindowStyle.None; // Remove window border and title bar
            ContentRendered += MainWindow_ContentRendered; // Subscribe to the ContentRendered event
        }

        private void ShowOptionsDialog()
        {
            OptionsDialog optionsDialog = new OptionsDialog();
            if (optionsDialog.ShowDialog() == true)
            {
                // Store preferences
                SplitDirection = optionsDialog.SplitDirection;
                NumberOfPlayers = optionsDialog.NumberOfPlayers;
                ExePath = optionsDialog.ExePath;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                retroarchProcesses = new Process[NumberOfPlayers];
                for (int i = 0; i < NumberOfPlayers; i++)
                {
                    retroarchProcesses[i] = Process.Start(ExePath);
                }

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

                while (true)
                {
                    Thread.Sleep(500);
                    foreach (var process in retroarchProcesses)
                    {
                        if (process == null) continue;
                        var windows = GetProcessWindows(process.Id);
                        if (!windows.Any())
                        {
                            closedWindows++;
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
                                    positionWindows(process, window, dpiScale);
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

        private void positionWindows(Process process, nint window, DpiScale dpiScale)
        {
            SetParent(window, new WindowInteropHelper(this).Handle);

            int processIndex = Array.IndexOf(retroarchProcesses, process);

            switch (NumberOfPlayers)
            {
                case 2:
                    twoPlayers(process, window, dpiScale, processIndex);
                    break;
                case 3:
                    threePlayers(process, window, dpiScale, processIndex);
                    break;
                case 4:
                    fourPlayers(process, window, dpiScale, processIndex);
                    break;
            }

            long windowStyles = GetWindowLong(window, GWL_STYLE);
            SetWindowLong(window, GWL_STYLE, windowStyles & ~WS_CAPTION & ~WS_THICKFRAME);
        }

        private void twoPlayers(Process process, nint window, DpiScale dpiScale, int processIndex)
        {
            if (SplitDirection == "Vertical")
            {
                // Vertical split logic
                panelWidth = (int)((TopLeftPanel.ActualWidth + TopRightPanel.ActualWidth) * dpiScale.DpiScaleX / 2);
                panelHeight = (int)((TopLeftPanel.ActualHeight + TopRightPanel.ActualHeight) * dpiScale.DpiScaleY);

                SetWindowPos(window, IntPtr.Zero, processIndex * (panelWidth + borderSize), 0, panelWidth - (borderSize / 2), panelHeight, SWP_NOZORDER);
            }
            else
            {
                // Horizontal split logic
                panelWidth = (int)((TopLeftPanel.ActualWidth + TopRightPanel.ActualWidth) * dpiScale.DpiScaleX);
                panelHeight = (int)((TopLeftPanel.ActualHeight + TopRightPanel.ActualHeight) * dpiScale.DpiScaleY / 2);

                SetWindowPos(window, IntPtr.Zero, 0, processIndex * (panelHeight + borderSize), panelWidth, panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
        }

        private void threePlayers(Process process, nint window, DpiScale dpiScale, int processIndex)
        {
            panelWidth = (int)(TopLeftPanel.ActualWidth * dpiScale.DpiScaleX);
            panelHeight = (int)(TopLeftPanel.ActualHeight * dpiScale.DpiScaleY);

            if (processIndex < 2)
            {
                SetWindowPos(window, IntPtr.Zero, processIndex * (panelWidth + borderSize), 0, panelWidth - (borderSize / 2), panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
            else
            {
                SetWindowPos(window, IntPtr.Zero, 0, panelHeight + borderSize, panelWidth + (int)TopRightPanel.ActualWidth, panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
        }

        private void fourPlayers(Process process, nint window, DpiScale dpiScale, int processIndex)
        {
            panelWidth = (int)(TopLeftPanel.ActualWidth * dpiScale.DpiScaleX);
            panelHeight = (int)(TopLeftPanel.ActualHeight * dpiScale.DpiScaleY);

            if (processIndex < 2)
            {
                SetWindowPos(window, IntPtr.Zero, processIndex * (panelWidth + borderSize), 0, panelWidth - (borderSize / 2), panelHeight - (borderSize / 2), SWP_NOZORDER);
            } else
            {
                SetWindowPos(window, IntPtr.Zero, (processIndex - 2) * (panelWidth + borderSize), panelHeight + borderSize, panelWidth - (borderSize / 2), panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
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

        private string SplitDirection { get; set; }
        private int NumberOfPlayers { get; set; }
        private string ExePath { get; set; }
    }
}
