using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Xna.Framework;

namespace ARFinity
{
    internal class XNAMatrixAnimation : IDisposable
    {
        private DispatcherTimer _timer;

        public delegate void AnimationEventHandler(XNAMatrixAnimation sender, XNAMatrixAnimationEventArgs e);
        public event AnimationEventHandler OnAnimating;
        public event AnimationEventHandler OnCompleted;

        public XNAMatrixAnimation(Matrix from, Matrix to, TimeSpan duration, float fps)
        {
            From = from;
            To = to;
            Duration = duration;
            FPS = fps;

            this.Restart();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            _time.Add(TimeSpan.FromSeconds(1 / FPS));

            _value.M11 = From.M11 + (To.M11 - From.M11) * _keyFrame / _totalKeyFrame;
            _value.M12 = From.M12 + (To.M12 - From.M12) * _keyFrame / _totalKeyFrame;
            _value.M13 = From.M13 + (To.M13 - From.M13) * _keyFrame / _totalKeyFrame;
            _value.M14 = From.M14 + (To.M14 - From.M14) * _keyFrame / _totalKeyFrame;

            _value.M21 = From.M21 + (To.M21 - From.M21) * _keyFrame / _totalKeyFrame;
            _value.M22 = From.M22 + (To.M22 - From.M22) * _keyFrame / _totalKeyFrame;
            _value.M23 = From.M23 + (To.M23 - From.M23) * _keyFrame / _totalKeyFrame;
            _value.M24 = From.M24 + (To.M24 - From.M24) * _keyFrame / _totalKeyFrame;

            _value.M31 = From.M31 + (To.M31 - From.M31) * _keyFrame / _totalKeyFrame;
            _value.M32 = From.M32 + (To.M32 - From.M32) * _keyFrame / _totalKeyFrame;
            _value.M33 = From.M33 + (To.M33 - From.M33) * _keyFrame / _totalKeyFrame;
            _value.M34 = From.M34 + (To.M34 - From.M34) * _keyFrame / _totalKeyFrame;

            _value.M41 = From.M41 + (To.M41 - From.M41) * _keyFrame / _totalKeyFrame;
            _value.M42 = From.M42 + (To.M42 - From.M42) * _keyFrame / _totalKeyFrame;
            _value.M43 = From.M43 + (To.M43 - From.M43) * _keyFrame / _totalKeyFrame;
            _value.M44 = From.M44 + (To.M44 - From.M44) * _keyFrame / _totalKeyFrame;

            if (_keyFrame < _totalKeyFrame)
            {
                _keyFrame++;
                if (OnAnimating != null)
                    OnAnimating(this, new XNAMatrixAnimationEventArgs(_keyFrame, _time, _value));
            }
            else
            {
                this.Stop();
                if (OnCompleted != null)
                    OnCompleted(this, new XNAMatrixAnimationEventArgs(_keyFrame, _time, To));
            }
        }

        /// <summary>
        /// frame per second
        /// </summary>
        public float FPS { get; private set; }
        public TimeSpan Duration { get; private set; }
        public Matrix From { get; private set; }
        public Matrix To { get; private set; }

        private Matrix _value;
        private int _keyFrame;
        private int _totalKeyFrame;
        private TimeSpan _time = TimeSpan.Zero;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Restart()
        {
            _time = TimeSpan.Zero;
            _keyFrame = 0;
            _totalKeyFrame = (int)(Duration.TotalSeconds * FPS);
            _value = From;

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = TimeSpan.FromSeconds(1 / FPS);
        }

        #region IDisposable
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }
        #endregion
    }

    internal class XNAMatrixAnimationEventArgs : EventArgs
    {
        public XNAMatrixAnimationEventArgs(int keyframe, TimeSpan time, Matrix value)
        {
            this.Value = value;
            this.KeyFrame = keyframe;
            this.Time = time;
        }

        /// <summary>
        /// Gets Matrix at that moment (at that keyframe)
        /// </summary>
        public Matrix Value { get; private set; }
        /// <summary>
        /// Gets Keyframe no
        /// </summary>
        public int KeyFrame { get; private set; }
        /// <summary>
        /// Gets relative Time at that keyframe
        /// </summary>
        public TimeSpan Time { get; private set; }
    }
}