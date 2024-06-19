using System.ServiceProcess;
using System.Threading;

namespace GxPService
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
#if DEBUG
        new GxPService().OnDebug(args);
        Thread.Sleep(Timeout.Infinite);
#else
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[]
        {
            new GxPService()
        };
        ServiceBase.Run(ServicesToRun);
#endif


        }
    }
}
