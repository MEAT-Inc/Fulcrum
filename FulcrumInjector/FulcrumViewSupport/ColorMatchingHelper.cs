using System.Drawing;
using System.Reflection;

namespace FulcrumInjector.FulcrumViewSupport
{
    public class ColorMatchingHelper
    {
        /// <summary>
        /// Color Matching result
        /// </summary>
        public enum MatchType
        {
            NoMatch,
            ExactMatch,
            ClosestMatch
        };

        /// <summary>
        /// Gets the closest known NAMED color from an input color
        /// </summary>
        /// <param name="InputColor">Color to match</param>
        /// <param name="MatchedColor">The resulting color name</param>
        /// <returns></returns>
        public static MatchType FindColor(Color InputColor, out Color MatchedColor)
        {
            // Setup values here
            MatchedColor = InputColor;
            int ClosestColorDifference = 0;
            MatchType MatchTypeFound = MatchType.NoMatch;

            // Loop all the colors and find the closest one
            foreach (PropertyInfo SysColorObject in typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                // Get the value and see if it matches our input 
                Color ColorValue = (Color)SysColorObject.GetValue(null, null);
                if (ColorValue == InputColor)
                {
                    // Exact color found. Store and return
                    MatchTypeFound = MatchType.ExactMatch;
                    MatchedColor = Color.FromName(SysColorObject.Name);

                    // Return the match  type
                    return MatchTypeFound;
                }

                // Differences in the ARGB Channels
                int IntAValue = InputColor.A - ColorValue.A;
                int IntRValue = InputColor.R - ColorValue.R;
                int IntGValue = InputColor.G - ColorValue.G;
                int IntBValue = InputColor.B - ColorValue.B;

                // Difference in color values for all four channels combined
                int ColorDifference = IntAValue * IntAValue +
                                      IntRValue * IntRValue +
                                      IntGValue * IntGValue +
                                      IntBValue * IntBValue;

                // If we can't get a match and there's too big of a gap
                if (MatchTypeFound != MatchType.NoMatch && ColorDifference >= ClosestColorDifference) continue;

                // Store values and break;
                MatchTypeFound = MatchType.ClosestMatch;
                ClosestColorDifference = ColorDifference;
                MatchedColor = Color.FromName(SysColorObject.Name);
            }

            // Return the matched color result here.
            return MatchTypeFound;
        }
    }
}
