using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumControls
{
    /// <summary>
    /// Custom ComboBox object which populates it's values based on a specified enumeration type
    /// </summary>
    public partial class FulcrumEnumComboBox : ComboBox, INotifyPropertyChanged
    {
        #region Custom Events

        // Event object for property changed events
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method used to fire a new property changed event routine
        /// </summary>
        /// <param name="PropertyName">Name of the property being updated</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            // Invoke a property changed event if it's configured
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        /// <summary>
        /// Used to set a backing Field FieldValue when a property is changed on this class instance
        /// </summary>
        /// <typeparam name="T">Type of the Field being set</typeparam>
        /// <param name="Field">The Field instance to update</param>
        /// <param name="FieldValue">Value to store on the Field</param>
        /// <param name="PropertyName">Name of the property being updated</param>
        /// <returns>True if the property was updated. False if not</returns>
        protected bool SetField<T>(ref T Field, T FieldValue, [CallerMemberName] string PropertyName = null)
        {
            // See if we need to fire a new event for property changed or not
            if (EqualityComparer<T>.Default.Equals(Field, FieldValue)) return false;

            // Update our field value and fire our property changed event as needed
            Field = FieldValue;
            OnPropertyChanged(PropertyName);
            return true;
        }

        #endregion // Custom Events

        #region Fields

        // Private logger instance for the ComboBox
        private SharpLogger _enumComboLogger;

        // Private static fields for resource dictionaries
        private static readonly string _singleSelectTemplateName = "SingleItemTemplate";
        private static readonly string _multiSelectTemplateName = "MultipleItemTemplate";

        // Private backing fields for the ComboBox object
        private bool _isMultiSelect;                    // Sets if this is a multi-selection enabled ComboBox
        private string _defaultValue;                   // Stores the default value/zero index value
        private Type _enumerationType;                  // Stores the current enumeration type being used
        private DataTemplate _currentTemplate;          // Stores the current data template in use for the ComboBox 
        private List<object> _selectedValues = new();   // Stores the currently selected values of our ComboBox

        #endregion // Fields

        #region Properties

        // Public facing properties for the ComboBox to bind onto 
        public Type EnumerationType
        {
            get => this._enumerationType;
            set
            {
                // Update our backing field and build our items source 
                this.SetField(ref this._enumerationType, value);
                this._rebuildComboBox();
            }
        }
        public string DefaultValue
        {
            get => this._defaultValue;
            set
            {
                // Update our backing field and build our items source 
                this.SetField(ref this._defaultValue, value);
                this._rebuildComboBox();
            }
        }
        public bool IsMultiSelect
        {
            get => this._isMultiSelect;
            set
            {
                // Set our backing field and apply a new template
                this.SetField(ref this._isMultiSelect, value);
                this._locateDataTemplate();
            }
        }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Default CTOR for an enumeration ComboBox object
        /// </summary>
        public FulcrumEnumComboBox()
        {
            // Initialize the ComboBox object and set our default template
            this.InitializeComponent();
            this._locateDataTemplate();
        }
        /// <summary>
        /// Builds a new Enumeration ComboBox using the given enumeration type for items 
        /// </summary>
        /// <param name="EnumerationType">The type of enumeration being stored</param>
        public FulcrumEnumComboBox(Type EnumerationType, bool IsMultiSelect = false)
        {
            // Initialize the ComboBox object
            this.InitializeComponent();

            // Store the type of enum in use and store our multi-select state value
            this.EnumerationType = EnumerationType;
            this.IsMultiSelect = IsMultiSelect; 

            // Generate an items source and apply our template
            this._generateItemsSource();
            this._locateDataTemplate();
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event handler to fire when the user checks an item in our multi-selection mode
        /// </summary>
        /// <param name="Sender">Checkbox that sent this event</param>
        /// <param name="E">Event args fired along with the checked event</param>
        private void cbIsSelected_OnChecked(object Sender, RoutedEventArgs E)
        {
            // Get the sending CheckBox and pull the value for it
            if (Sender is not CheckBox SendingCheckBox) return;
            if (SendingCheckBox.DataContext == null) return;  

            // Store the name of the value being added or removed
            // TODO: Finish this logic maybe? Might be useful at some point
        }
        /// <summary>
        /// Event handler to fire when the user changes their selection in the single selection mode
        /// </summary>
        /// <param name="Sender">ComboBox Item that sent this event</param>
        /// <param name="E">Event args fired along with the selection changed event</param>
        private void FulcrumEnumComboBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            // Get our sending object and pull the value for it
            if (Sender is not ComboBox SendingItem) return;
            if (SendingItem.DataContext == null) return;

            // Store our newly selected item value here
            this._selectedValues.Clear();
            string SelectedText = SendingItem.Text;
            this._selectedValues.Add(SelectedText);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the collection of items on the ComboBox and applies the correct template
        /// </summary>
        private void _rebuildComboBox()
        {
            // Rebuild items and load our desired template instance
            this._generateItemsSource(); 
            this._locateDataTemplate();

            // Log out some information about the rebuild routine here
            this._enumComboLogger?.WriteLog($"REGENERATED {this.EnumerationType.Name} ENUM COMBO BOX!", LogType.TraceLog);
        }
        /// <summary>
        /// Generates or updates the collection of items we're using for our ItemsSource
        /// </summary>
        private void _generateItemsSource()
        {
            // Make sure the enumeration type is configured
            if (this.EnumerationType == null) return;

            // Build a new logger instance if it's not configured
            this._enumComboLogger ??= new SharpLogger(
                LoggerActions.UniversalLogger,
                $"{this.EnumerationType.Name}_EnumComboLogger");

            // Build our enumeration items source here
            List<string> LoadedValues = Enum.GetNames(this.EnumerationType).ToList();
            if (!this.IsMultiSelect)
            {
                // Only add a default value object if we're using a single selection ComboBox
                LoadedValues.Insert(0, string.IsNullOrWhiteSpace(this.DefaultValue)
                    ? $"-- {this.EnumerationType.Name} --"
                    : $"-- {this.DefaultValue} --");
            }

            // Store the enumeration items and set our selected index to 0
            int CurrentIndex = this.SelectedIndex;
            this.ItemsSource = LoadedValues;
            this.SelectedIndex = CurrentIndex >= 0 ? CurrentIndex : 0;

            // Log out how many values we've entered into the ComobBox
            this._enumComboLogger.WriteLog($"STORED {this.Items.Count - 1} VALUES FOR ENUM {this.EnumerationType.Name}", LogType.TraceLog);
            this._enumComboLogger.WriteLog($"SET {this.DefaultValue} AS DEFAULT VALUE FOR {this.EnumerationType.Namespace} ENUM COMBO", LogType.TraceLog);
        }
        /// <summary>
        /// Finds and returns the data template needed for the current selection type 
        /// </summary>
        /// <param name="ApplyTemplate">When true, we set the template onto our ComboBox</param>
        /// <returns>The data template applied to our ComboBox</returns>
        private DataTemplate _locateDataTemplate(bool ApplyTemplate = true)
        {
            // If our enumeration type is null, exit out 
            if (this.EnumerationType == null) return new DataTemplate();

            // Build a new logger instance if it's not configured
            this._enumComboLogger ??= new SharpLogger(
                LoggerActions.UniversalLogger,
                $"{this.EnumerationType.Name}_EnumComboLogger");

            // Pull the resource dictionary for our EnumComboBox objects and return out the template needed
            DataTemplate SelectionTemplate = (DataTemplate)(this.IsMultiSelect
                ? this.Resources[_multiSelectTemplateName]
                : this.Resources[_singleSelectTemplateName]);

            // Log out the name of the template located here 
            this._enumComboLogger.WriteLog($"LOCATED TEMPLATE {SelectionTemplate.DataTemplateKey} FOR ENUM COMBOBOX CORRECTLY!", LogType.TraceLog);

            // Return out the located template if we're not setting it
            if (!ApplyTemplate) return SelectionTemplate;

            // Apply and store the new template, then return it out
            this._currentTemplate = SelectionTemplate;
            this.ItemTemplate = this._currentTemplate;
            return SelectionTemplate;
        }
    }
}
