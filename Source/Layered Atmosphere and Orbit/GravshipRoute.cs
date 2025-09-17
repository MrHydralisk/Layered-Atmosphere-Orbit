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
        public List<PlanetLayerDef> routePlanetLayers = new List<PlanetLayerDef>();

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

        public void AddRoutePoint(Vector3 point, PlanetLayerDef planetLayerDef)
        {
            routePoints.Add(point);
            routePlanetLayers.Add(planetLayerDef);
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
                }
                routeLength += GenMath.SphericalDistance(routePoints[routePoints.Count - 2].normalized, routePoints[routePoints.Count - 1].normalized);
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

        public Vector3 Evaluate(float x, out PlanetLayerDef planetLayerDef)
        {
            TryCache();
            Vector3 v = new Vector3(curveX.Evaluate(x),curveY.Evaluate(x),curveZ.Evaluate(x));
            int index = curveX.Points.FindLastIndex((CurvePoint cp) => x >= cp.x);
            planetLayerDef = routePlanetLayers[index];
            return v;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref routePoints, "routePoints", LookMode.Value);
            Scribe_Collections.Look(ref routePlanetLayers, "routePlanetLayers", LookMode.Def);
        }
    }
}