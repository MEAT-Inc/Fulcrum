using System.Windows;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{ 
    /// <summary>
    /// Class used for casting bools to inverse visibility values
    /// This was being done using a second bool value but wasn't true MVVM so I just wrote this little subclass of the
    /// booleanToOutput Converter I built which takes generics and converts them into outputs as defined by the CTOR 
    /// </summary>
    internal sealed class BoolToVisibilityConverter : BooleanToOutputConverter<Visibility>
    {
        /// <summary>
        /// Builds a new converter object class here.
        /// </summary>
        /// <param name="TrueValue">Value for visible</param>
        /// <param name="FalseValue">Value for hidden</param>
        public BoolToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed) { }
    }
}
