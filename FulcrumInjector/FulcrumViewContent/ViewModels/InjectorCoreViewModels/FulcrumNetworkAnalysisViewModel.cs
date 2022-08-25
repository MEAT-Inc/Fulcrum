using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models.SimulationModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

// Forced forms using for TreeView
using FormsTreeView = System.Windows.Forms.TreeView;

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
        private string[] _supportedJ2534Commands;
        private string _currentJ2534CommandName;
        private UIElement[] _currentJ2534CommandElements;

        // Public values for our View to bind onto
        public bool IsHardwareSetup { get => _isHardwareSetup; set => PropertyUpdated(value); }
        public string[] SupportedJ2534Commands { get => _supportedJ2534Commands; set => PropertyUpdated(value); }
        public string CurrentJ2534CommandName  { get => _currentJ2534CommandName; set => PropertyUpdated(value); }
        public UIElement[] CurrentJ2534CommandElements { get => _currentJ2534CommandElements; set => PropertyUpdated(value); }

        // Style objects for laying out view contents
        private readonly ResourceDictionary _viewStyleResources;
        private Style _noArgumentsNeededTextStyle => (Style)_viewStyleResources["GeneratedNoArgsNeededTextStyle"];
        private Style _argumentNameTextStyle => (Style)_viewStyleResources["GeneratedArgTitleTextStyle"];
        private Style _argumentNameTextStyleOptional => (Style)_viewStyleResources["GeneratedOptionalArgTitleTextStyle"];
        private Style _argumentValueTextBoxStyle => (Style)_viewStyleResources["GeneratedArgValueTextBoxStyle"];
        private Style _argumentValueComboBoxStyle => (Style)_viewStyleResources["GeneratedArgValueComboBoxStyle"];

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new simulation playback view model
        /// </summary>
        public FulcrumNetworkAnalysisViewModel()
        {
            // Store the command types we can issue using our API
            this._viewStyleResources = this._configureControlStyles();
            this.SupportedJ2534Commands = this._configureSupportedCommandTypes();

            // Log setup complete
            ViewModelLogger.WriteLog("BUILT NEW FULCRUM P2P VIEW MODEL OK!");
            ViewModelLogger.WriteLog("BUILT NEW CAN NETWORK ANALYSIS VIEW MODEL LOGGER AND INSTANCE OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a resource dictionary for our styles which are used to style the output controls for our generated UI Content
        /// </summary>
        /// <returns>The built resource dictionary for our styles</returns>
        private ResourceDictionary _configureControlStyles()
        {
            // Log building style sheet and begin setting up dictionaries
            ViewModelLogger.WriteLog("GENERATING STYLES FOR UI CONTROL GENERATION ROUTINES NOW...");

            // Get the current assembly name and the resource file URI for the styles
            string CurrentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string ResourceFileUri = $"/{CurrentAssemblyName};component/FulcrumViewSupport/FulcrumStyles/AppStyleSheets/FulcrumNetworkAnalysisStyles.xaml";

            // Now cast the URI Content to a ResourceDictionary and extract the dictionary with the styles for our controls here
            ResourceDictionary MergedStyleSheet = (ResourceDictionary)Application.LoadComponent(new Uri(ResourceFileUri, UriKind.Relative));
            ViewModelLogger.WriteLog($"LOCATED A TOTAL OF {MergedStyleSheet.MergedDictionaries.Count} MERGED DICTIONARIES FOR OUR INPUT STYLE SHEET", LogType.TraceLog);
            ResourceDictionary OutputStyleDictionary = MergedStyleSheet.MergedDictionaries.FirstOrDefault(MergedDict =>
            {
                // Get all the keys of our dictionary here and check if any exist
                var ResourceKeys = MergedDict.Keys;
                if (ResourceKeys.Count == 0) return false;

                // Loop the keys and validate the names of each one
                foreach (var KeyObject in ResourceKeys) 
                    if (!KeyObject.ToString().StartsWith("Generated")) return false;

                // Return true if there are keys found and all contain Generated in the names
                return true;
            });

            // Log passed and return the built dictionary
            ViewModelLogger.WriteLog($"IMPORTED A TOTAL OF {OutputStyleDictionary.Keys.Count} STYLE KEYS TO USE FOR GENERATED CONTENT!", LogType.TraceLog);
            ViewModelLogger.WriteLog("LOADED VIEW STYLES FOR OUR NETWORK ANALYSIS VIEW CONTENT OK!", LogType.InfoLog);
            return OutputStyleDictionary;
        }
        /// <summary>
        /// Gets all the possible command types we can issue to our J2534 interface by using reflection on the
        /// Sharp2534 API
        /// </summary>
        /// <returns>A string array containing all the command types we can support and issue</returns>
        private string[] _configureSupportedCommandTypes()
        {
            // Begin by getting the method objects we can invoke and get their names.
            ViewModelLogger.WriteLog("GETTING REFLECTED METHOD INFORMATION FOR OUR SHARPSESSION TYPE NOW...");
            MethodInfo[] SharpSessionMethods = typeof(Sharp2534Session)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            // Get the names of the methods and store them in an array ONLY for commands where a J2534 object is not defined
            ViewModelLogger.WriteLog($"FOUND A TOTAL OF {SharpSessionMethods.Length} UNFILTERED METHOD OBJECTS", LogType.TraceLog);
            string[] SharpSessionMethodNames = SharpSessionMethods
                .Where(MethodObject => MethodObject.GetParameters()
                    .All(ParamObj => 
                        ParamObj.ParameterType != typeof(J2534Filter) &&
                        ParamObj.ParameterType != typeof(J2534PeriodicMessage) &&
                        ParamObj.ParameterType == typeof(PassThruStructs.PassThruMsg[])))
                .Select(MethodObject => MethodObject.Name)
                .Where(MethodName => MethodName.StartsWith("PT"))
                .ToArray();

            // Return the array of methods here with a "Select A Command" Option at the top
            ViewModelLogger.WriteLog($"PULLED IN A TOTAL OF {SharpSessionMethodNames.Length} METHOD NAMES WHICH WE CAN EXECUTE ON OUR VIEW!", LogType.InfoLog);
            return SharpSessionMethodNames;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a set of UI controls to use for populating our command object based on the args of the method
        /// </summary>
        /// <param name="CommandName">Name of the command being pulled from our list of supported commands</param>
        /// <returns>The command configuration objects built for us to setup our command</returns>
        internal UIElement[] GenerateCommandConfigElements(string CommandName = null)
        {
            // If the command name is null, default to the one on our class
            CommandName ??= this.CurrentJ2534CommandName;
            if (CommandName == null) return null;

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

            // Build a list to hold our output command arg grids 
            List<UIElement> OutputControls = new List<UIElement>();
            if (CommandParameters.Length == 0 || CommandParameters.All(ParamObj => ParamObj.IsOut))
            {
                // Log no args needed and build a placeholder box.
                ViewModelLogger.WriteLog($"NO ARGUMENTS NEED TO BE CONFIGURED FOR COMMAND {CommandName}! RETURNING A GRID WITH THIS INFORMATION!", LogType.WarnLog);

                // Build a new TextBlock that just shows us there are no arguments to configure and style it accordingly
                TextBlock NoArgsTextBlock = new TextBlock() { Style = this._noArgumentsNeededTextStyle, Text = $"No arguments for command {CommandName}!" };
                OutputControls.Add(NoArgsTextBlock);

                // Store the command arg objects if they match up to the view model command name amd return output
                this.CurrentJ2534CommandElements = CommandName == this.CurrentJ2534CommandName ? OutputControls.ToArray() : null;
                return OutputControls.ToArray();
            }

            // Loop each parameter object and build a new UI control to add to our list of output
            foreach (var ParameterObject in CommandParameters)
            {
                // Find the name type of the parameter. Setup a grid which contains the name and a field to enter a value for it.
                string ParameterName = ParameterObject.Name;
                Type ParameterType = ParameterObject.ParameterType;
                string SplitCamelCaseName = Regex.Replace(ParameterName, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
                ViewModelLogger.WriteLog($"--> PARAMETER {SplitCamelCaseName} IS SEEN TO BE TYPE OF {ParameterType}", LogType.TraceLog);

                // If the parameter is an OUT parameter, then don't add anything here
                if (ParameterObject.IsOut) {
                    ViewModelLogger.WriteLog($"--> PARAMETER {ParameterObject.Name} IS AN OUT PARAMETER AND IS NOT BEING GIVEN AN INPUT CONTROL", LogType.WarnLog);
                    continue;
                }

                // Now build a new grid to store output controls on along with the width values for the columns.
                Grid ParameterGrid = new Grid();
                ParameterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(.65, GridUnitType.Star) });    
                ParameterGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });

                // Build a new TextBlock to hold our title value and style it
                TextBlock ParameterNameTextBlock = new TextBlock()
                {
                    Text = SplitCamelCaseName,
                    Style = ParameterObject.IsOptional 
                        ? _argumentNameTextStyleOptional
                        : this._argumentNameTextStyle
                };

                // Check the type of the parameter. Assign our value control accordingly.
                UIElement ParameterValueElement = null;
                if (ParameterType.FullName.Contains("Int") || ParameterType.FullName.Contains("String"))
                {
                    // Build a new TextBox object to store input values
                    ParameterValueElement = new TextBox()
                    {
                        Style = this._argumentValueTextBoxStyle,
                        Tag = $"Type: {ParameterType.FullName}",
                        ToolTip = ParameterObject.IsOptional
                            ? "Optional Parameter!"
                            : "Required Parameter"
                    };
                }
                else if (ParameterType.IsEnum)
                {
                    // Build a new ComboBox with all the enum values we can pick from
                    ViewModelLogger.WriteLog($"--> BUILDING COMBOBOX FOR ENUM TYPE {ParameterType.FullName} NOW...", LogType.WarnLog);
                    ParameterValueElement = new ComboBox()
                    {
                        Style = this._argumentValueComboBoxStyle,
                        ItemsSource = Enum.GetValues(ParameterType),
                        SelectedIndex = 0
                    };
                }
                else if (ParameterType == typeof(PassThruStructs.PassThruMsg))
                {
                    // TODO: BUILD LOGIC FOR J2534 MESSAGE TYPES AND ARRAYS
                    ViewModelLogger.WriteLog($"--> BUILDING LISTBOX FOR J2534 PASSTHRU MESSAGES TYPE NOW...", LogType.WarnLog);
                }

                // For all unknown casting types, just make a TextBox
                if (ParameterValueElement == null) {
                    ViewModelLogger.WriteLog($"--> ERROR! TYPE FOR INPUT PARAMETER WAS NOT VALID! TYPE {ParameterType} WAS NOT ASSIGNABLE!", LogType.ErrorLog);
                    ParameterValueElement = new TextBox() { Style = this._argumentValueTextBoxStyle, Tag = $"Generation Error!", ToolTip = $"Error! {ParameterType} was seen to be invalid!" };
                }
                
                // Set our column locations and store the child values
                Grid.SetColumn(ParameterNameTextBlock, 0); Grid.SetColumn(ParameterValueElement, 1);
                ParameterGrid.Children.Add(ParameterNameTextBlock); ParameterGrid.Children.Add(ParameterValueElement);

                // Add the grid to our list of output controls and return them
                OutputControls.Add(ParameterGrid);
                ViewModelLogger.WriteLog($"--> STORED NEW CHILD CONTROLS FOR PARAMETER {ParameterName} OK!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"--> TOTAL OF {OutputControls.Count} GRID SETS HAVE BEEN GENERATED SO FAR", LogType.TraceLog);
            }

            // Store the command arg objects if they match up to the view model command name
            this.CurrentJ2534CommandElements = CommandName == this.CurrentJ2534CommandName ? OutputControls.ToArray() : null;
            ViewModelLogger.WriteLog($"TOTAL OF {OutputControls.Count} GRID SETS WERE BUILT FOR COMMAND {CommandName}", LogType.InfoLog);
            return OutputControls.ToArray();
        }
    }
}
