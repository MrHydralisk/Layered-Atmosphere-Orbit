using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class PlanetLayerGroupDef : Def
    {
        public PlanetLayerDef defaultPlanetLayer;
        public PlanetDef planet;
        public List<PlanetLayerGroupDef> planetLayerGroupsToShowToo = new List<PlanetLayerGroupDef>();
        public List<PlanetLayerGroupDef> planetLayerGroupsDirectConnection = new List<PlanetLayerGroupDef>();
    }
}