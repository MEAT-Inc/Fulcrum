using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumLogic.PassThruWatchdog
{
    /// <summary>
    /// This class starts up a background refreshing routine that checks our Device Manager and returns out a J2534 interface
    /// if one is currently on our system. This will contain a list of currently connected devices and will be modified on the connect/disconnect
    /// events setup
    /// </summary>
    public class CheckDeviceManagerVciList
    {
    }
}
