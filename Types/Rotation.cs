using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class Rotation
    {
        public float Pitch, Yaw;

        public Rotation(float Pitch, float Yaw)
        {
            this.Pitch = Pitch;
            this.Yaw = Yaw;
        }
    }
}
