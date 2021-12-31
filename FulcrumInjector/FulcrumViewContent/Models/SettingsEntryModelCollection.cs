using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FulcrumInjector.FulcrumViewContent.Models
{
    /// <summary>
    /// Wrapper holding a list of settings sets
    /// </summary>
    public class SettingsEntryCollectionModel
    {
        // Title of section and the settings themselves
        public string SettingSectionTitle { get; set; }
        public SettingsEntryModel[] SettingsEntries { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

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

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new collection of settings objects
        /// </summary>
        /// <param name="SectionName"></param>
        /// <param name="SettingsEntries"></param>
        public SettingsEntryCollectionModel(string SectionName, SettingsEntryModel[] SettingsEntries)
        {
            // Store values
            this.SettingSectionTitle = SectionName;
            this.SettingsEntries = SettingsEntries;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds new settings into our list of setting entries here.
        /// </summary>
        /// <param name="SettingsToAdd"></param>
        /// <returns></returns>
        public SettingsEntryModel[] AddNewSetting(params SettingsEntryModel[] SettingsToAdd)
        {
            // Add one by one and replacing dupes.
            var TempList = SettingsEntries.ToList();
            foreach (var EntryObj in SettingsToAdd)
            {
                // Check if in the list object
                int ObjectIndex = TempList.FindIndex(SettingObj => SettingObj.SettingName == EntryObj.SettingName);
                if (ObjectIndex == -1) { TempList.Add(EntryObj); }
                else { TempList[ObjectIndex] = EntryObj; }
            }

            // Convert to array, store and return
            this.SettingsEntries = TempList.ToArray();
            return this.SettingsEntries;
        }

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
            var LocatedSettingValue = this.SettingsEntries
                .FirstOrDefault(SettingObj => SettingObj.SettingName == SettingName)?
                .SettingValue;

            // Check if the located value is null. If so, return the default. Otherwise return cast value
            return LocatedSettingValue != null ?
                (TResultType)Convert.ChangeType(LocatedSettingValue, typeof(TResultType)) :
                DefaultValue;
        }
    }
}
