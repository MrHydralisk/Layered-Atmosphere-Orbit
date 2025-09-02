using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LAOSettings : ModSettings
    {
        public float FuelPerKm = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref FuelPerKm, "FuelPerKm", defaultValue: 1);
        }
    }
}

