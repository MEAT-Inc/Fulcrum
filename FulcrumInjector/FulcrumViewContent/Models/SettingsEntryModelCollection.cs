using System.Linq;

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
    }
}
