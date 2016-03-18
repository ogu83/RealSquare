using System;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Windows;

namespace ARFinity
{
    internal sealed class Vector2Helper
    {
        /// <summary>
        /// Gets a rotated vector2 from given vector2
        /// </summary>
        /// <param name="angle">rotation angle in radians</param>
        /// <param name="v">vector2 to be rotated</param>
        /// <returns></returns>
        public static Vector2 RotateVector(float angle, Vector2 v)
        {
            float x = v.X * (float)Math.Cos(angle) - v.Y * (float)Math.Sin(angle);
            float y = v.X * (float)Math.Sin(angle) + v.Y * (float)Math.Cos(angle);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Returns angle in radians between two vectors
        /// </summary>
        /// <param name="vector1">1. vector</param>
        /// <param name="vector2">2. vector</param>
        /// <returns>angle in radians</returns>
        public static float AngleVectorToVector(Vector2 vector1, Vector2 vector2)
        {
            vector1.Normalize();
            vector2.Normalize();
            float cosTeta = Vector2.Dot(vector1, vector2);
            return (float)Math.Acos(cosTeta);
        }
    }
}
