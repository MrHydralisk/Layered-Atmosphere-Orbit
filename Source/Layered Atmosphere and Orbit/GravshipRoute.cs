using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GravshipRoute : IExposable
    {
        public List<Vector3> routePoints = new List<Vector3>();

        public float routeLength 
        {
            get;
            private set;
        }

        private SimpleCurve curveX;
        private SimpleCurve curveY;
        private SimpleCurve curveZ;

        private bool isCached;

        public GravshipRoute()
        {

        }

        public GravshipRoute(List<Vector3> route)
        {
            routePoints = route;
        }

        public void TryCache()
        {
            if (!isCached)
            {
                curveX = new SimpleCurve();
                curveY = new SimpleCurve();
                curveZ = new SimpleCurve();
                routeLength = 0;
                for (int i = 1; i < routePoints.Count - 1; i++)
                {
                    routeLength += Vector3.Distance(routePoints[i], routePoints[i - 1]) / 100;
                    Log.Message($"TryCache [{i}] {routeLength} += {Vector3.Distance(routePoints[i], routePoints[i - 1])} Vector3.Distance({routePoints[i]}|{routePoints[i].normalized}, {routePoints[i - 1]}|{routePoints[i - 1].normalized}) | {Vector3.Distance(routePoints[i].normalized, routePoints[i - 1].normalized)} | {GenMath.SphericalDistance(routePoints[i], routePoints[i - 1])} | {GenMath.SphericalDistance(routePoints[i].normalized, routePoints[i - 1].normalized)}");
                }
                routeLength += GenMath.SphericalDistance(routePoints[routePoints.Count - 2].normalized, routePoints[routePoints.Count - 1].normalized);
                Log.Message($"TryCache [{routePoints.Count - 1}] {routeLength} += {GenMath.SphericalDistance(routePoints[routePoints.Count - 2], routePoints[routePoints.Count - 1])} Vector3.Distance({routePoints[routePoints.Count - 2]}|{routePoints[routePoints.Count - 2].normalized}, {routePoints[routePoints.Count - 1]}|{routePoints[routePoints.Count - 1].normalized}) | {GenMath.SphericalDistance(routePoints[routePoints.Count - 2].normalized, routePoints[routePoints.Count - 1].normalized)} | {Vector3.Distance(routePoints[routePoints.Count - 2], routePoints[routePoints.Count - 1])} | {Vector3.Distance(routePoints[routePoints.Count - 2].normalized, routePoints[routePoints.Count - 1].normalized)}");
                float passedLength = 0;
                curveX.Add(0, routePoints[0].x);
                curveY.Add(0, routePoints[0].y);
                curveZ.Add(0, routePoints[0].z);
                for (int i = 1; i < routePoints.Count - 1; i++)
                {
                    passedLength += Vector3.Distance(routePoints[i], routePoints[i - 1]) / 100;
                    curveX.Add(passedLength / routeLength, routePoints[i].x);
                    curveY.Add(passedLength / routeLength, routePoints[i].y);
                    curveZ.Add(passedLength / routeLength, routePoints[i].z);
                }
                curveX.Add(1, routePoints[routePoints.Count - 1].x);
                curveY.Add(1, routePoints[routePoints.Count - 1].y);
                curveZ.Add(1, routePoints[routePoints.Count - 1].z);
                isCached = true;
            }
        }

        public Vector3 Evaluate(float x)
        {
            TryCache();
            Vector3 v = new Vector3(curveX.Evaluate(x),curveY.Evaluate(x),curveZ.Evaluate(x));
            Log.Message($"Evaluate {x} as {v} [{v.normalized}]");
            return v;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref routePoints, "routePoints", LookMode.Value);
        }
    }
}