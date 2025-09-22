using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class Crators : ModuleBase
    {
        private List<(Vector3, float)> impactPoints;
        private float inside;
        private float outside;
        private float steepnessInner;
        private float steepnessOuter;

        public Crators()
            : base(0)
        {
        }

        public Crators(List<(Vector3, float)> impactPoints, float inside = 0, float outside = 0, float steepnessInner = 1, float steepnessOuter = 3)
            : base(0)
        {
            this.impactPoints = impactPoints;
            this.inside = inside;
            this.outside = outside;
            this.steepnessInner = steepnessInner;
            this.steepnessOuter = steepnessOuter;
        }

        public override double GetValue(double x, double y, double z)
        {
            Vector3 position = new Vector3((float)x, (float)y, (float)z);
            float elevation = float.MinValue;
            IEnumerable<(float, float)> ImpactPoints = impactPoints.Select(ip => (Vector3.Distance(position, ip.Item1), ip.Item2));
            foreach ((float distance, float radius) in ImpactPoints)
            {
                float outerWall = 0;
                if (distance <= radius + steepnessOuter)
                {
                    outerWall = Mathf.Pow((distance - radius - steepnessOuter) / steepnessOuter, 2);
                }
                elevation = Mathf.Max(outside, outerWall, elevation);
            }
            float elevationInner = float.MaxValue;
            foreach ((float distance, float radius) in ImpactPoints)
            {
                float innerWall = 0;
                if (distance >= radius - steepnessInner)
                {
                    innerWall = Mathf.Pow((distance - radius + steepnessInner), 2);
                }
                elevationInner = Mathf.Min(Mathf.Max(inside, innerWall), elevationInner);
            }
            elevation = Mathf.Min(elevation, elevationInner);
            return elevation;
        }
    }
}

