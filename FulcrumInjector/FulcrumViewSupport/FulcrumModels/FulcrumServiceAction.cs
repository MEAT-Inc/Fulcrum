using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels
{
    /// <summary>
    /// Model object holding the definition for a service action to queue on our current machine
    /// </summary>
    public class FulcrumServiceAction
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Private backing fields for our service action configuration
        private string _actionName;         // Name of the action being invoked
        private TimeSpan _actionTime;       // The DateTime value for the action being invoked
        private string _actionScriptFile;   // The NAME of the file being used to invoke commands
        private string[] _actionCommands;   // The ACTIONS to invoke for this service action object

        #endregion // Fields

        #region Properties

        // Public facing properties holding information about the service action
        public string ActionName
        {
            private set => this._actionName = value;
            get => $"{this._actionName}_{this.ActionGuid.ToString("D").ToUpper()}";
        }
        public DateTime ActionTime
        {
            private set => _actionTime = value.TimeOfDay;
            get
            {
                // Check our timing type value first. If it's not timed, return a default date time value
                if (this.ActionTiming != ActionTimings.TIMED) return new DateTime(0, 0, 0);
                    
                // Pull our date time value from the backing field and update it to reflect today's date
                DateTime CurrentTime = DateTime.Now;
                DateTime TimeForToday = new DateTime(
                    CurrentTime.Year, CurrentTime.Month, CurrentTime.Day,
                    this._actionTime.Hours, this._actionTime.Minutes, this._actionTime.Seconds);

                // Return the built DateTime
                return TimeForToday;
            }
        }
        public Guid ActionGuid { get; private set; }
        public ActionTimings ActionTiming { get; private set; }

        // Public facing property holding our service action to invoke
        public string ActionScriptFile
        {
            get
            {
                // Pull the path for our injector tasks folder and combine it with our file name
                string TasksFolder = ValueLoaders.GetConfigValue<string>("FulcrumConstants.InjectorResources.FulcrumTasksPath");
                string BuiltCommandFilePath = Path.Combine(TasksFolder, this._actionScriptFile);

                // Return the built command file path
                return BuiltCommandFilePath;
            }
            private set
            {
                // Pull JUST the name for a file being provided and store it
                string FileNameOnly = Path.GetFileName(value);
                this._actionScriptFile = FileNameOnly;
            }
        }
        public string[] ActionCommands
        {
            // Try and read the content of our script file first. Otherwise return our local content
            get => File.Exists(this.ActionScriptFile) ? File.ReadAllLines(this.ActionScriptFile) : this._actionCommands;
            private set
            {
                // When we're setting the contents of our script action, we need to build our script file here
                string SplittingString = string.Join("", Enumerable.Repeat("#", 100));
                List<string> ScriptContents = new List<string>()
                {
                    SplittingString,
                    $"# Script:         {this.ActionName}",
                    $"# Script GUID:    {this.ActionGuid.ToString("D").ToUpper()}",
                    $"# Script Timing:  {this.ActionTiming.ToDescriptionString()}",
                    $"# Execution Time: {this.ActionTime:HH:mm:ss}",
                    SplittingString
                };

                // Store the commands into the script information and return out
                ScriptContents.AddRange(value.Select(ScriptLine => ScriptLine.TrimEnd()));
                this._actionCommands = ScriptContents.Select(ScriptLine => ScriptLine + "\n").ToArray();

                // Finally write our script file out so we can execute it later on
                File.WriteAllLines(this.ActionScriptFile, this.ActionCommands);
            }
        }

        #endregion // Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration holding our requested times/timings for execution of an action
        /// </summary>
        public enum ActionTimings
        {
            [Description("Manual")] MANUAL,
            [Description("User Login")] LOGIN,
            [Description("Time Triggered")] TIMED
        }

        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawns a new FulcrumServiceAction object used to help us invoke service actions
        /// </summary>
        /// <param name="ActionName">Name of the action being invoked</param>
        /// <param name="Timing">The timing type for the action being invoked</param>
        public FulcrumServiceAction(string ActionName, ActionTimings Timing, params string[] ActionCommands)
        {
            // Store the action name and timing value. Configure a new GUID value for it as well 
            this.ActionTiming = Timing;
            this.ActionName = ActionName;
            this.ActionGuid = Guid.NewGuid();

            // Setup the name of the script file for this action 
            this.ActionCommands = ActionCommands;
            this.ActionScriptFile = $"{this.ActionName}_{this.ActionGuid.ToString("D").ToUpper()}";

            // Store a default DateTime value for the timing since no value was given
            this._actionTime = new TimeSpan(12, 0, 0);
        }
        /// <summary>
        /// Spawns a new FulcrumServiceAction object used to help us invoke service actions
        /// </summary>
        /// <param name="ActionName">Name of the action being invoked</param>
        /// <param name="ActionTime">The time for the action to be invoked</param>
        public FulcrumServiceAction(string ActionName, TimeSpan ActionTime, params string[] ActionCommands)
        {
            // Store the name, time to execute, and configure a GUID value along with the timing type
            this.ActionName = ActionName;
            this._actionTime = ActionTime;
            this.ActionGuid = Guid.NewGuid();
            this.ActionTiming = ActionTimings.MANUAL;

            // Setup the name of the script file for this action 
            this.ActionCommands = ActionCommands;
            this.ActionScriptFile = $"{this.ActionName}_{this.ActionGuid.ToString("D").ToUpper()}";
        }
    }
}
