using System;
using System.Linq;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// Singleton instance builder for user controls.
    /// This forces us to only pass in ViewModelControl base instance objects.
    /// </summary>
    public class SingletonContentControl<TViewType, TViewModelType> where TViewModelType : ViewModelControlBase
    {
        // Singleton watchdog Logger. Build this once the pipe is built.
     //   private static SubServiceLogger SingletonLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("SingletonContentLogger", LoggerActions.SubServiceLogger);

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------

        // List of currently open objects for our singleton types and methods for finding existing ones.
        private static SingletonContentControl<TViewType, TViewModelType>[] _builtSingletonInstances = Array.Empty<SingletonContentControl<TViewType, TViewModelType>>();
        public static SingletonContentControl<TViewType, TViewModelType>[] BuiltSingletonInstances { get => _builtSingletonInstances; private set => _builtSingletonInstances = value; }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new lazy instance based on the values provided in the input args
        /// </summary>
        /// <typeparam name="TViewType">Type of user control for the view model content</typeparam>
        /// <param name="TypeToCreate"></param>
        /// <returns>True if built ok. False if not.</returns>
        internal static SingletonContentControl<TViewType, TViewModelType> CreateSingletonInstance(Type ViewType, Type ViewModelType)
        {
            // Build new instance of this singleton helper and return it
          //  SingletonLogger.WriteLog($"TRYING TO BUILD NEW SINGLETON INSTANCE FOR VIEW TYPE {ViewType.Name}...", LogType.WarnLog);
          //  SingletonLogger.WriteLog($"VIEWMODEL TYPE ASSOCIATED WITH CONTENT IS: {ViewModelType.Name}", LogType.InfoLog);
            var LocatedSingleton = LocateSingletonViewInstance(ViewType);
            if (LocatedSingleton != null)
            {
                // Log found existing instance and return it out
               // SingletonLogger.WriteLog("FOUND EXISTING INSTANCE OBJECT ENTRY! RETURNING IT NOW...", LogType.InfoLog);
              //  SingletonLogger.WriteLog($"EXISTING INSTANCE HAS BEEN BUILT AND DEFINED SINCE: {LocatedSingleton.TimeCreated:s}", LogType.TraceLog);

                // Return current instance.
                return LocatedSingleton;
            }

            // Build a new instance object type here and store values.
            TViewType ViewContent = (TViewType)Activator.CreateInstance(ViewType);
            TViewModelType ViewModelContent = (TViewModelType)ViewContent.GetType().GetProperty("ViewModel")?.GetValue(ViewContent);

          //  SingletonLogger.WriteLog("BUILT NEW INSTANCE FOR VIEW AND VIEW MODEL CONTENT OK!", LogType.WarnLog);
            var NewSingletonInstance = new SingletonContentControl<TViewType, TViewModelType>(ViewContent, ViewModelContent);
            BuiltSingletonInstances = BuiltSingletonInstances.Append(NewSingletonInstance).ToArray();
        
            // Log information and return.
         //   SingletonLogger.WriteLog("STORED NEW SINGLETON INSTANCE ON STATIC LIST OK!", LogType.InfoLog);
            return NewSingletonInstance;
        }
        /// <summary>
        /// Builds a new lazy instance based on the values provided in the input args
        /// </summary>
        /// <typeparam name="TViewType">Type of user control for the view model content</typeparam>
        /// <param name="ViewObject">View content to show</param>
        /// <returns>True if built ok. False if not.</returns>
        internal static SingletonContentControl<TViewType, TViewModelType> RegisterAsSingleton(TViewType ViewObject, TViewModelType ViewModelObject)
        {
            // Build new instance of this singleton helper and return it
          //  SingletonLogger.WriteLog($"TRYING TO REGISTER NEW SINGLETON INSTANCE FOR VIEW TYPE {ViewObject.GetType().Name}...", LogType.WarnLog);
          //  SingletonLogger.WriteLog($"VIEWMODEL TYPE ASSOCIATED WITH CONTENT IS: {ViewModelObject.GetType().Name}", LogType.InfoLog);
            var LocatedSingleton = LocateSingletonViewInstance(ViewObject.GetType());
            if (LocatedSingleton != null)
            {
                // Log found existing instance and return it out
             //   SingletonLogger.WriteLog("FOUND EXISTING INSTANCE OBJECT ENTRY! RETURNING IT NOW...", LogType.InfoLog);
             //   SingletonLogger.WriteLog($"EXISTING INSTANCE HAS BEEN BUILT AND DEFINED SINCE: {LocatedSingleton.TimeCreated:s}", LogType.TraceLog);
             //   SingletonLogger.WriteLog("UPDATING DEFINITIONS FOR THIS OBJECT NOW...", LogType.WarnLog);

                // Pull the singleton from our list and replace the content.
                int IndexOfSingleton = BuiltSingletonInstances.ToList().IndexOf(LocatedSingleton);
                BuiltSingletonInstances[IndexOfSingleton] = new SingletonContentControl<TViewType, TViewModelType>(ViewObject, ViewModelObject);
             //   SingletonLogger.WriteLog("UPDATED CONTENTS OF OUR SINGLETON VIEW OBJECT OK!", LogType.InfoLog);

                // Return current instance.
                return BuiltSingletonInstances[IndexOfSingleton];
            }

            // Build new instance and return output.
          //  SingletonLogger.WriteLog("BUILT NEW INSTANCE FOR VIEW AND VIEW MODEL CONTENT OK!", LogType.WarnLog);
            var NewSingletonInstance = new SingletonContentControl<TViewType, TViewModelType>(ViewObject, ViewModelObject);
            BuiltSingletonInstances = BuiltSingletonInstances.Append(NewSingletonInstance).ToArray();

            // Log information and return.
          //  SingletonLogger.WriteLog("STORED NEW SINGLETON INSTANCE ON STATIC LIST OK!", LogType.InfoLog);
            return NewSingletonInstance;
        }
        
        /// <summary>
        /// Returns the first or only instance object for our current instances of singletons
        /// </summary>
        /// <param name="ViewModelControl"></param>
        /// <returns></returns>
        public static SingletonContentControl<TViewType, TViewModelType> LocateSingletonViewInstance(Type ViewTypeToLocate)
        {
            // If not type of view model control base, then dump out.
            if (ViewTypeToLocate != typeof(TViewType) && ViewTypeToLocate.BaseType != typeof(TViewType))
            {
                // IF this failed, try using VMs before failing out.
                var ViewModelFallbackValue = LocateSingletonViewModelInstance(ViewTypeToLocate);
                if (ViewModelFallbackValue != null) return ViewModelFallbackValue;
                throw new InvalidCastException($"CAN NOT USE A NON USER CONTROL BASE TYPE FOR SINGLETON LOOKUPS!");
            }
            
            // Find first object with the type matching the given viewmodel type
            var PulledSingleton = BuiltSingletonInstances.FirstOrDefault(ViewObj => ViewObj.SingletonUserControl.GetType() == ViewTypeToLocate);
         //   if (PulledSingleton == null) SingletonLogger.WriteLog("FAILED TO LOCATE VALID SINGLETON INSTANCE!", LogType.ErrorLog);
            return PulledSingleton;
        }
        /// <summary>
        /// Returns the first or only instance object for our current instances of singletons
        /// </summary>
        /// <param name="ViewModelTypeToLocate"></param>
        /// <returns></returns>
        public static SingletonContentControl<TViewType, TViewModelType> LocateSingletonViewModelInstance(Type ViewModelTypeToLocate)
        {
            // If not type of view model control base, then dump out.
            if (ViewModelTypeToLocate != typeof(TViewModelType) && ViewModelTypeToLocate.BaseType != typeof(TViewModelType))
            { 
                // IF this failed, try using view objects before failing out.
                var UserControlFallbackValue = LocateSingletonViewInstance(ViewModelTypeToLocate);
                if (UserControlFallbackValue != null) return UserControlFallbackValue;
                throw new InvalidCastException($"FAILED TO LOCATE A USER CONTROL OR VIEW MODEL MATCHING TYPE: {ViewModelTypeToLocate.Name}!");
            }

            // Find first object with the type matching the given viewmodel type
            var PulledSingleton = BuiltSingletonInstances.FirstOrDefault(ViewObj => ViewObj.SingletonViewModel.GetType() == ViewModelTypeToLocate);
          //  if (PulledSingleton == null) SingletonLogger.WriteLog("FAILED TO LOCATE VALID SINGLETON INSTANCE!", LogType.ErrorLog);
            return PulledSingleton;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------

        // Generic time information about the instance.
        protected DateTime TimeCreated;                         // Time the instance was built.
        private protected SubServiceLogger InstanceLogger;      // Instance watchdog Logger. Build this once the pipe is built.

        // Instance class values for build objects
        public readonly TViewType SingletonUserControl;         // User control content for this control input.
        public readonly TViewModelType SingletonViewModel;      // Base view model control object used to build this singleton

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new instance of a singleton object helper type.
        /// </summary>
        private SingletonContentControl(TViewType SingletonUserControlContent, TViewModelType SingletonViewModelContent)
        {
            // Store time information.
            this.TimeCreated = DateTime.Now;
            BuiltSingletonInstances ??= Array.Empty<SingletonContentControl<TViewType, TViewModelType>>();

            // Build new logger instance for singleton object
            string TypeName = SingletonUserControlContent.GetType().Name;
            this.InstanceLogger = (SubServiceLogger)LoggerQueue.SpawnLogger($"Singleton_{TypeName}_InstanceLogger", LoggerActions.SubServiceLogger);
          //  this.InstanceLogger.WriteLog($"INSTANCE HAS BEEN CREATED AND TIMESTAMPED! TIME BUILT: {this.TimeCreated:s}", LogType.TraceLog);

            // Log building new singleton instance object
            this.SingletonUserControl = SingletonUserControlContent;
            this.SingletonViewModel = SingletonViewModelContent;
         //   this.InstanceLogger.WriteLog($"STORED NEW SINGLETON INSTANCE OBJECT FOR TYPE {typeof(TViewType)}!", LogType.InfoLog);
        }

        /// <summary>
        /// Deconstructor for singleton helper class object 
        /// </summary>
        ~SingletonContentControl()
        {
            // Log building new removed list and remove the object from static contents.
         //   this.InstanceLogger.WriteLog($"DECONSTRUCTING A SINGLETON USER CONTROL OBJECT FOR TYPE {typeof(TViewType)}...", LogType.WarnLog);
         //   this.InstanceLogger.WriteLog($"INSTANCE HAS BEEN ALIVE FOR A TOTAL OF {(DateTime.Now - this.TimeCreated).ToString("g")}", LogType.TraceLog);

            // Remove it from our list of instances here and store result.
            // var SingletonsCleaned = BuiltSingletonInstances.Where(SingletonObj => SingletonObj != this);
            // BuiltSingletonInstances = SingletonsCleaned.ToArray();
        }
    }
}
