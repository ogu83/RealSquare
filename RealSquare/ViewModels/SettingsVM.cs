using System;
using System.Collections.ObjectModel;
using ARFinity;
using System.Reflection;
using System.IO.IsolatedStorage;

namespace RealSquare.ViewModels
{
    public class SettingsVM : ViewModelBase
    {
        private static SettingsVM _instance = new SettingsVM();
        /// <summary>
        /// Gets the settings data of the RI componenet
        /// </summary>
        public static SettingsVM Instance { get { return _instance; } }

        public event EventHandler SearchRadiusChanged;

        /// <summary>
        /// Gets or Sets the bing map application key for Instance of this component
        /// If the MapView will be used this key should be provided from https://www.bingmapsportal.com/
        /// If application has allready a map and location solution this BingMapKey can be null.
        /// </summary>
        public string BingMapKey { get; set; }

        /// <summary>
        /// Gets or Sets foursquare application Client ID for Instance of this component
        /// If the foursquare location services will be used this key should be provided from https://www.foursquare.com/
        /// If application has allready a map and location solution this BingMapKey can be null.
        /// </summary>
        public string FoursquareClientID { get; set; }

        /// <summary>
        /// Gets or Sets foursquare application Client Secret for Instance of this component
        /// If the foursquare location services will be used this key should be provided from https://www.foursquare.com/
        /// If application has allready a map and location solution this BingMapKey can be null.
        /// </summary>
        public string FoursquareClientSecret { get; set; }

        private bool _accuracyRadiusVisible;
        /// <summary>
        /// Enable or Disable the blue-transparent circle on the map, which shows the radius of accuracy of gps signal
        /// </summary>
        public bool AccuracyRadiusVisible
        {
            get { return _accuracyRadiusVisible; }
            set
            {
                if (_accuracyRadiusVisible != value)
                {
                    _accuracyRadiusVisible = value;
                    NotifyPropertyChanged("AccuracyRadiusVisible");
                }
            }
        }

        public bool EnableLocation
        {
            get
            {
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("EnableLocation"))
                    return true;
                else
                    return (bool)IsolatedStorageSettings.ApplicationSettings["EnableLocation"];
            }
            set
            {
                IsolatedStorageSettings.ApplicationSettings["EnableLocation"] = value;
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        private int _searchRadius = 2000; //default searc radius 2000 meters
        /// <summary>
        /// Gets or sets the search radius for nearby locations
        /// </summary>
        public int SearchRadius
        {
            get { return _searchRadius; }
            set
            {
                if (_searchRadius != value)
                {
                    _searchRadius = value;

                    NotifyPropertyChanged("SearchRadius");

                    if (SearchRadiusChanged != null)
                        SearchRadiusChanged(this, null);
                }
            }
        }
    }
}
