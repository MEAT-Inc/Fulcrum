using System;
using System.Globalization;
using System.Windows.Data;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Converts a boolean value into a visibility but inverse based on value.
    /// True = Hidden. False = Visible 
    /// </summary>
    internal class BooleanToOutputConverter<TValueType> : IValueConverter
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        // True and false value object types for output types.
        public TValueType TrueOutput { get; set; }
        public TValueType FalseOutput { get; set; }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new converter 
        /// </summary>
        /// <param name="trueOutputValue"></param>
        /// <param name="falseOutputValue"></param>
        public BooleanToOutputConverter(TValueType TrueValue, TValueType FalseValue)
        {
            // Store value for true and false values
            TrueOutput = TrueValue;
            FalseOutput = FalseValue;
        }
        
        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Convert into a visibility type based on an input value of boolean
        /// </summary>
        /// <param name="InputValue">Value to check</param>
        /// <param name="TargetType">Type to cast into</param>
        /// <param name="Paramater">Object to apply into</param>
        /// <param name="CultureType">Culture information</param>
        /// <returns>Output object based on what the input value is</returns>
        public virtual object Convert(object InputValue, Type TargetType, object Paramater, CultureInfo CultureType)
        {
            // Convert bool and return output value based on what was setup in our CTOR
            return InputValue is bool BoolValue && BoolValue ? TrueOutput : FalseOutput;
        }
        /// <summary>
        /// Convert into a visibility type based on an input value of boolean
        /// </summary>
        /// <param name="InputValue">Value to check</param>
        /// <param name="TargetType">Type to cast into</param>
        /// <param name="Paramater">Object to apply into</param>
        /// <param name="CultureType">Culture information</param>
        /// <returns>TrueOutput if value is the true</returns>
        public virtual object ConvertBack(object InputValue, Type TargetType, object Paramater, CultureInfo CultureType)
        {
            // Convert bool and return true or false based on if the input value is equal to our true value
            return InputValue is TValueType && Equals(InputValue, TrueOutput);
        }
    }
}
