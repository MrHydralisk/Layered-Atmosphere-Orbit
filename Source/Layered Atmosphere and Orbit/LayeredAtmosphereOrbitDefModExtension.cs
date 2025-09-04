using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LayeredAtmosphereOrbitDefModExtension : DefModExtension
    {
        public bool isReplaceConnections = false;
        public bool isOptionToAutoAdd = false;
        public float elevation = 200;
        public PlanetLayerGroupDef planetLayerGroup;
        public List<PlanetLayerDef> forcedConnectToPlanetLayer = new List<PlanetLayerDef>();
    }
}