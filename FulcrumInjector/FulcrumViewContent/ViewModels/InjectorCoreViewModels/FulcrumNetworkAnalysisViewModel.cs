using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewContent.Models.SimulationModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels
{
    /// <summary>
    /// View model logic for our Peer to Peer network configuration routines on the Injector app
    /// </summary>
    public class FulcrumNetworkAnalysisViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorNetworkAnalysisViewModelLogger")) ?? new SubServiceLogger("InjectorNetworkAnalysisViewModelLogger");

        // Private Control Values
        private string[] _supportedJ2534Commands;

        // Public values for our View to bind onto
        public string[] SupportedJ2534Commands { get => _supportedJ2534Commands; set => PropertyUpdated(value); }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        public FulcrumNetworkAnalysisViewModel()
        {
            // Setup empty list of our events here
            ViewModelLogger.WriteLog("BUILT NEW FULCRUM P2P VIEW MODEL OK!");
            ViewModelLogger.WriteLog("BUILT NEW CAN NETWORK ANALYSIS VIEW MODEL LOGGER AND INSTANCE OK!", LogType.InfoLog);

            // Store the command types we can issue using our API
            this.SupportedJ2534Commands = this.ConfigureSupportedCommandTypes();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets all the possible command types we can issue to our J2534 interface by using reflection on the
        /// Sharp2534 API
        /// </summary>
        /// <returns>A string array containing all the command types we can support and issue</returns>
        public string[] ConfigureSupportedCommandTypes()
        {
            // Begin by getting the method objects we can invoke and get their names.
            MethodInfo[] SharpSessionMethods = typeof(Sharp2534Session)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .ToArray();

            // Get the names of the methods and store them in an array
            Array.Sort(SharpSessionMethods, (methodInfo1, methodInfo2) => methodInfo1.Name.CompareTo(methodInfo2.Name));
            List<string> SharpSessionMethodNames = SharpSessionMethods.Select(MethodObject => MethodObject.Name).ToList();

            // Return the array of methods here with a "Select A Command" Option at the top
            SharpSessionMethodNames.Insert(0, "-- Select A PassThru Command --");
            return SharpSessionMethodNames.ToArray();
        }
    }
}
