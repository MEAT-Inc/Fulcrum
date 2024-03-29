﻿using System;
using System.Windows.Forms;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using Newtonsoft.Json;
using SettingSectionTypes = FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels.FulcrumSettingsCollection.SettingSectionTypes;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels
{
    /// <summary>
    /// Model object for our settings entries
    /// </summary>
    [JsonConverter(typeof(SettingEntryJsonConverter))]
    public class FulcrumSettingEntryModel
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
        public SettingSectionTypes SettingSection { get; set; }

        // The type of control used to setup the settings entries
        public Type SettingControlType { get; set; }
        public ControlTypes TypeOfControl { get; set; }

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
        /// <param name="SettingSection">The type of setting being stored</param>
        /// <param name="Description">Description of setting</param>
        public FulcrumSettingEntryModel(string Name, object Value, ControlTypes ControlType, SettingSectionTypes SettingSection, string Description = "No Description")
        {
            // Store values for object onto class now.
            this.SettingName = Name;
            this.SettingValue = Value;
            this.SettingSection = SettingSection;
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
