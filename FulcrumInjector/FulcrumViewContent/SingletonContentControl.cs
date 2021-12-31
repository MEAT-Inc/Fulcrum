using System;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Forms;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent
{
    /// <summary>
    /// Singleton instance builder for user controls.
    /// This forces us to only pass in ViewModelControl base instance objects.
    /// </summary>
    public class SingletonContentControl<TViewType, TViewModelType> where TViewModelType : ViewModelControlBase
    {
        // Singleton watchdog Logger. Build this once the pipe is built.
        private static SubServiceLogger SingletonLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SingletonContentLogger")) ?? new SubServiceLogger("SingletonContentLogger");

        // List of currently open objects for our singleton types and methods for finding existing ones.
        public static SingletonContentControl<TViewType, TViewModelType>[] BuiltSingletonInstances = Array.Empty<SingletonContentControl<TViewType, TViewModelType>>();
        /// <summary>
        /// Builds a new lazy instance based on the values provided in the input args
        /// </summary>
        /// <typeparam name="TViewType">Type of user control for the view model content</typeparam>
        /// <param name="TypeToCreate"></param>
        /// <returns>True if built ok. False if not.</returns>
        internal static SingletonContentControl<TViewType, TViewModelType> CreateSingletonInstance(Type ViewType, Type ViewModelType)
        {
            // Build new instance of this singleton helper and return it
            SingletonLogger.WriteLog($"TRYING TO BUILD NEW SINGLETON INSTANCE FOR VIEW TYPE {ViewType.GetType().Name}...", LogType.WarnLog);
            SingletonLogger.WriteLog($"VIEWMODEL TYPE ASSOCIATED WITH CONTENT IS: {ViewModelType.GetType().Name}", LogType.InfoLog);
            var LocatedSingleton = LocateSingletonInstance(ViewType);
            if (LocatedSingleton != null)
            {
                // Log found existing instance and return it out
                SingletonLogger.WriteLog("FOUND EXISTING INSTANCE OBJECT ENTRY! RETURNING IT NOW...", LogType.InfoLog);
                SingletonLogger.WriteLog($"EXISTING INSTANCE HAS BEEN BUILT AND DEFINED SINCE: {LocatedSingleton.TimeCreated:s}", LogType.TraceLog);

                // Return current instance.
                return LocatedSingleton;
            }

            // Build a new instance object type here and store values.
            TViewType ViewContent = Activator.CreateInstance<TViewType>();
            TViewModelType ViewModelContent = (TViewModelType)ViewContent.GetType()
                .GetProperty("ViewModel")
                ?.GetValue(ViewContent);

            SingletonLogger.WriteLog("BUILT NEW INSTANCE FOR VIEW AND VIEW MODEL CONTENT OK!", LogType.WarnLog);
            var NewSingletonInstance = new SingletonContentControl<TViewType, TViewModelType>(ViewContent, ViewModelContent);
            BuiltSingletonInstances = BuiltSingletonInstances.Append(NewSingletonInstance).ToArray();
        
            // Log information and return.
            SingletonLogger.WriteLog("STORED NEW SINGLETON INSTANCE ON STATIC LIST OK!", LogType.InfoLog);
            return NewSingletonInstance;
        }
        /// <summary>
        /// Returns the first or only instance object for our current instances of singletons
        /// </summary>
        /// <param name="ViewModelControl"></param>
        /// <returns></returns>
        public static SingletonContentControl<TViewType, TViewModelType> LocateSingletonInstance(Type TypeToLocate)
        {
            // If not type of view model control base, then dump out.
            if (TypeToLocate.BaseType != typeof(TViewType) && TypeToLocate.BaseType != typeof(TViewModelType))
                throw new InvalidCastException($"CAN NOT USE A NON VIEW MODEL CONTROL BASE TYPE FOR SINGLETON LOOKUPS!");

            // Find first object with the type matching the given viewmodel type
            var PulledSingleton = BuiltSingletonInstances.FirstOrDefault(ViewObj => ViewObj.SingletonViewModel.GetType() == typeof(TViewType));
            if (PulledSingleton == null) SingletonLogger.WriteLog("FAILED TO LOCATE VALID SINGLETON INSTANCE!", LogType.ErrorLog);
            return PulledSingleton;
        }

        // -------------------------------------------------------------------------------------------------------

        // Generic time information about the instance.
        protected DateTime TimeCreated;                         // Time the instance was built.
        private protected SubServiceLogger InstanceLogger;      // Instance watchdog Logger. Build this once the pipe is built.

        // Instance class values for build objects
        public readonly TViewType SingletonUserControl;         // User control content for this control input.
        public readonly TViewModelType SingletonViewModel;      // Base view model control object used to build this singleton

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a singleton object helper type.
        /// </summary>
        private SingletonContentControl(TViewType singletonUserControlContent, TViewModelType singletonViewModelContent)
        {
            // Store time information.
            this.TimeCreated = DateTime.Now;

            // Build new logger instance for singleton object
            string TypeName = singletonUserControlContent.GetType().Name;
            this.InstanceLogger = (SubServiceLogger)(LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith($"Singleton_{TypeName}_InstanceLogger")) ?? 
                                                     new SubServiceLogger($"Singleton_{TypeName}_InstanceLogger"));
            this.InstanceLogger.WriteLog($"INSTANCE HAS BEEN CREATED AND TIMESTAMPED! TIME BUILT: {this.TimeCreated:s}", LogType.TraceLog);

            // Log building new singleton instance object
            this.SingletonUserControl = singletonUserControlContent;
            this.SingletonViewModel = singletonViewModelContent;
            this.InstanceLogger.WriteLog($"STORED NEW SINGLETON INSTANCE OBJECT FOR TYPE {typeof(TViewType)}!", LogType.InfoLog);
        }

        /// <summary>
        /// Deconstructor for singleton helper class object 
        /// </summary>
        ~SingletonContentControl()
        {
            // Log building new removed list and remove the object from static contents.
            this.InstanceLogger.WriteLog($"DECONSTRUCTING A SINGLETON USER CONTROL OBJECT FOR TYPE {typeof(TViewType)}...", LogType.WarnLog);
            this.InstanceLogger.WriteLog($"INSTANCE HAS BEEN ALIVE FOR A TOTAL OF {(DateTime.Now - this.TimeCreated).ToString("g")}", LogType.TraceLog);

            // Remove it from our list of instances here and store result.
            var SingletonsCleaned = BuiltSingletonInstances.Where(SingletonObj => SingletonObj != this);
            BuiltSingletonInstances = SingletonsCleaned.ToArray();
        }
    }
}
