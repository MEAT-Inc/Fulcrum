using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Extensions for building simulations from a set of expressions
    /// </summary>
    public static class GenerateSimulationExtensions
    {
        /// <summary>
        /// Pulls out channel id values from a set of input content expressions
        /// </summary>
        /// <param name="Expressions">Input Expressions</param>
        /// <returns></returns>
        public static void ExtractChannelIds(this PassThruExpression[] Expressions)
        {
            // Start by doing a group call out on the input expression objects.
            var ChannelIdGroups = Expressions.GroupBy(
                ExpressionObj => ExpressionObj.GetPropertyValue<int>("ChannelID"),
                ExpressionObj => ExpressionObj,
                (ChannelId, ExpressionGroup) => new { ChannelId, PassThruExpressions = ExpressionGroup.ToList() })
                .ToArray();

            // Make sure it was built out OK
            if (ChannelIdGroups == null) throw new NullReferenceException("FAILED TO BUILD CHANNEL GROUPINGS!");

            // var ChannelIdGroups = Expressions.GroupBy(ExpressionObj =>
            // {
            //     // Find all properties of the expression set.
            //     var ExpressionValues = ExpressionObj.GetExpressionProperties();
            //     var ChannelProperty = ExpressionValues
            //         .FirstOrDefault(FieldObj => FieldObj.Name
            //             .Replace(" ", string.Empty).ToUpper()
            //             .Contains("CHANNELID"));
            // 
            //     // Now make sure we have a property to use here.
            //     return ChannelProperty == null ? 0 : (int)ChannelProperty.GetValue(ExpressionObj);
            // }).ToArray();

            // Return the list of gropings here
            // return ChannelIdGroups;
        }
    }
}
