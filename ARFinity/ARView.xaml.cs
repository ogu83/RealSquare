using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Devices;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Matrix = Microsoft.Xna.Framework.Matrix;
using System.Windows.Navigation;

namespace ARFinity
{
    public partial class ARView : PhoneApplicationPage
    {
        /// <summary>
        /// ARView View Model Instance in this objects DataContext
        /// </summary>
        private ARViewVM myVM { get { return this.DataContext as ARViewVM; } }

        private PhotoCamera _cam;                                   //Windows Phone Camera API object
        private Motion _motion;                                     //Motion  API object

        private GameTimer _gameTimer;                               //XNA Game Timer
        private DispatcherTimer _gestureTimer;                      //rewind timer after gesture to start motion api
        private int _gestureTime = 0;                               //seconds
        private const int _gestureMaxTime = 1;                      //seconds
        private SpriteBatch _spriteBatch;                           //XNA Sprite drawer
        private GraphicsDevice _device;                             //XNA DirectX Device object
        private UIElementRenderer BackgroundRenderer;

        private Matrix _cameraMatrix;                               //camera position and look at.
        private Matrix _projectionMatrix;                           //3d world projection matrix.
        private Matrix _phoneWorld = Matrix.CreateTranslation(0, 0, 0);   //phone position in real wold. (camera world)

        /// <summary>
        /// last yaw angle of windows phone device
        /// </summary>
        private float _yaw;
        /// <summary>
        /// last roll angle of windows phone device
        /// </summary>
        private float _roll;
        /// <summary>
        /// last pitch angle of windows phone device
        /// </summary>
        private float _pitch;

        /// <summary>
        /// Gesture States:
        /// no touch = OnMotion
        /// move your finger = OnFreeDrag
        /// on touch specific item in the screen = OnPressHold
        /// </summary>
        private enum GestureStateEnum { OnMotion, OnFreeDrag, OnPressHold }
        /// <summary>
        /// Current Gesture State on the camera view        
        /// </summary>
        private GestureStateEnum _gestureState = GestureStateEnum.OnMotion;

        public ARView()
        {
            this.DataContext = new ARViewVM();
            InitializeComponent();
        }


        private void ARView_OnExternalUserControlChanged(UserControl u)
        {
            this.ExternalUserControlPlaceHolder.Children.Clear();
            if (u != null)
                ExternalUserControlPlaceHolder.Children.Add(u);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                myVM.PhoneOrientationChanged += new ARViewVM.PhoneOrientationChangedEventHandler(myVM_PhoneOrientationChanged);
                myVM.Initialize();

                OnExternalUserControlChanged += new OnExternalUserControlChangedEventHandler(ARView_OnExternalUserControlChanged);

                //****comment in for Radar Heading Debug Lines****//
                //Vector2 v0 = Vector2.Zero;
                //Vector2 v1 = new Vector2(65, -65);
                //Vector2 v2 = new Vector2(-65, -65);

                //System.Windows.Shapes.Line l1 = new System.Windows.Shapes.Line() { X1 = v0.X, Y1 = v0.Y, X2 = v1.X, Y2 = v1.Y, Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red), StrokeThickness = 2 };
                //System.Windows.Shapes.Line l2 = new System.Windows.Shapes.Line() { X1 = v0.X, Y1 = v0.Y, X2 = v2.X, Y2 = v2.Y, Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red), StrokeThickness = 2 };
                //System.Windows.Shapes.Line l3 = new System.Windows.Shapes.Line() { X1 = v1.X, Y1 = v1.Y, X2 = v2.X, Y2 = v2.Y, Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red), StrokeThickness = 2 };

                //radarCanvas.Children.Add(l1);
                //radarCanvas.Children.Add(l2);
                //radarCanvas.Children.Add(l3);

                foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
                {
                    l.InitializePlaceMark();
                    placeholderGrid.Children.Add(l.PlacemarkPresenter);

                    radarCanvas.Children.Add(l.RadarPointRepresenter);
                }

                InitializeCamera();
                InitializeXNA();
                InitializeMotion();
                InitializeGestures();
                InitializeGestureTimer();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            myVM.PhoneOrientationChanged -= myVM_PhoneOrientationChanged;
            _gameTimer.Update -= _gameTimer_Update;
            _gameTimer.Draw -= _gameTimer_Draw;

            OnExternalUserControlChanged -= ARView_OnExternalUserControlChanged;
            this.ExternalUserControlPlaceHolder.Children.Clear();

            myVM.Suspend();

            if (_motion != null)
            {
                _motion.Stop();
                _motion.Dispose();
                _motion = null;
            }

            if (_cam != null)
            {
                _cam.Dispose();
                _cam = null;
            }

            // Stop the timer
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
            }

            //Stop gesture Timer
            if (_gestureTimer != null)
            {
                _gestureTimer.Stop();
                _gestureTimer = null;
            }

            // Set the sharing mode of the graphics device to turn off XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(false);

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
            {
                placeholderGrid.Children.Remove(l.PlacemarkPresenter);
                radarCanvas.Children.Remove(l.RadarPointRepresenter);
                l.IsClickedOnView = false;
                l.IsAnimatingView = false;
                l.IsAnimatingWorld = false;
            }

            //if (e.NavigationMode == NavigationMode.Back)
            //    NavigationService.Navigate(new Uri("/RealSquare;component/MainPage.xaml", UriKind.Relative));

            base.OnNavigatingFrom(e);
        }


        private void InitializeCamera()
        {
            try
            {
                _cam = new Microsoft.Devices.PhotoCamera();
                viewfinderBrush.SetSource(_cam);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is a problem with camera.");
                NavigationService.Navigate(new Uri("/ARFinity;component/APMapView.xaml", UriKind.Relative));
            }
        }

        private void InitializeXNA()
        {
            _device = SharedGraphicsDeviceManager.Current.GraphicsDevice;

            // Set the sharing mode of the graphics device to turn on XNA rendering
            _device.SetSharingMode(true);

            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(_device);

            BackgroundRenderer = new UIElementRenderer(this, _device.Viewport.Width, _device.Viewport.Height);
            // Create a timer for this page
            _gameTimer = new GameTimer();
            _gameTimer.UpdateInterval = TimeSpan.FromTicks(333333);
            _gameTimer.Update += new EventHandler<GameTimerEventArgs>(_gameTimer_Update);
            _gameTimer.Draw += new EventHandler<GameTimerEventArgs>(_gameTimer_Draw);

            _cameraMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)(this.LayoutRoot.ActualWidth / this.LayoutRoot.ActualHeight), 0.01f, 10000.0f);

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
            {
                l.Object3dEffect = new BasicEffect(_device);
                l.Object3dEffect.World = l.WorldMatrix;
                l.Object3dEffect.View = _cameraMatrix;
                l.Object3dEffect.Projection = _projectionMatrix;
                l.Object3dEffect.TextureEnabled = true;
            }

            _gameTimer.Start();
        }

        private void InitializeMotion()
        {
            if (!Motion.IsSupported)
            {
                MessageBox.Show("The Motion is not supported on this device.");
                NavigationService.Navigate(new Uri("/ARFinity;component/ARMapView.xaml", UriKind.Relative));
                //NavigationService.GoBack();
            }

            if (_motion == null)
            {
                try
                {
                    _motion = new Motion();
                    _motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(100);
                    _motion.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<MotionReading>>(_motion_CurrentValueChanged);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There is a problem with motion detection.");
                    NavigationService.Navigate(new Uri("/ARFinity;component/ARMapView.xaml", UriKind.Relative));
                    //NavigationService.GoBack();
                }
            }

            try
            {
                _motion.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start the Motion API.");
                NavigationService.Navigate(new Uri("/ARFinity;component/ARMapView.xaml", UriKind.Relative));
                //NavigationService.GoBack();
            }
        }

        private void InitializeGestureTimer()
        {
            _gestureTimer = new DispatcherTimer();
            _gestureTimer.Tick += new EventHandler(_gestureTimer_Tick);
            _gestureTimer.Interval = TimeSpan.FromSeconds(1);
        }

        private void InitializeGestures()
        {
            TouchPanel.EnabledGestures = GestureType.FreeDrag | GestureType.Hold | GestureType.DragComplete | GestureType.Tap;
        }


        private void _gestureTimer_Tick(object sender, EventArgs e)
        {
            _gestureTime++;
            if (_gestureTime >= _gestureMaxTime)
            {
                _gestureTimer.Stop();
                AnimateCameraAfterFreeDrag();
            }
        }

        private void _motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            double motionDelta = 0.01;

            if (!_motion.IsDataValid)
                return;

            if (Math.Abs(e.SensorReading.Attitude.Yaw - _yaw) > motionDelta)
                _yaw = e.SensorReading.Attitude.Yaw;

            if (Math.Abs(e.SensorReading.Attitude.Roll - _roll) > motionDelta)
                _roll = e.SensorReading.Attitude.Roll;

            if (Math.Abs(e.SensorReading.Attitude.Pitch - _pitch) > motionDelta)
                _pitch = e.SensorReading.Attitude.Pitch;

            Dispatcher.BeginInvoke(delegate()
            {
                if (myVM.ClickedNearbyLocation != null)
                    myVM.ClickedNearbyLocation.CalculateHeadingAngle(MathHelper.ToDegrees(_yaw));
            });

            if (_onDragEndAnimation)
                return;

            if (_gestureState == GestureStateEnum.OnFreeDrag)
            {
                _cameraMatrixBehindDrag = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);
                _cameraMatrixBehindDrag *= Matrix.CreateRotationY(-_yaw);
                _cameraMatrixBehindDrag *= Matrix.CreateRotationZ(_roll);
                _cameraMatrixBehindDrag *= Matrix.CreateRotationX(-_pitch + MathHelper.PiOver2);
            }
            else
            {
                _cameraMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1), Vector3.Up);
                _cameraMatrix *= Matrix.CreateRotationY(-_yaw);
                _cameraMatrix *= Matrix.CreateRotationZ(_roll);
                _cameraMatrix *= Matrix.CreateRotationX(-_pitch + MathHelper.PiOver2);

                //rotate radar
                Dispatcher.BeginInvoke(delegate()
                {
                    (radarCanvas.RenderTransform as System.Windows.Media.RotateTransform).Angle = MathHelper.ToDegrees((_yaw));
                });
            }
        }

        private void _gameTimer_Draw(object sender, GameTimerEventArgs e)
        {
            BackgroundRenderer.Render();

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
                l.PlacemarkPresenter.Renderer.Render();

            _device.Clear(Color.Black);

            _spriteBatch.Begin();
            _spriteBatch.Draw(BackgroundRenderer.Texture, Vector2.Zero, Color.White);
            _spriteBatch.End();

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
            {
                l.PlacemarkPresenter.Object3D.shapeTexture = l.PlacemarkPresenter.Renderer.Texture;

                foreach (EffectPass pass in l.Object3dEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    l.Object3dEffect.Texture = l.PlacemarkPresenter.Object3D.shapeTexture;
                    l.PlacemarkPresenter.Object3D.RenderShape(_device);
                }
            }
        }

        private void _gameTimer_Update(object sender, GameTimerEventArgs e)
        {
            UpdateLocations();
            UpdateRadar();
            HandleGesture();
        }

        private void myVM_PhoneOrientationChanged(ARViewVM.OrientationEnum e)
        {
            Dispatcher.BeginInvoke(delegate()
            {
                switch (e)
                {
                    case ARViewVM.OrientationEnum.Map:
                        NavigationService.Navigate(new Uri("/ARFinity;component/ARMapView.xaml", UriKind.Relative));
                        //NavigationService.GoBack();
                        break;
                    case ARViewVM.OrientationEnum.Portrait:
                        radarGrid.VerticalAlignment = VerticalAlignment.Top;
                        radarGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        (radarGrid.RenderTransform as System.Windows.Media.RotateTransform).Angle = 0;
                        break;
                    case ARViewVM.OrientationEnum.Landscape:
                        radarGrid.VerticalAlignment = VerticalAlignment.Top;
                        radarGrid.HorizontalAlignment = HorizontalAlignment.Right;
                        (radarGrid.RenderTransform as System.Windows.Media.RotateTransform).Angle = 90;
                        break;
                    case ARViewVM.OrientationEnum.PortraitDown:
                        radarGrid.VerticalAlignment = VerticalAlignment.Bottom;
                        radarGrid.HorizontalAlignment = HorizontalAlignment.Right;
                        (radarGrid.RenderTransform as System.Windows.Media.RotateTransform).Angle = 180;
                        break;
                    case ARViewVM.OrientationEnum.LandscapeDown:
                        radarGrid.VerticalAlignment = VerticalAlignment.Bottom;
                        radarGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        (radarGrid.RenderTransform as System.Windows.Media.RotateTransform).Angle = 270;
                        break;
                }
            });
        }


        private void UpdateLocations()
        {
            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
                if (!l.IsClickedOnView && !l.IsAnimatingWorld && !l.IsAnimatingView)
                {
                    l.Object3dEffect.View = _cameraMatrix;
                    l.Object3dEffect.World = l.WorldMatrix;
                }
        }

        private void UpdateRadar()
        {
            float radarAngle = MathHelper.ToRadians(-(float)((radarCanvas.RenderTransform as System.Windows.Media.RotateTransform).Angle));
            Vector2 v0 = Vector2.Zero;
            Vector2 v1 = new Vector2(60, -60);
            Vector2 v2 = new Vector2(-60, -60);
            v1 = Vector2Helper.RotateVector(radarAngle, v1);
            v2 = Vector2Helper.RotateVector(radarAngle, v2);
            Triangle radarHeadingTriangle = new Triangle(v0, v1, v2);

            //****comment in for Radar Heading Debug Lines****//
            //Dispatcher.BeginInvoke(delegate()
            //{
            //    var l1 = radarCanvas.Children[0] as System.Windows.Shapes.Line;
            //    var l2 = radarCanvas.Children[1] as System.Windows.Shapes.Line;
            //    l1.X2 = v1.X;
            //    l1.Y2 = v1.Y;
            //    l2.X2 = v2.X;
            //    l2.Y2 = v2.Y;
            //});
            //****comment in for Radar Heading Debug Lines****//

            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
            {
                if (radarHeadingTriangle.IsPointIn(l.RadarPoint))
                    l.RadarPointRepresenter.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/images/yellow-dot.png", UriKind.Relative));
                else
                    l.RadarPointRepresenter.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/images/white-dot.png", UriKind.Relative));
            }
        }

        private void HandleGesture()
        {
            while (TouchPanel.IsGestureAvailable)
            {
                GestureSample gestureSample = TouchPanel.ReadGesture();
                switch (gestureSample.GestureType)
                {
                    case GestureType.FreeDrag:
                        _gestureTimer.Stop();
                        _gestureState = GestureStateEnum.OnFreeDrag;

                        float rotateRate = 0.01f;
                        float translateRate = 0.1f;
                        _cameraMatrix = _cameraMatrix * Matrix.CreateRotationY(-gestureSample.Delta.X * rotateRate);
                        _cameraMatrix = _cameraMatrix * Matrix.CreateTranslation(0f, -gestureSample.Delta.Y * translateRate, 0f);

                        //rotate radar
                        Dispatcher.BeginInvoke(delegate()
                        {
                            (radarCanvas.RenderTransform as System.Windows.Media.RotateTransform).Angle += MathHelper.ToDegrees((gestureSample.Delta.X * rotateRate));
                        });

                        break;
                    case GestureType.DragComplete:
                        _gestureTime = 0;
                        _gestureTimer.Start();

                        break;
                    case GestureType.Tap:
                        _gestureState = GestureStateEnum.OnMotion;

                        if (myVM.ClickedNearbyLocation != null)
                        {
                            AnimateLocationDeselection(myVM.ClickedNearbyLocation);
                            myVM.ClickedNearbyLocation.IsClickedOnView = false;
                            if (OnLocationReleased != null)
                                OnLocationReleased(this, myVM.ClickedNearbyLocation);
                        }

                        if (_gestureState != GestureStateEnum.OnPressHold)
                        {
                            _gestureState = GestureStateEnum.OnPressHold;

                            float mouseX = gestureSample.Position.X;
                            float mouseY = gestureSample.Position.Y;
                            Vector3 nearsource = new Vector3((float)mouseX, (float)mouseY, 0f);
                            Vector3 farsource = new Vector3((float)mouseX, (float)mouseY, -1f);

                            Vector3 nearPoint = _device.Viewport.Unproject(nearsource, _projectionMatrix, _cameraMatrix, _phoneWorld);
                            Vector3 farPoint = _device.Viewport.Unproject(farsource, _projectionMatrix, _cameraMatrix, _phoneWorld);

                            Vector3 direction = -farPoint + nearPoint;
                            direction.Normalize();
                            Ray pickRay = new Ray(nearPoint, direction);

                            if (myVM.ClickedNearbyLocation != null)
                            {
                                AnimateLocationDeselection(myVM.ClickedNearbyLocation);
                                myVM.ClickedNearbyLocation.IsClickedOnView = false;
                                if (OnLocationReleased != null)
                                    OnLocationReleased(this, myVM.ClickedNearbyLocation);
                            }

                            foreach (LocationsVM.Location l in myVM.SelectedNearbyLocations)
                            {
                                float? result = pickRay.Intersects(l.PlacemarkBoundingSphere);
                                if (result.HasValue)
                                {
                                    AnimateLocationSelection(l);
                                    l.IsClickedOnView = true;
                                    if (OnLocationSelected != null)
                                        OnLocationSelected(this, l);

                                    break;
                                }
                            }
                        }

                        break;
                }
            }
        }


        #region 3d Animations

        private Matrix _cameraMatrixBehindDrag;
        private bool _onDragEndAnimation;

        /// <summary>
        /// Animates Deselected Location center and zoom backwards at original position in camera
        /// </summary>
        /// <param name="l">Location will be animated</param>
        private void AnimateLocationDeselection(LocationsVM.Location l)
        {
            l.IsAnimatingView = true;
            XNAMatrixAnimation animCam = new XNAMatrixAnimation(l.Object3dEffect.View, _cameraMatrix, TimeSpan.FromSeconds(0.3), 30);
            animCam.OnAnimating += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.View = e.Value;
            };
            animCam.OnCompleted += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.View = e.Value;
                l.IsAnimatingView = false;
            };
            animCam.Start();

            l.IsAnimatingWorld = true;
            XNAMatrixAnimation animWorld = new XNAMatrixAnimation(l.Object3dEffect.World, l.WorldMatrix, TimeSpan.FromSeconds(0.3), 30);
            animWorld.OnAnimating += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.World = e.Value;
            };
            animWorld.OnCompleted += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.World = e.Value;
                l.IsAnimatingWorld = false;
            };
            animWorld.Start();
        }

        /// <summary>
        /// Animates Selected Location Center and zoom in camera
        /// </summary>
        /// <param name="l">Location will be animated</param>
        private void AnimateLocationSelection(LocationsVM.Location l)
        {
            float zoomFactor = 0.20f;
            Vector3 v = new Vector3(l.VectorPoint.X, 0, l.VectorPoint.Z);

            Matrix cam = Matrix.CreateLookAt(new Vector3(0, 0, 0), v, Vector3.Up);
            l.IsAnimatingView = true;
            XNAMatrixAnimation animCam = new XNAMatrixAnimation(l.Object3dEffect.View, cam, TimeSpan.FromSeconds(0.3), 30);
            animCam.OnAnimating += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.View = e.Value;
            };
            animCam.OnCompleted += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.View = e.Value;
                l.IsAnimatingView = false;
            };
            animCam.Start();

            Matrix world = l.WorldMatrix * Matrix.CreateTranslation(new Vector3(-l.VectorPoint.X * zoomFactor, -l.VectorPoint.Y, -l.VectorPoint.Z * zoomFactor));
            l.IsAnimatingWorld = true;
            XNAMatrixAnimation animWorld = new XNAMatrixAnimation(l.Object3dEffect.World, world, TimeSpan.FromSeconds(0.3), 30);
            animWorld.OnAnimating += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.World = e.Value;
            };
            animWorld.OnCompleted += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                l.Object3dEffect.World = e.Value;
                l.IsAnimatingWorld = false;
            };
            animWorld.Start();
        }

        /// <summary>
        /// Animates the camera view matrix after user screen drag completed
        /// </summary>
        private void AnimateCameraAfterFreeDrag()
        {
            _onDragEndAnimation = true;
            XNAMatrixAnimation camAnim = new XNAMatrixAnimation(_cameraMatrix, _cameraMatrixBehindDrag, TimeSpan.FromSeconds(0.5), 30);
            camAnim.OnAnimating += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                _cameraMatrix = e.Value;
            };
            camAnim.OnCompleted += (XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e) =>
            {
                _cameraMatrix = e.Value;
                _onDragEndAnimation = false;
                _gestureState = GestureStateEnum.OnMotion;
            };
            camAnim.Start();
        }

        #endregion

        #region static methods
        /// <summary>
        /// Sets the location collection which will be shown in map
        /// </summary>
        /// <param name="locations">Location collection</param>
        public static void SetLocations(ObservableCollection<LocationsVM.Location> locations)
        {
            ARViewVM._NearbyLocations = locations;
        }

        public delegate void OnLocationSelectEventHandler(ARView sender, LocationsVM.Location location);
        /// <summary>
        /// happens when user selects a location in camera view with touching it
        /// </summary>
        public static event OnLocationSelectEventHandler OnLocationSelected;
        /// <summary>
        /// happens when user releases touch from the selected location in camera view with tapping it again
        /// </summary>
        public static event OnLocationSelectEventHandler OnLocationReleased;


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