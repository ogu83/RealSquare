using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Shell;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;

namespace ARFinity
{
    public partial class ARMapView : PhoneApplicationPage
    {
        private MapViewVM myVM { get { return this.DataContext as MapViewVM; } }
        private bool _isCompassStoryBoardRunning;
        private bool _onFirstResolve;

        public ARMapView()
        {
            this.DataContext = new MapViewVM();
            InitializeComponent();
        }

        private void ARMapView_OnExternalUserControlChanged(UserControl u)
        {
            this.ExternalUserControlPlaceHolder.Children.Clear();
            if (u != null)
                ExternalUserControlPlaceHolder.Children.Add(u);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                NavigationService.Navigate(new Uri("/RealSquare;component/MainPage.xaml", UriKind.Relative));
                return;
            }

            _onFirstResolve = true;
            myVM.TrackingDotVisibility = false;

            myVM.CenterChanged += new MapViewVM.CenterChangedEventHandler(myVM_CenterChanged);
            myVM.CompassChanged += new MapViewVM.CompassChangedEventhandler(myVM_CompassChanged);
            myVM.ErrorRaised += new MapViewVM.ErrorEventhandler(myVM_ErrorRaised);
            myVM.PhoneOrientationChanged += new MapViewVM.PhoneOrientationChangedEventHandler(myVM_PhoneOrientationChanged);

            OnExternalUserControlChanged += new OnExternalUserControlChangedEventHandler(ARMapView_OnExternalUserControlChanged);

            if (!DesignerProperties.GetIsInDesignMode(this))
                myVM.InternalInitialize();

            foreach (LocationsVM.Location location in myVM.SelectedNearbyLocations)
            {
                Pushpin p = new Pushpin();
                p.Location = location.Coordinate;
                p.DataContext = location;
                p.RenderTransform = new CompositeTransform() { CenterX = 0.5, CenterY = 0.5 };
                p.Style = Resources["pushpinStyle1"] as Style;
                p.MouseLeftButtonDown += new MouseButtonEventHandler(pushpin_MouseLeftButtonDown);
                p.MouseLeftButtonUp += new MouseButtonEventHandler(pushpin_MouseLeftButtonUp);

                Binding binding = new Binding();
                binding.Source = myVM;
                binding.Path = new PropertyPath("TrackingDotVisibility");
                binding.Mode = BindingMode.OneWay;
                binding.Converter = new BooleanToVisibilityConverter();
                p.SetBinding(Pushpin.VisibilityProperty, binding);

                myMap.Children.Add(p);
            }

            myMap.MapResolved += new EventHandler(myMap_MapResolved);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            myVM.CenterChanged -= myVM_CenterChanged;
            myVM.CompassChanged -= myVM_CompassChanged;
            myVM.ErrorRaised -= myVM_ErrorRaised;
            myVM.PhoneOrientationChanged -= myVM_PhoneOrientationChanged;
            myMap.MapResolved -= myMap_MapResolved;

            myVM.InternalSuspend();

            for (int i = 1; i < myMap.Children.Count; i++)
                myMap.Children.RemoveAt(i);

            OnExternalUserControlChanged -= ARMapView_OnExternalUserControlChanged;
            this.ExternalUserControlPlaceHolder.Children.Clear();

            if (myVM.ClickedNearbyLocation != null)
                myVM.ClickedNearbyLocation.IsClickedOnView = false;

            if (e.NavigationMode == NavigationMode.Back)
                NavigationService.Navigate(new Uri("/RealSquare;component/MainPage.xaml", UriKind.Relative));

            base.OnNavigatingFrom(e);
        }


        private void myMap_MapResolved(object sender, EventArgs e)
        {
            if (_onFirstResolve && myVM.Zoom < myVM.DefaultZoom)
                myVM.Zoom = myVM.DefaultZoom;

            myVM.TrackingDotVisibility = true;

            _onFirstResolve = false;
        }

        private void myVM_ErrorRaised(Enums.ErrorsEnum error)
        {
            switch (error)
            {
                case Enums.ErrorsEnum.CompassNotExist:
                    MessageBox.Show("There is no compass sensor on device.", "Sensor Missing", MessageBoxButton.OK);
                    break;
                case Enums.ErrorsEnum.GpsNotExist:
                    MessageBox.Show("There is no GPS on device.", "GPS Missing", MessageBoxButton.OK);
                    break;
            }
        }

        private void myVM_PhoneOrientationChanged()
        {
            Dispatcher.BeginInvoke(delegate()
            {
                ArragneVectorsByDistance();

                ARFinity.ARView.SetLocations(myVM.NearbyLocations);

                NavigationService.Navigate(new Uri("/ARFinity;component/ARView.xaml", UriKind.Relative));
            });
        }

        private void ArragneVectorsByDistance()
        {
            float yBack = -2f;
            float yFront = -2f;

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
            {
                l.CalculateVectorPoint(myVM.Center, 0);

                if (l.VectorPoint.X > 0)
                {
                    l.CalculateVectorPoint(myVM.Center, yFront);
                    yFront += 4f;
                }
                else
                {
                    l.CalculateVectorPoint(myVM.Center, yBack);
                    yBack += 4f;
                }
            }
        }

        private void myVM_CompassChanged(double TrueHeading)
        {
            if (!_isCompassStoryBoardRunning)
                Dispatcher.BeginInvoke(delegate() { RotateMap(TrueHeading); });

            Dispatcher.BeginInvoke(delegate()
            {
                if (myVM.ClickedNearbyLocation != null)
                    myVM.ClickedNearbyLocation.CalculateHeadingAngle(TrueHeading);
            });
        }

        private void RotateMap(double TrueHeading)
        {
            Storyboard sb = new Storyboard();

            sb.Completed += (object sender, EventArgs e) =>
            {
                _isCompassStoryBoardRunning = false;

                foreach (Pushpin p in myMap.Children)
                    p.RenderTransform = new RotateTransform() { Angle = TrueHeading, CenterX = 0, CenterY = 0 };
            };

            var ef = new CubicEase();
            ef.EasingMode = EasingMode.EaseInOut;

            DoubleAnimation da = new DoubleAnimation();
            CompositeTransform transform = myMap.RenderTransform as CompositeTransform;

            da.From = transform.Rotation;
            da.To = 360 - TrueHeading;

            if (Math.Abs(da.From.Value - da.To.Value) > Math.Abs((da.From.Value - 360) - da.To.Value))
                da.From -= 360;
            else if (Math.Abs(da.To.Value - da.From.Value) > Math.Abs((da.To.Value - 360) - da.From.Value))
                da.To -= 360;

            da.Duration = new Duration(new TimeSpan(0, 0, 0, 0, 1000));
            da.EasingFunction = ef;
            sb.Duration = da.Duration;
            sb.Children.Add(da);

            Storyboard.SetTarget(da, transform);
            Storyboard.SetTargetProperty(da, new PropertyPath("(Rotation)"));

            sb.Begin();
            _isCompassStoryBoardRunning = true;
        }

        private void myVM_CenterChanged(MapViewVM sender, GeoCoordinate center)
        {
            if (center != null)
            {
                if (!_onFirstResolve)
                    myMap.SetView(center, myMap.ZoomLevel, myMap.Heading, myMap.Pitch);
                else
                    myMap.SetView(center, myVM.DefaultZoom, myMap.Heading, myMap.Pitch);
            }
        }


        private void btnLockCenter_Click(object sender, EventArgs e)
        {
            if (myVM.FollowMode == Enums.FollowModeEnum.Follow)
                myVM.FollowMode = Enums.FollowModeEnum.FollowWithHeading;
            else
                myVM.FollowMode = Enums.FollowModeEnum.Follow;

            if (OnFollowModeChanged != null)
                OnFollowModeChanged(this, myVM.FollowMode);

            if (myVM.ClickedNearbyLocation != null)
                myVM.ClickedNearbyLocation.IsClickedOnView = false;
        }

        private void myMap_MouseMove(object sender, MouseEventArgs e)
        {
            myVM.FollowMode = Enums.FollowModeEnum.Free;

            if (OnFollowModeChanged != null)
                OnFollowModeChanged(this, myVM.FollowMode);

            if (myVM.ClickedNearbyLocation != null)
                myVM.ClickedNearbyLocation.IsClickedOnView = false;

            if (OnMapDrag != null)
                OnMapDrag(this, null);
        }

        private void pushpin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Pushpin senderPushpin = (sender as Pushpin);
            if (senderPushpin == null)
                return;

            LocationsVM.Location location = (sender as Pushpin).DataContext as LocationsVM.Location;
            if (location == null)
                return;

            location.IsClickedOnView = true;
            if (OnLocationTouchDown != null)
                OnLocationTouchDown(this, location);
        }

        private void pushpin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Pushpin senderPushpin = (sender as Pushpin);
            if (senderPushpin == null)
                return;

            LocationsVM.Location location = (sender as Pushpin).DataContext as LocationsVM.Location;
            if (location == null)
                return;

            location.IsClickedOnView = true;
            if (OnLocationTouchUp != null)
                OnLocationTouchUp(this, location);
        }

        private void btnLocations_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            //NavigationService.GoBack();
        }

        private void btnMapMode_Click(object sender, EventArgs e)
        {
            if (myVM.MapMode == Enums.MapModeEnum.Road)
            {
                myMap.Mode = new AerialMode(true);
                myVM.MapMode = Enums.MapModeEnum.Aerial;
                (sender as ApplicationBarIconButton).Text = "Road Mode";
            }
            else
            {
                myMap.Mode = new RoadMode();
                myVM.MapMode = Enums.MapModeEnum.Road;
                (sender as ApplicationBarIconButton).Text = "Aerial Mode";
            }
        }

        private void btnCaptureImage_Click(object sender, EventArgs e)
        {
            ScreenCapturer.CaptureImage(this);
        }

        #region static methods
        /// <summary>
        /// Sets the location collection which will be shown in map
        /// </summary>
        /// <param name="locations">Location collection</param>
        public static void SetLocations(ObservableCollection<LocationsVM.Location> locations)
        {
            MapViewVM._NearbyLocations = locations;
        }

        /// <summary>
        /// Sets the bing map key for map view
        /// </summary>
        /// <param name="key">Bing Map Key</param>
        public static void SetBingMapKey(string key)
        {
            MapViewVM._BingMapKey = key;
        }

        /// <summary>
        /// Sets the blue circle of accuracy by gps 
        /// </summary>
        /// <param name="value">on or of</param>
        public static void SetAccuracyRadiusVisibility(bool value)
        {
            MapViewVM._AccuracyRadiusVisible = value;
        }

        /// <summary>
        /// Sets the Application Name, which will be shown in Map View
        /// </summary>
        /// <param name="name">Application Name</param>
        public static void SetApplicationName(string name)
        {
            MapViewVM._ApplicationName = name;
        }

        /// <summary>
        /// Location Event Handler
        /// </summary>
        /// <param name="sender">Map View, which is sended this touch event</param>
        /// <param name="location">Touched location</param>
        public delegate void OnLocationTouchEventHandler(ARMapView sender, LocationsVM.Location location);
        /// <summary>
        /// happens when user touches down the pushpin of a specific location in the map view
        /// </summary>
        public static event OnLocationTouchEventHandler OnLocationTouchDown;
        /// <summary>
        /// happens when user touches up the pushpin of a specific location in the map view
        /// </summary>
        public static event OnLocationTouchEventHandler OnLocationTouchUp;
        /// <summary>
        /// happens when user touches and moves the map, without touching a puspin
        /// </summary>
        public static event EventHandler OnMapDrag;

        /// <summary>
        /// Follow mode changed event handler
        /// </summary>
        /// <param name="sender">Map view, which is sended this event</param>
        /// <param name="mode"></param>
        public delegate void OnFollowModeChangedEventHandler(ARMapView sender, Enums.FollowModeEnum mode);
        /// <summary>
        /// Happens when user change the following mode with clicking the follow mode icon
        /// </summary>
        public static event OnFollowModeChangedEventHandler OnFollowModeChanged;

        private delegate void OnExternalUserControlChangedEventHandler(UserControl u);
        private static event OnExternalUserControlChangedEventHandler OnExternalUserControlChanged;

        private static UserControl _externalUserControl;
        /// <summary>
        /// External UserControl which will be overlays the mapview
        /// </summary>
        public static UserControl ExternalUserControl
        {
            get { return _externalUserControl; }
            set
            {
                _externalUserControl = value;
                if (OnExternalUserControlChanged != null)
                    OnExternalUserControlChanged(_externalUserControl);
            }
        }

        #endregion
    }
}
