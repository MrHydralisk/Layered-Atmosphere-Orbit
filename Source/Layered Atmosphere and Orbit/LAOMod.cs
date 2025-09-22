using RimWorld;
using RimWorld.Planet;
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

        private float prevHeight = float.MaxValue;
        private Vector2 scrollPos;

        private string inputKmPerFuelSpace;

        private string worldObjectDefName = "LAO_FloatingIslandDebug";
        WorldObjectDef worldObjectDef;
        private string inputDebugFloatingIslandRotation;
        private string inputDebugFloatingIslandPerlinSeedA;
        private string inputDebugFloatingIslandPerlinSeedB;
        private string inputDebugFloatingIslandRadius;
        private string inputDebugFloatingIsland1Scale;
        private string inputDebugFloatingIsland4Perlin;
        private string inputDebugFloatingIsland4PerlinA;
        private string inputDebugFloatingIsland4PerlinB;
        private string inputDebugFloatingIsland4PerlinC;
        private string inputDebugFloatingIsland4Const;
        private string inputDebugFloatingIsland5Perlin;
        private string inputDebugFloatingIsland5PerlinA;
        private string inputDebugFloatingIsland5PerlinB;
        private string inputDebugFloatingIsland5PerlinC;
        private string inputDebugFloatingIsland5Const;
        private string inputDebugFloatingIslandConst;

        private string inputDebugFloatingIslandwidthOffsetPerCell;
        private string inputDebugFloatingIslandmaxOpenTunnelsPerRockGroup;
        private string inputDebugFloatingIslandmaxClosedTunnelsPerRockGroup;
        private string inputDebugFloatingIslandminTunnelWidth;
        private string inputDebugFloatingIslandopenTunnelsPer10k;

        public LAOMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LAOSettings>();
            worldObjectDefName = "LAO_FloatingIslandDebug";
            worldObjectDef = DefDatabase<WorldObjectDef>.GetNamed(worldObjectDefName, false);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Rect rect = new Rect(0f, 0f, inRect.width - 20f, prevHeight);
            Widgets.BeginScrollView(inRect, ref scrollPos, rect);
            Listing_Standard options = new Listing_Standard();
            options.Begin(rect);
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.UseFuelCostBetweenLayers".Translate().RawText, ref Settings.UseFuelCostBetweenLayers);
            options.Label("LayeredAtmosphereOrbit.Settings.FuelPerKm".Translate(Settings.FuelPerKm));
            Settings.FuelPerKm = Mathf.Round(options.Slider(Settings.FuelPerKm, 0f, 5f) * 100f) / 100f;
            options.Label("LayeredAtmosphereOrbit.Settings.KmPerFuelSpace".Translate(Settings.KmPerFuelSpace));
            options.TextFieldNumeric(ref Settings.KmPerFuelSpace, ref inputKmPerFuelSpace, -1, 100000);
            options.GapLine();
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.GravshipRoute".Translate().RawText, ref Settings.GravshipRoute);
            options.GapLine();
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.ShowLayerInGroup".Translate().RawText, ref Settings.ShowLayerInGroup);
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.AutoSwapLayerOnSelection".Translate().RawText, ref Settings.AutoSwapLayerOnSelection);
            options.Label("LayeredAtmosphereOrbit.Settings.TransparentInGroup".Translate(Settings.TransparentInGroup.ToStringPercent()));
            Settings.TransparentInGroup = Mathf.Round(options.Slider(Settings.TransparentInGroup, 0f, 1f) * 100f) / 100f;
            options.Label("LayeredAtmosphereOrbit.Settings.TransparentInGroupSub".Translate(Settings.TransparentInGroupSub.ToStringPercent()));
            Settings.TransparentInGroupSub = Mathf.Round(options.Slider(Settings.TransparentInGroupSub, 0f, 1f) * 100f) / 100f;
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.ReplaceAllViewLayerGizmo".Translate().RawText, ref Settings.ReplaceAllViewLayerGizmo);
            options.GapLine();
            options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.PlanetPatches".Translate().RawText, ref Settings.PlanetPatches);
            //options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.HideOtherPlanets".Translate().RawText, ref Settings.HideOtherPlanets);
            options.GapLine();
            options.Label("LayeredAtmosphereOrbit.Settings.AutoAddLayersDefName.Total".Translate());
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
            //debug
            if (Prefs.DevMode)
            {
                //options.GapLine();
                //PlanetLayer planetLayer = Find.WorldSelector?.SelectedLayer;
                //if (planetLayer != null)
                //{
                //    if (options.ButtonText($"Remove current {planetLayer.Def.defName}"))
                //    {
                //        Find.WorldSelector.SelectedLayer = Find.WorldGrid.PlanetLayers.Values.FirstOrDefault(pl => pl != planetLayer);
                //        List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjectsOnLayer(planetLayer);
                //        for (int i = worldObjects.Count - 1; i > -1; i--)
                //        {
                //            worldObjects[i].Destroy();
                //        }
                //        Find.WorldGrid.RemovePlanetLayer(planetLayer);
                //    }
                //}
                options.GapLine();
                if (options.ButtonText("IncidentDef Per PlanetLayer"))
                {
                    List<IncidentDef> AllIncidentDefs = DefDatabase<IncidentDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available BiomeDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName} {pl.Def.onlyAllowWhitelistedIncidents}\n{string.Join("\n", AllIncidentDefs.Select((id) => $"      {id.defName} {pl.Def.TestIncidentDefOnLayerDef(id)} {id.canOccurOnAllPlanetLayers}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available BiomeDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName} {pl.onlyAllowWhitelistedIncidents}\n{string.Join("\n", AllIncidentDefs.Select((id) => $"      {id.defName} {pl.TestIncidentDefOnLayerDef(id)} {id.canOccurOnAllPlanetLayers}"))}"))}");
                    }
                }
                if (options.ButtonText("BiomeDef Per PlanetLayer"))
                {
                    List<BiomeDef> AllBiomeDefs = DefDatabase<BiomeDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available BiomeDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName} {pl.Def.onlyAllowWhitelistedBiomes}\n{string.Join("\n", AllBiomeDefs.Select((bd) => $"      {bd.defName} {pl.Def.TestBiomeDefOnLayerDef(bd)} {pl.Def.onlyAllowWhitelistedBiomes}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available BiomeDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName} {pl.onlyAllowWhitelistedBiomes}\n{string.Join("\n", AllBiomeDefs.Select((bd) => $"      {bd.defName} {pl.TestBiomeDefOnLayerDef(bd)} {pl.onlyAllowWhitelistedBiomes}"))}"))}");
                    }
                }
                if (options.ButtonText("GameConditionDef Per PlanetLayer"))
                {
                    List<GameConditionDef> AllGameConditionDefs = DefDatabase<GameConditionDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available GameConditionDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName} {pl.Def.onlyAllowWhitelistedGameConditions}\n{string.Join("\n", AllGameConditionDefs.Select((gcd) => $"      {gcd.defName} {pl.Def.TestGameConditionDefOnLayerDef(gcd)} {gcd.canAffectAllPlanetLayers}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available GameConditionDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName} {pl.onlyAllowWhitelistedGameConditions}\n{string.Join("\n", AllGameConditionDefs.Select((gcd) => $"      {gcd.defName} {pl.TestGameConditionDefOnLayerDef(gcd)} {gcd.canAffectAllPlanetLayers}"))}"))}");
                    }
                }
                if (options.ButtonText("QuestScriptDef Per PlanetLayer"))
                {
                    List<QuestScriptDef> AllQuestScriptDefs = DefDatabase<QuestScriptDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available QuestScriptDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName} {pl.Def.onlyAllowWhitelistedQuests}\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"      {qsd.defName} {pl.Def.TestQuestScriptDefOnLayerDef(qsd)} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available QuestScriptDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName} {pl.onlyAllowWhitelistedQuests}\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"      {qsd.defName} {pl.TestQuestScriptDefOnLayerDef(qsd)} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}"))}"))}");
                    }
                }
                if (options.ButtonText("FactionDefs arrival Per PlanetLayer"))
                {
                    List<FactionDef> AllFactionDefs = DefDatabase<FactionDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available arrival FactionDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName}\n{string.Join("\n", AllFactionDefs.Select((fd) => $"      {fd.defName} {pl.Def.TestArrivalFactionDefOnLayerDef(fd)} {fd.techLevel}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available arrival FactionDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", AllFactionDefs.Select((fd) => $"      {fd.defName} {pl.TestArrivalFactionDefOnLayerDef(fd)} {fd.techLevel}"))}"))}");
                    }
                }
                if (options.ButtonText("FactionDefs Per PlanetLayer"))
                {
                    List<FactionDef> AllFactionDefs = DefDatabase<FactionDef>.AllDefs.ToList();
                    if (Find.WorldGrid != null)
                    {
                        List<PlanetLayer> AllPlanetLayers = Find.WorldGrid.PlanetLayers.Values.ToList();
                        Log.Message($"Available FactionDef:\n{string.Join("\n", AllPlanetLayers.Select((pl) => $"   {pl.Def.defName}\n{string.Join("\n", AllFactionDefs.Select((fd) => $"      {fd.defName} {FactionGenerator.CanExistOnLayer(pl, fd)} {fd.techLevel}"))}"))}");
                    }
                    else
                    {
                        List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
                        Log.Message($"Available FactionDef:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", AllFactionDefs.Select((fd) => $"      {fd.defName} {pl.TestFactionDefOnLayerDef(fd)} {fd.techLevel}"))}"))}");
                    }
                }
                options.GapLine();
                options.CheckboxLabeled("LayeredAtmosphereOrbit.Settings.isOpenDebugFloatingIslandMapGen".Translate().RawText, ref Settings.isOpenDebugFloatingIslandMapGen);
                if (Settings.isOpenDebugFloatingIslandMapGen)
                {
                    options.Label($"DebugFloatingIslandRotation {Settings.DebugFloatingIslandRotation}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandRotation, ref inputDebugFloatingIslandRotation, 0, 360f);
                    options.Label($"DebugFloatingIslandPerlinSeedA {Settings.DebugFloatingIslandPerlinSeedA}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandPerlinSeedA, ref inputDebugFloatingIslandPerlinSeedA);
                    options.Label($"DebugFloatingIslandPerlinSeedB {Settings.DebugFloatingIslandPerlinSeedB}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandPerlinSeedB, ref inputDebugFloatingIslandPerlinSeedB);
                    options.Label($"DebugFloatingIslandRadius {Settings.DebugFloatingIslandRadius}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandRadius, ref inputDebugFloatingIslandRadius, 0.01f, 1);
                    options.CheckboxLabeled($"is1Scale {Settings.is1Scale}", ref Settings.is1Scale);
                    options.Label($"DebugFloatingIsland1Scale {Settings.DebugFloatingIsland1Scale}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland1Scale, ref inputDebugFloatingIsland1Scale, 0.01f, 1);
                    options.CheckboxLabeled($"is2Rotate {Settings.is2Rotate}", ref Settings.is2Rotate);
                    options.CheckboxLabeled($"is3Translate {Settings.is3Translate}", ref Settings.is3Translate);
                    options.CheckboxLabeled($"is4Blend {Settings.is4Blend}", ref Settings.is4Blend);
                    options.Label($"DebugFloatingIsland4Perlin {Settings.DebugFloatingIsland4Perlin}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland4Perlin, ref inputDebugFloatingIsland4Perlin, 0.01f, 1);
                    options.Label($"DebugFloatingIsland4PerlinA {Settings.DebugFloatingIsland4PerlinA}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland4PerlinA, ref inputDebugFloatingIsland4PerlinA, 0, 10);
                    options.Label($"DebugFloatingIsland4PerlinB {Settings.DebugFloatingIsland4PerlinB}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland4PerlinB, ref inputDebugFloatingIsland4PerlinB, 0, 10);
                    options.Label($"DebugFloatingIsland4PerlinC {Settings.DebugFloatingIsland4PerlinC}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland4PerlinC, ref inputDebugFloatingIsland4PerlinC, 0, 10);
                    options.Label($"DebugFloatingIsland4Const {Settings.DebugFloatingIsland4Const}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland4Const, ref inputDebugFloatingIsland4Const, 0.01f, 1);
                    options.CheckboxLabeled($"is5Blend {Settings.is5Blend}", ref Settings.is5Blend);
                    options.Label($"DebugFloatingIsland5Perlin {Settings.DebugFloatingIsland5Perlin}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland5Perlin, ref inputDebugFloatingIsland5Perlin, 0.01f, 1);
                    options.Label($"DebugFloatingIsland5PerlinA {Settings.DebugFloatingIsland5PerlinA}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland5PerlinA, ref inputDebugFloatingIsland5PerlinA, 0, 10);
                    options.Label($"DebugFloatingIsland5PerlinB {Settings.DebugFloatingIsland5PerlinB}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland5PerlinB, ref inputDebugFloatingIsland5PerlinB, 0, 10);
                    options.Label($"DebugFloatingIsland5PerlinC {Settings.DebugFloatingIsland5PerlinC}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland5PerlinC, ref inputDebugFloatingIsland5PerlinC, 0, 10);
                    options.Label($"DebugFloatingIsland5Const {Settings.DebugFloatingIsland5Const}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIsland5Const, ref inputDebugFloatingIsland5Const, 0.01f, 1);
                    options.CheckboxLabeled($"is6Power {Settings.is6Power}", ref Settings.is6Power);
                    options.Label($"DebugFloatingIslandConst {Settings.DebugFloatingIslandConst}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandConst, ref inputDebugFloatingIslandConst, 0.01f, 1);
                    options.Label($"DebugFloatingIslandFloorThreshold {Settings.DebugFloatingIslandFloorThreshold}");
                    Settings.DebugFloatingIslandFloorThreshold = Mathf.Round(options.Slider(Settings.DebugFloatingIslandFloorThreshold, 0, 1) * 100f) / 100f;
                    options.Label($"DebugFloatingIslandWallThreshold {Settings.DebugFloatingIslandWallThreshold}");
                    Settings.DebugFloatingIslandWallThreshold = Mathf.Round(options.Slider(Settings.DebugFloatingIslandWallThreshold, 0, 1) * 100f) / 100f;


                    options.Label($"DebugFloatingIslandwidthOffsetPerCell {Settings.DebugFloatingIslandwidthOffsetPerCell}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandwidthOffsetPerCell, ref inputDebugFloatingIslandwidthOffsetPerCell, 0.001f, 1);
                    options.Label($"DebugFloatingIslandmaxOpenTunnelsPerRockGroup {Settings.DebugFloatingIslandmaxOpenTunnelsPerRockGroup}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandmaxOpenTunnelsPerRockGroup, ref inputDebugFloatingIslandmaxOpenTunnelsPerRockGroup, 0, 50);
                    options.Label($"DebugFloatingIslandmaxClosedTunnelsPerRockGroup {Settings.DebugFloatingIslandmaxClosedTunnelsPerRockGroup}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandmaxClosedTunnelsPerRockGroup, ref inputDebugFloatingIslandmaxClosedTunnelsPerRockGroup, 0, 50);
                    options.Label($"DebugFloatingIslandminTunnelWidth {Settings.DebugFloatingIslandminTunnelWidth}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandminTunnelWidth, ref inputDebugFloatingIslandminTunnelWidth, 0.01f, 5);
                    options.Label($"DebugFloatingIslandbranchChance {Settings.DebugFloatingIslandbranchChance}");
                    Settings.DebugFloatingIslandbranchChance = Mathf.Round(options.Slider(Settings.DebugFloatingIslandbranchChance, 0, 1) * 100f) / 100f;
                    options.Label($"DebugFloatingIslandopenTunnelsPer10k {Settings.DebugFloatingIslandopenTunnelsPer10k}");
                    options.TextFieldNumeric(ref Settings.DebugFloatingIslandopenTunnelsPer10k, ref inputDebugFloatingIslandopenTunnelsPer10k, 0.01f, 12);

                    string defName = options.TextEntryLabeled($"worldObjectDefName{(worldObjectDef == null ? " failed" : "")}", worldObjectDefName);
                    if (defName != worldObjectDefName)
                    {
                        worldObjectDefName = defName;
                        worldObjectDef = DefDatabase<WorldObjectDef>.GetNamed(worldObjectDefName, false);
                        Log.Message($"loaded {worldObjectDef?.label ?? "---"}");
                    }
                    if (options.ButtonText("GenMap") && worldObjectDef != null)
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            MapParent mapParent2 = (MapParent)WorldObjectMaker.MakeWorldObject(worldObjectDef);
                            mapParent2.Tile = TileFinder.RandomStartingTile();
                            mapParent2.SetFaction(Faction.OfPlayer);
                            Find.WorldObjects.Add(mapParent2);
                            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(mapParent2.Tile, Find.World.info.initialMapSize, null);
                            Current.Game.CurrentMap = orGenerateMap;
                            CameraJumper.TryJump(orGenerateMap.Center, orGenerateMap);
                        }, "GeneratingMap", doAsynchronously: true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap, showExtraUIInfo: true, forceHideUI: false, delegate
                        {
                            MapParent mapParent = Find.WorldObjects.MapParentAt(Find.WorldSelector.SelectedTile);
                            if (mapParent != null)
                            {
                                Current.Game.CurrentMap = mapParent.Map;
                                CameraJumper.TryJump(mapParent.Map.Center, mapParent.Map);
                            }
                        });
                    }
                }
            }
            prevHeight = options.CurHeight;
            options.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
        {
            return "LayeredAtmosphereOrbit.Settings.Title".Translate().RawText;
        }
    }
}
