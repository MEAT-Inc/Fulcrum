using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using Newtonsoft.Json;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Builds a new template selection object which helps us find templates
    /// </summary>
    internal class UserSettingsDataTemplateSelector : DataTemplateSelector
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Logger object to write exceptions thrown inside this template
        private static SharpLogger _templateLogger;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Data Template location routine
        /// Pulls in the type of the item in the template and then finds a style based on the output
        /// </summary>
        /// <param name="InputItem"></param>
        /// <param name="ObjectContainer"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object InputItem, DependencyObject ObjectContainer)
        {
            // Configure the logger instance if needed
            _templateLogger ??= new SharpLogger(LoggerActions.UniversalLogger);

            // Check if we can use this selector object or not.
            if (ObjectContainer is FrameworkElement InputElement && InputItem is FulcrumSettingEntryModel SettingModelObject)
            {
                // Now find the type of control to use
                switch (SettingModelObject.TypeOfControl)
                {
                    // Found control type
                    case FulcrumSettingEntryModel.ControlTypes.CHECKBOX_CONTROL: return InputElement.FindResource("CheckboxSettingEntryDataTemplate") as DataTemplate;
                    case FulcrumSettingEntryModel.ControlTypes.TEXTBOX_CONTROL: return InputElement.FindResource("TextBoxSettingEntryDataTemplate") as DataTemplate;
                    case FulcrumSettingEntryModel.ControlTypes.COMBOBOX_CONTROL: return InputElement.FindResource("ComboBoxSettingEntryDataTemplate") as DataTemplate;

                    // If failed
                    case FulcrumSettingEntryModel.ControlTypes.NOT_DEFINED:
                        _templateLogger.WriteLog($"FAILED TO FIND NEW CONTROL TYPE FOR VALUE {SettingModelObject.TypeOfControl}!", LogType.ErrorLog);
                        return null;
                }
            }

            // Failed to find control template output
            _templateLogger.WriteLog("ERROR! INVALID CONTROL TYPE WAS PROCESSED! NOT RETURNING A DATATEMPLATE FOR IT", LogType.ErrorLog);
            _templateLogger.WriteLog($"CONTROL PASSED CONTENT: {JsonConvert.SerializeObject(InputItem, Formatting.None)}", LogType.TraceLog);
            return null;
        }
    }
}
