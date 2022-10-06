using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumNetworkAnalysisView.xaml
    /// </summary>
    public partial class FulcrumNetworkAnalysisView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorNetworkAnalysisViewLogger")) ?? new SubServiceLogger("InjectorNetworkAnalysisViewLogger");

        // ViewModel object to bind onto
        public FulcrumNetworkAnalysisViewModel ViewModel { get; set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our simulation playback
        /// </summary>
        public FulcrumNetworkAnalysisView()
        {
            // Initialize new UI Component
            InitializeComponent();
            this.ViewModel = FulcrumConstants.FulcrumNetworkAnalysisViewModel ?? new FulcrumNetworkAnalysisViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumNetworkAnalysisView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Processes events for when the index of our command object is changed.
        /// </summary>
        /// <param name="Sender">Combobox sending this request</param>
        /// <param name="E">Events fired along with the combobox changed</param>
        private void PassThruCommandComboBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Start by checking the index. If it's 0 then disable our execute buttons
            ComboBox SendingComboBox = (ComboBox)Sender;

            // Generate our control set for the selected command object
            this.ViewLogger.WriteLog("SETTING UP COMMAND CONFIG VALUES NOW...", LogType.InfoLog);
            this.ViewModel.CurrentJ2534CommandName = (SharpSessionCommandType)Enum.Parse(
                typeof(SharpSessionCommandType),
                SendingComboBox.SelectedItem.ToString()
            );

            // Store the controls on our items collection inside the viewer object now
            PassThruCommandArgsViewer.ItemsSource = this.ViewModel.GenerateCommandConfigElements();
            this.ViewLogger.WriteLog($"STORED NEW COMMAND NAME: {this.ViewModel.CurrentJ2534CommandName}", LogType.InfoLog);
            this.ViewLogger.WriteLog($"BUILT A TOTAL OF {PassThruCommandArgsViewer.Items.Count} CONTROL SETS FOR OUR COMMAND CONFIG!");
            this.ViewLogger.WriteLog("STORED CONTROLS OK! CONTENT SHOULD BE DISPLAYED ON OUR VIEW NOW...", LogType.InfoLog);
        }
        /// <summary>
        /// Processes a command execution request for the network testing view
        /// </summary>
        /// <param name="Sender">Sending button for this command</param>
        /// <param name="E">Event args fired along with the button click action</param>
        private void ExecuteOrQueuePassThruCommand_Click(object Sender, RoutedEventArgs E)
        {
            // Toggle the sending button to be disabled when the button is clicked
            Button SendingButton = (Button)Sender;
            SendingButton.IsEnabled = false;

            // Log extracting controls here and get our child values now
            this.ViewLogger.WriteLog("PREPARING TO STORE CONTROL VALUES FROM COMMAND ARGS VIEW NOW...", LogType.WarnLog);

            // Stop the vehicle monitoring routine on the Connection View if it's currently running
            bool ShouldMonitor = false;
            if (SendingButton.Content.ToString().Contains("Execute")) 
            {
                // If the sending button contains Execute, then we need to toggle our monitoring routines.
                ShouldMonitor = FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.IsMonitoring;
                if (ShouldMonitor) FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.StopVehicleMonitoring();
                this.ViewLogger.WriteLog("TOGGLED HARDWARE MONITORING VALUE IF NEEDED!", LogType.InfoLog);
            }

            // Store default button values
            Brush DefaultBackground = SendingButton.Background;
            string DefaultContent = SendingButton.Content.ToString();

            // Get the control objects on the view for our arguments and store their values here
            List<object> CurrentArgValues = new List<object>();
            foreach (var ControlObject in this.PassThruCommandArgsViewer.Items)
            {
                // Log the control type
                this.ViewLogger.WriteLog($"--> CONTROL FOUND! TYPE OF {ControlObject.GetType().Name}");
                if (ControlObject.GetType() != typeof(Grid)) {
                    this.ViewLogger.WriteLog("--> NOT FINDING VALUES OF CHILD OBJECTS FOR CONTROL SINCE IT WAS NOT A GRID!", LogType.InfoLog);
                    continue;
                }

                // Cast to a child grid and store the next set of values
                Grid CastChildGrid = (Grid)ControlObject;
                CurrentArgValues.Add(this._extractContentsForControl(CastChildGrid, out bool MissingRequired));
                this.ViewLogger.WriteLog($"   --> ADDED VALUES FOR GRID CONTROL WITHOUT ISSUES!", LogType.InfoLog);

                // Check missing required
                if (!MissingRequired) continue;
                this.ViewLogger.WriteLog("ERROR! MISSING A REQUIRED ARGUMENT! SETTING SENDING BUTTON TO SHOW THIS INFORMATION!", LogType.ErrorLog);

                // If there's no value for a required parameter, then return out of here and update the button
                SendingButton.Background = Brushes.Red;
                SendingButton.Content = "Set All Arguments!";
                SendingButton.Click -= this.ExecuteOrQueuePassThruCommand_Click;
                SendingButton.IsEnabled = true;

                // Wait for 2.5 seconds and reset the button
                Task.Run(() =>
                {
                    Thread.Sleep(2500);
                    Dispatcher.Invoke(() =>
                    {
                        // Reset the button here
                        SendingButton.Content = DefaultContent;
                        SendingButton.Background = DefaultBackground;
                        SendingButton.Click += this.ExecuteOrQueuePassThruCommand_Click;
                    });
                });

                // Exit out of this routine if we got here
                if (ShouldMonitor) FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.StopVehicleMonitoring();
                this.ViewLogger.WriteLog("TOGGLED HARDWARE MONITORING VALUE IF NEEDED!", LogType.InfoLog);
                return;
            }

            // Using this list of controls, invoke the current method using a sharp session object on the view model.
            this.ViewLogger.WriteLog("GENERATING EXECUTION ACTION FOR COMMAND NOW...", LogType.InfoLog);
            var GeneratedCommand = this.ViewModel.GenerateCommandExecutionAction(CurrentArgValues.ToArray());

            // Execute the action if needed
            if (SendingButton.Content.ToString().Contains("Execute"))
            {                
                // Log execution started, get the newest command entry and execute it.
                this.ViewLogger.WriteLog("EXECUTING NEXT COMMAND OBJECT NOW...", LogType.InfoLog);
                bool ExecutionResult = GeneratedCommand.ExecuteCommandAction();

                // Update sending button based on execution results
                SendingButton.Background = ExecutionResult ? Brushes.DarkGreen : Brushes.Red;
                SendingButton.Content = $"Execution {(ExecutionResult ? "Passed" : "Failed")}!";
                SendingButton.Click -= this.ExecuteOrQueuePassThruCommand_Click;
                SendingButton.IsEnabled = true;
            }
            else
            {
                // If we're not executing, then just set the button to show command building passed
                SendingButton.Background = Brushes.DarkGreen;
                SendingButton.Content = "Processed OK!";
                SendingButton.Click -= this.ExecuteOrQueuePassThruCommand_Click;
                SendingButton.IsEnabled = true;
            }

            // Wait for 2.5 seconds and reset the button
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                Dispatcher.Invoke(() =>
                {
                    // Reset the button here
                    SendingButton.Content = DefaultContent;
                    SendingButton.Background = DefaultBackground;
                    SendingButton.Click += this.ExecuteOrQueuePassThruCommand_Click;
                });
            });

            // Toggle monitoring if needed
            if (ShouldMonitor) FulcrumConstants.FulcrumVehicleConnectionInfoViewModel.StopVehicleMonitoring();
            this.ViewLogger.WriteLog("TOGGLED HARDWARE MONITORING VALUE IF NEEDED!", LogType.InfoLog);

            // Reenable the sending button here
            SendingButton.IsEnabled = true;
        }
        /// <summary>
        /// Toggles our flyout for the command execution queue to show the user what commands are being queued
        /// </summary>
        /// <param name="Sender">Sending button</param>
        /// <param name="E">Event args passed with this click event</param>
        private void ToggleCommandQueueFlyout_Click(object Sender, RoutedEventArgs E)
        {
            // Get the current flyout state and toggle it.
            this.CommandQueueFlyout.IsOpen = !this.CommandQueueFlyout.IsOpen;
            this.ViewLogger.WriteLog($"TOGGLED EXECUTION QUEUE FLYOUT OK! IS OPEN VALUE IS NOW {this.CommandQueueFlyout.IsOpen}");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loops the child elements of a grid and pulls out the values of each child
        /// </summary>
        /// <param name="ChildGrid">Grid to loop the children of</param>
        /// <param name="MissingRequired">Sets if this child grid is missing one or more required paramaters</param>
        /// <returns>An array of strings showing the values of the controls in the grid</returns>
        private object[] _extractContentsForControl(Grid ChildGrid, out bool MissingRequired)
        {
            // Default missing value to false
            MissingRequired = false;

            // Loop the children and find the values. Store them in this list
            List<object> OutputGridValues = new List<object>();
            foreach (var ChildElement in ChildGrid.Children)
            {
                // Find the element type and store the values of it
                if (ChildElement.GetType() == typeof(Grid))
                    OutputGridValues.AddRange(this._extractContentsForControl((Grid)ChildElement, out MissingRequired).Select(ArgObj => ArgObj));
                else if (ChildElement.GetType() == typeof(TextBox) || ChildElement.GetType() == typeof(ComboBox) || ChildElement.GetType() == typeof(CheckBox))
                    OutputGridValues.Add(this._extractContentsForControl((UIElement)ChildElement, out MissingRequired));
                
                // Check if we're missing a required value or not.
                if (!MissingRequired) continue;
                this.ViewLogger.WriteLog("ERROR! ONE OR MORE CHILD GRID REQUIRED ARGUMENTS IS MISSING!", LogType.ErrorLog);
                return null;
            }
            
            // Cast our output objects to an array and return them
            return OutputGridValues.ToArray();
        }
        /// <summary>
        /// Pulls out the value for a UI Control of type TextBox or ComboBox
        /// </summary>
        /// <param name="ChildElement">Element to pull value out of</param>
        /// <param name="MissingRequired">Sets if the value of the required argument is not specified</param>
        /// <returns>The value of the argument object or null if no value found</returns>
        private string _extractContentsForControl(UIElement ChildElement, out bool MissingRequired)
        {
            // Check if it's not a TextBox, ComboBox, or CheckBox If it isn't return null
            MissingRequired = true;
            if (ChildElement.GetType() != typeof(TextBox) && 
                ChildElement.GetType() != typeof(ComboBox) && 
                ChildElement.GetType() != typeof(CheckBox))
                return null;

            // Get the value and store it as a cast control to extract the value of it.
            switch (ChildElement)
            {
                case TextBox TextBoxControl:
                {
                    // To Determine if it's a required value or not, check the tag of the textbox
                    string ChildCastTextBoxText = TextBoxControl.Text.Trim();
                    string NameOfArgument = TextBoxControl.ToolTip.ToString().Split(':')[0].Trim();
                    if (TextBoxControl.ToolTip.ToString().Contains("Required") && string.IsNullOrEmpty(ChildCastTextBoxText))
                    {
                        // Log missing required argument and return null/set the missing flag to true
                        this.ViewLogger.WriteLog("--> ERROR! REQUIRED ARGUMENT IS MISSING A VALUE!", LogType.ErrorLog);
                        MissingRequired = true;
                        return null;
                    }

                    // Get the tag value and add it to our output type
                    string ArgTypeString = TextBoxControl.Tag.ToString().Split(':')[1].Trim();
                    this.ViewLogger.WriteLog($"--> FOUND TYPE STRING VALUE FOR CONTROL TO BE {ArgTypeString}");

                    // Add the text value and log it out
                    MissingRequired = false;
                    this.ViewLogger.WriteLog($"--> CHILD TEXTBOX CONTROL VALUE PULLED: {NameOfArgument}: {ChildCastTextBoxText}");
                    return $"{NameOfArgument}: {ChildCastTextBoxText} - {ArgTypeString}";
                }
                case CheckBox CheckBoxControl:
                {
                    // Get the CheckBox Value and store it
                    string ChildCastCheckBoxBoxText = (bool)CheckBoxControl.IsChecked ? "True" : "False";
                    string NameOfArgument = CheckBoxControl.ToolTip.ToString().Split(':')[0].Trim();
                    this.ViewLogger.WriteLog($"--> CHILD CHECKBOX CONTROL VALUE PULLED: {NameOfArgument}: {ChildCastCheckBoxBoxText}");

                    // Get the tag value and add it to our output type
                    MissingRequired = false;
                    string ArgTypeString = CheckBoxControl.Tag.ToString().Split(':')[1].Trim();
                    this.ViewLogger.WriteLog($"--> FOUND TYPE STRING VALUE FOR CONTROL TO BE {ArgTypeString}");

                    // Return the Name, value, and type as a string array
                    return $"{NameOfArgument}: {ChildCastCheckBoxBoxText} - {ArgTypeString}";
                }
                case ComboBox ComboBoxControl:
                {
                    // Get the ComboBox Value and store it
                    string ChildCastComboBoxBoxText = ComboBoxControl.SelectedItem.ToString().Trim();
                    string NameOfArgument = ComboBoxControl.ToolTip.ToString().Split(':')[0].Trim();
                    this.ViewLogger.WriteLog($"--> CHILD COMBOBOX CONTROL VALUE PULLED: {NameOfArgument}: {ChildCastComboBoxBoxText}");

                    // Get the tag value and add it to our output type
                    MissingRequired = false;
                    string ArgTypeString = ComboBoxControl.Tag.ToString().Split(':')[1].Trim();
                    this.ViewLogger.WriteLog($"--> FOUND TYPE STRING VALUE FOR CONTROL TO BE {ArgTypeString}");

                    // Return the Name, value, and type as a string array
                    return $"{NameOfArgument}: {ChildCastComboBoxBoxText} - {ArgTypeString}";
                }
                default:
                    // For all other control types/unknown controls fail out
                    throw new InvalidOperationException("INVALID CONTROL TYPE IDENTIFIED! FAILED TO PULL IN ONE OR MORE ARGS!");
            }
        }
    }
}
