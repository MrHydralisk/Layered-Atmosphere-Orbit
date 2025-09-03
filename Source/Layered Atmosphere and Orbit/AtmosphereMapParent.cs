using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static UnityEngine.UI.GridLayoutGroup;

namespace LayeredAtmosphereOrbit
{
    public class AtmosphereMapParent : SpaceMapParent
    {
        public override MapGeneratorDef MapGeneratorDef => def.mapGenerator ?? DefOfLocal.LAO_Atmosphere;
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (preciousResource != null)
            {
                text += "\n" + "TracesOfPreciousResource".Translate(NamedArgumentUtility.Named(preciousResource, "RESOURCE"));
            }
            return text.Trim();
        }
    }
}