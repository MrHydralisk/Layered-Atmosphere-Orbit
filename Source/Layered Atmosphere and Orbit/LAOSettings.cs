using System.Collections.Generic;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class LAOSettings : ModSettings
    {
        public bool UseFuelCostBetweenLayers = false;
        public float FuelPerKm = 1;
        public List<string> AutoAddLayersDefNames = new List<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UseFuelCostBetweenLayers, "UseFuelCostBetweenLayers", defaultValue: false);
            Scribe_Values.Look(ref FuelPerKm, "FuelPerKm", defaultValue: 1);
            Scribe_Collections.Look(ref AutoAddLayersDefNames, "AutoAddLayersDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (AutoAddLayersDefNames == null)
                {
                    AutoAddLayersDefNames = new List<string>();
                }
            }
        }
    }
}

