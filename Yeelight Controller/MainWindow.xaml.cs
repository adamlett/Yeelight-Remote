using NHotkey;
using NHotkey.Wpf;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace Yeelight_Controller
{
    public partial class MainWindow : Window
    {
        private int currentBrightness = 50;
        private bool dragActive = false;
        private bool ready = false;
        private string address;
        private TcpClient tcpClient;
        private KBShortcut toggleKeyboardShortcut, increaseBrightnessShortcut, decreaseBrightnessShortcut;

        public MainWindow()
        {
            InitializeComponent();
            RestoreKeyboardShortcuts();
            ready = true;
            RestoreLastConnection();
            
        }

        #region Keyboard Shortcuts

        private void RestoreKeyboardShortcuts()
        {
            // Restore the keyboard shortcuts
            toggleKeyboardShortcut = SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Toggle Lights"), 
                ToggleLightsKeyPress, "Toggle Lights", ModifierKeys.Shift, Key.F1, toggleShortcutBox);
            increaseBrightnessShortcut = SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Brightness Up"), 
                IncreaseBrightnessKeyPress, "Brightness Up", ModifierKeys.Shift, Key.F3, brightnessUpShortcut);
            decreaseBrightnessShortcut = SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Brightness Down"), 
                DecreaseBrightnessKeyPress, "Brightness Down", ModifierKeys.Shift, Key.F2, brightnessDownShortcut);
        }

        private KBShortcut SetKeyboardShortcut(KBShortcut shortcut, EventHandler<HotkeyEventArgs> handler, string name, ModifierKeys default_modifiers, 
            Key default_key, System.Windows.Controls.TextBox textbox)
        {
            shortcut = Settings.restoreSetting<KBShortcut>(name);
            if (shortcut == null)
                shortcut = new KBShortcut(name, default_modifiers, default_key);
            HotkeyManager.Current.AddOrReplace(name, shortcut.key, shortcut.modifiers, handler);
            textbox.Text = shortcut.description;

            return shortcut;
        }

        #endregion

        private void RestoreLastConnection()
        {
            address = Settings.ReadIPFromFile();
            if (address != null)
            {
                ipEntryBox.Text = address;
                InitialiseConnection();
            }
        }

        #region Button Listeners

        private void ScanClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Scan");
            scanningGrid.Visibility = Visibility.Visible;
        }

        private void ToggleClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Toggled");
            ToggleLights();
        }

        private void ChangeShortcutButton(string name, string description, EventHandler<HotkeyEventArgs> handler, KBShortcut shortcut)
        {
            HotkeyManager.Current.AddOrReplace(name, shortcut.key, shortcut.modifiers, handler);
            System.Windows.MessageBox.Show(description + shortcut.description);
            Settings.WriteShortcutToFile(toggleKeyboardShortcut);
        }

        private void ToggleLightsClick(object sender, RoutedEventArgs e)
        {
            ChangeShortcutButton("Toggle", "Toggle lights shortcut set to: \n", ToggleLightsKeyPress, toggleKeyboardShortcut);
        }

        private void BrightnessUpClick(object sender, RoutedEventArgs e)
        {
            ChangeShortcutButton("Brightness Up", "Increase brightness shortcut set to: \n", IncreaseBrightnessKeyPress, increaseBrightnessShortcut);
        }

        private void BrightnessDownClick(object sender, RoutedEventArgs e)
        {
            ChangeShortcutButton("Brightness Down", "Decrease brightness shortcut set to: \n", DecreaseBrightnessKeyPress, decreaseBrightnessShortcut);
        }

        #endregion

        #region Slider Listeners
        private void UpdateBrightness()
        {
            Console.WriteLine(Math.Round(slider.Value));
            int brightness = (int)Math.Round(slider.Value);
            int smoothness = 500;

            SetBrightness(brightness, smoothness);
            
        }

        private void BrightnessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // If changed without dragging, like using arrow keys etc
            if (!dragActive)
            {
                UpdateBrightness();
            }
        }

        private void BrightnessSlider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            dragActive = true;
            Console.WriteLine("Drag Started");
        }

        private void BrightnessSlider_DragEnded(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Drag Ended");
            UpdateBrightness();
            dragActive = false;
        }

        #endregion

        private bool InitialiseConnection()
        {
            if (!ready)
                return false;

            // Define ports to use for TCP, the default IP address to use etc.
            IPAddress ip = IPAddress.Parse(address);

            // Setup the end-point
            IPEndPoint sending_end_point = new IPEndPoint(ip, 55443);

            // Initialise TCP connection
            tcpClient = new TcpClient();

            try
            {
                scanningGrid.Visibility = Visibility.Visible;
                tcpClient.Connect(sending_end_point);
            }
            catch (SocketException)
            {
                Console.WriteLine("Couldn't connect to socket. The IP may be incorrect or there may be no network connection");
                //Environment.Exit(1);
                connectionLabel.Content = "Not connected.";
            }

            if (tcpClient.Connected)
            {
                scanningGrid.Visibility = Visibility.Hidden;
                Console.Write("Connected to device.");
                connectionLabel.Content = "Connected.";

                //Save the current IP address
                Settings.WriteIPToFile(address);
            }
            else
            {
                Console.Write("Failed to connect to device.");
            }

            return tcpClient.Connected;
        }

        #region Keyboard Shortcuts

        private void ToggleShortcut_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            var converter = new KeysConverter();
            string modifierString = converter.ConvertToString(e.KeyboardDevice.Modifiers);
            string key = converter.ConvertToString(e.Key);

            // Supress unwanted keys
            if (key == "LeftCtrl" || key == "RightCtrl" || key == "LeftShift" || key == "RightShift"  || key == "System" || key == "LWin" || key == "RWin")
                key = "";
            System.Windows.Controls.TextBox textbox = (System.Windows.Controls.TextBox)sender;
           
            switch (textbox.Name)
            {
                case "toggleShortcutBox":
                    toggleKeyboardShortcut = new KBShortcut("Toggle Lights", e.KeyboardDevice.Modifiers, e.Key);
                    textbox.Text = toggleKeyboardShortcut.description;
                    break;
                case "brightnessUpShortcut":
                    increaseBrightnessShortcut = new KBShortcut("Brightness Up", e.KeyboardDevice.Modifiers, e.Key);
                    textbox.Text = increaseBrightnessShortcut.description;
                    break;
                case "brightnessDownShortcut":
                    decreaseBrightnessShortcut = new KBShortcut("Brightness Down", e.KeyboardDevice.Modifiers, e.Key);
                    textbox.Text = decreaseBrightnessShortcut.description;
                    break;
            }

        }

        #endregion

        #region Keypresses

        private void ToggleLightsKeyPress(object sender, HotkeyEventArgs e)
        {
            ToggleLights();
        }

        private void IncreaseBrightnessKeyPress(object sender, HotkeyEventArgs e)
        {
            if (currentBrightness != 100)
            {
                int brightness = Math.Min(currentBrightness + 20, 100);
                SetBrightness(brightness, 250);
            }
        }

        private void DecreaseBrightnessKeyPress(object sender, HotkeyEventArgs e)
        {
            if (currentBrightness != 1)
            {
                int brightness = Math.Max(currentBrightness - 20, 1);
                SetBrightness(brightness, 250);
            }
        }

        #endregion

        #region Commands

        private void ToggleLights()
        {
            SendCommand("{\"id\":1,\"method\":\"toggle\",\"params\":[]}\r\n");
        }

        private void ConnectBtn(object sender, RoutedEventArgs e)
        {
            if (ipEntryBox.Text.Length == 0)
            {
                System.Windows.MessageBox.Show("Empty input. Enter a valid IP or hostname");
            }
            else
            {
                address = ipEntryBox.Text;
                InitialiseConnection();
            }
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void SetBrightness(int brightness, int smooth_time)
        {
            currentBrightness = brightness;

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"id\":1,\"method\":\"set_bright\",\"params\":[");
            sb.Append(brightness);
            sb.Append(", \"smooth\", ");
            sb.Append(smooth_time);
            sb.Append("]}\r\n");

            SendCommand(sb.ToString());
        }

        private void SendCommand(string json_message)
        {
            if (tcpClient == null || !tcpClient.Connected)
                InitialiseConnection();

            // Convert text to a byte array
            byte[] send_buffer = Encoding.UTF8.GetBytes(json_message);

            try
            {
                tcpClient.Client.Send(send_buffer);
                Console.WriteLine("Command sent");
            }
            catch(Exception send_exception)
            {
                Console.WriteLine("Error occured while sending the command. " + send_exception.Message);
            }

        }
        #endregion
    }
}
