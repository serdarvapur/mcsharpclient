using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class Location
    {
        public double X, Y, Z, Stance;

        public Location(double X, double Y, double Z, double Stance)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Stance = Stance;
        }
    }
}
