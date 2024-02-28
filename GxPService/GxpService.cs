using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace GxPService
{
    public partial class GxpService : ServiceBase
    {       
        Timer timer = new Timer();

        public GxpService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Helper.WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds
            timer.Enabled = true;

            new PolicyClient().ListenForPolicies();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            Helper.WriteToFile("Service is stopped at " + DateTime.Now);
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            Helper.WriteToFile("Service is recall at " + DateTime.Now);
        }

    }
}
