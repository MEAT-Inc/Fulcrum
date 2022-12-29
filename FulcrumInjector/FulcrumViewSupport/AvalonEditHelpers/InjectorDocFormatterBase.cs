using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

// Color Brushes
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers
{
    /// <summary>
    /// Base helper class instance for injector coloring configuration
    /// </summary>
    public class InjectorDocFormatterBase : DocumentColorizingTransformer
    {
        // Logger object and color brushes for formatting output.
        protected internal Tuple<MediaBrush, MediaBrush>[] _coloringBrushes;
        protected internal SubServiceLogger FormatLogger => (SubServiceLogger)LoggerQueue.SpawnLogger($"{this.GetType().Name}Logger", LoggerActions.SubServiceLogger); 

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        protected InjectorDocFormatterBase(OutputFormatHelperBase FormatBase)
        {
            // Pull in our color values. Store format helper.
            if (this.GetType() == typeof(InjectorDocFormatterBase)) return;
            this._coloringBrushes = FormatBase.PullColorForCommand(this.GetType());
        }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Appends a new set of brushes for the number of matches if they are not equal numbers.
        /// </summary>
        /// <param name="MatchesFound"></param>
        /// <returns>New output brush set.</returns>
        protected internal bool UpdateBrushesForMatches(int MatchesFound)
        {
            // Validate our match count and brush values match correctly.
            if (MatchesFound == this._coloringBrushes.Length) return true;
            while (this._coloringBrushes.Length < MatchesFound)
            {
                // Append in a new brush set of White FG and no background.
                var NewSet = new Tuple<MediaBrush, MediaBrush>(MediaBrushes.White, MediaBrushes.Transparent);
                this._coloringBrushes = this._coloringBrushes.Append(NewSet).ToArray();
            }

            // Return new built set (Also the private brush set)
            return false;
        }
        /// <summary>
        /// Applies new output formatting to line object based on input match objects.
        /// </summary>
        /// <param name="InputLine"></param>
        /// <param name="MatchesFound"></param>
        protected internal void ColorNewMatches(DocumentLine InputLine, Match MatchFound)
        {
            // First fix our coloring count if needed.
            this.UpdateBrushesForMatches(MatchFound.Groups.Count);

            // Now from all our matches made, loop and apply color values.
            int LineStartOffset = InputLine.Offset;
            string LineText = CurrentContext.Document.GetText(InputLine);
            for (int GroupIndex = 1; GroupIndex < MatchFound.Groups.Count; GroupIndex++)
            {
                // Pull the current group object value
                string GroupFound = MatchFound.Groups[GroupIndex].Value;
                int GroupPositionStart = LineStartOffset + LineText.IndexOf(GroupFound);
                int GroupPositionEnd = GroupPositionStart + GroupFound.Length;

                // Now apply a color value based on the type of contents provided for it.
                base.ChangeLinePart(GroupPositionStart, GroupPositionEnd, (NextMatchElement) =>
                {
                    // Colorize our logger name here.
                    NextMatchElement.TextRunProperties.SetForegroundBrush(this._coloringBrushes[GroupIndex - 1].Item1);
                    NextMatchElement.TextRunProperties.SetBackgroundBrush(this._coloringBrushes[GroupIndex - 1].Item2);
                });
            }
        }
        /// <summary>
        /// Colors an index based set of values by the brushes given at index values specified.
        /// </summary>
        /// <param name="BrushSet">Brushes to color</param>
        /// <param name="IndexBounds">Index to set from</param>
        protected internal void ColorMatch(DocumentLine InputLine, MediaBrush[] BrushSet, int[] IndexBounds = null)
        {
            // See if the index values and Brushes exist or not.
            IndexBounds ??= new[] { InputLine.Offset, InputLine.EndOffset };
            BrushSet ??= new[] { MediaBrushes.White, MediaBrushes.Transparent };

            // Now apply a color value based on the type of contents provided for it.
            base.ChangeLinePart(IndexBounds[0], IndexBounds[1], (NextLineObject) =>
            {
                // Colorize our logger name here.
                NextLineObject.TextRunProperties.SetForegroundBrush(BrushSet.FirstOrDefault());
                NextLineObject.TextRunProperties.SetBackgroundBrush(BrushSet.LastOrDefault());
            });
        }

        // -----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // The base definition will just print the output using our base command
            this.FormatLogger.WriteLog("BASE FORMATTER TYPE HIT! THIS SHOULDN'T BE POSSIBLE!", LogType.TraceLog);
            throw new InvalidOperationException("CAN NOT ACCESS THE OVERRIDE FOR WRITING OBJECTS ON THE BASE FORMAT HELPER COMMAND CLASS!");
        }
    }
}
