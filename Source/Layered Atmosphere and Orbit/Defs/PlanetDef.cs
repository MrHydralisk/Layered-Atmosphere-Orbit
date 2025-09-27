using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class PlanetDef : Def
    {
        [NoTranslate]
        public string typeTag = "Planet";
        public Vector3 posFromRimworld = Vector3.zero;
        public float gravityWellExitElevation = 1000;
        public float gravityWellRadius = 200;
        public List<GameConditionDef> permamentGameConditionDefs = new List<GameConditionDef>();
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