using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;


// Scans for bulbs on the network by multicasting a request and receving responses

namespace Yeelight_Controller
{
    class BulbScanner
    {
        private static readonly IPAddress multicastAddress = IPAddress.Parse("239.255.255.250");
        private static readonly IPEndPoint multicastEndPoint = new IPEndPoint(multicastAddress, 1982);
        private const string requestString = "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1982\r\nMAN: \"ssdp:discover\"\r\nST: wifi_bulb";
        private static readonly byte[] requestBuffer = Encoding.UTF8.GetBytes(requestString);
        private const int udpSourcePort = 47740;
        private MainWindow window;
        private UdpClient udpClient;

        public BulbScanner(MainWindow window)
        {
            this.window = window;
        }


        // Multicasts a discovery request which bulbs can respond to
        public void SendDiscoveryRequest()
        {
            if (udpClient == null)
            {
                udpClient = new UdpClient(udpSourcePort);
                udpClient.JoinMulticastGroup(multicastAddress);
            }
            
            Console.WriteLine("Joined multicast");
            udpClient.Send(requestBuffer, requestBuffer.Length, multicastEndPoint);
            ListenForResponses();
            //udpClient.Close();
        }

        // Listen for responses to the discovery message
        public void ListenForResponses()
        {
            if (udpClient == null)
            {
                udpClient = new UdpClient(udpSourcePort);
            }
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, udpSourcePort);

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            System.Timers.Timer timer = new System.Timers.Timer();
            // Cancel the listening after 30 seconds
            timer.Elapsed += new ElapsedEventHandler((s, e) => {
                Console.WriteLine("Listening cancelled.");
                tokenSource.Cancel();
                CloseUdpClient();
            });
            timer.AutoReset = false;
            timer.Interval = 10000;
            timer.Enabled = true;

       
            // Reeceive and process any replies
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (udpClient.Available > 0) // Only read if we have some data queued
                        {
                            byte[] data = udpClient.Receive(ref remote);
                            Console.WriteLine("Bulb found");
                            //MessageBox.Show(Encoding.ASCII.GetString(data));


                            string response = Encoding.UTF8.GetString(data);
                            UpdateValuesFromResponse(response);

                            //Regex regex = new Regex("(?<=\"params\": ).*?(?=})");
                            //Match m = regex.Match(response);
                            //MessageBox.Show(m.Value);




                            // For now, just automatically connect to the first reply
                            window.InitiateNewConnection("192.168.0.13");
                            // Stop listening
                            tokenSource.Cancel();
                            CloseUdpClient();

                        }
                        Thread.Sleep(10);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }, token);
        }

        private void CloseUdpClient()
        {
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
        }

        private string GetParameterValue(string paramName, string response)
        {
            // Match only the value between the param name and the new line
            Regex regex = new Regex("(?<=" + paramName + ": ).*?(?=\r\n)");
            Match m = regex.Match(response);
            return m.Value;
        }

        // Updates the current values shown on the GUI to match those received in the response
        private void UpdateValuesFromResponse(string response)
        {
            string powerState = GetParameterValue("power", response);
            window.UpdatePowerState(powerState);

            int brightness = int.Parse(GetParameterValue("bright", response));
            window.ChangeSliderPosition(brightness);
        }

    }
}
