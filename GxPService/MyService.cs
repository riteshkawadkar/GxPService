using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GxPService
{
    internal class MyService
    {
        public void ShowToastNotification(string type, string message)
        {
            // Path to the executable of your Windows Form application
            string appPath = @"C:\Users\rites\source\repos\ToastNotification\ToastNotification\bin\Debug\ToastNotification.exe";

            // Arguments to pass to the application (type and message)
            string arguments = $"{type} \"{message}\"";

            // Start the process
            Process.Start(appPath, arguments);
        }
    }
}
