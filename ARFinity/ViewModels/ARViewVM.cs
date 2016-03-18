using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Devices.Sensors;

namespace ARFinity
{
    internal sealed class ARViewVM : ViewModelBase
    {
        #region IPhoneVM Members

        public override void Initialize()
        {
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

            NearbyLocations = _NearbyLocations;

            base.Initialize();
        }

        public override void Suspend()
        {
            if (_accelerometer != null)
            {
                _accelerometer.Stop();
                _accelerometer.Dispose();
                _accelerometer = null;
            }
        }

        public override event ErrorEventhandler ErrorRaised;

        #endregion

        private Accelerometer _accelerometer;
        internal enum OrientationEnum { Map, Portrait, Landscape, PortraitDown, LandscapeDown };
        private OrientationEnum _currentOrientation = OrientationEnum.Portrait;
        internal delegate void PhoneOrientationChangedEventHandler(OrientationEnum e);
        internal event PhoneOrientationChangedEventHandler PhoneOrientationChanged;

        private void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            if (PhoneOrientationChanged == null)
                return;

            float x = e.SensorReading.Acceleration.X;
            float y = e.SensorReading.Acceleration.Y;
            float z = e.SensorReading.Acceleration.Z;

            if (-z > 0.5)
                PhoneOrientationChanged(OrientationEnum.Map);

            OrientationEnum newOrientation = _currentOrientation;

            if (x > -0.5 && x < 0.5 && y > 0.5)
                newOrientation = OrientationEnum.PortraitDown;
            else if (x > -0.5 && x < 0.5 && y < -0.5)
                newOrientation = OrientationEnum.Portrait;
            else if (y > -0.5 && y < 0.5 && x > 0.5)
                newOrientation = OrientationEnum.LandscapeDown;
            else if (y > -0.5 && y < 0.5 && x < -0.5)
                newOrientation = OrientationEnum.Landscape;

            if (newOrientation != _currentOrientation)
            {
                _currentOrientation = newOrientation;
                PhoneOrientationChanged(_currentOrientation);
            }
        }


        /// <summary>
        /// Gets NearbyLocations for MapView, could be set before construction of MapView from SettingsVM.Instance.CurrentNearbyLocations 
        /// </summary>
        public ObservableCollection<LocationsVM.Location> NearbyLocations { get; private set; }

        /// <summary>
        /// Gets a sub collection from NearbyLocations which is selected condition for showing in camera
        /// </summary>
        public IEnumerable<LocationsVM.Location> SelectedNearbyLocations
        {
            get
            {
                var retVal = from n in NearbyLocations where n.IsVisibleInMapAndView orderby n.IsClickedOnView ascending select n;
                return retVal;
            }
        }

        /// <summary>
        /// Gets a clicked location from SelectedNearbyLocations, if there is one.
        /// </summary>
        public LocationsVM.Location ClickedNearbyLocation
        {
            get { return SelectedNearbyLocations.Where(p => p.IsClickedOnView).FirstOrDefault(); }
        }

        #region static properties

        internal static ObservableCollection<LocationsVM.Location> _NearbyLocations { get; set; }

        #endregion
    }
}
