// Using calls for Assembly Info updating
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// General Information
[assembly: AssemblyTitle("FulcrumService")]
[assembly: AssemblyDescription("Supporting logic for any of the FulcrumInjector service objects")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("MEAT Inc")]
[assembly: AssemblyProduct("FulcrumService")]
[assembly: AssemblyCopyright("Copyright ©MEAT Inc 2023")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Internal Visibility for testing
[assembly: ComVisible(false)]
[assembly: Guid("9ed477f5-f3dc-4643-b7cb-e2fc3f33d652")]

// TODO: Uncomment this out to enable access to internal members for child services
// [assembly: InternalsVisibleTo("FulcrumDriveService")]
// [assembly: InternalsVisibleTo("FulcrumEmailService")]
// [assembly: InternalsVisibleTo("FulcrumUpdaterService")]
// [assembly: InternalsVisibleTo("FulcrumWatchdogService")]

// Version information
[assembly: AssemblyVersion("0.5.5.174")]
[assembly: AssemblyFileVersion("0.5.5.174")]
[assembly: NeutralResourcesLanguageAttribute( "en-US" )]

