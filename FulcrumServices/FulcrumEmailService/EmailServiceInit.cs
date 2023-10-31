using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumEmailService
{
    internal static class EmailServiceInit
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // Build and store a new service instance
            FulcrumEmail ServiceInstance = new FulcrumEmail();

            // Either fire the start service routine or run the service instance here
            if (Debugger.IsAttached) ServiceInstance.StartService();
            else ServiceBase.Run(ServiceInstance);
        }
    }
}
