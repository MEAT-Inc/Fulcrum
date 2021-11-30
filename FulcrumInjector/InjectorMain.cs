using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector
{
    /// <summary>
    /// Main class for fulcrum injector configuration application
    /// </summary>
    public class InjectorMain
    {
        /// <summary>
        /// Main entry point for the Fulcrum Injector configuration application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // First up, configure our new pipe servers for reading information.
            var PipeAlpha = new FulcrumPipeReader(FulcrumPipe.PipeAlpha);
            var PipeBravo = new FulcrumPipeReader(FulcrumPipe.PipeBravo);
        }
    }
}
