using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class PlanetLayerGroupDef : Def
    {
        public PlanetLayerDef defaultPlanetLayer;
        public PlanetDef planet;
        public List<PlanetLayerGroupDef> planetLayerGroupsToShowToo = new List<PlanetLayerGroupDef>();
        public List<PlanetLayerGroupDef> planetLayerGroupsDirectConnection = new List<PlanetLayerGroupDef>();
        [NoTranslate]
        public string viewGizmoTexPath;
        [Unsaved(false)]
        private Texture2D cachedGizmoTexture;

        public Texture2D ViewGizmoTexture
        {
            get
            {
                if (!cachedGizmoTexture)
                {
                    return cachedGizmoTexture = ContentFinder<Texture2D>.Get(viewGizmoTexPath);
                }
                return cachedGizmoTexture;
            }
        }
    }
}