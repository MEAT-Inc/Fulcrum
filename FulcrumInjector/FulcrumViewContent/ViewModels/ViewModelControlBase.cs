using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// Base class for Model objects on the UI
    /// </summary>
    public class ViewModelControlBase : INotifyPropertyChanged
    {
        // Logger object.
        private static SubServiceLogger ViewModelPropLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("ViewModelPropLogger", LoggerActions.SubServiceLogger);

        // --------------------------------------------------------------------------------------------------------------------------

        // View object to setup and custom setter
        internal UserControl BaseViewControl;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new instance of our view model control methods
        /// </summary>
        /// <param name="ContentView"></param>
        internal virtual void SetupViewControl(UserControl ContentView) { BaseViewControl = ContentView; }

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
            // Property Changed Event
            OnPropertyChanged(PropertyName);

            // Update VM Value and Global value
            UpdateViewModelPropertyValue(this);
            UpdatePrivatePropertyValue(this, PropertyName, Value);
        }

        /// <summary>
        /// Property Changed without model binding
        /// </summary>
        /// <param name="PropertyName">Name of property to emit change for</param>
        /// <param name="NotifierObject">Object sending this out</param>
        private void UpdatePrivatePropertyValue(object NotifierObject, string PropertyName, object NewPropValue)
        {
            // Store the type of the sender
            var InputObjType = NotifierObject.GetType();

            // Loop all fields, find the private value and store it
            var MembersFound = InputObjType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var MemberObject = MembersFound.FirstOrDefault(FieldObj =>
                FieldObj.Name.Contains("_") &&
                FieldObj.Name.Substring(1).ToUpper() == PropertyName.ToUpper());

            // Try serialization here. Set if failed.
            switch (MemberObject.MemberType)
            {
                // For Field Objects
                case MemberTypes.Field:
                    FieldInfo InvokerField = (FieldInfo)MemberObject;
                    InvokerField.SetValue(NotifierObject, NewPropValue);
                    break;

                // For Property objects
                case MemberTypes.Property:
                    PropertyInfo InvokerProperty = (PropertyInfo)MemberObject;
                    InvokerProperty.SetValue(NotifierObject, NewPropValue);
                    break;

                // Throw if value to modify is not found
                default: throw new NotImplementedException($"THE INVOKED MEMBER {PropertyName} COULD NOT BE FOUND!");
            }
        }
        /// <summary>
        /// Updates the globals with the new values configured into this object 
        /// </summary>
        /// <param name="ViewModelObject">Object to update</param>
        private void UpdateViewModelPropertyValue(ViewModelControlBase ViewModelObject)
        {
            // Get the types on the globals first.
            var AppViewStoreType = typeof(FulcrumConstants);
            var ViewModelTypeName = Type.GetType(ViewModelObject.ToString());
            if (AppViewStoreType == null) { throw new NullReferenceException($"THE TYPE {typeof(FulcrumConstants).ToString()} COULD NOT BE FOUND!"); }

            // Gets all the members and sets one to update
            var AppStoreMembers = AppViewStoreType.GetMembers();
            if (FulcrumConstants.InjectorMainWindow == null) { return; }

            // If the main window isn't null keep going.
            var MemberToUpdate = AppStoreMembers.FirstOrDefault((MemberObj) =>
            {
                // Remove the viewmodel text and search.
                string ComponentTypeRemoved = ViewModelTypeName.Name.Replace("ViewModel", string.Empty);
                return MemberObj.Name.StartsWith(ComponentTypeRemoved) && MemberObj.Name.Contains("ViewModel");
            });

            // Find if the VM is a core option or misc singleton
            bool IsViewModelType =
                ViewModelTypeName.Name.Contains("CoreViewModels") ||
                ViewModelTypeName.Name.Contains("OptionViewModels") ||
                ViewModelTypeName.Name.Contains("MiscViewModels");

            // Switch for fields vs properties
            switch (MemberToUpdate.MemberType)
            {
                // For Field based objects
                case MemberTypes.Field:
                    FieldInfo MemberAsField = (FieldInfo)MemberToUpdate;
                    try
                    {
                        // Check for singleton content object
                        if (!IsViewModelType)
                        {
                            // Try setting value inside this block in case VM value has no public setter.
                            MemberAsField.SetValue(null, ViewModelObject);
                            return;
                        }

                        // Try and find Object for our singleton instance and store a value to it. If this fails, default back to no singleton.
                        var PulledSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(ViewModelObject.GetType());
                        SingletonContentControl<UserControl, ViewModelControlBase>.RegisterAsSingleton(PulledSingleton.SingletonUserControl, ViewModelObject);
                        return;
                    }
                    catch { return; }

                // For Property Based objects
                case MemberTypes.Property:
                    PropertyInfo MemberAsProperty = (PropertyInfo)MemberToUpdate;
                    try
                    {
                        // Check for singleton content object
                        if (!IsViewModelType)
                        {
                            // Try setting value inside this block in case VM value has no public setter.
                            MemberAsProperty.SetValue(null, ViewModelObject);
                            return;
                        }

                        // Try and find Object for our singleton instance and store a value to it. If this fails, default back to no singleton.
                        var PulledSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(ViewModelObject.GetType());
                        SingletonContentControl<UserControl, ViewModelControlBase>.RegisterAsSingleton(PulledSingleton.SingletonUserControl, ViewModelObject);
                        return;
                    }
                    catch { return; }

                // If neither field or property fail out
                default: throw new NotImplementedException($"THE REQUESTED MEMBER {nameof(ViewModelObject)} COULD NOT BE FOUND!");
            }
        }
    }
}
