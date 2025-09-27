using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class WITab_PlanetLayer : WITab
    {
        private Vector2 scrollPosition;

        private float lastDrawnHeight;

        private static string cachedGrowingQuadrumsDescription;
        private static PlanetTile cachedGrowingQuadrumsTile;

        private bool isShowPlanetLayerGroup = false;
        private bool isShowPlanet = false;
        private bool isShowBiome = false;

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
            PlanetTile selPlanetTile = base.SelPlanetTile;
            WorldObject worldObject = SelObject;
            bool isHaveBiome = selTile.PrimaryBiome != null;
            bool isSurface = planetLayer.Def.canFormCaravans;
            bool isSpace = planetLayer.Def.isSpace;
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.verticalSpacing = 0f;
            listing_Standard.Begin(rect);
            PlanetLayerGroupDef planetLayerGroup = LAODefModExtension?.planetLayerGroup;
            if (planetLayerGroup != null)
            {
                Text.Font = GameFont.Medium;
                if (listing_Standard.LabelInvisButton($"{planetLayerGroup.LabelCap} [{planetLayer.Def.LabelCap}]", tooltip: "DefInfoTip".Translate(), labelIcon: TexButton.Info))
                {
                    isShowPlanetLayerGroup = !isShowPlanetLayerGroup;
                }
                Text.Font = GameFont.Small;
                if (isShowPlanetLayerGroup)
                {
                    listing_Standard.Label(planetLayerGroup.description);
                }
                listing_Standard.GapLine();
                if (planetLayerGroup.planet != null)
                {
                    Text.Font = GameFont.Medium;
                    if (listing_Standard.LabelInvisButton($"LayeredAtmosphereOrbit.TabPlanetLayer.PlanetTag.{planetLayerGroup.planet.typeTag}".Translate(planetLayerGroup.planet.LabelCap), tooltip: "DefInfoTip".Translate(), labelIcon: TexButton.Info))
                    {
                        isShowPlanet = !isShowPlanet;
                    }
                    Text.Font = GameFont.Small;
                    if (isShowPlanet)
                    {
                        listing_Standard.Label(planetLayerGroup.planet.description);
                        listing_Standard.LabelDouble("LayeredAtmosphereOrbit.TabPlanetLayer.RimworldName".Translate(), Find.World.info.name);
                    }
                    listing_Standard.GapLine();
                }
            }
            if (isHaveBiome)
            {
                Text.Font = GameFont.Medium;
                if (listing_Standard.LabelInvisButton("LayeredAtmosphereOrbit.TabPlanetLayer.Biome".Translate(selTile.PrimaryBiome.LabelCap), tooltip: "DefInfoTip".Translate(), labelIcon: TexButton.Info))
                {
                    isShowBiome = !isShowBiome;
                }
                Text.Font = GameFont.Small;
                if (isShowBiome)
                {
                    listing_Standard.Label(selTile.PrimaryBiome.description);
                }
                listing_Standard.GapLine();
                if (!selTile.PrimaryBiome.implemented)
                {
                    listing_Standard.Label(string.Format("{0} {1}", selTile.PrimaryBiome.LabelCap, "BiomeNotImplemented".Translate()));
                }
            }
            if (selTile.HillinessLabel != 0)
            {
                listing_Standard.LabelDouble("Terrain".Translate(), selTile.HillinessLabel.GetLabelCap());
            }
            if (selTile is SurfaceTile surfaceTile)
            {
                if (surfaceTile.Roads != null)
                {
                    listing_Standard.LabelDouble("Road".Translate(), surfaceTile.Roads.Select((SurfaceTile.RoadLink roadlink) => roadlink.road.label).Distinct().ToCommaList(useAnd: true)
                        .CapitalizeFirst());
                }
                if (surfaceTile.Rivers != null)
                {
                    listing_Standard.LabelDouble("River".Translate(), surfaceTile.Rivers.MaxBy((SurfaceTile.RiverLink riverlink) => riverlink.river.degradeThreshold).river.LabelCap);
                }
            }
            if (isSurface && !Find.World.Impassable(selPlanetTile))
            {
                StringBuilder stringBuilder = new StringBuilder();
                PlanetTile tile2 = selPlanetTile;
                StringBuilder explanation = stringBuilder;
                string rightLabel = (WorldPathGrid.CalculatedMovementDifficultyAt(tile2, perceivedStatic: false, null, explanation) * Find.WorldGrid.GetRoadMovementDifficultyMultiplier(selPlanetTile, PlanetTile.Invalid, stringBuilder)).ToString("0.#");
                if (WorldPathGrid.WillWinterEverAffectMovementDifficulty(selPlanetTile) && WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(selPlanetTile) < 2f)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                    stringBuilder.Append(" (");
                    stringBuilder.Append("MovementDifficultyOffsetInWinter".Translate($"+{2f:0.#}"));
                    stringBuilder.Append(")");
                }
                listing_Standard.LabelDouble("MovementDifficulty".Translate(), rightLabel, stringBuilder.ToString());
            }
            if (worldObject is FloatingIslandMapParent floatingIslandMapParent && floatingIslandMapParent.rockDef != null)
            {
                listing_Standard.LabelDouble("LayeredAtmosphereOrbit.TabPlanetLayer.RockType".Translate(), floatingIslandMapParent.rockDef.LabelCap);
            }
            else if (isHaveBiome && selTile.PrimaryBiome.canBuildBase)
            {
                listing_Standard.LabelDouble("StoneTypesHere".Translate(), (from rt in Find.World.NaturalRockTypesIn(selPlanetTile)
                                                                            select rt.label).ToCommaList(useAnd: true).CapitalizeFirst());
            }
            listing_Standard.LabelDouble("Elevation".Translate(), selTile.Layer.Def.elevationString.Formatted(selTile.elevation.ToString("F0")));
            if (ModsConfig.OdysseyActive && selTile.Landmark != null)
            {
                listing_Standard.LabelDouble("Landmark".Translate(), selTile.Landmark.name, selTile.Landmark.def.description);
            }
            if (selTile.Mutators.Any())
            {
                IOrderedEnumerable<TileMutatorDef> source = selTile.Mutators.OrderBy((TileMutatorDef m) => -m.displayPriority);
                listing_Standard.LabelDouble("TileMutators".Translate(), source.Select((TileMutatorDef m) => m.Label(selPlanetTile)).ToCommaList().CapitalizeFirst(), source.Select((TileMutatorDef m) => m.Label(selPlanetTile).Colorize(ColoredText.TipSectionTitleColor).CapitalizeFirst() + "\n" + m.Description(selPlanetTile)).ToStringList("\n\n"));
            }
            listing_Standard.GapLine();
            listing_Standard.LabelDouble("AvgTemp".Translate(), GetAverageTemperatureLabel(selPlanetTile));
            if (!isSpace)
            {
                string rightLabel = cachedGrowingQuadrumsDescription;
                if (cachedGrowingQuadrumsTile != selPlanetTile)
                {
                    rightLabel = (cachedGrowingQuadrumsDescription = Zone_Growing.GrowingQuadrumsDescription(selPlanetTile));
                    cachedGrowingQuadrumsTile = selPlanetTile;
                }
                listing_Standard.LabelDouble("OutdoorGrowingPeriod".Translate(), rightLabel);
                listing_Standard.LabelDouble("Rainfall".Translate(), selTile.rainfall.ToString("F0") + "mm");
            }
            if (isSurface)
            {
                if (isHaveBiome)
                {
                    if (selTile.PrimaryBiome.foragedFood != null && selTile.PrimaryBiome.forageability > 0f)
                    {
                        listing_Standard.LabelDouble("Forageability".Translate(), selTile.PrimaryBiome.forageability.ToStringPercent() + " (" + selTile.PrimaryBiome.foragedFood.label + ")");
                    }
                    else
                    {
                        listing_Standard.LabelDouble("Forageability".Translate(), "0%");
                    }
                }
                listing_Standard.LabelDouble("AnimalsCanGrazeNow".Translate(), VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(selPlanetTile) ? "Yes".Translate() : "No".Translate());
            }
            if (ModsConfig.BiotechActive && isSurface)
            {
                listing_Standard.GapLine();
                listing_Standard.LabelDouble("TilePollution".Translate(), Find.WorldGrid[selPlanetTile].pollution.ToStringPercent(), "TerrainPollutionTip".Translate());
                string text = "";
                foreach (IGrouping<float, CurvePoint> item in from p in WorldPollutionUtility.NearbyPollutionOverDistanceCurve
                                                              group p by p.y)
                {
                    if (!text.NullOrEmpty())
                    {
                        text += "\n";
                    }
                    if (item.Count() > 1)
                    {
                        CurvePoint curvePoint = item.MinBy((CurvePoint p) => p.x);
                        CurvePoint curvePoint2 = item.MaxBy((CurvePoint p) => p.x);
                        text += string.Format(" - {0}-{1} {2}, {3}x {4}", curvePoint.x, curvePoint2.x, "NearbyPollutionTilesAway".Translate(), item.Key, "PollutionLower".Translate());
                    }
                    else
                    {
                        text += string.Format(" - {0} {1}, {2}x {3}", item.First().x, "NearbyPollutionTilesAway".Translate(), item.Key, "PollutionLower".Translate());
                    }
                }
                TaggedString taggedString = "NearbyPollutionTip".Translate(4, text);
                float num = WorldPollutionUtility.CalculateNearbyPollutionScore(selPlanetTile);
                if (num >= GameConditionDefOf.NoxiousHaze.minNearbyPollution)
                {
                    float num2 = GameConditionDefOf.NoxiousHaze.mtbOverNearbyPollutionCurve.Evaluate(num);
                    taggedString += "\n\n" + "NoxiousHazeInterval".Translate(num2);
                }
                else
                {
                    taggedString += "\n\n" + "NoxiousHazeNeverOccurring".Translate();
                }
                listing_Standard.LabelDouble("TilePollutionNearby".Translate(), WorldPollutionUtility.CalculateNearbyPollutionScore(selPlanetTile).ToStringByStyle(ToStringStyle.FloatTwo), taggedString);
            }
            listing_Standard.GapLine();
            if (isHaveBiome)
            {
                listing_Standard.LabelDouble("AverageDiseaseFrequency".Translate(), string.Format("{0:F1} {1}", 60f / selTile.PrimaryBiome.diseaseMtbDays, "PerYear".Translate()));
            }
            listing_Standard.LabelDouble("TimeZone".Translate(), GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(selPlanetTile).x).ToStringWithSign());
            if (Prefs.DevMode)
            {
                listing_Standard.GapLine();
                listing_Standard.LabelDouble("PlanetSeed".Translate(), Find.World.info.seedString);
                listing_Standard.LabelDouble("PlanetCoverageShort".Translate(), Find.World.info.planetCoverage.ToStringPercent());
                if (worldObject != null)
                {
                    listing_Standard.LabelDouble("Debug world object def name", worldObject.def.defName);
                }
                LayeredAtmosphereOrbitDefModExtension LAOObjectDefModExtension = worldObject?.def.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                if (LAOObjectDefModExtension?.availableBiomes.NullOrEmpty() ?? true)
                {
                    listing_Standard.LabelDouble("Debug biome", $"{selTile.PrimaryBiome.LabelCap} [{selTile.PrimaryBiome.defName}]");
                }
                else
                {
                    if (listing_Standard.ButtonText($"Debug biome {selTile.PrimaryBiome.LabelCap} [{selTile.PrimaryBiome.defName}]"))
                    {
                        List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                        foreach (BiomeDef biomeDef in LAOObjectDefModExtension.availableBiomes)
                        {
                            floatMenuOptions.Add(new FloatMenuOption($"{biomeDef.LabelCap} [{biomeDef.defName}]", delegate
                            {
                                selTile.PrimaryBiome = biomeDef;
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                    }
                }
                listing_Standard.LabelDouble("Debug world tile ID", selPlanetTile.ToString());
            }
            lastDrawnHeight = rect.y + listing_Standard.CurHeight;
            listing_Standard.End();
            Widgets.EndScrollView();
        }

        public static string GetAverageTemperatureLabel(PlanetTile tile)
        {
            if (!tile.Valid)
            {
                return 21f.ToStringTemperature();
            }
            if (tile.Tile.PrimaryBiome?.constantOutdoorTemperature.HasValue ?? false)
            {
                return tile.Tile.PrimaryBiome?.constantOutdoorTemperature.Value.ToStringTemperature();
            }
            return Find.World.tileTemperatures.GetOutdoorTemp(tile).ToStringTemperature() + string.Format(" ({0} {1} {2})", GenTemperature.MinTemperatureAtTile(tile).ToStringTemperature("F0"), "RangeTo".Translate(), GenTemperature.MaxTemperatureAtTile(tile).ToStringTemperature("F0"));
        }
    }
}

