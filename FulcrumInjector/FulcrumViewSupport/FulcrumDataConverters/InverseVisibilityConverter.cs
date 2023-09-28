using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Inverts a given visibility value and returns it out
    /// </summary>
    internal class InverseVisibilityConverter : IValueConverter
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new converter 
        /// </summary>
        public InverseVisibilityConverter() { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Inverts a given visibility value assuming it is valid.
        /// Providing Visible returns collapsed. Anything else will return visible
        /// </summary>
        /// <param name="InputValue">Value to check</param>
        /// <param name="TargetType">Type to cast into</param>
        /// <param name="Paramater">Object to apply into</param>
        /// <param name="CultureType">Culture information</param>
        /// <returns>Visible output if the input object is collapsed</returns>
        public virtual object Convert(object InputValue, Type TargetType, object Paramater, CultureInfo CultureType)
        {
            // Convert the input argument into a visibility value and return out based on the input
            if (InputValue is not Visibility InputVisibility) return Visibility.Visible; 
            return InputVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        /// <summary>
        /// Return the original visibility state for the input object
        /// </summary>
        /// <param name="InputValue">Value to check</param>
        /// <param name="TargetType">Type to cast into</param>
        /// <param name="Paramater">Object to apply into</param>
        /// <param name="CultureType">Culture information</param>
        /// <returns>Visible output if value is the true</returns>
        public virtual object ConvertBack(object InputValue, Type TargetType, object Paramater, CultureInfo CultureType)
        {
            // Return the inverse of the input conversion based on the arguments given
            return InputValue is not Visibility InputVisibility ? Visibility.Collapsed : InputVisibility;
        }
    }
}
