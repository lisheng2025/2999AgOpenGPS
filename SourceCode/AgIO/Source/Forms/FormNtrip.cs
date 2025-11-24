using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AgIO.Controls;
using AgLibrary.Logging;

namespace AgIO
{
    public partial class FormNtrip : Form
    {
        //class variables
        private readonly FormLoop mf;

        private bool ntripStatusChanged = false;

        public FormNtrip(Form callingForm)
        {
            mf = callingForm as FormLoop;
            InitializeComponent();

            //this.groupBox2.Text = gStr.gsNetworking;
            //this.cboxIsNTRIPOn.Text = gStr.gsNTRIPOn;
            //this.label6.Text = gStr.gsPort;
            //this.label4.Text = gStr.gsEnterBroadcasterURLOrIP;
            //this.label7.Text = gStr.gsToUDPPort;

            //this.label3.Text = gStr.gsUsername;
            //this.label12.Text = gStr.gsPassword;
            //this.label13.Text = gStr.gsMount;
            //this.label15.Text = gStr.gsGGAIntervalSecs;
            //this.btnGetIP.Text = gStr.gsConfirmIP;

            //this.label9.Text = gStr.gsCurrentGPSFix;
            //this.label17.Text = gStr.gsSendToManualFix;
            //this.btnSetManualPosition.Text = gStr.gsSendToManualFix;
            //this.label18.Text = gStr.gsSetToZeroForSerial;
            //this.btnGetSourceTable.Text = gStr.gsGetSourceTable;

            //this.label1.Text = gStr.gsRestartRequired;
            //this.label19.Text = gStr.gsZeroEqualsOff;

            //this.Text = gStr.gsNTRIPClientSettings;

            //turn off the little arrows
            nudCasterPort.Controls[0].Enabled = false;
            nudGGAInterval.Controls[0].Enabled = false;
            nudLatitude.Controls[0].Enabled = false;
            nudLongitude.Controls[0].Enabled = false;
            nudSendToUDPPort.Controls[0].Enabled = false;
        }

        private void FormNtrip_Load(object sender, EventArgs e)
        {
            cboxIsNTRIPOn.Checked = Properties.Settings.Default.setNTRIP_isOn;

            if (!cboxIsNTRIPOn.Checked) tabControl1.Enabled = false;
            string hostName = Dns.GetHostName(); // Retrieve the Name of HOST
            tboxHostName.Text = hostName;

            //IPAddress[] ipaddress = Dns.GetHostAddresses(hostName);
            GetIPAddressList(); // ✅ 修改：方法名改为更准确的名称

            cboxToSerial.Checked = Properties.Settings.Default.setNTRIP_sendToSerial;
            cboxToUDP.Checked = Properties.Settings.Default.setNTRIP_sendToUDP;
            nudSendToUDPPort.Value = Properties.Settings.Default.setNTRIP_sendToUDPPort;

            tboxEnterURL.Text = Properties.Settings.Default.setNTRIP_casterURL;

            tboxCasterIP.Text = Properties.Settings.Default.setNTRIP_casterIP;
            nudCasterPort.Value = Properties.Settings.Default.setNTRIP_casterPort;

            tboxUserName.Text = Properties.Settings.Default.setNTRIP_userName;
            tboxUserPassword.Text = Properties.Settings.Default.setNTRIP_userPassword;
            tboxMount.Text = Properties.Settings.Default.setNTRIP_mount;

            nudGGAInterval.Value = Properties.Settings.Default.setNTRIP_sendGGAInterval;

            nudLatitude.Value = (decimal)Properties.Settings.Default.setNTRIP_manualLat;
            nudLongitude.Value = (decimal)Properties.Settings.Default.setNTRIP_manualLon;
            tboxCurrentLat.Text = Properties.Settings.Default.setNTRIP_manualLat.ToString();
            tboxCurrentLon.Text = Properties.Settings.Default.setNTRIP_manualLon.ToString();

            checkBoxusetcp.Checked = Properties.Settings.Default.setNTRIP_isTCP;

            if (Properties.Settings.Default.setNTRIP_isGGAManual) cboxGGAManual.Text = "Use Manual Fix";
            else cboxGGAManual.Text = "Use GPS Fix";

            if (Properties.Settings.Default.setNTRIP_isHTTP10) cboxHTTP.Text = "1.0";
            else cboxHTTP.Text = "1.1";

            comboboxPacketSize.Text = mf.packetSizeNTRIP.ToString();
        }

        private void cboxIsNTRIPOn_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.setNTRIP_isOn = cboxIsNTRIPOn.Checked;

            if (cboxIsNTRIPOn.Checked)
            {
                Properties.Settings.Default.setRadio_isOn = mf.isRadio_RequiredOn = false;
                Properties.Settings.Default.setPass_isOn = mf.isSerialPass_RequiredOn = false;
                Log.EventWriter("NTRIP Turned on");
            }
            else
            {
                Log.EventWriter("NTRIP Turned off");
            }

            Properties.Settings.Default.Save();

            mf.YesMessageBox("Restart of AgIO is Required - Restarting");
            Log.EventWriter("Program Reset: Selecting NTRIP Feature");

            Program.Restart();
        }

        // ✅ 修改：方法名改为更准确的名称，同时显示IPv4和IPv6地址
        public void GetIPAddressList()
        {
            listboxIP.Items.Clear();

            try
            {
                foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (IPA.AddressFamily == AddressFamily.InterNetwork)
                    {
                        listboxIP.Items.Add($"IPv4: {IPA}");
                    }
                    else if (IPA.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // 过滤掉IPv6链路本地和环回地址，只显示有意义的地址
                        if (!IPA.IsIPv6LinkLocal && !IPA.IsIPv6SiteLocal && !IPAddress.IsLoopback(IPA))
                        {
                            listboxIP.Items.Add($"IPv6: {IPA}");
                        }
                    }
                }

                // 如果没有找到任何地址，添加提示
                if (listboxIP.Items.Count == 0)
                {
                    listboxIP.Items.Add("No network addresses found");
                }
            }
            catch (Exception ex)
            {
                listboxIP.Items.Add($"Error getting addresses: {ex.Message}");
                Log.EventWriter($"Error in GetIPAddressList: {ex}");
            }
        }

        private void btnGetIP_Click(object sender, EventArgs e)
        {
            string actualIP = tboxEnterURL.Text.Trim();
            
            // ✅ 改进：如果是直接输入的IP地址，直接使用
            if (CheckIPValid(actualIP))
            {
                tboxCasterIP.Text = actualIP;
                mf.broadCasterIP = actualIP;
                Properties.Settings.Default.setNTRIP_casterIP = mf.broadCasterIP;
                Properties.Settings.Default.Save();
                mf.TimedMessageBox(2500, "IP Valid", "Using provided IP: " + actualIP);
                return;
            }

            try
            {
                IPAddress[] addresslist = Dns.GetHostAddresses(actualIP);
                if (addresslist != null && addresslist.Length > 0)
                {
                    tboxCasterIP.Text = "";
                    IPAddress ipv4Address = null;
                    IPAddress ipv6Address = null;
                    
                    foreach (IPAddress address in addresslist)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipv4Address = address;
                            break; // 优先使用IPv4
                        }
                        else if (address.AddressFamily == AddressFamily.InterNetworkV6 && ipv6Address == null)
                        {
                            // ✅ 改进：过滤掉IPv6特殊地址
                            if (!address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !IPAddress.IsLoopback(address))
                            {
                                ipv6Address = address;
                            }
                        }
                    }
                    
                    // 在循环外部解析IP地址
                    string resolvedIP = (ipv4Address ?? ipv6Address)?.ToString().Trim();
                    if (!string.IsNullOrEmpty(resolvedIP))
                    {
                        tboxCasterIP.Text = resolvedIP;
                        mf.broadCasterIP = resolvedIP;
                        Properties.Settings.Default.setNTRIP_casterIP = mf.broadCasterIP;
                        Properties.Settings.Default.Save();
                        
                        string addressType = (ipv4Address != null) ? "IPv4" : "IPv6";
                        mf.TimedMessageBox(2500, "IP Located", $"Verified: {actualIP} ({addressType})");
                    }
                    else
                    {
                        mf.YesMessageBox("Can't Find Valid IP for: " + actualIP);
                        Log.EventWriter("Can't Find Valid Caster IP");
                    }
                }
                else
                {
                    mf.YesMessageBox("No IP Addresses Found for: " + actualIP);
                    Log.EventWriter("No IP Addresses Found for Caster");
                }
            }
            catch (Exception ex)
            {
                mf.YesMessageBox("DNS Resolution Failed for: " + actualIP);
                Log.EventWriter($"DNS Resolution Failed for Caster IP: {ex.Message}");
            }
        }

        public Boolean CheckIPValid(String strIP)
        {
            // Return true for COM Port
            if (strIP.Contains("COM")) return true;
        
            // ✅ 改进：支持IPv4和IPv6验证
            if (IPAddress.TryParse(strIP, out IPAddress address))
            {
                // 允许所有有效的IP地址类型
                return address.AddressFamily == AddressFamily.InterNetwork ||
                       address.AddressFamily == AddressFamily.InterNetworkV6;
            }
            return false;
        }

        private void tboxCasterIP_Validating(object sender, CancelEventArgs e)
        {
            if (!CheckIPValid(tboxCasterIP.Text))
            {
                tboxCasterIP.Text = "127.0.0.1";
                tboxCasterIP.Focus();
                mf.TimedMessageBox(2000, "Invalid IP Address", "Set to Default Local 127.0.0.1");
            }
        }

        private void btnSerialOK_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.setNTRIP_casterIP = tboxCasterIP.Text;
            Properties.Settings.Default.setNTRIP_casterPort = (int)nudCasterPort.Value;
            Properties.Settings.Default.setNTRIP_sendToUDPPort = (int)nudSendToUDPPort.Value;

            Properties.Settings.Default.setNTRIP_isOn = cboxIsNTRIPOn.Checked;

            if (cboxIsNTRIPOn.Checked)
            {
                Properties.Settings.Default.setRadio_isOn = mf.isRadio_RequiredOn = false;
                Properties.Settings.Default.setPass_isOn = mf.isSerialPass_RequiredOn = false;
            }

            Properties.Settings.Default.setNTRIP_userName = tboxUserName.Text;
            Properties.Settings.Default.setNTRIP_userPassword = tboxUserPassword.Text;
            Properties.Settings.Default.setNTRIP_mount = tboxMount.Text;

            Properties.Settings.Default.setNTRIP_sendGGAInterval = (int)nudGGAInterval.Value;
            Properties.Settings.Default.setNTRIP_manualLat = (double)nudLatitude.Value;
            Properties.Settings.Default.setNTRIP_manualLon = (double)nudLongitude.Value;

            Properties.Settings.Default.setNTRIP_casterURL = tboxEnterURL.Text;
            Properties.Settings.Default.setNTRIP_isGGAManual = cboxGGAManual.Text == "Use Manual Fix";
            Properties.Settings.Default.setNTRIP_isHTTP10 = cboxHTTP.Text == "1.0";
            Properties.Settings.Default.setNTRIP_isTCP = checkBoxusetcp.Checked;

            Properties.Settings.Default.setNTRIP_sendToSerial = cboxToSerial.Checked;
            Properties.Settings.Default.setNTRIP_sendToUDP = cboxToUDP.Checked;

            mf.isSendToSerial = cboxToSerial.Checked;
            mf.isSendToUDP = cboxToUDP.Checked;

            mf.packetSizeNTRIP = Convert.ToInt32(comboboxPacketSize.Text);
            Properties.Settings.Default.setNTRIP_packetSize = Convert.ToInt32(comboboxPacketSize.Text);

            if (Properties.Settings.Default.setNTRIP_isOn && Properties.Settings.Default.setRadio_isOn)
            {
                mf.TimedMessageBox(2000, "Radio also enabled", "Disable the Radio NTRIP");
                Properties.Settings.Default.setRadio_isOn = false;
            }

            Properties.Settings.Default.Save();

            if (!ntripStatusChanged)
            {
                Close();
                mf.ConfigureNTRIP();
            }
            else
            {
                Log.EventWriter("Program Reset: Button Ok on Ntrip Form");

                Program.Restart();
            }
        }

        private void btnSetManualPosition_Click(object sender, EventArgs e)
        {
            nudLatitude.Value = (decimal)mf.latitude;
            nudLongitude.Value = (decimal)mf.longitude;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tboxCurrentLat.Text = mf.latitude.ToString();
            tboxCurrentLon.Text = mf.longitude.ToString();
        }

        private readonly List<string> dataList = new List<string>();

        private void btnGetSourceTable_Click(object sender, EventArgs e)
        {
            btnGetSourceTable.Enabled = false;
            
            // ✅ 改进：更好的IP地址解析和错误处理
            if (!IPAddress.TryParse(tboxCasterIP.Text.Trim(), out IPAddress casterIP))
            {
                mf.TimedMessageBox(2000, "Invalid IP", "Please enter a valid IP address");
                btnGetSourceTable.Enabled = true;
                return;
            }

            int casterPort = (int)nudCasterPort.Value;

            Socket sckt;
            dataList?.Clear();

            try
            {
                AddressFamily family = casterIP.AddressFamily;
                sckt = new Socket(family, SocketType.Stream, ProtocolType.Tcp)
                {
                    Blocking = true,
                    ReceiveTimeout = 5000, // ✅ 添加超时设置
                    SendTimeout = 5000
                };

                // ✅ 改进：IPv6双栈模式设置
                if (family == AddressFamily.InterNetworkV6)
                {
                    sckt.DualMode = true;
                }

                sckt.Connect(new IPEndPoint(casterIP, casterPort));

                string msg = "GET / HTTP/1.0\r\n" + "User-Agent: NTRIP iter.dk\r\n" +
                                    "Accept: */*\r\nConnection: close\r\n" + "\r\n";

                //Send request
                byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                sckt.Send(data);
                int bytes = 0;
                byte[] bytesReceived = new byte[1024];
                string page = String.Empty;
                Thread.Sleep(200);

                do
                {
                    bytes = sckt.Receive(bytesReceived, bytesReceived.Length, SocketFlags.None);
                    page += Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                }
                while (bytes > 0);

                if (page.Length > 0)
                {
                    string[] words = page.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < words.Length; i++)
                    {
                        string[] words2 = words[i].Split(';');

                        if (words2[0] == "STR")
                        {
                            dataList.Add(words2[1].Trim().ToString() + "," + words2[9].ToString() + "," + words2[10].ToString()
                          + "," + words2[3].Trim().ToString() + "," + words2[6].Trim().ToString()
                                );
                        }
                    }
                }

                sckt.Close(); // ✅ 确保Socket被关闭
            }
            catch (SocketException ex)
            {
                mf.TimedMessageBox(2000, "Socket Exception", $"Invalid IP:Port - {ex.SocketErrorCode}");
                btnGetSourceTable.Enabled = true;
                Log.EventWriter($"Socket Exception, Invalid IP:Port - {ex}");
                return;
            }
            catch (Exception ex)
            {
                mf.TimedMessageBox(2000, "Exception", "Get Source Table Error");
                btnGetSourceTable.Enabled = true;
                Log.EventWriter($"Get Source Table Error: {ex}");
                return;
            }

            if (dataList.Count > 0)
            {
                string syte = "http://monitor.use-snip.com/?hostUrl=" + tboxCasterIP.Text + "&port=" + nudCasterPort.Value.ToString();
                using (FormSource form = new FormSource(this, dataList, mf.latitude, mf.longitude, syte))
                {
                    form.ShowDialog(this);
                }
            }
            else
            {
                mf.TimedMessageBox(2000, "Error", "No Source Data Received");
            }

            btnGetSourceTable.Enabled = true;
        }

        private void NudCasterPort_Enter(object sender, EventArgs e)
        {
            ((NumericUpDown)sender).ShowKeypad(this);
            btnSerialCancel.Focus();
        }

        private void NudGGAInterval_Enter(object sender, EventArgs e)
        {
            ((NumericUpDown)sender).ShowKeypad(this);
            btnSerialCancel.Focus();
        }

        private void NudLatitude_Enter(object sender, EventArgs e)
        {
            ((NumericUpDown)sender).ShowKeypad(this);
            btnSerialCancel.Focus();
        }

        private void NudLongitude_Enter(object sender, EventArgs e)
        {
            ((NumericUpDown)sender).ShowKeypad(this);
            btnSerialCancel.Focus();
        }

        private void NudSendToUDPPort_Enter(object sender, EventArgs e)
        {
            ((NumericUpDown)sender).ShowKeypad(this);
            btnSerialCancel.Focus();
        }

        private void tboxEnterURL_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                ((TextBox)sender).ShowKeyboard(this);
                btnSerialCancel.Focus();
            }
            btnGetIP.PerformClick();
        }

        private void tboxMount_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                ((TextBox)sender).ShowKeyboard(this);
                btnSerialCancel.Focus();
            }
        }

        private void tboxUserName_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                ((TextBox)sender).ShowKeyboard(this);
                btnSerialCancel.Focus();
            }
        }

        private void tboxUserPassword_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                ((TextBox)sender).ShowKeyboard(this);
                btnSerialCancel.Focus();
            }
        }

        private void btnPassUsername_Click(object sender, EventArgs e)
        {
            if (tboxUserName.PasswordChar == '*') tboxUserName.PasswordChar = '\0';
            else tboxUserName.PasswordChar = '*';
            tboxUserName.Invalidate();
        }

        private void btnPassPassword_Click(object sender, EventArgs e)
        {
            if (tboxUserPassword.PasswordChar == '*') tboxUserPassword.PasswordChar = '\0';
            else tboxUserPassword.PasswordChar = '*';
            tboxUserPassword.Invalidate();
        }

        private void cboxToUDP_Click(object sender, EventArgs e)
        {
            ntripStatusChanged = true;
            if (cboxToSerial.Checked) cboxToSerial.Checked = false;
        }

        private void cboxToSerial_Click(object sender, EventArgs e)
        {
            ntripStatusChanged = true;
            if (cboxToUDP.Checked) cboxToUDP.Checked = false;
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(gStr.gsNTRIP_Help);
        }
    }
}
