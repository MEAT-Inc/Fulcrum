using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Static extension class used to scramble and unscramble keys/strings from our settings file
    /// </summary>
    internal static class StringScrambler
    {
        /// <summary>
        /// Scrambles a given string by reversing it and encoding it to base64
        /// </summary>
        /// <param name="InputString">The string to scramble</param>
        /// <returns>The scrambled string</returns>
        public static string ScrambleString(this string InputString)
        {
            // Reverse the string and encode it to base64
            string EncodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes(InputString));
            string ReversedString = string.Join(string.Empty, EncodedString.Reverse());

            // Return out the encoded string 
            return ReversedString;
        }
        /// <summary>
        /// Unscrambles a given string by reversing it and decoding it from base64
        /// </summary>
        /// <param name="InputString">The string to unscramble</param>
        /// <returns>The unscrambled string</returns>
        public static string UnscrambleString(this string InputString)
        {
            // Reverse the input string
            string ReversedString = string.Join(string.Empty, InputString.Reverse());
            string DecodedString = Encoding.UTF8.GetString(Convert.FromBase64String(ReversedString));
            
            // Return the decoded string 
            return DecodedString; 
        }
    }
}
