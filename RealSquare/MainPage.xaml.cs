using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;

using Microsoft.Phone.Controls;

using RealSquare.ViewModels;
using ARFinity;
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;

namespace RealSquare
{
    public partial class MainPage : PhoneApplicationPage
    {
        private MainPageVM myVM { get { return this.DataContext as MainPageVM; } }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            ARFinity.ARMapView.OnLocationTouchDown += new ARMapView.OnLocationTouchEventHandler(ARMapView_OnLocationTouchDown);
            ARFinity.ARMapView.OnLocationTouchUp += new ARMapView.OnLocationTouchEventHandler(ARMapView_OnLocationTouchUp);
            ARFinity.ARMapView.OnMapDrag += new EventHandler(ARMapView_OnMapDrag);
            ARFinity.ARMapView.OnFollowModeChanged += new ARMapView.OnFollowModeChangedEventHandler(ARMapView_OnFollowModeChanged);

            ARFinity.ARView.OnLocationSelected += new ARView.OnLocationSelectEventHandler(ARView_OnLocationSelected);
            ARFinity.ARView.OnLocationReleased += new ARView.OnLocationSelectEventHandler(ARView_OnLocationReleased);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                myVM.Initialize();

            myVM.OnGotoSettings += myVM_OnGotoSettings;
            
            try
            {
                for (int i = 0; i < NavigationService.BackStack.Count(); i++)
                    NavigationService.RemoveBackEntry();
            }
            catch (Exception ex)
            {
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            myVM.Suspend();

            myVM.OnGotoSettings -= myVM_OnGotoSettings;

            base.OnNavigatingFrom(e);
        }

        private void btnMap_Click(object sender, EventArgs e)
        {
            if (!myVM.MySettingsVM.EnableLocation)
            {
                MessageBox.Show("Location Services are disabled for this application. Application cannot be used without location services. Please read the privacy statement and activate location services", "Location Services", MessageBoxButton.OK);
                myVM_OnGotoSettings(this, null);
            }
            else
            {
                ARFinity.ARMapView.SetLocations(myVM.MyLocationsVM.NearByLocations);
                ARFinity.ARMapView.SetBingMapKey(myVM.MySettingsVM.BingMapKey);
                ARFinity.ARMapView.SetAccuracyRadiusVisibility(myVM.MySettingsVM.AccuracyRadiusVisible);
                ARFinity.ARMapView.SetApplicationName(myVM.ApplicationTitle);

                NavigationService.Navigate(new Uri("/ARFinity;component/ARMapView.xaml", UriKind.Relative));
            }
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as Panorama).SelectedIndex == 0)
                if (myVM.LastRadius != myVM.MySettingsVM.SearchRadius)
                    myVM.RefreshNearByLocations();
        }

        private void btnCaptureImage_Click(object sender, EventArgs e)
        {
            ARFinity.ScreenCapturer.CaptureImage(this);
        }


        #region ARFinity Delegates
        private void ARMapView_OnLocationTouchUp(ARMapView sender, ARFinity.LocationsVM.Location location)
        {
            MessageBox.Show(string.IsNullOrEmpty(location.Address) ? "" : location.Address,
                            string.IsNullOrEmpty(location.Name) ? "" : location.Name,
                            MessageBoxButton.OK);
        }

        private void ARMapView_OnLocationTouchDown(ARMapView sender, ARFinity.LocationsVM.Location location)
        {

        }

        private void ARMapView_OnMapDrag(object sender, EventArgs e)
        {

        }

        private void ARMapView_OnFollowModeChanged(ARMapView sender, Enums.FollowModeEnum mode)
        {
            ARMapView.ExternalUserControl = null;
        }


        private void ARView_OnLocationSelected(ARView sender, LocationsVM.Location location)
        {
            if (ARView.ExternalUserControl == null)
                ARView.ExternalUserControl = new ExternalControl() { DataContext = location };
            else
                ARView.ExternalUserControl = null;
        }

        private void ARView_OnLocationReleased(ARView sender, LocationsVM.Location location)
        {
            ARView.ExternalUserControl = null;
        }

        private void myVM_OnGotoSettings(object sender, EventArgs e)
        {
            mainPanaromaControl.DefaultItem = mainPanaromaControl.Items[1];
        }

        private void txtPrivacy_Tap(object sender, GestureEventArgs e)
        {
            new EmailComposeTask
            {
                To = "oguzkoroglu@oguzkoroglu.net",
                Subject = "Question about real square"
            }.Show();
        }
        #endregion
    }
}