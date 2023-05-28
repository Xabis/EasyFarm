// ///////////////////////////////////////////////////////////////////
// This file is a part of EasyFarm for Final Fantasy XI
// Copyright (C) 2013 Mykezero
//  
// EasyFarm is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// EasyFarm is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// If not, see <http://www.gnu.org/licenses/>.
// ///////////////////////////////////////////////////////////////////
using System;

namespace MemoryAPI.Navigation
{
    public class Position
    {
        public float H { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public override string ToString()
        {
            return "(X: " + X + " Y: " + Y + " Z: " + Z + ")";
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ H.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            var other = obj as Position;
            if (other == null) return false;

            var deviation = Math.Abs(this.X - other.X) + 
                Math.Abs(this.Y - other.Y) + 
                Math.Abs(this.Z - other.Z) + 
                Math.Abs(this.H - other.H);

            return Math.Abs(deviation) <= 0;
        }

        public double Distance(Position other)
        {
            return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Z - other.Z, 2));
        }

        public float[] ToDetourPosition()
        {
            float[] detourPosition = new float[3];

            detourPosition[0] = X;
            detourPosition[1] = -Y;
            detourPosition[2] = -Z;

            return detourPosition;
        }

        public static int Dot(Position A, Position B)
        {
            return
                (int)((A.X * B.X) +
                (A.Y * B.Y) +
                (A.Z * B.Z));
        }

        public static float DotF(Position A, Position B)
        {
            return
                (A.X * B.X) +
                (A.Y * B.Y) +
                (A.Z * B.Z);
        }


        public static Position Cross(Position A, Position B)
        {
            return new Position()
            {
                X = A.Y * B.Z - A.Z * B.Y,
                Y = -(A.X * B.Z - A.Z * B.X),
                Z = A.X * B.Y - A.Y * B.X
            };
        }

        public Position Normalized()
        {
            var length = (float)Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
            return new Position
            {
                X = X / length,
                Y = Y / length,
                Z = Z / length
            };
        }

        public static Position operator -(Position A, Position B)
        {
            return new Position
            {
                X = A.X - B.X,
                Y = A.Y - B.Y,
                Z = A.Z - B.Z
            };
        }

        public static Position operator +(Position A, Position B)
        {
            return new Position
            {
                X = A.X + B.X,
                Y = A.Y + B.Y,
                Z = A.Z + B.Z
            };
        }
        public static Position operator *(Position A, Position B)
        {
            return new Position
            {
                X = A.X * B.X,
                Y = A.Y * B.Y,
                Z = A.Z * B.Z
            };
        }
        public static Position operator *(Position A, float B)
        {
            return new Position
            {
                X = A.X * B,
                Y = A.Y * B,
                Z = A.Z * B
            };
        }
        public static Position operator *(float A, Position B)
        {
            return new Position
            {
                X = A * B.X,
                Y = A * B.Y,
                Z = A * B.Z
            };
        }

        public Position HeadingVector()
        {
            return new Position
            {
                X = (float)Math.Cos(H),
                Y = 0,
                Z = -(float)Math.Sin(H),
            }.Normalized();
        }

        public Position PerpendicularVectorFromHeading()
        {
            Position heading = HeadingVector();
            return new Position
            {
                X = heading.Z,
                Z = -heading.X,
            };
        }

        /// <summary>
        /// Projects a clamped point onto line AB that is perpendicular from position P. 
        /// </summary>
        /// <param name="A">Point 1 in line AB</param>
        /// <param name="B">Point 2 in line AB</param>
        /// <param name="P">Position to project from</param>
        /// <returns>A point projected onto the line. Result will be clamped to either end, if the projected point lies beyond it.</returns>
        public static Position ProjectClosestPoint(Position A, Position B, Position P)
        {
            Position AP = P - A;
            Position AB = B - A;
            float dP = DotF(AP, AB);
            float dB = DotF(AB, AB);
            if (dP < 0)
                return A;
            else if (dP > dB)
                return B;
            else
                return A + dP / dB * AB;
        }
    }
}