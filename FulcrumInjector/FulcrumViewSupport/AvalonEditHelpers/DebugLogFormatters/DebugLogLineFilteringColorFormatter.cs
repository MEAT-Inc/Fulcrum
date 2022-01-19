using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters
{
    /// <summary>
    /// Shows or hides line objects based on the filtering methods
    /// </summary>
    public class DebugLogLineFilteringColorFormatter : DocumentColorizingTransformer
    {
        // Filtering Key Value
        public readonly string FilterKey;

        // Lines from the input generation method
        public readonly TextEditor DocumentEditor;
        public readonly string OriginalInputLines;

        /// <summary>
        /// DCTOR For this object type. Resets the text content to be the input original contents
        /// </summary>
        ~DebugLogLineFilteringColorFormatter() { this.DocumentEditor.Text = OriginalInputLines; }
        
        /// <summary>
        /// Configures line filtering helper
        /// </summary>
        /// <param name="InputTextLines"></param>
        public DebugLogLineFilteringColorFormatter(TextEditor InputEditor, string FilteringKey)
        {
            // Store the matching string. Remove Regex call if needed.
            this.FilterKey = FilteringKey;
            this.DocumentEditor = InputEditor;
            this.OriginalInputLines = InputEditor.Text;
        }

        // --------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Color the line containing the text where we need to select
        /// </summary>
        /// <param name="InputLine"></param>
        protected override void ColorizeLine(DocumentLine InputLine)
        {
            // Find if the line provided is in the lines of our editor
            string InputText = DocumentEditor.Document.GetText(InputLine);
            if (InputText.Contains(this.FilterKey)) return;

            // Filter our the ones we don't need now.
            var DocLinesFiltered = DocumentEditor.Document.Lines.Where(DocObj => DocObj != InputLine).ToList();
            string NewTextValue = string.Join("\n", DocLinesFiltered.Select(DocLine => DocumentEditor.Document.GetText(DocLine).Trim()));
            this.DocumentEditor.Text = NewTextValue;
        }
        /// <summary>
        /// Resets the document object content to the original line values
        /// </summary>
        public void ResetDocumentContent()
        {
            // Reset text values here and return out
            this.DocumentEditor.Text = this.OriginalInputLines;
        }
    }
}
