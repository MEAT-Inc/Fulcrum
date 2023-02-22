using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FulcrumInjector.FulcrumViewContent.Models.SettingsModels
{
    /// <summary>
    /// Wrapper holding a list of settings sets
    /// </summary>
    internal class FulcrumSettingsCollectionModel : IEnumerable<FulcrumSettingsEntryModel>
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our collections of settings
        public readonly string SettingSectionTitle;
        private readonly List<FulcrumSettingsEntryModel> _settingsEntries;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Overrides the output for ToString to contain section name and all the setting names in the section
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Build output string and return it formatted
            string[] SettingNameSet = SettingsEntries == null ? 
                new[] { "No Settings Imported" } :
                SettingsEntries?.Select(SettingObj => SettingObj.SettingName).ToArray();

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
        public IEnumerator<FulcrumSettingsEntryModel> GetEnumerator()
        {
            // Return our settings objects ordered by their names as a collection
            return (IEnumerator<FulcrumSettingsEntryModel>)this._settingsEntries.GetEnumerator();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new collection of settings objects
        /// </summary>
        /// <param name="SectionName"></param>
        /// <param name="SettingsEntries"></param>
        public FulcrumSettingsCollectionModel(string SectionName, IEnumerable<FulcrumSettingsEntryModel> SettingsEntries)
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
        public IEnumerable<FulcrumSettingsEntryModel> UpdateSetting(params FulcrumSettingsEntryModel[] SettingsToAdd)
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
