using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Custom ComboBox object which populates it's values based on a specified enumeration type
    /// </summary>
    internal class FulcrumEnumComboBox : ComboBox, INotifyPropertyChanged
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

        // Private backing fields for the ComboBox object
        private bool _isMultiSelect;            // Sets if this is a multi-selection enabled ComboBox
        private string _defaultValue;           // Stores the default value/zero index value
        private Type _enumerationType;          // Stores the current enumeration type being used

        #endregion // Fields

        #region Properties

        // Public facing properties for the ComboBox to bind onto 
        public Type EnumerationType
        {
            get => this._enumerationType; 
            set
            {
                // Update our backing field and setup the items collection 
                this.SetField(ref this._enumerationType, value);
                this._enumComboLogger ??= new SharpLogger(
                    LoggerActions.UniversalLogger,
                    $"{value.Name}_EnumComboLogger");

                // Build our enumeration items source here
                List<string> LoadedValues = Enum.GetNames(value).ToList();
                LoadedValues.Insert(0, string.IsNullOrWhiteSpace(this.DefaultValue)
                    ? $"-- {value.Name} --"
                    : $"-- {this.DefaultValue} --");

                // Store the enumeration items and set our selected index to 0
                this.ItemsSource = LoadedValues;
                this.SelectedIndex = 0;

                // Log out how many values we've entered into the ComobBox
                this._enumComboLogger.WriteLog($"STORED {this.Items.Count - 1} VALUES FOR ENUM {value.Name}");
            }
        }
        public bool IsMultiSelect
        {
            get => this._isMultiSelect;
            set => this.SetField(ref this._isMultiSelect, value);
        }
        public string DefaultValue
        {
            get => this._defaultValue;
            set => this.SetField(ref this._defaultValue, value);
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

        }
        /// <summary>
        /// Builds a new Enumeration ComboBox using the given enumeration type for items 
        /// </summary>
        /// <param name="EnumerationType">The type of enumeration being stored</param>
        public FulcrumEnumComboBox(Type EnumerationType)
        {
            // Store the type for the ComboBox object and populate our items
            this.EnumerationType = EnumerationType;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------
    }
}
