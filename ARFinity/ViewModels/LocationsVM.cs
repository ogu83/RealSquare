using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Matrix = Microsoft.Xna.Framework.Matrix;

namespace ARFinity
{
    public class LocationsVM : ViewModelBase
    {
        public class Location : ViewModelBase
        {
            /// <summary>
            /// Constructs a Location with parameters
            /// </summary>
            /// <param name="name">Name of the location</param>
            /// <param name="coordinate">Coordinate of the location</param>
            /// <param name="distance">Distance of the according to the Device </param>
            /// <param name="address">address of this location (can be null)</param>
            /// <param name="city">city of this location (can be null)</param>
            /// <param name="iconUri">icon iamge url for this location (can be null, no image will be displayed)</param>
            public Location(string name, GeoCoordinate coordinate, string address, string city, int distance, string iconUri)
            {
                Name = name;
                Coordinate = coordinate;
                Distance = distance;
                Address = address;
                City = city;
                IconUri = iconUri;
            }

            /// <summary>
            /// gets or sets location name string length in mapview area
            /// </summary>
            public int ShortNameMaxLength = 30;

            private string _name;
            /// <summary>
            /// Gets the name of this location
            /// </summary>
            public string Name
            {
                get { return _name; }
                protected set
                {
                    if (value != _name)
                    {
                        _name = value == null ? "" : value;
                        NotifyPropertyChanged("ShortName");
                        NotifyPropertyChanged("Name");
                    }
                }
            }

            /// <summary>
            /// Gets short name of this location
            /// </summary>
            public string ShortName
            {
                get { return _name.Substring(0, Math.Min(_name.Length, ShortNameMaxLength)) + (_name.Length > ShortNameMaxLength ? "..." : ""); }
            }

            private GeoCoordinate _coordinate;
            /// <summary>
            /// Gets the coordinate of this location
            /// </summary>
            public GeoCoordinate Coordinate
            {
                get { return _coordinate; }
                protected set
                {
                    if (value != _coordinate)
                    {
                        _coordinate = value;
                        NotifyPropertyChanged("Coordinate");
                    }
                }
            }

            private double _headingAngle;
            /// <summary>
            /// Gets the heading angle in degrees while moving the phone 
            /// It is avaible when ARView or ARMapView is running
            /// </summary>
            public double HeadingAngle
            {
                get { return _headingAngle; }
                private set
                {
                    if (_headingAngle != value)
                    {
                        _headingAngle = value;
                        NotifyPropertyChanged("HeadingAngle");
                    }
                }
            }

            /// <summary>
            /// Calculates the HeadingAngle according to device (motion or compass) position angle
            /// and sets the HeadingAngle property of this Location
            /// </summary>
            /// <param name="deviceRotationAngle">device position angle in degrees</param>
            internal void CalculateHeadingAngle(double deviceRotationAngle)
            {
                double locationAngle = MathHelper.ToDegrees(Vector2Helper.AngleVectorToVector(RadarPoint, new Vector2(0, -45)));
                HeadingAngle = deviceRotationAngle - locationAngle
                    + Convert.ToInt32(RadarPoint.X > 0 && RadarPoint.Y < 0) * 90
                    - Convert.ToInt32(RadarPoint.X > 0 && RadarPoint.Y > 0) * 90;
            }

            private string _address;
            /// <summary>
            /// Gets the adress of this location
            /// </summary>
            public string Address
            {
                get { return _address; }
                protected set
                {
                    if (_address != value)
                    {
                        _address = value == null ? "" : value;
                        NotifyPropertyChanged("Address");
                    }
                }
            }

            private string _city;
            /// <summary>
            /// Gets the City of this location
            /// </summary>
            public string City
            {
                get { return _city; }
                protected set
                {
                    if (value != _city)
                    {
                        _city = value == null ? "" : value;
                        NotifyPropertyChanged("City");
                    }
                }
            }

            private bool _isVisibleInMapAndView;
            /// <summary>
            /// Select or deselect this location for view in map and camera
            /// </summary>
            public bool IsVisibleInMapAndView
            {
                get { return _isVisibleInMapAndView; }
                set
                {
                    if (value != _isVisibleInMapAndView)
                    {
                        _isVisibleInMapAndView = value;
                        NotifyPropertyChanged("IsVisibleInMapAndView");
                    }
                }
            }

            private bool _isClickedOnView;
            internal bool IsClickedOnView
            {
                get { return _isClickedOnView; }
                set
                {
                    _isClickedOnView = value;

                    if (PlacemarkPresenter != null)
                        PlacemarkPresenter.Dispatcher.BeginInvoke(delegate()
                        {
                            if (PlacemarkPresenter != null)
                                PlacemarkPresenter.backgroundImage.Opacity = 0.7 + 0.3 * Convert.ToInt32(_isClickedOnView);
                        });
                }
            }

            private int _distance;
            /// <summary>
            /// Gets or sets the distance according to the gpsposition
            /// </summary>
            public int Distance
            {
                get { return _distance; }
                set
                {
                    if (value != _distance)
                    {
                        _distance = value;
                        NotifyPropertyChanged("Distance");
                    }
                }
            }

            private string _iconUri;
            /// <summary>
            /// Gets the Icon Url of the locations category
            /// </summary>
            public string IconUri
            {
                get { return _iconUri; }
                protected set
                {
                    if (value != _iconUri)
                    {
                        _iconUri = value;
                        NotifyPropertyChanged("IconUri");
                        NotifyPropertyChanged("IconUriWidth");
                        NotifyPropertyChanged("IconUri64");
                    }
                }
            }

            /// <summary>
            /// Gets the Icon Url of the locations category 64x64
            /// </summary>
            public string IconUri64
            {
                get
                {
                    if (!string.IsNullOrEmpty(IconUri))
                        return IconUri.Replace(".png", "_64.png");
                    else
                        return string.Empty;
                }
            }

            /// <summary>
            /// Gets the Icon width, if exist returns 32 else returns 0
            /// </summary>
            public int IconUriWidth
            {
                get { return string.IsNullOrEmpty(_iconUri) ? 0 : 32; }
            }

            /// <summary>
            /// gets or sets the point of this location in 3d world
            /// </summary>
            internal Vector3 VectorPoint;

            /// <summary>
            /// gets or sets the world matrix of this location on the 3d wold
            /// </summary>
            internal Matrix WorldMatrix;

            /// <summary>
            /// Initializes World matrix of this location on the 3d world
            /// </summary>
            private void InitializeWorldMatrix()
            {
                if (VectorPoint.Z > 0)
                    WorldMatrix = Matrix.CreateRotationY((float)Math.Atan(VectorPoint.X / VectorPoint.Z));
                else
                    WorldMatrix = Matrix.CreateRotationY(-MathHelper.Pi + (float)Math.Atan(VectorPoint.X / VectorPoint.Z));

                WorldMatrix = WorldMatrix * Matrix.CreateTranslation(VectorPoint);
            }

            /// <summary>
            /// gets or sets the object representation of this location
            /// </summary>
            internal BasicEffect Object3dEffect;

            /// <summary>
            /// gets or set is this object in matrix animation or not
            /// </summary>
            internal bool IsAnimatingWorld;
            /// <summary>
            /// gets or set is this object in matrix animation or not
            /// </summary>
            internal bool IsAnimatingView;

            /// <summary>
            /// gets the Image representation of this location for radar
            /// </summary>
            internal Image RadarPointRepresenter { get; private set; }

            /// <summary>
            /// gets the radar point coordinates in 2d plane
            /// </summary>
            internal Vector2 RadarPoint { get; private set; }

            /// <summary>
            /// Initializes the radar image, which representation of this location, from VectorPoint
            /// </summary>
            private void InitializeRadarPoint()
            {
                float factor = 45;

                this.RadarPointRepresenter = new Image() { Width = factor / 3, Height = factor / 3, DataContext = this };

                Vector3 v = this.VectorPoint;
                v.Normalize();
                v *= factor;

                Canvas.SetLeft(this.RadarPointRepresenter, v.X);
                Canvas.SetTop(this.RadarPointRepresenter, v.Z);

                RadarPoint = new Vector2(v.X, v.Z);
            }

            /// <summary>
            /// Calculates the VectorPoint of this location on the camera according to the gps and compass position
            /// </summary>
            /// <param name="compassTrueHeading">heading of compass</param>
            /// <param name="geoCoordinate">gps location of the device</param>
            /// <param name="y">Y Axis hegiht in Cam</param>
            internal void CalculateVectorPoint(GeoCoordinate geoCoordinate, float y)
            {
                //position multipiler factor
                float factor = 10;
                float factor1 = factor * 25000;

                //depth in the camera
                VectorPoint.Z = (float)((this.Coordinate.Latitude - geoCoordinate.Latitude) * -factor1);
                //horizontal position on the camera
                VectorPoint.X = (float)((this.Coordinate.Longitude - geoCoordinate.Longitude) * factor1);

                VectorPoint.Normalize();
                VectorPoint *= factor;

                //vertical position on the camera
                VectorPoint.Y = y;

                InitializeWorldMatrix();
                InitializeRadarPoint();
            }

            /// <summary>
            /// Initializes the placemark representer object for this location
            /// </summary>
            internal void InitializePlaceMark()
            {
                _placemarkPresenter = new Placemark() { RenderTransform = new TranslateTransform() };
                _placemarkPresenter.DataContext = this;
                _placemarkPresenter.SetValue(Canvas.ZIndexProperty, 2);
            }

            private Placemark _placemarkPresenter;
            /// <summary>
            /// Gets the represented text or image object for the cameraview
            /// </summary>
            internal Placemark PlacemarkPresenter
            {
                get { return _placemarkPresenter; }
            }

            /// <summary>
            /// Gets the bounding sphere of placemark representer of this location in 3d world.
            /// </summary>
            internal BoundingSphere PlacemarkBoundingSphere
            {
                get { return new BoundingSphere(this.VectorPoint, Math.Max(_placemarkPresenter.Object3D.shapeSize.X, _placemarkPresenter.Object3D.shapeSize.Y)); }
            }

            /// <summary>
            /// Return calculated the distance of this location to another location (to)
            /// </summary>
            /// <param name="to">another location, which distance will be calculated to this location</param>
            /// <returns></returns>
            public double DistanceTo(GeoCoordinate to)
            {
                return Math.Sqrt(Math.Pow((Coordinate.Latitude - to.Longitude), 2) + Math.Pow((Coordinate.Longitude - to.Longitude), 2));
            }


        }

        #region IDisposable Members

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion

        #region IMobile Members

        public override event ViewModelBase.ErrorEventhandler ErrorRaised;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Suspend()
        {

            base.Suspend();
        }
        #endregion

        private bool _isRefreshing;
        /// <summary>
        /// Gets the nearby locations are refreshing or not
        /// </summary>
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                if (value != _isRefreshing)
                {
                    _isRefreshing = value;
                    NotifyPropertyChanged("IsRefreshing");
                }
            }
        }

        private ObservableCollection<Location> _nearbyLocations = new ObservableCollection<Location>();
        /// <summary>
        /// Gets or sets near by locations according to current gps device
        /// </summary>
        public ObservableCollection<Location> NearByLocations
        {
            get { return _nearbyLocations; }
            set
            {
                if (value != _nearbyLocations)
                {
                    _nearbyLocations = value;
                    NotifyPropertyChanged("NearByLocations");
                }
            }
        }

    }
}
