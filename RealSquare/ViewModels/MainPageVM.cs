using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Windows;
using FoursquareApi;
using FoursquareApi.Objects;
using ARFinity;

namespace RealSquare.ViewModels
{
    public class MainPageVM : ViewModelBase
    {
        #region IDisposable Members

        public override void Dispose()
        {
            MyLocationsVM.Dispose();
            base.Dispose();
        }

        #endregion

        #region IMobile Members

        public override void Initialize()
        {
            if (MyLocationsVM != null)
                MyLocationsVM.Initialize();

            if (!MySettingsVM.EnableLocation)
            {
                MessageBox.Show("Location Services are disabled for this application. Application cannot be used without location services. Please read the privacy statement and activate location services", "Location Services", MessageBoxButton.OK);                
            }
            else
                if (_watcher != null)
                {
                    _watcher.Start();
                    _watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(_watcher_PositionChanged);
                }

            base.Initialize();
        }

        public override void Suspend()
        {
            if (MyLocationsVM != null)
                MyLocationsVM.Suspend();

            if (_watcher != null)
            {
                _watcher.Stop();
            }

            base.Suspend();
        }

        #endregion

        #region Properties

        private GeoCoordinateWatcher _watcher = new GeoCoordinateWatcher();

        /// <summary>
        /// A pointer for component settings for RI
        /// </summary>
        public SettingsVM MySettingsVM { get { return SettingsVM.Instance; } }

        /// <summary>
        /// Gets the Last NearbySearch Radius
        /// </summary>
        public int LastRadius { get; protected set; }

        private LocationsVM _myLocationsVM = new LocationsVM();
        /// <summary>
        /// NearByLocations Data
        /// </summary>
        public LocationsVM MyLocationsVM
        {
            get { return _myLocationsVM; }
            set
            {
                if (value != _myLocationsVM)
                {
                    _myLocationsVM = value;
                    NotifyPropertyChanged("MyLocationsVM");
                }
            }
        }

        /// <summary>
        /// Refresh NearByLocations with Foursquare GeoCoordinate Service
        /// </summary>
        internal void RefreshNearByLocations()
        {
            GeoCoordinate gpsPosition = _watcher.Position.Location;

            MyLocationsVM.IsRefreshing = true;
            MyLocationsVM.NearByLocations.Clear();

            if (string.IsNullOrEmpty(SettingsVM.Instance.FoursquareClientID) || string.IsNullOrEmpty(SettingsVM.Instance.FoursquareClientSecret))
                throw new Exception("Foursquare keys are missing, please provide Client ID and Client Secret from http://www.foursquare.com for using forsquare api v2");

            FoursquareClient fc = new FoursquareClient(SettingsVM.Instance.FoursquareClientID, SettingsVM.Instance.FoursquareClientSecret);
            fc.GetNearbyVenuesFailed += (object sender, EventArgs e) =>
            {
                if (MessageBox.Show("foursquare is unreachable right now, do you want to try again?", "searching for nearby locations", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    RefreshNearByLocations();
            };
            fc.GetNearbyVenuesCompleted += (List<Nearby> nearbys) =>
            {
                MyLocationsVM.IsRefreshing = false;
                LastRadius = SettingsVM.Instance.SearchRadius;

                var nearbysSorted = from p in nearbys orderby p.location.distance ascending select p;

                MyLocationsVM.NearByLocations.Clear();
                foreach (Nearby n in nearbysSorted)
                    MyLocationsVM.NearByLocations.Add(new LocationsVM.Location(n.name,
                                                      new GeoCoordinate(n.location.lat, n.location.lng),
                                                      n.location.address,
                                                      n.location.city,
                                                      n.location.distance,
                                                      n.categories != null ? (n.categories.Count > 0 ? n.categories[0].icon : "") : ""));
            };
            fc.GetNearbyVenuesAsync(gpsPosition.Latitude, gpsPosition.Longitude, SettingsVM.Instance.SearchRadius);
        }

        private void _watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            RefreshNearByLocations();
            _watcher.Stop();
        }

        /// <summary>
        /// Gets the application title
        /// </summary>
        public string ApplicationTitle { get { return "real square"; } }

        #endregion

        #region events
        public event EventHandler OnGotoSettings;
        #endregion

        public MainPageVM()
        {
            //a bing map development key shuold be provided, if using RIComponent map solution intended.
            //if there is a map solution allready BingMapKey could be null
            MySettingsVM.BingMapKey = "AkYJKL9OPV--0tmHZv1XfLK02-oSxFOE2LapFq30wI3giF7OoSizSLSZMlo5tkwv";
            MySettingsVM.FoursquareClientID = "KEQBJ2GGUEDCPFP3EG0CF2GVUVHELHJT4FYJRDWZPKOEYY0O";
            MySettingsVM.FoursquareClientSecret = "40QT5NOVLA415B4QMOMC14PT4VWLERY1GVNJSXYRPSDY3EMT";
        }
    }
}
