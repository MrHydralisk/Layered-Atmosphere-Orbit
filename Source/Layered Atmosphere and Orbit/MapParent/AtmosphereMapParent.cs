using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class AtmosphereMapParent : SpaceMapParent
    {
        public override MapGeneratorDef MapGeneratorDef => def.mapGenerator ?? DefOfLocal.LAO_Atmosphere;
        public override string GetInspectString()
        {
            List<string> inspectStrings = new List<string>();
            string extraIS = GetExtraInspectString();
            if (!extraIS.NullOrEmpty())
            {
                inspectStrings.Add(extraIS);
            }
            string baseIS = base.GetInspectString();
            if (!baseIS.NullOrEmpty())
            {
                inspectStrings.Add(baseIS);
            }
            return inspectStrings.NullOrEmpty() ? "" : String.Join("\n", inspectStrings);
        }

        public virtual string GetExtraInspectString()
        {
            if (preciousResource != null)
            {
                return "TracesOfPreciousResource".Translate(NamedArgumentUtility.Named(preciousResource, "RESOURCE"));
            }
            return "";
        }
    }
}