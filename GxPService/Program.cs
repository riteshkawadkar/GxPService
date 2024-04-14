using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace GxPService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if DEBUG
            new GxpService();
            //var myService = new MyService();
            //myService.ShowToastNotification("SUCCESS", "This is a success message");
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new GxpService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
