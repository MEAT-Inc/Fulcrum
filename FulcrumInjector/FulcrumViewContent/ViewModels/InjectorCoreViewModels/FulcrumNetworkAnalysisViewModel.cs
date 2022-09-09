using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using FulcrumInjector.FulcrumViewContent.Models.SimulationModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrapper;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

// Forced using calls for types
using TextBox = System.Windows.Controls.TextBox;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;


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
        private PassThruExecutionAction[] _j2534CommandQueue;

        // Public values for our View to bind onto
        public bool IsHardwareSetup { get => _isHardwareSetup; set => PropertyUpdated(value); }
        public string[] SupportedJ2534Commands { get => _supportedJ2534Commands; set => PropertyUpdated(value); } 
        public string CurrentJ2534CommandName { get => _currentJ2534CommandName; set => PropertyUpdated(value); }
        public PassThruExecutionAction[] J2534CommandQueue { get => _j2534CommandQueue; set => PropertyUpdated(value); }

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
                        ParamObj.ParameterType != typeof(PassThruStructs.PassThruMsg[])))
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
                if (new[] { "Int", "Uint", "String", "Byte" }.Any(ParameterType.FullName.Contains))
                {
                    // Build a new TextBox object to store input values
                    ViewModelLogger.WriteLog($"--> BUILDING TEXTBOX FOR VALUE TYPE {ParameterType.FullName} NOW...", LogType.WarnLog);
                    ParameterValueElement = new TextBox()
                    {
                        Style = _argumentValueTextBoxStyle,
                        Tag = $"Type: {ParameterType.FullName}",
                        Text = ParameterObject.HasDefaultValue ?
                            ParameterObject.DefaultValue.ToString() :
                            string.Empty,
                        ToolTip = ParameterObject.IsOptional
                            ? $"{ParameterName}: Optional Parameter!"
                            : $"{ParameterName}: Required Parameter"
                    };
                }
                else if (ParameterType.FullName.Contains("Bool"))
                {
                    // Build a checkbox object here
                    ViewModelLogger.WriteLog($"--> BUILDING CHECKBOX FOR VALUE TYPE {ParameterType.FullName} NOW...", LogType.WarnLog);
                    ParameterValueElement = new CheckBox()
                    {
                        IsChecked = true,
                        Tag = $"Type: {ParameterType.FullName}",
                        Style = _argumentValueComboBoxStyle,
                        ToolTip = $"{ParameterName}: Type of {ParameterType.Name}",
                    };
                }
                else if (ParameterType.IsEnum)
                {
                    // Build a new ComboBox with all the enum values we can pick from
                    ViewModelLogger.WriteLog($"--> BUILDING COMBOBOX FOR ENUM TYPE {ParameterType.FullName} NOW...", LogType.WarnLog);
                    ParameterValueElement = new ComboBox()
                    {
                        SelectedIndex = 0,
                        Tag = $"Type: {ParameterType.AssemblyQualifiedName}",
                        Style = this._argumentValueComboBoxStyle,
                        ItemsSource = Enum.GetValues(ParameterType),
                        ToolTip = $"{ParameterName}: Type of {ParameterType.Name}",
                    };
                }
                else if (ParameterType == typeof(PassThruStructs.PassThruMsg))
                {
                    // Log building new message object
                    ViewModelLogger.WriteLog($"--> BUILDING LISTBOX FOR J2534 PASSTHRU MESSAGES TYPE NOW...", LogType.WarnLog);

                    // Build a new grid containing fields for the message object to populate
                    Grid OutputContentGrid = new Grid();
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Message Data
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Message Flags
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Message Protocol

                    // Build field objects for the content grid
                    TextBox MessageValueTextBox = new TextBox()
                    {
                        Style = this._argumentValueTextBoxStyle,
                        Tag = $"Type: {typeof(string).FullName}",
                        ToolTip = $"Message Data: Required Parameter!"
                    };
                    ComboBox MessageFlagsComboBox = new ComboBox()
                    {
                        SelectedIndex = 0,
                        Tag = $"Type: {typeof(TxFlags).AssemblyQualifiedName}",
                        Style = this._argumentValueComboBoxStyle,
                        ItemsSource = Enum.GetNames(typeof(TxFlags)),
                        ToolTip = $"Tx Flags: Required Parameter!",
                    };
                    ComboBox MessageProtocolComboBox = new ComboBox()
                    {
                        SelectedIndex = 0,
                        Style = this._argumentValueComboBoxStyle,
                        Tag = $"Type: {typeof(ProtocolId).AssemblyQualifiedName}",
                        ItemsSource = Enum.GetNames(typeof(ProtocolId)),
                        ToolTip = $"Protocol ID: Required Parameter!",
                    };

                    // Set child object rows and store the grid
                    Grid.SetRow(MessageValueTextBox, 0);
                    Grid.SetRow(MessageFlagsComboBox, 1);
                    Grid.SetRow(MessageProtocolComboBox, 2);

                    // Store children and return the grid object
                    OutputContentGrid.Children.Add(MessageValueTextBox);
                    OutputContentGrid.Children.Add(MessageFlagsComboBox);
                    OutputContentGrid.Children.Add(MessageProtocolComboBox);

                    // Store the element as our out value
                    ParameterValueElement = OutputContentGrid;
                }
                else if (ParameterType == typeof(PassThruStructs.ISO15765ChannelDescriptor))
                {
                    // Log building new message object
                    ViewModelLogger.WriteLog($"--> BUILDING LISTBOX FOR J2534 CHANNEL DESCRIPTOR OBJECT TYPE NOW...", LogType.WarnLog);

                    // Build a new grid containing fields for the descriptor object to populate
                    Grid OutputContentGrid = new Grid();
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Local Address
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Remote Address
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Local Flags
                    OutputContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });    // Remote Flags

                    // Build field objects for the content grid
                    TextBox LocalAddressTextBox = new TextBox()
                    {
                        Style = this._argumentValueTextBoxStyle,
                        Tag = $"Type: {typeof(byte[]).FullName}",
                        ToolTip = $"Local Address: Required Parameter!"
                    };
                    TextBox RemoteAddressTextBox = new TextBox()
                    {
                        Style = this._argumentValueTextBoxStyle,
                        Tag = $"Type: {typeof(byte[]).FullName}",
                        ToolTip = $"Remote Address: Required Parameter!"
                    };
                    ComboBox LocalTxFlagsComboBox = new ComboBox()
                    {
                        SelectedIndex = 0,
                        Tag = $"Type: {typeof(PassThroughConnect).AssemblyQualifiedName}",
                        Style = this._argumentValueComboBoxStyle,
                        ItemsSource = Enum.GetNames(typeof(PassThroughConnect)),
                        ToolTip = $"Local Tx Flags: Required Parameter!",
                    };
                    ComboBox RemoteTxFlagsComboBox = new ComboBox()
                    {
                        SelectedIndex = 0,
                        Tag = $"Type: {typeof(PassThroughConnect).AssemblyQualifiedName}",
                        Style = this._argumentValueComboBoxStyle,
                        ItemsSource = Enum.GetNames(typeof(PassThroughConnect)),
                        ToolTip = $"Remote Tx Flags: Required Parameter!",
                    };

                    // Set child object rows and store the grid
                    Grid.SetRow(LocalAddressTextBox, 0);
                    Grid.SetRow(RemoteAddressTextBox, 1);
                    Grid.SetRow(LocalTxFlagsComboBox, 2);
                    Grid.SetRow(RemoteTxFlagsComboBox, 3);

                    // Store children and return the grid object
                    OutputContentGrid.Children.Add(LocalAddressTextBox);
                    OutputContentGrid.Children.Add(RemoteAddressTextBox);
                    OutputContentGrid.Children.Add(LocalTxFlagsComboBox);
                    OutputContentGrid.Children.Add(RemoteTxFlagsComboBox);

                    // Store the element as our out value
                    ParameterValueElement = OutputContentGrid;
                }

                // For all unknown casting types, just make a TextBox and mark an error was thrown
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
            ViewModelLogger.WriteLog($"TOTAL OF {OutputControls.Count} GRID SETS WERE BUILT FOR COMMAND {CommandName}", LogType.InfoLog);
            return OutputControls.ToArray();
        }
        /// <summary>
        /// Builds a given command name with the specified argument objects into an action to invoke
        /// </summary>
        /// <param name="CurrentArgValues">Arguments for the command</param>
        /// <param name="CommandName">Name of the command to invoke</param>
        internal PassThruExecutionAction GenerateCommandExecutionAction(object[] CurrentArgValues, string CommandName = null)
        {
            // Store the current command name if needed and build a new execution object
            CommandName ??= this.CurrentJ2534CommandName;

            // Parse and convert our objects in the input args array into the desired types for each argument
            List<object> CastArgumentValues = new List<object>();
            foreach (object[] ArgumentObject in CurrentArgValues)
            {
                // Get the string value of the argument and split it up
                List<object> ArgumentObjectValues = new List<object>();
                string[] ArgStringArray = ArgumentObject.Select(ArgObj => ArgObj.ToString()).ToArray();
                foreach (var ArgumentStringSet in ArgStringArray)
                {
                    // Store new values for the arg name, the arg type and the arg value
                    string ArgValueString = ArgumentStringSet.Split(':')[1].Split('-')[0].Trim();
                    string ArgTypeString = ArgumentStringSet.Split(':')[1].Split('-')[1].Trim();

                    // Now find the type from the type string and cast the argument string to that type.
                    Type ArgType = Type.GetType(ArgTypeString);
                    if (ArgType.IsEnum) ArgumentObjectValues.Add(Enum.Parse(ArgType, ArgValueString));
                    else if (ArgTypeString.Contains("String")) ArgumentObjectValues.Add(ArgValueString);
                    else if (ArgTypeString.Contains("Int")) ArgumentObjectValues.Add(Int32.Parse(ArgValueString));
                    else if (ArgTypeString.Contains("Bool")) ArgumentObjectValues.Add(Boolean.Parse(ArgValueString));
                    else if (ArgTypeString.Contains("Uint")) ArgumentObjectValues.Add(UInt32.Parse(ArgValueString.Replace("0x", string.Empty).Trim()));
                    else if (ArgTypeString.Contains("Byte"))
                    {
                        // Remove the 0x padding from the input if needed and split the bytes into an array
                        ArgValueString = ArgValueString.Replace("0x", string.Empty).Replace("  ", " ");
                        string[] SplitByteStrings = ArgValueString.Split(' ')
                            .Where(SplitPart => !string.IsNullOrWhiteSpace(SplitPart))
                            .ToArray();

                        // Convert each byte and return the output value
                        if (SplitByteStrings.Length == 1) ArgumentObjectValues.Add(Convert.ToByte(SplitByteStrings[0], 16));
                        else
                        {
                            // Build output byte array object to return our output
                            byte[] OutputBytes = new byte[SplitByteStrings.Length];
                            for (var ByteIndex = 0; ByteIndex < SplitByteStrings.Length; ByteIndex++)
                            {
                                var ByteString = SplitByteStrings[ByteIndex];
                                OutputBytes[ByteIndex] = Convert.ToByte(ByteString, 16);
                            }

                            // Store the output byte array to be returned
                            ArgumentObjectValues.Add(OutputBytes);
                        }
                    }
                    else
                    {
                        // If none of the above types, log this failure and append null
                        ViewModelLogger.WriteLog($"ERROR! UNKNOWN INPUT ARGUMENT TYPE OF {ArgType.FullName} WAS PROVIDED!");
                        ArgumentObjectValues.Add(null);
                    }
                }
                
                // Add the argument cast object to our list of output argument objects here assuming it's not a complex type
                if (ArgumentObjectValues.Count == 1) CastArgumentValues.Add(ArgumentObjectValues[0]);
                else
                {
                    // Get the arg types for the current command name and find the type for it.
                    MethodInfo CommandMethodInfo = typeof(Sharp2534Session).GetMethods()
                        .FirstOrDefault(MethodObj => MethodObj.Name == CommandName);

                    // Now find the parameter based on the index of our loop here
                    int IndexOfArgs = CurrentArgValues.ToList().IndexOf(ArgumentObject);
                    ParameterInfo CurrentParameterInfo = CommandMethodInfo.GetParameters()[IndexOfArgs];

                    // Store the type of the parameter and cast the object based on the type of it
                    Type CurrentParameterType = CurrentParameterInfo.ParameterType;
                    if (CurrentParameterType == typeof(PassThruStructs.PassThruMsg))
                    {
                        // Build a new J2534 message object from the arguments provided
                        ViewModelLogger.WriteLog($"GENERATING A NEW PASSTHRU MESSAGE FROM ARGUMENT OBJECTS NOW...", LogType.WarnLog);
                        var BuiltMessageObject = J2534Device.CreatePTMsgFromString(
                            (ProtocolId)ArgumentObjectValues[2],
                            (uint)ArgumentObjectValues[1],
                            ArgumentObjectValues[0].ToString()
                        );

                        // Store the generated message on our list of output
                        CastArgumentValues.Add(BuiltMessageObject);
                        ViewModelLogger.WriteLog("BUILT AND STORED NEW PASSTHRU MESSAGE OK! INFORMATION FOR IT IS BELOW", LogType.InfoLog);
                        ViewModelLogger.WriteLog($"--> MESSAGE DATA BUILT: {BuiltMessageObject.DataToHexString()}");
                        ViewModelLogger.WriteLog($"--> MESSAGE PROTOCOL: {BuiltMessageObject.ProtocolId}");
                        ViewModelLogger.WriteLog($"--> MESSAGE FLAGS: {BuiltMessageObject.TxFlags}");
                    }
                    else if (CurrentParameterType == typeof(PassThruStructs.ISO15765ChannelDescriptor))
                    {
                        // Build a new J2534 descriptor object from the arguments provided
                        ViewModelLogger.WriteLog($"GENERATING A NEW ISO15765 DESCRIPTOR FROM ARGUMENT OBJECTS NOW...", LogType.WarnLog);
                        var BuiltDescriptorObject = new PassThruStructs.ISO15765ChannelDescriptor(
                            (byte[])ArgumentObjectValues[0],
                            (byte[])ArgumentObjectValues[1],
                            (uint)ArgumentObjectValues[2],
                            (uint)ArgumentObjectValues[3]
                        );

                        // Store the generated message on our list of output
                        CastArgumentValues.Add(BuiltDescriptorObject);
                        ViewModelLogger.WriteLog("BUILT AND STORED NEW DESCRIPTOR OBJECT OK! INFORMATION FOR IT IS BELOW", LogType.InfoLog);
                        ViewModelLogger.WriteLog($"--> LOCAL ADDRESS BUILT: {BuiltDescriptorObject.LocalAddress}");
                        ViewModelLogger.WriteLog($"--> REMOTE ADDRESS BUILT: {BuiltDescriptorObject.RemoteAddress}");
                        ViewModelLogger.WriteLog($"--> LOCAL MESSAGE FLAGS: {BuiltDescriptorObject.LocalTxFlags}");
                        ViewModelLogger.WriteLog($"--> REMOTE MESSAGE FLAGS: {BuiltDescriptorObject.RemoteTxFlags}");
                    }
                    else
                    {
                        // Throw a new exception showing we have an invalid argument type here
                        ViewModelLogger.WriteLog($"ERROR! UNKNOWN INPUT ARGUMENT WAS PROVIDED FOR CREATION FROM A LISTBOX OBJECT!", LogType.ErrorLog);
                        CastArgumentValues.Add(null);
                    }
                }
            }

            // Build the new command action object here and store it
            PassThruExecutionAction CommandAction = new PassThruExecutionAction(
                FulcrumConstants.SharpSessionAlpha, CommandName, 
                CastArgumentValues.ToArray()
            );

            // Log built new action object and the list of arguments built for that action type
            ViewModelLogger.WriteLog($"BUILT NEW ACTION OBJECT FOR COMMAND {CommandName}!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"LOGGING THE LIST OF ARGUMENT OBJECTS FOR THIS COMMAND NOW...\n{CommandAction.CommandArgumentsString}");

            // Configure the action to actually invoke for our execution routine
            return CommandAction;
        }
    }
}
