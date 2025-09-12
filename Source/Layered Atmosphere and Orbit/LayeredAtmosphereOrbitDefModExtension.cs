using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LayeredAtmosphereOrbitDefModExtension : DefModExtension
    {
        public bool isReplaceConnections = false;
        public bool isOptionToAutoAdd = false;
        public bool isPreventQuestIfNotWhitelisted = false;
        public bool isRockColored = false;
        public float elevation = 200;
        public float tempOffest = 0;
        public PlanetLayerGroupDef planetLayerGroup;
        public List<PlanetLayerDef> forcedConnectToPlanetLayer = new List<PlanetLayerDef>();
        public List<BiomeDef> availableBiomes = new List<BiomeDef>();
    }
}