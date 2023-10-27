using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using FulcrumService;

namespace FulcrumDriveService
{
    internal static class DriveServiceInit
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // Build and run a new service instance
            ServiceBase.Run(new FulcrumDrive());
        }
    }
}
