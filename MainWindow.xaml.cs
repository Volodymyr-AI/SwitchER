using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net; // IPHostEntry
using System.Net.Sockets;
using System.Runtime.InteropServices; // fot DllImport
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwitchER
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint physicalAddrLen);

        List<TableHost> _host = new List<TableHost>();
        IPHostEntry entry;
        string[] ipToString = new string[4];

        
        public MainWindow()
        {
            InitializeComponent();

            String host = System.Net.Dns.GetHostName();
            // Get ip address.
            System.Net.IPAddress ip = System.Net.Dns.GetHostByName(host).AddressList[0];
            // View my address in label7.
            label7.Content = "Your IP - " + ip.ToString();
            ipToString = ip.ToString().Split('.');
            Reader_from_txt();

        }

        string[] ip_Text;
        string[] host_Text;
        string[] mac_Text;

        private void Reader_from_txt()
        {
            string parth = Environment.CurrentDirectory + "\\IP_MAC.txt";
            try
            {
                string[] str = File.ReadAllLines(parth);

                ip_Text = new string[str.Length];
                host_Text = new string[str.Length];
                mac_Text = new string[str.Length];
                for (int i = 0; i < str.Length; i++)
                {
                    string[] s = str[i].Split('#');
                    ip_Text[i] = s[0];
                    host_Text[i] = s[1];
                    mac_Text[i] = s[2];
                    comboBox1.Items.Add(s[1]);
                }
            }
            catch
            {
                File.Create(parth);
            }
        }

        private void WakeupFunction(string MAC_ADDRESS)
        {
            WOLClass client = new WOLClass();
            client.Connect(new IPAddress(0xffffffff), 0x2fff);
            client.SetClientToBrodcastMode();
            int counter = 0;
            //buffer for sending
            byte[] bytes = new byte[1024];
            //we send first 6 bytes
            for (int i = 0; i <= 6; i++)
            {
                bytes[counter++] = 0xFF;
            }
            //repeat MAC address for 16 times
            for (int i = 0; i < 16; i++)
            {
                int j = 0;
                for (int k = 0; k < 6; k++)
                {
                    bytes[counter++] = byte.Parse(MAC_ADDRESS.Substring(i, 2), NumberStyles.HexNumber);
                    j += 2;
                }
            }
            //now we can send our magic packet
            int magic_packet = client.Send(bytes, 1024); //Magic packet — це спеціальна послідовність байтів, яку для нормального проходження по локальних мережах можна вкласти в пакети UDP або IPX.

        }
        //get ip, name and mac
        private void GetInform(string textName)
        {
            string IP_Address = "";
            string HostName = "";
            string MacAddress = "";

            try
            {
                //Checking if IP exists
                entry = Dns.GetHostEntry(textName);
                foreach (IPAddress a in entry.AddressList)
                {
                    IP_Address = a.ToString();
                    break;
                }

                //Get HostName
                HostName = entry.HostName;

                //Get Mac-address
                try
                {
                    IPAddress dst = IPAddress.Parse(textName);

                    byte[] MacAddr = new byte[6];
                    uint MacAddrLen = (uint)MacAddr.Length;

                    if (SendARP(BitConverter.ToInt32(dst.GetAddressBytes(), 0), 0, MacAddr, ref MacAddrLen) != 0)
                    {
                        throw new InvalidOperationException("SendARP failed.");
                    }

                    string[] str = new string[(int)MacAddrLen];
                    for (int i = 0; i < MacAddrLen; i++)
                        str[i] = MacAddr[i].ToString("x2");

                    MacAddress = string.Join(":", str);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Did not find any mac address");
                }

                //If everything fine we add info into ListView_1
                Dispatcher.Invoke(new Action(() =>
                {

                    _host.Add(new TableHost() { ipAdress = IP_Address, nameComputer = HostName, MacAddress = MacAddress });
                    ListView_1.ItemsSource = null;
                    ListView_1.ItemsSource = _host;
                }));
            }
            catch { }
        }

        private void Button2_Click(object sender, RoutedEventArgs e) //scanning for local computers
        {
            ProcessStartInfo newProcess = new ProcessStartInfo(@"cmd.exe", @"/C arp -a");
            newProcess.WindowStyle = ProcessWindowStyle.Hidden;
            newProcess.RedirectStandardOutput = true;
            newProcess.UseShellExecute = false;
            newProcess.StandardOutputEncoding = Encoding.UTF7;
            newProcess.CreateNoWindow = true;
            Process procCommand = Process.Start(newProcess);
            StreamReader srIncome = procCommand.StandardOutput;
           
            string output;
            while ((output = srIncome.ReadLine()) != null)
            {
                string[] mas = output.Split(new char[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    IPAddress ip_A = IPAddress.Parse(string.Format($"{mas[0]}.{mas[1]}.{mas[2]}.{mas[3]}"));
                    GetInform(ip_A.ToString());
                }
                catch { }
            }
            procCommand.WaitForExit();
        }

        private void PowerOn_Click(object sender, RoutedEventArgs e) //Switch on button
        {
            try
            {
                WakeupFunction(_host[ListView_1.SelectedIndex].MacAddress.ToString().Replace(":", ""));
                MessageBox.Show("Operation done successfully!", "Attention!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Запрос некорретный!", "Внимание!Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearList_Click(object sender, RoutedEventArgs e)//Clear List button
        {
            _host.Clear();
            ListView_1.Items.Refresh();
        }

        private void copyIP_Click(object sender, RoutedEventArgs e) // copy IP
        {
            Clipboard.SetText(_host[ListView_1.SelectedIndex].ipAdress.ToString());
        }

        private void copyName_Click(object sender, RoutedEventArgs e) // copy Name
        {
            Clipboard.SetText(_host[ListView_1.SelectedIndex].nameComputer.ToString());
        }

        private void copyMACaddress_Click(object sender, RoutedEventArgs e) // copy MAC address
        {
            Clipboard.SetText(_host[ListView_1.SelectedIndex].MacAddress.ToString());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) // close window
        {
            StreamWriter write = new StreamWriter(@"IPMAC.txt", true);

            for (int index = 0; index < _host.Count; index++)
            {

                if (!mac_Text.Contains(_host[index].MacAddress))
                    write.WriteLine(_host[index].ipAdress + "#" + _host[index].nameComputer + "#" + _host[index].MacAddress);
            }
            write.Close();
        }
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBox2.Text = ip_Text[comboBox1.SelectedIndex];
            textBox3.Text = host_Text[comboBox1.SelectedIndex];
            textBox4.Text = mac_Text[comboBox1.SelectedIndex];
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WakeupFunction(textBox4.Text.Replace(":", ""));
                MessageBox.Show("Операция выполнена успешно!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Запрос некорретный!", "Внимание!Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

    }
    public class TableHost
    {
        public string ipAdress { get; set; }
        public string nameComputer { get; set; }
        public string MacAddress { get; set; }
    }

    public class WOLClass : UdpClient // Wake-on-LAN protocol https://uk.wikipedia.org/wiki/Wake-on-LAN 
    {
        public WOLClass() : base() { }
        public void SetClientToBrodcastMode()
        {
            if (this.Active)
                this.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);
        }
    }
}
