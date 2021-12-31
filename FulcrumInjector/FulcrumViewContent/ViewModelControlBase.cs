using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent
{
    /// <summary>
    /// Base class for Model objects on the UI
    /// </summary>
    public class ViewModelControlBase : INotifyPropertyChanged
    {
        // Logger object.
        private static SubServiceLogger ViewModelPropLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("ViewModelPropLogger")) ?? new SubServiceLogger("ViewModelPropLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // View object to setup and custom setter
        internal UserControl BaseViewControl;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new instance of our view model control methods
        /// </summary>
        /// <param name="ContentView"></param>
        public virtual void SetupViewControl(UserControl ContentView) { BaseViewControl = ContentView; }

        // --------------------------------------------------------------------------------------------------------------------------

        #region Property Changed Event Setup

        // Property Changed event.
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion

        /// <summary>
        /// Updates the property on this view model and sets a prop notify event
        /// </summary>
        /// <typeparam name="TPropertyType">Type of property</typeparam>
        /// <param name="PropertyName">Name of property </param>
        /// <param name="Value">Value being used</param>
        internal void PropertyUpdated(object Value, [CallerMemberName] string PropertyName = null, bool ForceSilent = false)
        {
            // Run prop changed event and set private value
            OnPropertyChanged(PropertyName);

            // Update Globals and the current value. Log value change done.
            bool ValueChanged = UpdatePrivatePropertyValue(this, PropertyName, Value) || UpdateViewModelPropertyValue(this);
            if (ValueChanged && !ForceSilent) ViewModelPropLogger.WriteLog($"PROPERTY {PropertyName} IS BEING UPDATED NOW WITH VALUE {Value}", LogType.TraceLog);
        }

        /// <summary>
        /// Property Changed without model binding
        /// </summary>
        /// <param name="PropertyName">Name of property to emit change for</param>
        /// <param name="NotifierObject">Object sending this out</param>
        private bool UpdatePrivatePropertyValue(object NotifierObject, string PropertyName, object NewPropValue)
        {
            // Store the type of the sender
            var InputObjType = NotifierObject.GetType();

            // Loop all fields, find the private value and store it
            var MembersFound = InputObjType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var MemberObject = MembersFound.FirstOrDefault(FieldObj =>
                FieldObj.Name.Contains("_") &&
                FieldObj.Name.Substring(1).ToUpper() == PropertyName.ToUpper());

            // Set the model property value here and raise an args value.
            bool ValueChanged = false;
            string NewJson = "";

            // Try serialization here. Set if failed.
            try { NewJson = JsonConvert.SerializeObject(NewPropValue); }
            catch (Exception ExThrown) { ValueChanged = false; }

            // Set Value
            switch (MemberObject.MemberType)
            {
                // Sets the value on the class into the current invoking object
                case MemberTypes.Field:
                    FieldInfo InvokerField = (FieldInfo)MemberObject;
                    try { ValueChanged = NewJson != JsonConvert.SerializeObject(InvokerField.GetValue(NotifierObject)); }
                    catch { ValueChanged = false; }
                    InvokerField.SetValue(NotifierObject, NewPropValue);
                    break;

                case MemberTypes.Property:
                    PropertyInfo InvokerProperty = (PropertyInfo)MemberObject;
                    try { ValueChanged = NewJson != JsonConvert.SerializeObject(InvokerProperty.GetValue(NotifierObject)); }
                    catch { ValueChanged = false; }
                    InvokerProperty.SetValue(NotifierObject, NewPropValue);
                    break;

                default:
                    ValueChanged = false;
                    throw new NotImplementedException($"THE INVOKED MEMBER {PropertyName} COULD NOT BE FOUND!");
            }

            // Return value changed.
            return ValueChanged;
        }
        /// <summary>
        /// Updates the globals with the new values configured into this object 
        /// </summary>
        /// <param name="ViewModelObject">Object to update</param>
        private bool UpdateViewModelPropertyValue(ViewModelControlBase ViewModelObject)
        {
            // Get the types on the globals first.
            var AppViewStoreType = typeof(InjectorConstants);
            var ViewModelTypeName = Type.GetType(ViewModelObject.ToString());
            if (AppViewStoreType == null) { throw new NullReferenceException($"THE TYPE {typeof(InjectorConstants).ToString()} COULD NOT BE FOUND!"); }

            // Gets all the members and sets one to update
            var AppStoreMembers = AppViewStoreType.GetMembers();
            if (InjectorConstants.InjectorMainWindow == null) { return false; }

            // If the main window isn't null keep going.
            var MemberToUpdate = AppStoreMembers.FirstOrDefault((MemberObj) =>
            {
                // Remove the viewmodel text and search.
                string ComponentTypeRemoved = ViewModelTypeName.Name.Replace("ViewModel", string.Empty);
                return MemberObj.Name.StartsWith(ComponentTypeRemoved) && MemberObj.Name.Contains("ViewModel");
            });
            if (MemberToUpdate == null) { throw new NullReferenceException($"THE MEMBER {ViewModelTypeName.Name} COULD NOT BE FOUND!"); }

            // Set the value here
            string NewJson = "";
            bool ValueChanged = false;

            // Try serialization here. Set if failed.
            try { NewJson = JsonConvert.SerializeObject(ViewModelObject); }
            catch (Exception ExThrown) { ValueChanged = false; }

            // Set Value
            switch (MemberToUpdate.MemberType)
            {
                // Sets the value on the class into the current invoking object
                case MemberTypes.Field:
                    FieldInfo MemberAsField = (FieldInfo)MemberToUpdate;
                    try { ValueChanged = NewJson != JsonConvert.SerializeObject(MemberAsField.GetValue(ViewModelObject)); }
                    catch { ValueChanged = false; }
                    MemberAsField.SetValue(null, ViewModelObject);
                    break;

                case MemberTypes.Property:
                    PropertyInfo MemberAsProperty = (PropertyInfo)MemberToUpdate;
                    try { ValueChanged = NewJson != JsonConvert.SerializeObject(MemberAsProperty.GetValue(ViewModelObject)); }
                    catch { ValueChanged = false; }
                    MemberAsProperty.SetValue(null, ViewModelObject);
                    break;

                // If neither field or property fail out
                default:
                    ValueChanged = false;
                    throw new NotImplementedException($"THE REQUESTED MEMBER {nameof(ViewModelObject)} COULD NOT BE FOUND!");
            }

            // Return Changed Value
            return ValueChanged;
        }
    }
}
