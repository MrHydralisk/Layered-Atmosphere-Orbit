using RimWorld;
using RimWorld.Planet;
using Verse;

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