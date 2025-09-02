using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LAOMod : Mod
    {
        public static LAOSettings Settings { get; private set; }

        public static List<PlanetLayerDef> AutoAddLayerOptions = new List<PlanetLayerDef>();

        public LAOMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LAOSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard options = new Listing_Standard();
            options.Begin(inRect);
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.UseFuelCostBetweenLayers".Translate().RawText, ref Settings.UseFuelCostBetweenLayers);
            options.Label("LayeredAtmosphereOrbit.Settings.FuelPerKm".Translate(Settings.FuelPerKm));
            Settings.FuelPerKm = Mathf.Round(options.Slider(Settings.FuelPerKm, 0f, 5f) * 100f) / 100f;
            options.GapLine();
            options.Label("LayeredAtmosphereOrbit.Settings.AutoAddLayersDefName.Total".Translate(Settings.FuelPerKm));
            foreach (PlanetLayerDef planetLayerDef in AutoAddLayerOptions)
            {
                bool isCurrentlyAutoAdd = Settings.AutoAddLayersDefNames.Contains(planetLayerDef.defName);
                bool isAutoAdd = isCurrentlyAutoAdd;
                options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.AutoAddLayersDefName.Option".Translate(planetLayerDef.label).RawText, ref isAutoAdd);
                if (isAutoAdd && !isCurrentlyAutoAdd)
                {
                    Settings.AutoAddLayersDefNames.AddDistinct(planetLayerDef.defName);
                }
                if (!isAutoAdd && isCurrentlyAutoAdd)
                {
                    Settings.AutoAddLayersDefNames.RemoveAll((string s) => s == planetLayerDef.defName);
                }
            }
            foreach (string planetLayerDefName in Settings.AutoAddLayersDefNames.Except(AutoAddLayerOptions.Select(pld => pld.defName)))
            {
                bool isAutoAdd = true;
                options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.AutoAddLayersDefName.OptionOutdated".Translate(planetLayerDefName).RawText, ref isAutoAdd);
                if (!isAutoAdd)
                {
                    Settings.AutoAddLayersDefNames.RemoveAll((string s) => s == planetLayerDefName);
                }
            }
            options.End();
        }

        public override string SettingsCategory()
        {
            return "LayeredAtmosphereOrbit.Settings.Title".Translate().RawText;
        }
    }
}
