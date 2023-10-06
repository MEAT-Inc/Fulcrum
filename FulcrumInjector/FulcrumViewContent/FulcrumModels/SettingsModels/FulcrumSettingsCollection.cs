using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Wrapper holding a list of settings sets
    /// </summary>
    public class FulcrumSettingsCollection : IEnumerable<FulcrumSettingEntryModel>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our collections of settings and the title of the setting set
        private string _settingSectionTitle;
        private readonly List<FulcrumSettingEntryModel> _settingsEntries;

        #endregion //Fields

        #region Properties

        // Public facing title property for this collection of settings
        public SettingSectionTypes SectionType { get; private set; }
        public string SettingSectionTitle => this.SectionType.ToDescriptionString();

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration used to index settings sets and to lookup values easier
        /// Stored on each of the settings collection types and inherited by child values
        /// </summary>
        public enum SettingSectionTypes
        {
            [Description("No Section Type")] NO_SECTION_TYPE,
            [Description("FulcrumShim DLL Settings")] SHIM_DLL_SETTINGS, 
            [Description("Pipe Server Settings")] PIPE_SERVER_SETTINGS,
            [Description("File Watchdog Settings")] FILE_WATCHDOG_SETTINGS,
            [Description("Hardware Configuration Settings")] HARDWARE_CONFIGURATION_SETTINGS,
            [Description("Log File Conversion Settings")] LOG_FILE_CONVERSION_SETTINGS,
            [Description("Debug Log Viewer Settings")] DEBUG_LOG_VIEWER_SETTINGS,
            [Description("Injector Pipe Settings")] INJECTOR_PIPE_SETTINGS,
            [Description("PassThru DLL Output Regex Settings")] DLL_OUTPUT_REGEX_SETTINGS,
            [Description("PassThru DLL Output Syntax Settings")] DLL_OUTPUT_SYNTAX_SETTINGS,
            [Description("Debug Log Viewer Syntax Settings")] DEBUG_LOG_VIEWER_SYNTAX_SETTINGS
        }

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Overrides the output for ToString to contain section name and all the setting names in the section
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Build output string and return it formatted
            string[] SettingNameSet = !this.Any() ? 
                new[] { "No Settings Imported" } :
                this.Select(SettingObj => SettingObj.SettingName).ToArray();

            // Build output value from the name set generated
            var OutputString = $"{this.SettingSectionTitle} --> [{string.Join(",", SettingNameSet)}]";
            return OutputString;
        }

        /// <summary>
        /// Returns our generic enumerator for this collection
        /// </summary>
        /// <returns>An IEnumerator collection of objects holding our settings</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            // Return the enumerator object
            return GetEnumerator();
        }
        /// <summary>
        /// Returns our enumerator using the cast collection of settings on this class instance
        /// </summary>
        /// <returns></returns>
        public IEnumerator<FulcrumSettingEntryModel> GetEnumerator()
        {
            // Return our settings objects ordered by their names as a collection
            return (IEnumerator<FulcrumSettingEntryModel>)this._settingsEntries.GetEnumerator();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new collection of settings objects
        /// </summary>
        /// <param name="SectionType"></param>
        /// <param name="SettingsEntries"></param>
        public FulcrumSettingsCollection(SettingSectionTypes SectionType, IEnumerable<FulcrumSettingEntryModel> SettingsEntries)
        {
            // Store values for the setting collection name and setting objects 
            this.SectionType = SectionType;
            this._settingsEntries = SettingsEntries.ToList();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls in a setting value from the given name and returns it
        /// </summary>
        /// <typeparam name="TResultType">Type of value</typeparam>
        /// <param name="SettingName">Name of the setting to pull</param>
        /// <param name="DefaultValue">Default value of the setting</param>
        /// <returns>Setting value or the default value</returns>
        public TResultType GetSettingValue<TResultType>(string SettingName, TResultType DefaultValue)
        {
            // Pull the value object here.
            var LocatedSettingValue = this._settingsEntries
                .FirstOrDefault(SettingObj => SettingObj.SettingName == SettingName)?
                .SettingValue;

            // Check if the located value is null. If so, return the default. Otherwise return cast value
            return LocatedSettingValue != null ?
                (TResultType)Convert.ChangeType(LocatedSettingValue, typeof(TResultType)) :
                DefaultValue;
        }
        /// <summary>
        /// Adds new settings into our list of setting entries here.
        /// </summary>
        /// <param name="SettingsToAdd"></param>
        /// <returns></returns>
        public IEnumerable<FulcrumSettingEntryModel> UpdateSetting(params FulcrumSettingEntryModel[] SettingsToAdd)
        {
            // Add one by one and replacing dupes.
            foreach (var SettingEntry in SettingsToAdd)
            {
                // Find if the setting exists in our list of current setting objects
                int SettingIndex = this._settingsEntries.FindIndex(SettingObj => SettingObj.SettingName == SettingEntry.SettingName);

                // If the setting is not found, we insert it at the end of our collection. If it is found, then we replace it
                if (SettingIndex == -1) this._settingsEntries.Add(SettingEntry);
                else { this._settingsEntries[SettingIndex] = SettingEntry; }
            }

            // Return our the newly built list of settings
            return this._settingsEntries;
        }
    }
}
