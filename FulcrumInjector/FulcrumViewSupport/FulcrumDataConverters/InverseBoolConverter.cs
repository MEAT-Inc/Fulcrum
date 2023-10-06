namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Class used for casting bools to inverse bool values
    /// This was being done using a second bool value but wasn't true MVVM so I just wrote this little subclass of the
    /// booleanToOutput Converter I built which takes generics and converts them into outputs as defined by the CTOR 
    /// </summary>
    internal sealed class InverseBoolConverter : BoolToObjectConverter<bool>
    {
        /// <summary>
        /// Builds a new converter object class here.
        /// </summary>
        public InverseBoolConverter() : base(false, true) { }
    }
}
