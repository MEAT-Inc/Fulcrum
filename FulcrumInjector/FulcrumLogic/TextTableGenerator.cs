using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FulcrumInjector.FulcrumLogic
{
    /// <summary>
    /// Class used to convert a set of tuples of values into an ASCII printed text table.
    /// </summary>
    public static class TextTableGenerator
    {
        /// <summary>
        /// Converts table object into our string output.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="TableValues"></param>
        /// <param name="ColumnHeaders"></param>
        /// <param name="ValueSelectors"></param>
        /// <returns></returns>
        public static string ToStringTable<T>(this IEnumerable<T> TableValues, string[] ColumnHeaders, params Func<T, object>[] ValueSelectors) {
            return ToStringTable(TableValues.ToArray(), ColumnHeaders, ValueSelectors);
        }
        /// <summary>
        /// Converts tuple set into a table object for conversion
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="TableValues"></param>
        /// <param name="ColumnHeaders"></param>
        /// <param name="ValueSelectors"></param>
        /// <returns></returns>
        public static string ToStringTable<T>(this T[] TableValues, string[] ColumnHeaders, params Func<T, object>[] ValueSelectors)
        {
            // Fill headers in using the given params for this method.
            var ArrayValues = new string[TableValues.Length + 1, ValueSelectors.Length];
            for (int ColIndex = 0; ColIndex < ArrayValues.GetLength(1); ColIndex++) 
                ArrayValues[0, ColIndex] = ColumnHeaders[ColIndex];

            // Fill table rows in looping the contents over time.
            for (int RowIndex = 1; RowIndex < ArrayValues.GetLength(0); RowIndex++) 
                for (int ColIndex = 0; ColIndex < ArrayValues.GetLength(1); ColIndex++)
                    ArrayValues[RowIndex, ColIndex] = ValueSelectors[ColIndex]
                      .Invoke(TableValues[RowIndex - 1]).ToString();

            // Return built table output as a string here.
            return ToStringTable(ArrayValues);
        }
        /// <summary>
        /// Converts object of array type into a table string output.
        /// </summary>
        /// <param name="InputArrayValues"></param>
        /// <returns>String cast table object.</returns>
        public static string ToStringTable(this string[,] InputArrayValues)
        {
            // Set Col Width value and the header splitting value string size.
            int[] MaxColWidth = GetMaxColumnsWidth(InputArrayValues);
            var HeaderSplitter = new string('-', MaxColWidth.Sum(i => i + 3) - 1);

            // Now build output via string builder.
            var TableBuilder = new StringBuilder();
            for (int RowIndex = 0; RowIndex < InputArrayValues.GetLength(0); RowIndex++) {
                for (int ColIndex = 0; ColIndex < InputArrayValues.GetLength(1); ColIndex++) 
                {
                    // Print cell object into our string builder.
                    string cell = InputArrayValues[RowIndex, ColIndex];
                    cell = cell.PadRight(MaxColWidth[ColIndex]);
                    TableBuilder.Append(" | ");
                    TableBuilder.Append(cell);
                }

                // Print end of line to builder
                TableBuilder.Append(" | ");
                TableBuilder.AppendLine();

                // Print splitter if not on the first row
                if (RowIndex != 0) continue;
                TableBuilder.AppendFormat(" |{0}| ", HeaderSplitter);
                TableBuilder.AppendLine();
            }

            // Return built output from string builder here.
            string TableString = TableBuilder.ToString();
            int TableWidth = TableString.Split('\n')[0].Length;
            string PaddingLineString = $"+{string.Join("", Enumerable.Repeat("=", TableWidth - 5))}+";
            string FinalTableString = $"{PaddingLineString}\n{TableString}{PaddingLineString}".Replace("\r\n", "\n");
            return string.Join("\n", FinalTableString.Split('\n').Select(LineObj => LineObj.TrimStart()));
        }


        /// <summary>
        /// Gets the max size of a column width based on the input values given
        /// </summary>
        /// <param name="InputArrayValues"></param>
        /// <returns>Sizes based on array input values.</returns>
        private static int[] GetMaxColumnsWidth(string[,] InputArrayValues)
        {
            // Find the new max size and store them into an equal size output array
            var MaxWidth = new int[InputArrayValues.GetLength(1)];
            for (int ColIndex = 0; ColIndex < InputArrayValues.GetLength(1); ColIndex++) 
                for (int rowIndex = 0; rowIndex < InputArrayValues.GetLength(0); rowIndex++) 
                {
                    // Store new and old size values. Compare and see if we need to update
                    int NewLength = InputArrayValues[rowIndex, ColIndex].Length;
                    int OldLength = MaxWidth[ColIndex];
                    if (NewLength > OldLength) { MaxWidth[ColIndex] = NewLength; }
                }
            
            // Return the output size array object
            return MaxWidth;
        }
    }
}
