using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO.MemoryMappedFiles;
using System.Windows.Input;

namespace GithubTestDirDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Updater? updater;
        private bool _updateAvailable = false;
        private bool _updateFailed = false;
        private string _basePath;
        private string _updatePath;
        public MainWindow()
        {
            InitializeComponent();

            _basePath = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "CS2 AutoAccept");
            _updatePath = Path.Combine(_basePath, "UPDATE");

            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                // Application was updated
                if (arg.ToLower().Equals("--updated"))
                {
                    // Try to delete the update folder
                    try
                    {
                        Debug.WriteLine("Updated!");
                        Directory.Delete(_updatePath, true);
                    }
                    catch (Exception)
                    { }
                }
            }

            _ = UpdateHeaderVersion();
            updater = new Updater();
            updater.DownloadProgress += Updater_ProgressUpdated!;

            Thread UpdateThread = new Thread(CheckForUpdate);
            UpdateThread.Start();
            UpdateThread.IsBackground = true;

            if (Directory.Exists(_updatePath))
            {
                try
                {
                    File.Copy(Path.Combine(_basePath, "settings.cs2_auto"), Path.Combine(_updatePath, "settings.cs2_auto"));
                }
                catch (Exception)
                { }
                try
                {
                    File.Copy(Path.Combine(_basePath, "hotkeys.cs2_auto"), Path.Combine(_updatePath, "hotkeys.cs2_auto"));
                }
                catch (Exception)
                { }
                try
                {
                    File.Copy(Path.Combine(_basePath, "sha_cache.cs2_auto"), Path.Combine(_updatePath, "sha_cache.cs2_auto"));
                }
                catch (Exception)
                { }

                string runPath = AppContext.BaseDirectory;

                if (runPath.LastIndexOf('\\') == runPath.Length - 1)
                {
                    runPath = runPath[..^1]; // Remove the last character
                }

                // If true, the exe was run from inside the UPDATE folder
                if (runPath == _updatePath)
                {
                    string[] updatedFiles = Directory.GetFiles(_updatePath, "*", SearchOption.TopDirectoryOnly);
                    string[] updatedDirectories = Directory.GetDirectories(_updatePath, "*", SearchOption.TopDirectoryOnly);

                    // Delete all the old files and folders
                    DeleteAllExceptFolder(_basePath, "UPDATE");

                    // Move all the new files and folders, to the basePath
                    foreach (string filePath in updatedFiles)
                    {
                        string fileName = filePath.Substring(_updatePath.Length + 1);
                        string destinationPath = Path.Combine(_basePath, fileName);

                        try
                        {
                            File.Move(filePath, destinationPath, true);
                            //Debug.WriteLine($"Copied: {fileName}");
                        }
                        catch (Exception)
                        {
                            //Debug.WriteLine($"Error copying {fileName}: {ex.Message}");
                        }
                    }

                    foreach (string directoryPath in updatedDirectories)
                    {
                        string directoryName = directoryPath.Substring(_updatePath.Length + 1);
                        string destinationPath = Path.Combine(_basePath, directoryName);

                        try
                        {
                            Directory.Move(directoryPath, destinationPath);
                            //Debug.WriteLine($"Moved: {directoryName}");
                        }
                        catch (Exception)
                        {
                            //Debug.WriteLine($"Error copying {directoryName}: {ex.Message}");
                        }
                    }

                    // Start the updated program, in the new default path
                    Process.Start(Path.Combine(_basePath, "CS2-AutoAccept"), "--updated");
                    Environment.Exit(0);
                }
                else
                {
                    // Try to delete the update folder, if not run from that path
                    try
                    {
                        Directory.Delete(_updatePath, true);
                    }
                    catch (Exception)
                    {
                        //Debug.WriteLine($"Failed to delete the update directoryPath: {ex.Message}");
                    }
                }
            }
        }

        #region EventHandlers
        private void SetToggleOnOffHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
        }
        private void ClearToggleOnOffHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
        }
        // Handle the event when another instance tries to start
        
        private void OnTrayIconDoubleClick(object sender, RoutedEventArgs e)
        {
            // Show the window and restore it to normal state
            Show();
            WindowState = WindowState.Normal;
        }
        /// <summary>
        /// Event handler for download progress
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="progress"></param>
        private void Updater_ProgressUpdated(object sender, ProgressEventArgs e)
        {
            if (!_updateFailed)
            {
                // Update the UI with the progress value
                Dispatcher.Invoke(() =>
                {
                    if (e.Status != "" && e.Status != null)
                    {
                        Progress_Download.Visibility = Visibility.Collapsed;
                        TextBlock_Progress.Visibility = Visibility.Collapsed;

                        _ = UpdateHeaderVersion();
                        Button_Update.IsEnabled = true;
                        Program_state.Visibility = Visibility.Visible;
                        Program_state_continuously.Visibility = Visibility.Visible;
                        Run_at_startup_state.Visibility = Visibility.Visible;
                        Button_LaunchCS.Visibility = Visibility.Visible;

                        System.Windows.MessageBox.Show($"Update Failed, please try again later, or download it directly from the Github page!\n\nError Message: {e.Status}", "CS2 AutoAccept", MessageBoxButton.OK, MessageBoxImage.Error);
                        _updateFailed = true;
                    }
                    else if (e.Progress < 100)
                    {
                        // Update your UI elements with the progress value, e.g., a ProgressBar
                        Progress_Download.Visibility = Visibility.Visible;
                        TextBlock_Progress.Visibility = Visibility.Visible;
                        Progress_Download.Value = e.Progress;
                        TextBlock_Progress.Text = $"{e.Progress}%";
                    }
                });
            }
        }
        /// <summary>
        /// Minimize button
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Button_Click_Minimize(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Button_Click_Close}");
            WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// Maximize the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Button_Click_Maximize(object sender, RoutedEventArgs e)
        {
            if (WindowState.Equals(WindowState.Maximized))
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }
        /// <summary>
        /// Close button
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Button_Click_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// Open Github to download the newest version
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private async void Button_Update_Click(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Button_Update_Click}");
            if (_updateAvailable)
            {
                updater!.DownloadUpdate(_basePath, _updatePath);

                _updateFailed = false;
                Button_Update.IsEnabled = false;
                Button_Update.Content = "Updating...";
                Program_state.IsChecked = false;
                Program_state.Visibility = Visibility.Collapsed;
                CurrentHotkeyText.Visibility = Visibility.Collapsed;
                SetHotkeyButton.Visibility = Visibility.Collapsed;
                ClearHotkeyButton.Visibility = Visibility.Collapsed;
                Program_state_continuously.Visibility = Visibility.Collapsed;
                Run_at_startup_state.Visibility = Visibility.Collapsed;
                Button_LaunchCS.Visibility = Visibility.Collapsed;
            }
            else
            {
                _updateAvailable = await UpdateHeaderVersion();
            }
        }
        /// <summary>
        /// Open Discord
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Button_Click_Discord(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Button_Click_Discord}");
        }
        /// <summary>
        /// Launch CS2
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Button_Click_LaunchCS2(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Button_Click_LaunchCS}");
            Button_LaunchCS.Content = "Launching CS2";
        }
        /// <summary>
        /// Drag header
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void WindowHeader_Mousedown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        /// <summary>
        /// State ON event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Program_state_Checked(object sender, RoutedEventArgs e)
        {
            if (Program_state.IsEnabled)
            {
                // PrintToLog("{Program_state_Checked}");
                // Change to a brighter color
                Program_state.Foreground = new SolidColorBrush(Colors.LawnGreen);
                Program_state.Content = "AutoAccept (ON)";
            }
        }
        /// <summary>
        /// State OFF event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Program_state_Unchecked(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Program_state_Unchecked}");
            Program_state_continuously.IsChecked = false;

            // Change to a darker color
            Program_state.Foreground = new SolidColorBrush(Colors.Red);
            Program_state.Content = "AutoAccept (OFF)";
        }
        /// <summary>
        /// 24/7 State ON event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Program_state_continuously_Checked(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Program_state_continuously_Checked}");
            Program_state.IsChecked = true;

            // Change to a brighter color
            Program_state_continuously.Foreground = new SolidColorBrush(Colors.LawnGreen);
            Program_state_continuously.Content = "Auto Accept Every Match (ON)";
        }
        /// <summary>
        /// 24/7 State OFF event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Program_state_continuously_Unchecked(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Program_state_continuously_Unchecked}");

            // Change to a darker color
            Program_state_continuously.Foreground = new SolidColorBrush(Colors.Red);
            Program_state_continuously.Content = "Auto Accept Every Match (OFF)";
        }
        /// <summary>
        /// Run at startup State ON event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Run_at_startup_state_Checked(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// Run at startup State OFF event
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void Run_at_startup_state_Unchecked(object sender, RoutedEventArgs e)
        {
            // PrintToLog("{Run_at_startup_state_Unchecked}");
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

                if (key.GetValue("CS2-AutoAccept") != null)
                {
                    key.DeleteValue("CS2-AutoAccept");
                }

                key.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "CS2 AutoAccept", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Change to a darker color
            Run_at_startup_state.Foreground = new SolidColorBrush(Colors.Red);
            Run_at_startup_state.Content = "Run at startup (OFF)";
        }
        /// <summary>
        /// Run when window size is changed
        /// </summary>
        private void WindowSizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            SettingsModel userSettings = new SettingsModel(newWidth, newHeight);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };

            string jsonString = JsonSerializer.Serialize(userSettings, jsonOptions);

            File.WriteAllText(Path.Combine(_basePath, "settings.cs2_auto"), jsonString);
        }
        #endregion

        private void CheckForUpdate()
        {
            if (!_updateAvailable)
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    _updateAvailable = await UpdateHeaderVersion();
                }));

                Thread.Sleep(5 * 60 * 1000);
                CheckForUpdate();
            }
        }

        private async Task<bool> UpdateHeaderVersion()
        {
            string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion!;

            List<int> serverVersion = new List<int>();
            List<int> clientVersion = fileVersion.Split('.').Select(int.Parse).ToList();
            UpdateInfo serverUpdateInfo;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                    {
                        NoCache = true
                    };

                    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

                    serverUpdateInfo = JsonSerializer.Deserialize<UpdateInfo>(await client.GetStringAsync("https://raw.githubusercontent.com/tsgsOFFICIAL/GithubTestDirDownloader/main/CS2-AutoAccept/UpdateInfo.json")) ?? new UpdateInfo();
                    serverVersion = serverUpdateInfo.Version!.Split(".").Select(int.Parse).ToList();
                }

                // PrintToLog("{UpdateHeaderVersion} You are up-to-date!");
                Button_Update.Content = "You are up-to-date!";
                Button_Update.ToolTip = $"You are on the newest version ({clientVersion[0]}.{clientVersion[1]}.{clientVersion[2]}.{clientVersion[3]})\nYou can click at anytime to check again";
                Button_Update.Foreground = new SolidColorBrush(Colors.LawnGreen);
                _updateAvailable = false;

                // Is the update newer
                if ((clientVersion[0] < serverVersion[0]) || (clientVersion[1] < serverVersion[1] && clientVersion[0] <= serverVersion[0]) || (clientVersion[2] < serverVersion[2] && clientVersion[1] <= serverVersion[1] && clientVersion[0] <= serverVersion[0]) || (clientVersion[3] < serverVersion[3] && clientVersion[2] <= serverVersion[2] && clientVersion[1] <= serverVersion[1] && clientVersion[0] <= serverVersion[0]))
                {
                    // PrintToLog("{UpdateHeaderVersion} Update available");
                    Button_Update.Content = "Update Now";
                    Button_Update.ToolTip = $"Version {serverVersion[0]}.{serverVersion[1]}.{serverVersion[2]}.{serverVersion[3]} is now available!\nYou're on version {clientVersion[0]}.{clientVersion[1]}.{clientVersion[2]}.{clientVersion[3]}\nClick to update now";
                    Button_Update.Foreground = new SolidColorBrush(Colors.Orange);
                    _updateAvailable = true;
                }

                // Check if the user is on a newer build than the server
                if ((clientVersion[0] > serverVersion[0]) || (clientVersion[1] > serverVersion[1] && clientVersion[0] >= serverVersion[0]) || (clientVersion[2] > serverVersion[2] && clientVersion[1] >= serverVersion[1] && clientVersion[0] >= serverVersion[0]) || (clientVersion[3] > serverVersion[3] && clientVersion[2] >= serverVersion[2] && clientVersion[1] >= serverVersion[1] && clientVersion[0] >= serverVersion[0]))
                {
                    // PrintToLog("{UpdateHeaderVersion} You're on a dev build");
                    Button_Update.Content = "You're on a dev build";
                    Button_Update.ToolTip = $"Woooo! Look at you, you're on a dev build, version: {clientVersion[0]}.{clientVersion[1]}.{clientVersion[2]}.{clientVersion[3]}\nBe careful, Dev builds don't tend to be as stable.. ;)";
                    Button_Update.Foreground = new SolidColorBrush(Colors.GreenYellow);
                    _updateAvailable = false;
                }

                Button_Update.ToolTip += $"\n\nChangelog: {serverUpdateInfo.Changelog}\nType: {serverUpdateInfo.Type}";
                // Catch if the client.DownloadString failed, maybe the link changed, the server is down or the client is offline
            }
            catch (Exception)
            {
                // PrintToLog("{UpdateHeaderVersion} EXCEPTION: " + ex.Message);
                //Debug.WriteLine(ex.Message);
                Button_Update.Foreground = new SolidColorBrush(Colors.Red);
                Button_Update.Content = "You're offline!";
                Button_Update.ToolTip = $"You are on version ({clientVersion[0]}.{clientVersion[1]}.{clientVersion[2]}.{clientVersion[3]})";
                _updateAvailable = false;
            }

            return _updateAvailable;
        }

        private static void DeleteAllExceptFolder(string directoryPath, string folderToKeep)
        {
            foreach (string directory in Directory.GetDirectories(directoryPath))
            {
                if (Path.GetFileName(directory) != folderToKeep)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (Exception)
                    { }
                }
            }

            foreach (string file in Directory.GetFiles(directoryPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                { }
            }

            // Recursively process subdirectories
            foreach (string subdirectory in Directory.GetDirectories(directoryPath))
            {
                if (Path.GetFileName(subdirectory) != folderToKeep)
                {
                    DeleteAllExceptFolder(subdirectory, folderToKeep);
                }
            }
        }
    }
}