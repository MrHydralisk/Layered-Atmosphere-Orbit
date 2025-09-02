using RimWorld;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LAOMod : Mod
    {
        public static LAOSettings Settings { get; private set; }

        public LAOMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LAOSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard options = new Listing_Standard();
            options.Begin(inRect);
            options.Label("LayeredAtmosphereOrbit.Settings.FuelPerKm".Translate(Settings.FuelPerKm));
            Settings.FuelPerKm = Mathf.Round(options.Slider(Settings.FuelPerKm, 0f, 5f) * 100f) / 100f;

            options.End();
        }

        public override string SettingsCategory()
        {
            return "LayeredAtmosphereOrbit.Settings.Title".Translate().RawText;
        }
    }
}
