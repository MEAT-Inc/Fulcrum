using System.Text.RegularExpressions;
using FulcrumInjector.FulcrumViewContent.Models.PassThruModels;
using ICSharpCode.AvalonEdit.Document;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters.MessageDataFormatters
{
    /// <summary>
    /// Color output data read from a PTRead messages command
    /// </summary>
    public class MessageDataSentColorFormatter : InjectorDocFormatterBase
    {
        /// <summary>
        /// Builds a new PTRead messages data format helper
        /// </summary>
        /// <param name="FormatBase"></param>
        public MessageDataSentColorFormatter(OutputFormatHelperBase FormatBase) : base(FormatBase) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Color our line output for the stack trace
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find the message data values here
            Regex MessageDataRegex = new(PassThruRegexModelShare.MessageSentInfo.ExpressionPattern);
            Match FoundMatch = MessageDataRegex.Match(CurrentContext.Document.GetText(InputLine));
            if (!FoundMatch.Success) return;

            // Now run our coloring definitions and return out
            this.ColorNewMatches(InputLine, FoundMatch);
        }
    }
}