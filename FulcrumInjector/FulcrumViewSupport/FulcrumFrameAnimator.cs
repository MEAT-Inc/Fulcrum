using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Animates frame objects
    /// This is used in the nav helper service to control events that are fired when we change tabs in the hamburger core
    /// </summary>
    public class FulcrumFrameAnimator
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields
        #endregion //Fields

        #region Properties

        /// <summary>
        /// Navigation helper which pulls our Storyboard and metadata contents on request
        /// </summary>
        public static readonly DependencyProperty FrameNavigationStoryboardProperty
            = DependencyProperty.RegisterAttached(
                "FrameNavigationStoryboard",
                typeof(Storyboard),
                typeof(FulcrumFrameAnimator),
                new FrameworkPropertyMetadata(null, OnFrameNavigationStoryboardChanged));

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>Helper for setting <see cref="FrameNavigationStoryboardProperty"/> on <paramref name="control"/>.</summary>
        /// <param name="control"><see cref="DependencyObject"/> to set <see cref="FrameNavigationStoryboardProperty"/> on.</param>
        /// <param name="storyboard">FrameNavigationStoryboard property value.</param>
        public static void SetFrameNavigationStoryboard(DependencyObject control, Storyboard storyboard)
        {
            control.SetValue(FrameNavigationStoryboardProperty, storyboard);
        }
        /// <summary>Helper for getting <see cref="FrameNavigationStoryboardProperty"/> from <paramref name="control"/>.</summary>
        /// <param name="control"><see cref="DependencyObject"/> to read <see cref="FrameNavigationStoryboardProperty"/> from.</param>
        /// <returns>FrameNavigationStoryboard property value.</returns>
        [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
        public static Storyboard GetFrameNavigationStoryboard(DependencyObject control)
        {
            return (Storyboard)control.GetValue(FrameNavigationStoryboardProperty);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event to fire when a frame is navigating
        /// </summary>
        /// <param name="SendingControl">The control which has fired a navigation request</param>
        /// <param name="EventArgs">Cancellation event args to stop this event if wanted</param>
        private static void Frame_Navigating(object SendingControl, System.Windows.Navigation.NavigatingCancelEventArgs EventArgs)
        {
            if (SendingControl is Frame frame)
            {
                var sb = GetFrameNavigationStoryboard(frame);
                if (sb != null)
                {
                    var presenter = frame.FindChild<ContentPresenter>();
                    sb.Begin((FrameworkElement)presenter ?? frame);
                }
            }
        }
        /// <summary>
        /// Event handler to fire when the navigation state changes for a storyboard
        /// </summary>
        /// <param name="DepObj">The property firing this event</param>
        /// <param name="DepObjArgs">EventArgs fired along with this event</param>
        private static void OnFrameNavigationStoryboardChanged(DependencyObject DepObj, DependencyPropertyChangedEventArgs DepObjArgs)
        {
            if (DepObj is Frame frame && DepObjArgs.OldValue != DepObjArgs.NewValue)
            {
                frame.Navigating -= Frame_Navigating;
                if (DepObjArgs.NewValue is Storyboard)
                {
                    frame.Navigating += Frame_Navigating;
                }
            }
        }
    }
}
