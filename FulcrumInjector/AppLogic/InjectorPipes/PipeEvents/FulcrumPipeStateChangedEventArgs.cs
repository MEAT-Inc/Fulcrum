using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.AppLogic.InjectorPipes.PipeEvents
{
    /// <summary>
    /// Args for when the state of a pipe is modified
    /// </summary>
    public class FulcrumPipeStateChangedEventArgs : EventArgs
    {
        // Values for our new event args
        public DateTime TimeChanged;
        public FulcrumPipeState OldState;
        public FulcrumPipeState NewState;

        /// <summary>
        /// Builds our new instance of the event args
        /// </summary>
        public FulcrumPipeStateChangedEventArgs() { }
    }
}
