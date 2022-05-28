using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Used to color and format message data contents out inside our injector log file outputs
    /// </summary>
    public class MessageFilterDataColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new color format helping object.
        /// </summary>
        public MessageFilterDataColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // --------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the message data values here
            Regex MessageDataRegex = new(PassThruRegexModelShare.MessageFilterInfo.ExpressionPattern);
            Match FoundMatch = MessageDataRegex.Match(CurrentContext.Document.GetText(InputLine));
            if (!FoundMatch.Success) return;

            // Now run our coloring definitions and return out
            this.ColorNewMatches(InputLine, FoundMatch);
        }
    }
}
