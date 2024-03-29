﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumUpdaterService
{
    internal static class UpdaterServiceInit
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // Build and store a new service instance
            FulcrumUpdater ServiceInstance = new FulcrumUpdater();

            // Either fire the start service routine or run the service instance here
            if (!Debugger.IsAttached) ServiceBase.Run(ServiceInstance);
            else
            {
                // Boot our service and wait forever
                ServiceInstance.StartService();
                while (true) continue;
            }
        }
    }
}
