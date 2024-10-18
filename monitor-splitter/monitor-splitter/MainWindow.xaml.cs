using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private string SplitDirection { get; set; }
        private int NumberOfPlayers { get; set; }
        private string ExePath { get; set; }
        private string ConfigPath { get; set; }
        private double ScaleFactor { get; set; }
        private double OriginalScaleFactor;
        private int OriginalMaxUsers;

        public MainWindow()
        {
            InitializeComponent();
            ShowOptionsDialog();
            SetScaleFactor(ScaleFactor, true);
            limitMaxPlayers(true);
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
                ConfigPath = optionsDialog.ConfigPath;
                ScaleFactor = optionsDialog.ScaleFactor;
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        // Setting the menu scale factor to what the user specifies for all intances
        // Default is 0.5 (works well for most screens)
        public void SetScaleFactor(double updatedScaleFactor, bool launching)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string[] configLines = File.ReadAllLines(ConfigPath);

                    for (int i = 0; i < configLines.Length; i++)
                    {
                        if (configLines[i].StartsWith("menu_scale_factor"))
                        {
                            // Store the original scale factor value
                            if (launching) OriginalScaleFactor = double.Parse(configLines[i].Split('=')[1].Trim().Trim('"'));

                            configLines[i] = $"menu_scale_factor = \"{updatedScaleFactor}\"";
                            // Save the modified configuration back to the file
                            File.WriteAllLines(ConfigPath, configLines);
                            Console.WriteLine("Scale factor set successfully.");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("retroarch.cfg file not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void setJoypadIndex(int currPlayerNum)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string[] configLines = File.ReadAllLines(ConfigPath);

                    for (int i = 0; i < configLines.Length; i++)
                    {
                        // Only checking for player1, since configs are set to only allow 1 player per instance
                        if (configLines[i].StartsWith("input_player1_joypad_index"))
                        {
                            configLines[i] = $"input_player1_joypad_index = \"{currPlayerNum}\"";
                            
                            // Save the modified configuration back to the file
                            File.WriteAllLines(ConfigPath, configLines);
                            Console.WriteLine($"Joypad Index for player \"{ currPlayerNum }\" set successfully.");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("retroarch.cfg file not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void limitMaxPlayers(bool launching)
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string[] configLines = File.ReadAllLines(ConfigPath);

                    for (int i = 0; i < configLines.Length; i++)
                    {
                        if (configLines[i].StartsWith("input_max_users"))
                        {
                            // Store the original max users value
                            if (launching)
                            {
                                OriginalMaxUsers = int.Parse(configLines[i].Split('=')[1].Trim().Trim('"'));
                                configLines[i] = $"input_max_users = \"1\"";
                            }
                            else
                            {
                                configLines[i] = $"input_max_users = \"{OriginalMaxUsers}\"";
                            }
                            
                            // Save the modified configuration back to the file
                            File.WriteAllLines(ConfigPath, configLines);
                            Console.WriteLine("Max Users set successfully.");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("retroarch.cfg file not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                retroarchProcesses = new Process[NumberOfPlayers];
                for (int i = 0; i < NumberOfPlayers; i++)
                {
                    setJoypadIndex(i);
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
                                SetScaleFactor(OriginalScaleFactor, false);
                                limitMaxPlayers(false);
                                setJoypadIndex(0);
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
                    twoPlayers(window, dpiScale, processIndex);
                    break;
                case 3:
                    threePlayers(window, dpiScale, processIndex);
                    break;
                case 4:
                    fourPlayers(window, dpiScale, processIndex);
                    break;
            }

            long windowStyles = GetWindowLong(window, GWL_STYLE);
            SetWindowLong(window, GWL_STYLE, windowStyles & ~WS_CAPTION & ~WS_THICKFRAME);
        }

        private void twoPlayers(nint window, DpiScale dpiScale, int processIndex)
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

        private void threePlayers(nint window, DpiScale dpiScale, int processIndex)
        {
            panelWidth = (int)(TopLeftPanel.ActualWidth * dpiScale.DpiScaleX);
            panelHeight = (int)(TopLeftPanel.ActualHeight * dpiScale.DpiScaleY);

            if (processIndex < 2)
            {
                SetWindowPos(window, IntPtr.Zero, processIndex * (panelWidth + borderSize), 0, panelWidth - (borderSize / 2), panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
            else
            {
                SetWindowPos(window, IntPtr.Zero, 0, panelHeight + borderSize, panelWidth + (int)(TopRightPanel.ActualWidth * dpiScale.DpiScaleX), panelHeight - (borderSize / 2), SWP_NOZORDER);
            }
        }

        private void fourPlayers(nint window, DpiScale dpiScale, int processIndex)
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

    }
}
