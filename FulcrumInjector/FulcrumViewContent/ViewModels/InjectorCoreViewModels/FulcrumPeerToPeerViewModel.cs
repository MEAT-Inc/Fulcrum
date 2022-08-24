using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.SimulationModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// View model logic for our Peer to Peer network configuration routines on the Injector app
    /// </summary>
    public class FulcrumPeerToPeerViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorPeerToPeerViewModelLogger")) ?? new SubServiceLogger("InjectorPeerToPeerViewModelLogger");

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        public FulcrumPeerToPeerViewModel()
        {
            // Setup empty list of our events here
            ViewModelLogger.WriteLog("BUILT NEW FULCRUM P2P VIEW MODEL OK!");
            ViewModelLogger.WriteLog("BUILT NEW P2P CONFIGURATION VIEW MODEL LOGGER AND INSTANCE OK!", LogType.InfoLog);
        }
    }
}
