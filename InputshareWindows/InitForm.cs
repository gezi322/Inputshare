using InputshareLib;
using InputshareLibWindows.Windows;
using InputshareWindows.Client;
using InputshareWindows.Server;
using InputshareWindows.ServiceClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InputshareWindows
{
    public partial class InitForm : Form
    {
        private string[] startArgs;

        public static void Main(string[] args)
        {
            ISLogger.EnableConsole = true;
            ISLogger.EnableLogFile = true;
            ISLogger.SetLogFileName("InputshareWindows.log");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            
            Application.Run(new InitForm(args));
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            ISLogger.Write("--------------------------");
            ISLogger.Write("UNHANDLED EXCEPTION");
            ISLogger.Write(ex.Message);
            ISLogger.Write(ex.StackTrace);
            ISLogger.Write("--------------------------");
            Thread.Sleep(2000);
        }

        public InitForm(string[] args)
        {
            startArgs = args;
            InitializeComponent();
            this.FormClosed += InitForm_FormClosed;
        }

        private void InitForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ISLogger.Exit();
        }

        private void InitForm_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;

            ArgumentParser.LaunchArgs inputArgs = ArgumentParser.ParseArgs(startArgs).Args;

            if (inputArgs.HasFlag(ArgumentParser.LaunchArgs.StartServer))
                LaunchServer(4441);
            else if (inputArgs.HasFlag(ArgumentParser.LaunchArgs.StartServiceClient))
                LaunchServiceClient();
            else if (inputArgs.HasFlag(ArgumentParser.LaunchArgs.StartClient))
                LaunchClient();

        }

        private void serverButton_Click(object sender, EventArgs e)
        {
            LaunchServer();
        }

        private void ShowForm(Form form)
        {
            this.Hide();
            form.Show();
            form.FormClosed += Form_FormClosed;
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Show();
        }

        private void buttonClient_Click(object sender, EventArgs e)
        {
            LaunchClient();
        }

        private void buttonServiceClient_Click(object sender, EventArgs e)
        {
            LaunchServiceClient();
        }

        private void LaunchClient(IPEndPoint autoConnectHost = null)
        {
            ShowForm(new ClientForm());
        }

        private void LaunchServer(int autoStartPort = 0)
        {
            ShowForm(new ServerForm(autoStartPort));
        }

        private void LaunchServiceClient() 
        {
            ShowForm(new ServiceClientForm());
        }

        private void RelaunchAsAdministrator()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                Verb = "runas",
                UseShellExecute = true,
                FileName = @"inputsharewindows.exe",
                Arguments = "startserviceclient",
            };

            Process.Start(info);
            Process.GetCurrentProcess().Kill();
        }

        private bool IsAdministrator()
        {
            using (WindowsIdentity id = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal p = new WindowsPrincipal(id);
                List<Claim> claims = new List<Claim>(p.UserClaims);
                Claim admin = claims.Find(p => p.Value.Contains("S-1-5-32-544"));
                return admin != null;
            }
        }
    }
}
