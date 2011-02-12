using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class Location : Vector3
    {

        public float Stance;

        public Location(float x, float y, float z, float stance) : base(x,y,z)
        {
            this.Stance = stance;
        }
    }
}
