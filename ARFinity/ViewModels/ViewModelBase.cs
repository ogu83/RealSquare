using System;
using System.ComponentModel;


namespace ARFinity
{
    /// <summary>
    /// Base of the view models with PropertyChanged Interface
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable, IMobileVM
    {
        /// <summary>
        /// Error
        /// </summary>
        /// <param name="error"></param>
        public delegate void ErrorEventhandler(Enums.ErrorsEnum error);
        /// <summary>
        /// Error raised event for all of the view models of this application.
        /// </summary>
        public virtual event ErrorEventhandler ErrorRaised;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes this view model
        /// </summary>
        public virtual void Dispose()
        {

        }

        #endregion

        #region IMobileVM Members

        /// <summary>
        /// can begin with on navigated of parent page
        /// inits the necessary resources of this viewmodel
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// can begin with on navigated from event of parent page
        /// suspends the necessary resources of this viewmodel
        /// </summary>
        public virtual void Suspend()
        {

        }

        internal virtual void InternalInitialize() { }

        internal virtual void InternalSuspend() { }

        #endregion
    }
}