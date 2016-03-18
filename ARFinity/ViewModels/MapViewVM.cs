using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls.Maps;

namespace ARFinity
{
    public sealed class MapViewVM : ViewModelBase
    {
        #region IDisposable Members

        public override void Dispose()
        {
            if (_watcher != null)
                _watcher.Stop();

            if (_compass != null)
                _compass.Stop();

            base.Dispose();
        }

        #endregion

        #region IMobile Members

        public override event ViewModelBase.ErrorEventhandler ErrorRaised;

        internal override void InternalInitialize()
        {
            try
            {
                _watcher = new GeoCoordinateWatcher();
                _watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
                _watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
                _watcher.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (ErrorRaised != null)
                    ErrorRaised(Enums.ErrorsEnum.GpsNotExist);
            }

            try
            {
                _compass = new Compass();
                _compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(10);
                _compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
                _compass.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (ErrorRaised != null)
                    ErrorRaised(Enums.ErrorsEnum.CompassNotExist);
            }

            try
            {
                _accelerometer = new Accelerometer();
                _accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(500);
                _accelerometer.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
                _accelerometer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (ErrorRaised != null)
                    ErrorRaised(Enums.ErrorsEnum.AccelerometerNotExist);
            }

            //this.NearbyLocations = SettingsVM.Instance.CurrentNearbyLocations;
            this.Zoom = 16;

            NearbyLocations = _NearbyLocations;
            BingMapKey = _BingMapKey;
            AccuracyRadiusVisible = _AccuracyRadiusVisible;
            ApplicationName = _ApplicationName;

            base.InternalInitialize();
            base.Initialize();
        }

        internal override void InternalSuspend()
        {
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher.Dispose();
                _watcher = null;
            }

            if (_accelerometer != null)
            {
                _accelerometer.Stop();
                _accelerometer.Dispose();
                _accelerometer = null;
            }

            if (_compass != null)
            {
                _compass.Stop();
                _compass.Dispose();
                _compass = null;
            }

            base.InternalSuspend();
            base.Suspend();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Credentials according to BingMapKey, which is set in Settings.
        /// </summary>
        internal CredentialsProvider BingMapCredentials
        {
            get
            {
                if (string.IsNullOrEmpty(BingMapKey))
                    throw new Exception("Bing Map Key Required for using Map View, please provide a Bing Map Key from https://www.bingmapsportal.com/");
                else
                    return (new ApplicationIdCredentialsProvider(BingMapKey));
            }
        }

        internal string BingMapKey { get; set; }

        private string _watcherStatus;
        /// <summary>
        /// Gets GPS Location Device Status
        /// </summary>
        internal string WatcherStatus
        {
            get { return _watcherStatus; }
            private set
            {
                if (value != _watcherStatus)
                {
                    _watcherStatus = value;
                    NotifyPropertyChanged("WatcherStatus");
                }
            }
        }

        internal double DefaultZoom = 16;
        private double _zoom = 16;
        /// <summary>
        /// zoom factor of the map object in the UI
        /// </summary>
        public double Zoom
        {
            get { return _zoom; }
            set
            {
                if (value != _zoom)
                {
                    _zoom = value;
                    NotifyPropertyChanged("Zoom");
                    NotifyPropertyChanged("AccuracyDiameter");
                }
            }
        }

        /// <summary>
        /// we assume that this device has pixels/inch value of 96
        /// </summary>
        private double _getDPI
        {
            get { return 96; }
        }

        private double _scale
        {
            get
            {
                double ScreenRes = 1 / _getDPI; //pixels/inch
                //39.37 inches/meter
                //156543.04 meters/pixel
                if (Center != null)
                    return Math.Abs(ScreenRes * 39.37 * 156543.04 * Math.Cos(Center.Latitude) / (Math.Pow(2, Zoom)));
                else
                    return 1;
            }
        }

        private GeoCoordinate _center = new GeoCoordinate(41.02, 28.96); //Initial start center coordinate is Istanbul, beacuse the creator lives in istanbul ;)
        /// <summary>
        /// Gets or sets calculated geo position of the device
        /// </summary>
        public GeoCoordinate Center
        {
            get { return _center; }
            private set
            {
                if (value != _center)
                {
                    _center = value;

                    CoordinateString = _center.ToString();

                    if (FollowMode != Enums.FollowModeEnum.Free)
                    {
                        if (CenterChanged != null)
                            CenterChanged(this, _center);
                    }

                    NotifyPropertyChanged("Center");
                }
            }
        }

        private string _coordinateString;
        /// <summary>
        /// Gets calculated Geo position of the device as string
        /// </summary>
        internal string CoordinateString
        {
            get { return _coordinateString; }
            private set
            {
                if (_coordinateString != value)
                {
                    _coordinateString = value;
                    NotifyPropertyChanged("CoordinateString");
                }
            }
        }

        private string _compassString;
        /// <summary>
        /// gets heading of Compass as string
        /// </summary>
        internal string CompassString
        {
            get { return _compassString; }
            private set
            {
                if (_compassString != value)
                {
                    _compassString = value;
                    NotifyPropertyChanged("CompassString");
                }
            }
        }

        private double _compassTrueHeading;
        /// <summary>
        /// gets heading of the compass
        /// </summary>
        internal double CompassTrueHeading
        {
            get { return _compassTrueHeading; }
            private set
            {
                if (_compassTrueHeading != value)
                {
                    _compassTrueHeading = value;
                    NotifyPropertyChanged("CompassTrueHeading");
                }
            }
        }

        private double _horizontalAccuracy;
        /// <summary>
        /// Gets the accuracy diameter of the gps signal
        /// </summary>
        public double HorizontalAccuracy
        {
            get { return _horizontalAccuracy; }
            private set
            {
                if (_horizontalAccuracy != value)
                {
                    _horizontalAccuracy = value;
                    NotifyPropertyChanged("HorizontalAcuuracy");
                    NotifyPropertyChanged("AccuracyDiameter");
                }
            }
        }

        private double _verticalAccuracy;
        /// <summary>
        /// Gets the vertical accuracy diameter of the gps signal
        /// </summary>
        public double VerticalAccuracy
        {
            get { return _verticalAccuracy; }
            set
            {
                if (_verticalAccuracy != value)
                {
                    _verticalAccuracy = value;
                    NotifyPropertyChanged("VerticalAccuracy");
                    NotifyPropertyChanged("AccuracyDiameter");
                }
            }
        }

        /// <summary>
        /// Gets scaled horizontal accuracy diameter according to the maps zoom
        /// </summary>
        public double AccuracyDiameter
        {
            get { return _horizontalAccuracy / _scale; }
        }

        private Enums.FollowModeEnum _followMode = Enums.FollowModeEnum.FollowWithHeading;
        /// <summary>
        /// Gets or sets the fallow mode of the component
        /// 1. Free mode, 2. Follow the device on the map, 3. Fallow the device on the map with heading
        /// </summary>
        internal Enums.FollowModeEnum FollowMode
        {
            get { return _followMode; }
            set
            {
                if (_followMode != value)
                {
                    _followMode = value;

                    //if (_followMode != Enums.FollowModeEnum.Free && Zoom < DefaultZoom)
                    //    Zoom = DefaultZoom;

                    if (CenterChanged != null && _followMode != Enums.FollowModeEnum.Free)
                        CenterChanged(this, _center);

                    NotifyPropertyChanged("HeadingVisibility");
                    NotifyPropertyChanged("OnFreeFollowMode");
                    NotifyPropertyChanged("NotOnFreeFollowMode");
                }
            }
        }

        /// <summary>
        /// Gets the heading visibility, which is enabled or disabled by FollowMode
        /// </summary>
        public bool HeadingVisibility
        {
            get { return _followMode == Enums.FollowModeEnum.FollowWithHeading && _trackingDotVisibility; }
        }

        /// <summary>
        /// Gets the follow mode is free mode 
        /// </summary>
        public bool OnFreeFollowMode
        {
            get { return _followMode == Enums.FollowModeEnum.Free; }
        }

        /// <summary>
        /// gets the follow mode is other than free mode
        /// </summary>
        public bool NotOnFreeFollowMode
        {
            get { return _followMode != Enums.FollowModeEnum.Free; }
        }

        private bool _accuracyRadiusVisible;
        /// <summary>
        /// Gets or sets the Accuracy Presenter visibility from the settings viewmodel of the current instance
        /// </summary>
        public bool AccuracyRadiusVisible
        {
            get { return _accuracyRadiusVisible && _trackingDotVisibility; }
            set
            {
                if (_accuracyRadiusVisible != value)
                {
                    _accuracyRadiusVisible = value;
                    NotifyPropertyChanged("AccuracyRadiusVisible");
                }
            }
        }

        private bool _trackingDotVisibility;
        /// <summary>
        /// Gets or sets tracking visibility of the blue dot center of the map.
        /// </summary>
        public bool TrackingDotVisibility
        {
            get { return _trackingDotVisibility; }
            set
            {
                if (_trackingDotVisibility != value)
                {
                    _trackingDotVisibility = value;
                    NotifyPropertyChanged("TrackingDotVisibility");
                    NotifyPropertyChanged("AccuracyRadiusVisible");
                    NotifyPropertyChanged("HeadingVisibility");
                }
            }
        }

        private string _applicationName;
        /// <summary>
        /// Gets the name of the application
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (value != _applicationName)
                {
                    _applicationName = value;
                    NotifyPropertyChanged("ApplicationName");
                }
            }
        }

        /// <summary>
        /// Gets NearbyLocations for MapView, could be set before construction of MapView from SettingsVM.Instance.CurrentNearbyLocations 
        /// </summary>
        internal ObservableCollection<LocationsVM.Location> NearbyLocations { get; private set; }

        /// <summary>
        /// Gets a sub collection from NearbyLocations which is selected condition for showing in map
        /// </summary>
        internal IEnumerable<LocationsVM.Location> SelectedNearbyLocations
        {
            get
            {
                return from n in NearbyLocations
                       where n.IsVisibleInMapAndView
                       orderby n.Distance ascending
                       select n;
            }
        }

        /// <summary>
        /// Gets a clicked location from SelectedNearbyLocations, if there is one.
        /// </summary>
        internal LocationsVM.Location ClickedNearbyLocation
        {
            get { return SelectedNearbyLocations.Where(p => p.IsClickedOnView).FirstOrDefault(); }
        }

        #endregion

        #region Variables
        private GeoCoordinateWatcher _watcher;
        private Compass _compass;
        private Accelerometer _accelerometer;

        /// <summary>
        /// Mode of the bing map
        /// </summary>
        internal Enums.MapModeEnum MapMode { get; set; }
        #endregion

        #region Events
        internal delegate void CenterChangedEventHandler(MapViewVM sender, GeoCoordinate center);
        internal event CenterChangedEventHandler CenterChanged;

        internal delegate void CompassChangedEventhandler(double TrueHeading);
        internal event CompassChangedEventhandler CompassChanged;

        internal delegate void PhoneOrientationChangedEventHandler();
        internal event PhoneOrientationChangedEventHandler PhoneOrientationChanged;
        #endregion

        #region Delegate Voids
        private void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            if (PhoneOrientationChanged != null)
                if (-e.SensorReading.Acceleration.Z <= 0.5)
                    PhoneOrientationChanged();
        }

        private void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            if (Math.Abs(e.SensorReading.TrueHeading - CompassTrueHeading) > 10
                && _followMode == Enums.FollowModeEnum.FollowWithHeading
                && -_accelerometer.CurrentValue.Acceleration.Z > 0.8)
            {
                CompassTrueHeading = e.SensorReading.TrueHeading;

                if (CompassChanged != null)
                    CompassChanged(CompassTrueHeading);
            }
        }

        private void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Center = e.Position.Location;
            HorizontalAccuracy = e.Position.Location.HorizontalAccuracy;
        }

        private void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    WatcherStatus = "GPS Off";
                    break;
                case GeoPositionStatus.Initializing:
                    WatcherStatus = "GPS Init";
                    break;
                case GeoPositionStatus.NoData:
                    WatcherStatus = "GPS NoData";
                    break;
                case GeoPositionStatus.Ready:
                    WatcherStatus = "GPS Ready";
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region static properties

        internal static ObservableCollection<LocationsVM.Location> _NearbyLocations { get; set; }
        internal static string _BingMapKey { get; set; }
        internal static bool _AccuracyRadiusVisible { get; set; }
        internal static string _ApplicationName { get; set; }

        #endregion
    }
}