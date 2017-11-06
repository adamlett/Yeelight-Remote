using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Yeelight_Controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class KBShortcut
        {
            public ModifierKeys modifiers;
            public Key key;
            public string name;
            public string description;

            public KBShortcut(string name, ModifierKeys modifiers, Key pressedKey)
            {
                var converter = new KeysConverter();
                string modifierString = converter.ConvertToString(modifiers);
                string key = converter.ConvertToString(pressedKey);

                // Supress unwanted keys
                if (key == "LeftCtrl" || key == "RightCtrl" || key == "LeftShift" || key == "RightShift" || key == "System" || key == "LWin" || key == "RWin")
                    key = "";

                this.name = name;
                this.modifiers = modifiers;
                this.key = pressedKey;
                this.description = modifierString + " + " + key;
            }

            public KBShortcut() { } // Required to serialize class
        }

        public MainWindow()
        {
            InitializeComponent();
            RestoreKeyboardShortcuts();
        }

        private int currentBrightness = 50;
        private string address;
        private TcpClient tcpClient;
        private bool dragActive = false;

        private KBShortcut toggleKeyboardShortcut, increaseBrightnessShortcut, decreaseBrightnessShortcut;

        private void RestoreKeyboardShortcuts()
        {
            // Restore the keyboard shortcuts
            SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Toggle Lights"), ToggleLightsKeyPress, "Toggle Lights", ModifierKeys.Shift, Key.F1, toggleShortcutBox);
            SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Brightness Up"), IncreaseBrightnessKeyPress, "Brightness Up", ModifierKeys.Shift, Key.F3, brightnessUpShortcut);
            SetKeyboardShortcut(Settings.restoreSetting<KBShortcut>("Brightness Down"), DecreaseBrightnessKeyPress, "Brightness Down", ModifierKeys.Shift, Key.F2, brightnessDownShortcut);
        }

        private void SetKeyboardShortcut(KBShortcut shortcut, EventHandler<HotkeyEventArgs> handler, string name, ModifierKeys default_modifiers, 
            Key default_key, System.Windows.Controls.TextBox textbox)
        {
            shortcut = Settings.restoreSetting<KBShortcut>(name);
            if (shortcut == null)
                shortcut = new KBShortcut(name, default_modifiers, default_key);
            HotkeyManager.Current.AddOrReplace(name, shortcut.key, shortcut.modifiers, handler);
            textbox.Text = shortcut.description;
        }

        #region Button Listeners

        private void scan_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Scan");
            scanningGrid.Visibility = Visibility.Visible;
        }

        private void toggle_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Toggled");
            ToggleLights();
        }

        private void Toggle_shortcut_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.Current.AddOrReplace("Toggle", toggleKeyboardShortcut.key, toggleKeyboardShortcut.modifiers, ToggleLightsKeyPress);
            Console.WriteLine("Toggled Shortcut");
            System.Windows.MessageBox.Show("Toggle lights shortcut set to: \n" + toggleKeyboardShortcut.description);

            Settings.WriteShortcutToFile(toggleKeyboardShortcut);
        }

        private void Brightness_up_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.Current.AddOrReplace("Brightness Up", increaseBrightnessShortcut.key, increaseBrightnessShortcut.modifiers, IncreaseBrightnessKeyPress);
            Console.WriteLine("Brightness Up Keyboard Set");
            System.Windows.MessageBox.Show("Increase brightness shortcut set to: \n" + increaseBrightnessShortcut.description);

            Settings.WriteShortcutToFile(increaseBrightnessShortcut);
        }

        private void Brightness_down_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.Current.AddOrReplace("Brightness Down", decreaseBrightnessShortcut.key, decreaseBrightnessShortcut.modifiers, DecreaseBrightnessKeyPress);
            Console.WriteLine("Brightness Down Keyboard Set");

            System.Windows.MessageBox.Show("Decrease brightness shortcut set to: \n" + decreaseBrightnessShortcut.description);

            Settings.WriteShortcutToFile(decreaseBrightnessShortcut);
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

        private void GetBrightnessStatus()
        {
            // Requests the current brightness status

            // Send a JSON request to the lighbulb, at the address of the connected bulb
            HttpWebRequest request;
        }

        private bool InitialiseConnection()
        {
            // Define ports to use for TCP, the default IP address to use etc.
            address = "192.168.1.225";
            IPAddress ip = IPAddress.Parse(address);

            // Setup the end-point
            IPEndPoint sending_end_point = new IPEndPoint(ip, 55443);

            // Initialise TCP connection
            tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(sending_end_point);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Couldn't connect to socket. Check the Network Connection.");
                Environment.Exit(1);
            }

           

            if (tcpClient.Connected)
            {
                Console.Write("Connected to device.");
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
            if (key == "LeftCtrl" || key == "RightCtrl" || key == "LeftShift" || key == "RightShift" 
                || key == "System" || key == "LWin" || key == "RWin")
            {
                key = "";
            }

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
