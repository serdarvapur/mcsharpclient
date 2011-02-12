using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class Vector3
    {

        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3()
        {
            this.X = 0f;
            this.Y = 0f;
            this.Z = 0f;
        }

    }
}
