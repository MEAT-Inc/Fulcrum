using System;
using System.Windows.Forms;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewContent.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Model object for our settings entries
    /// </summary>
    [JsonConverter(typeof(SettingsCollectionJsonConverter))]
    internal class FulcrumSettingsEntryModel
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // Basic Setting configurations
        public string SettingName { get; set; }
        public object SettingValue { get; set; }
        public string SettingDescription { get; set; }

        // The type of control used to setup the settings entries
        public ControlTypes TypeOfControl { get; set; }
        public Type SettingControlType { get; set; }

        #endregion //Properties

        #region Structs and Classes

        /// <summary>
        /// Types of possible controls we can use here.
        /// </summary>
        public enum ControlTypes
        {
            NOT_DEFINED,        // Bad enum type parse
            CHECKBOX_CONTROL,   // Checkbox
            TEXTBOX_CONTROL,    // Textbox
            COMBOBOX_CONTROL,   // Combobox
        }

        #endregion //Structs and Classes

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new settings entry object.
        /// </summary>
        /// <param name="Name">Setting name</param>
        /// <param name="Value">Setting value</param>
        /// <param name="ControlType">Type of UI Control</param>
        /// <param name="Description">Description of setting</param>
        public FulcrumSettingsEntryModel(string Name, object Value, ControlTypes ControlType, string Description = "No Description")
        {
            // Store values for object onto class now.
            this.SettingName = Name;
            this.SettingValue = Value;
            this.SettingDescription = Description;

            // Configure a new settings logger

            // Set our control type
            this.TypeOfControl = ControlType;
            switch (this.TypeOfControl)
            {
                // Checkbox Control
                case ControlTypes.CHECKBOX_CONTROL: 
                    this.SettingControlType = typeof(CheckBox);
                    this.SettingValue = bool.Parse(Value.ToString());
                    break;

                // ComboBox
                case ControlTypes.COMBOBOX_CONTROL:
                    this.SettingControlType = typeof(ComboBox);
                    this.SettingValue = Value.ToString().Split(',');
                    break;

                // TextBox
                case ControlTypes.TEXTBOX_CONTROL:
                    this.SettingControlType = typeof(TextBox);
                    break;

                // Not Valid
                case ControlTypes.NOT_DEFINED:
                    this.SettingControlType = null;
                    throw new ArgumentException($"Error! Control type was not defined! Unable to store setting entry {Name}!");
            }
        }
    }
}
