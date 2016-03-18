using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Xna.Framework;

namespace ARFinity
{
    internal struct Triangle
    {
        /// <summary>
        /// ctor for triange
        /// </summary>
        /// <param name="a">1. Corner of the Triangle</param>
        /// <param name="b">2. Corner of the Triangle</param>
        /// <param name="c">3. Corner of the Triangle</param>
        public Triangle(Vector2 a, Vector2 b, Vector2 c)
            : this()
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>
        /// Gets A Corner of the triangle
        /// </summary>
        public Vector2 A { get; private set; }
        /// <summary>
        /// Gets B Corner of the triangle
        /// </summary>
        public Vector2 B { get; private set; }
        /// <summary>
        /// Gets C Corner of the triangle
        /// </summary>
        public Vector2 C { get; private set; }

        /// <summary>
        /// Returns wether point P in this triangle
        /// </summary>
        /// <param name="P">a point</param>
        /// <returns>wether point in this rectangle (true or false)</returns>
        public bool IsPointIn(Vector2 P)
        {
            ////Compute Vectors
            //Vector2 v0 = C - A;
            //Vector2 v1 = B - A;
            //Vector2 v2 = P - A;

            ////Compute dot products
            //float dot00 = Vector2.Dot(v0, v0);
            //float dot01 = Vector2.Dot(v0, v1);
            //float dot02 = Vector2.Dot(v0, v2);
            //float dot11 = Vector2.Dot(v1, v1);
            //float dot12 = Vector2.Dot(v1, v2);

            //// Compute barycentric coordinates
            //float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            //float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            //float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            //// Check if point is in triangle
            //return (u >= 0) && (v >= 0) && (u + v < 1);

            return PointInTriangle(new Vector3(A, 0), new Vector3(B, 0), new Vector3(C, 0), new Vector3(P, 0));
        }

        /// <summary>
        /// Determine whether a point P is inside the triangle ABC. Note, this function
        /// assumes that P is coplanar with the triangle.
        /// </summary>
        /// <returns>True if the point is inside, false if it is not.</returns>
        public static bool PointInTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
        {
            // Prepare our barycentric variables
            Vector3 u = B - A;
            Vector3 v = C - A;
            Vector3 w = P - A;

            Vector3 vCrossW = Vector3.Cross(v, w);
            Vector3 vCrossU = Vector3.Cross(v, u);

            // Test sign of r
            if (Vector3.Dot(vCrossW, vCrossU) < 0)
                return false;

            Vector3 uCrossW = Vector3.Cross(u, w);
            Vector3 uCrossV = Vector3.Cross(u, v);

            // Test sign of t
            if (Vector3.Dot(uCrossW, uCrossV) < 0)
                return false;

            // At this piont, we know that r and t and both > 0
            float denom = uCrossV.Length();
            float r = vCrossW.Length() / denom;
            float t = uCrossW.Length() / denom;

            return (r <= 1 && t <= 1 && r + t <= 1);
        }

    }
}
