using System;
using System.Reflection;
using System.Xaml;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Class used to configure schema configuration for markdown release note viewing routines
    /// </summary>
    internal class MarkdownXamlSchemaContext : XamlSchemaContext
    {
        /// <summary>
        /// Method used to pull in the correct refs to markDig.wpf instead of the default markdig dll
        /// </summary>
        /// <param name="XamlNamespace">Namespace to locate</param>
        /// <param name="CompatibleNamespace">Namespace found as a result</param>
        /// <returns></returns>
        public override bool TryGetCompatibleXamlNamespace(string XamlNamespace, out string CompatibleNamespace)
        {
            // No namespace located, try and force a new one to be found
            if (!XamlNamespace.Equals("clr-namespace:Markdig.Wpf", StringComparison.Ordinal))
                return base.TryGetCompatibleXamlNamespace(XamlNamespace, out CompatibleNamespace);
            
            // If the namespace is founbd, return the name of it and true
            CompatibleNamespace = $"clr-namespace:Markdig.Wpf;assembly={Assembly.GetAssembly(typeof(Markdig.Wpf.Styles)).FullName}"; 
            return true;
        }
    }
}
