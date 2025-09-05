using RimWorld;
using RimWorld.Planet;
using System.Drawing;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class WITab_PlanetLayer : WITab
    {
        private Vector2 scrollPosition;

        private float lastDrawnHeight;

        private static readonly Vector2 WinSize = new Vector2(432f, 540f);

        public PlanetLayer planetLayer => SelPlanetTile.Layer;
        public LayeredAtmosphereOrbitDefModExtension LAODefModExtension => planetLayer.Def.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();

        public override bool IsVisible
        {
            get
            {
                if (ModsConfig.OdysseyActive && base.SelPlanetTile.Valid)
                {
                    return LAODefModExtension != null;
                }
                return false;
            }
        }

        public WITab_PlanetLayer()
        {
            size = WinSize;
            labelKey = "LayeredAtmosphereOrbit.TabPlanetLayer.Label";
        }

        protected override void FillTab()
        {
            Rect outRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastDrawnHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect);
            Tile selTile = base.SelTile;
            PlanetTile selPlanerTile = base.SelPlanetTile;
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.verticalSpacing = 0f;
            listing_Standard.Begin(rect);
            PlanetLayerGroupDef planetLayerGroup = LAODefModExtension?.planetLayerGroup;
            if (planetLayerGroup != null)
            {
                Text.Font = GameFont.Medium;
                listing_Standard.Label(planetLayerGroup.LabelCap);
                Text.Font = GameFont.Small;
                listing_Standard.Label(planetLayerGroup.description);
                listing_Standard.GapLine();
            }
            if (selTile.PrimaryBiome != null)
            {
                Text.Font = GameFont.Medium;
                listing_Standard.Label(selTile.PrimaryBiome.LabelCap);
                Text.Font = GameFont.Small;
                listing_Standard.Label(selTile.PrimaryBiome.description);
                listing_Standard.GapLine();
                if (!selTile.PrimaryBiome.implemented)
                {
                    listing_Standard.Label(string.Format("{0} {1}", selTile.PrimaryBiome.LabelCap, "BiomeNotImplemented".Translate()));
                }
            }
            listing_Standard.LabelDouble("Elevation".Translate(), selTile.Layer.Def.elevationString.Formatted(selTile.elevation.ToString("F0")));
            listing_Standard.LabelDouble("AvgTemp".Translate(), GenTemperature.GetAverageTemperatureLabel(selPlanerTile));
            listing_Standard.GapLine();
            listing_Standard.LabelDouble("TimeZone".Translate(), GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(selPlanerTile).x).ToStringWithSign());
            if (Prefs.DevMode)
            {
                listing_Standard.LabelDouble("Debug world tile ID", selPlanerTile.ToString());
            }
            lastDrawnHeight = rect.y + listing_Standard.CurHeight;
            listing_Standard.End();
            Widgets.EndScrollView();
        }
    }
}

