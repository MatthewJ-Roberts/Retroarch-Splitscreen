using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace monitor_splitter
{
    public partial class OptionsDialog : Window
    {
        public string SplitDirection { get; private set; }
        public int NumberOfPlayers { get; private set; }
        public string ExePath { get; private set; }
        public string ConfigPath { get; private set; }
        public string ScaleFactor { get; private set; }

        public OptionsDialog()
        {
            InitializeComponent();
            LoadPreferences();
        }

        private void LoadPreferences()
        {
            // Load previously saved preferences
            SplitDirectionComboBox.SelectedIndex = Settings.Default.SplitDirection == "Vertical" ? 1 : 0;
            NumberOfPlayersComboBox.SelectedIndex = Settings.Default.NumberOfPlayers - 2;
            ExePathTextBox.Text = Settings.Default.ExePath;
            ConfigPathTextBox.Text = Settings.Default.ConfigPath;
            ScaleFactorTextBox.Text = Settings.Default.ScaleFactor.ToString();

            // Set initial state of SplitDirectionComboBox
            SplitDirectionComboBox.IsEnabled = Settings.Default.NumberOfPlayers == 2;
        }

        private void BrowseButton_Click_Exe(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                ExePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void BrowseButton_Click_Config(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Configuration files (*.cfg)|*.cfg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                ConfigPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            NumberOfPlayers = NumberOfPlayersComboBox.SelectedIndex + 2;
            if (NumberOfPlayers == 2)
            {
                SplitDirection = ((ComboBoxItem)SplitDirectionComboBox.SelectedItem).Content.ToString();
            } else
            {
                SplitDirection = "N/A";
            }
            
            ExePath = ExePathTextBox.Text;
            ConfigPath = ConfigPathTextBox.Text;

            // Save preferences
            Settings.Default.SplitDirection = SplitDirection;
            Settings.Default.NumberOfPlayers = NumberOfPlayers;
            Settings.Default.ExePath = ExePath;
            Settings.Default.ConfigPath = ConfigPath;
            ScaleFactorTextBox.Text = Settings.Default.ScaleFactor.ToString();
            Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void NumberOfPlayersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NumberOfPlayersComboBox.SelectedIndex >= 1) // 3 or 4 players
            {
                SplitDirectionComboBox.IsEnabled = false;
                if (!SplitDirectionComboBox.Items.Contains("N/A"))
                {
                    SplitDirectionComboBox.Items.Add("N/A");
                    SplitDirectionComboBox.SelectedItem = "N/A";
                }
            }
            else
            {
                SplitDirectionComboBox.IsEnabled = true;
                SplitDirectionComboBox.Items.Remove("N/A");
                SplitDirectionComboBox.SelectedIndex = Settings.Default.SplitDirection == "Vertical" ? 1 : 0;
            }
        }
    }
}
