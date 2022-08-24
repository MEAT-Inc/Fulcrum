using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private bool _isHardwareSetup;
        private bool _canExecuteCommands;
        private string[] _supportedJ2534Commands;

        // Public values for our View to bind onto
        public bool IsHardwareSetup { get => _isHardwareSetup; set => PropertyUpdated(value); }
        public bool CanExecuteCommands { get => _canExecuteCommands; set => PropertyUpdated(value); }
        public string[] SupportedJ2534Commands { get => _supportedJ2534Commands; set => PropertyUpdated(value); }

        // Style objects for laying out view contents
        private readonly ResourceDictionary _viewStyleResources;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        public FulcrumNetworkAnalysisViewModel()
        {
            // Setup empty list of our events here
            ViewModelLogger.WriteLog("BUILT NEW FULCRUM P2P VIEW MODEL OK!");
            ViewModelLogger.WriteLog("BUILT NEW CAN NETWORK ANALYSIS VIEW MODEL LOGGER AND INSTANCE OK!", LogType.InfoLog);

            // Import our view styles
            string CurrentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string ResourceFileUri = $"/{CurrentAssemblyName};component/FulcrumViewSupport/FulcrumStyles/AppStyleSheets/FulcrumNetworkAnalysisStyles.xaml";
            ResourceDictionary MergedStyleSheet = (ResourceDictionary)Application.LoadComponent(new Uri(ResourceFileUri, UriKind.Relative));
            this._viewStyleResources = MergedStyleSheet.MergedDictionaries[1];
            ViewModelLogger.WriteLog("LOADED VIEW STYLES FOR OUR NETWORK ANALYSIS VIEW CONTENT OK!", LogType.InfoLog);

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
            ViewModelLogger.WriteLog("GETTING REFLECTED METHOD INFORMATION FOR OUR SHARPSESSION TYPE NOW...");
            MethodInfo[] SharpSessionMethods = typeof(Sharp2534Session)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            // Get the names of the methods and store them in an array
            ViewModelLogger.WriteLog($"FOUND A TOTAL OF {SharpSessionMethods.Length} UNFILTERED METHOD OBJECTS", LogType.TraceLog);
            string[] SharpSessionMethodNames = SharpSessionMethods
                .Select(MethodObject => MethodObject.Name)
                .Where(MethodName => MethodName.StartsWith("PT"))
                .ToArray();

            // Return the array of methods here with a "Select A Command" Option at the top
            ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {SharpSessionMethodNames.Length} METHOD NAMES WHICH WE CAN EXECUTE ON OUR VIEW!", LogType.InfoLog);
            return SharpSessionMethodNames;
        }
        /// <summary>
        /// Builds a set of UI controls to use for populating our command object based on the args of the method
        /// </summary>
        /// <param name="CommandName">Name of the command being pulled from our list of supported commands</param>
        /// <returns>The command configuration objects built for us to setup our command</returns>
        public UIElement[] GenerateCommandConfigElements(string CommandName)
        {
            // Start by getting the method information for the command object
            MethodInfo CommandMethodInfo = typeof(Sharp2534Session).GetMethods()
                .FirstOrDefault(MethodObj => MethodObj.Name == CommandName);

            // Ensure the command method info is not null
            if (CommandMethodInfo == null) {
                ViewModelLogger.WriteLog($"ERROR! COMMAND {CommandName} WAS NOT FOUND AS A VALID COMMAND OBJECT ON A SHARPSESSION!", LogType.ErrorLog);
                return null;
            }

            // Get the method information for the command here
            ParameterInfo[] CommandParameters = CommandMethodInfo.GetParameters();
            ViewModelLogger.WriteLog($"FOUND COMMAND INFORMATION FOR COMMAND {CommandName} OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"DISSECTED METHOD AND EXTRACTED A TOTAL OF {CommandParameters.Length} PARAMETERS WHICH NEED TO BE PASSED!", LogType.InfoLog);

            // Loop each parameter object and build a new UI control to add to our list of output
            List<UIElement> OutputControls = new List<UIElement>();
            foreach (var ParameterObject in CommandParameters)
            {
                // Find the name type of the parameter. Setup a grid which contains the name and a field to enter a value for it.
                string ParameterName = ParameterObject.Name;
                Type ParameterType = ParameterObject.ParameterType;
                ViewModelLogger.WriteLog($"--> PARAMETER {ParameterName} IS SEEN TO BE TYPE OF {ParameterType}", LogType.TraceLog);

                // Now build a new grid to store output controls on along with the width values for the columns.
                Grid ParameterGrid = new Grid();
                ParameterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(.5, GridUnitType.Star) });    
                ParameterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });

                // Build a new TextBlock to hold our title value and style it
                // Style ParameterNameStyle = 
                // TextBlock ParameterNameTextBlock = new TextBlock() { Text = ParameterName };
            }

            // Return the list of built elements
            return OutputControls.ToArray();
        }
    }
}
