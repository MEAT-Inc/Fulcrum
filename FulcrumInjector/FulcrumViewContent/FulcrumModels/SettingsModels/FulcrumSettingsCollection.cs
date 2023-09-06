using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Wrapper holding a list of settings sets
    /// </summary>
    internal class FulcrumSettingsCollection : IEnumerable<FulcrumSettingEntryModel>
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
        public string SettingSectionTitle
        {
            get => this._settingSectionTitle;
            private set => this._settingSectionTitle = value;
        }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Enumeration used to index settings sets and to lookup values easier
        /// Stored on each of the settings collection types and inherited by child values
        /// </summary>
        public enum SettingSectionTypes
        {
            NO_SECTION_TYPE = 0,
            SHIM_DLL_SETTINGS = 1,
            HARDWARE_SETTINGS = 2,
            PIPE_SERVER_SETTINGS = 3,
            FILE_WATCHDOG_SETTINGS = 4,
            DLL_OUTPUT_REGEX_SETTINGS = 5,
            DLL_OUTPUT_SYNTAX_SETTINGS = 6,
            DEBUG_VIEWER_SETTINGS = 7,
            DEBUG_VIEWER_SYNTAX = 8,
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
        /// <param name="SectionName"></param>
        /// <param name="SettingsEntries"></param>
        public FulcrumSettingsCollection(string SectionName, IEnumerable<FulcrumSettingEntryModel> SettingsEntries)
        {
            // Store values for the setting collection name and setting objects 
            this.SettingSectionTitle = SectionName;
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
