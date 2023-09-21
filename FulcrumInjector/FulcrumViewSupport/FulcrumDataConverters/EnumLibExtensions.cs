using System;
using System.ComponentModel;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Extension class for pulling description attributes from the enums
    /// </summary>
    internal static class EnumLibExtensions
    {
        /// <summary>
        /// Gets a descriptor string for the enum type provided.
        /// </summary>
        /// <param name="EnumValue">Enum to convert/get description on</param>
        /// <returns>Enum description</returns>
        public static string ToDescriptionString<TEnumType>(this TEnumType EnumValue)
        {
            // Get the descriptor from the enum attributes pulled.
            DescriptionAttribute[] EnumAtribs = (DescriptionAttribute[])EnumValue
               .GetType()
               .GetField(EnumValue.ToString())
               .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return EnumAtribs.Length > 0 ? EnumAtribs[0].Description : string.Empty;
        }
        /// <summary>
        /// Converts an input string (enum descriptor) into an output enum object.
        /// </summary>
        /// <param name="EnumDescription">The enum object output we wish to use.</param>
        /// <returns>A parsed enum if passed. Otherwise an invalid arg exception is thrown</returns>
        public static TEnumType ToEnumValue<TEnumType>(this string EnumDescription)
        {
            // Find the types first, then pull the potential file value types.
            foreach (var EnumFieldObj in typeof(TEnumType).GetFields())
            {
                // Check the attributes here. If one matches the type provided and the description is correct, return it.
                if (Attribute.GetCustomAttribute(EnumFieldObj, typeof(DescriptionAttribute)) is DescriptionAttribute EnumAtrib)
                    if (EnumAtrib.Description == EnumDescription) return (TEnumType)EnumFieldObj.GetValue(null);
                    else { if (EnumFieldObj.Name == EnumDescription) return (TEnumType)EnumFieldObj.GetValue(null); }
            }

            // Throw invalid description type 
            throw new ArgumentException($"Unable to convert the input type {EnumDescription} to a valid {typeof(TEnumType).Name} enum", nameof(EnumDescription));
        }
    }
}
