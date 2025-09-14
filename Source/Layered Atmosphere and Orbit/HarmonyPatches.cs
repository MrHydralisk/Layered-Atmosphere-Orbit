using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Tilemaps;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.LayeredAtmosphereOrbit");

            LayeredAtmosphereOrbitUtility.ResetLayerData();
            InjectPlanetLayersDefs();
            InjectScenarios();
            InjectWorldObjectsDefs();
            InjectWhitelists();

            if (LAOMod.Settings.ReplaceAllViewLayerGizmo)
            {
                val.Patch(AccessTools.Method(typeof(WorldGrid).GetNestedTypes(AccessTools.all).First((Type t) => t.Name.Contains("<GetGizmos>d__103")), "MoveNext"), transpiler: new HarmonyMethod(patchType, "WG_GetGizmos_Transpiler"));
            }
            val.Patch(AccessTools.Method(typeof(WorldGrid), "GetGizmos"), postfix: new HarmonyMethod(patchType, "WG_GetGizmos_Postfix"));
            if (LAOMod.Settings.ShowLayerInGroup)
            {
                val.Patch(AccessTools.Method(typeof(ExpandableWorldObjectsUtility), "TransitionPct"), postfix: new HarmonyMethod(patchType, "EWOU_TransitionPct_Postfix"));
                val.Patch(AccessTools.Property(typeof(WorldObject), "VisibleInBackground").GetGetMethod(), postfix: new HarmonyMethod(patchType, "WO_VisibleInBackground_Postfix"));
            }
            if (LAOMod.Settings.AutoSwapLayerOnSelection)
            {
                val.Patch(AccessTools.Method(typeof(WorldSelector), "Select"), postfix: new HarmonyMethod(patchType, "WS_Select_Postfix"));
            }
            val.Patch(AccessTools.Method(typeof(TileTemperaturesComp.CachedTileTemperatureData), "CalculateOutdoorTemperatureAtTile"), postfix: new HarmonyMethod(patchType, "TTCCTT_CalculateOutdoorTemperatureAtTile_Postfix"));
            val.Patch(AccessTools.Method(typeof(GenTemperature), "GetTemperatureFromSeasonAtTile"), postfix: new HarmonyMethod(patchType, "GT_GetTemperatureFromSeasonAtTile_Postfix"));
            //val.Patch(AccessTools.FirstMethod(typeof(TileFinder), (MethodInfo mi) => mi.Name == "TryFindNewSiteTile" && mi.GetParameters().Count((ParameterInfo PI) => PI.ParameterType.Name.Contains(typeof(PlanetTile).Name)) > 1), transpiler: new HarmonyMethod(patchType, "TF_TryFindNewSiteTile_Transpiler"));
            val.Patch(AccessTools.FirstMethod(typeof(TileFinder), (MethodInfo mi) => mi.Name == "TryFindNewSiteTile" && mi.GetParameters().Count((ParameterInfo PI) => PI.ParameterType.Name.Contains(typeof(PlanetTile).Name)) > 1), prefix: new HarmonyMethod(patchType, "TF_TryFindNewSiteTile_Prefix"));
            val.Patch(AccessTools.Method(typeof(QuestNode_Root_Site), "TryGetLayer"), prefix: new HarmonyMethod(patchType, "QNRS_TryGetLayer_Prefix"));
        }

        public static void InjectPlanetLayersDefs()
        {
            List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
            foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            {
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                if (laoDefModExtension != null)
                {
                    if (laoDefModExtension.isOptionToAutoAdd)
                    {
                        LAOMod.AutoAddLayerOptions.Add(planetLayerDef);
                    }
                    if (laoDefModExtension.planetLayerGroup != null && !planetLayerDef.cachedTabs.Any((WITab wit) => wit is WITab_PlanetLayer))
                    {
                        planetLayerDef.cachedTabs.Add((WITab)Activator.CreateInstance(typeof(WITab_PlanetLayer)));
                    }
                }
            }
        }

        public static void InjectScenarios()
        {
            foreach (Scenario scenario in ScenarioLister.AllScenarios())
            {
                LayeredAtmosphereOrbitUtility.TryAddPlanetLayerts(scenario);
            }
        }

        public static void InjectWorldObjectsDefs()
        {
            List<GeneratedLocationDef> AllGeneratedLocationDefs = DefDatabase<GeneratedLocationDef>.AllDefs.ToList();
            foreach (GeneratedLocationDef generatedLocationDef in AllGeneratedLocationDefs)
            {
                if (generatedLocationDef.LayerDefs.Any((PlanetLayerDef pld) => pld.LayerGroup() != null) && !generatedLocationDef.worldObjectDef.inspectorTabsResolved.Any((InspectTabBase itb) => itb is WITab_PlanetLayer))
                {
                    generatedLocationDef.worldObjectDef.inspectorTabsResolved.Add((InspectTabBase)Activator.CreateInstance(typeof(WITab_PlanetLayer)));
                }

            }
        }

        public static void InjectWhitelists()
        {
            List<PlanetLayerDef> AlwaysArrivalFactionPlanetLayerDefs = new List<PlanetLayerDef>();
            List<PlanetLayerDef> AlwaysIncidentPlanetLayerDefs = new List<PlanetLayerDef>();
            List<PlanetLayerDef> AlwaysGameConditionPlanetLayerDefs = new List<PlanetLayerDef>();
            List<PlanetLayerDef> AlwaysQuestScriptPlanetLayerDefs = new List<PlanetLayerDef>();
            List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
            foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            {
                if (!planetLayerDef.onlyAllowWhitelistedArrivals)
                {
                    AlwaysArrivalFactionPlanetLayerDefs.Add(planetLayerDef);
                }
                if (!planetLayerDef.onlyAllowWhitelistedIncidents)
                {
                    AlwaysIncidentPlanetLayerDefs.Add(planetLayerDef);
                }
                if (!planetLayerDef.onlyAllowWhitelistedGameConditions)
                {
                    AlwaysGameConditionPlanetLayerDefs.Add(planetLayerDef);
                }
                if (!planetLayerDef.onlyAllowWhitelistedQuests)
                {
                    AlwaysQuestScriptPlanetLayerDefs.Add(planetLayerDef);
                }
            }
            foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            {
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                if (laoDefModExtension != null)
                {
                    if (!laoDefModExtension.WhitelistArrivalFactionDef.NullOrEmpty())
                    {
                        foreach (FactionDef factionDef in laoDefModExtension.WhitelistArrivalFactionDef)
                        {
                            if (factionDef.arrivalLayerWhitelist == null)
                            {
                                factionDef.arrivalLayerWhitelist = AlwaysArrivalFactionPlanetLayerDefs.ToList();
                            }
                            factionDef.arrivalLayerWhitelist.AddDistinct(planetLayerDef);
                        }
                    }
                    if (!laoDefModExtension.WhitelistFactionDef.NullOrEmpty())
                    {
                        foreach (FactionDef factionDef in laoDefModExtension.WhitelistFactionDef)
                        {
                            if (factionDef.layerWhitelist == null)
                            {
                                factionDef.layerWhitelist = new List<PlanetLayerDef>();
                            }
                            factionDef.layerWhitelist.AddDistinct(planetLayerDef);
                        }
                    }
                    if (!laoDefModExtension.WhitelistIncidentDef.NullOrEmpty())
                    {
                        foreach (IncidentDef incidentDef in laoDefModExtension.WhitelistIncidentDef)
                        {
                            if (incidentDef.layerWhitelist == null)
                            {
                                incidentDef.layerWhitelist = AlwaysIncidentPlanetLayerDefs.ToList();
                            }
                            incidentDef.layerWhitelist.AddDistinct(planetLayerDef);
                        }
                    }
                    if (!laoDefModExtension.WhitelistGameConditionDef.NullOrEmpty())
                    {
                        foreach (GameConditionDef gameConditionDef in laoDefModExtension.WhitelistGameConditionDef)
                        {
                            if (gameConditionDef.layerWhitelist == null)
                            {
                                gameConditionDef.layerWhitelist = AlwaysGameConditionPlanetLayerDefs.ToList();
                            }
                            gameConditionDef.layerWhitelist.AddDistinct(planetLayerDef);
                        }
                    }
                    if (!laoDefModExtension.WhitelistQuestScriptDef.NullOrEmpty())
                    {
                        foreach (QuestScriptDef questScriptDef in laoDefModExtension.WhitelistQuestScriptDef)
                        {
                            if (questScriptDef.layerWhitelist == null)
                            {
                                questScriptDef.layerWhitelist = AlwaysQuestScriptPlanetLayerDefs.ToList();
                            }
                            questScriptDef.layerWhitelist.AddDistinct(planetLayerDef);
                        }
                    }
                    if (!laoDefModExtension.BlacklistQuestScriptDef.NullOrEmpty())
                    {
                        foreach (QuestScriptDef questScriptDef in laoDefModExtension.BlacklistQuestScriptDef)
                        {
                            if (questScriptDef.layerBlacklist == null)
                            {
                                questScriptDef.layerBlacklist = AlwaysQuestScriptPlanetLayerDefs.ToList();
                            }
                            questScriptDef.layerBlacklist.AddDistinct(planetLayerDef);
                        }
                    }
                }
            }
            List<PlanetLayerGroupDef> AllPlanetLayerGroupDefs = DefDatabase<PlanetLayerGroupDef>.AllDefs.ToList();
            foreach (PlanetLayerGroupDef planetLayerGroupDef in AllPlanetLayerGroupDefs)
            {
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                if (laoDefModExtension != null)
                {
                    if (!laoDefModExtension.WhitelistArrivalFactionDef.NullOrEmpty())
                    {
                        foreach (FactionDef factionDef in laoDefModExtension.WhitelistArrivalFactionDef)
                        {
                            if (factionDef.arrivalLayerWhitelist == null)
                            {
                                factionDef.arrivalLayerWhitelist = AlwaysArrivalFactionPlanetLayerDefs.ToList();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                factionDef.arrivalLayerWhitelist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                    if (!laoDefModExtension.WhitelistFactionDef.NullOrEmpty())
                    {
                        foreach (FactionDef factionDef in laoDefModExtension.WhitelistFactionDef)
                        {
                            if (factionDef.layerWhitelist == null)
                            {
                                factionDef.layerWhitelist = new List<PlanetLayerDef>();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                factionDef.layerWhitelist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                    if (!laoDefModExtension.WhitelistIncidentDef.NullOrEmpty())
                    {
                        foreach (IncidentDef incidentDef in laoDefModExtension.WhitelistIncidentDef)
                        {
                            if (incidentDef.layerWhitelist == null)
                            {
                                incidentDef.layerWhitelist = AlwaysIncidentPlanetLayerDefs.ToList();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                incidentDef.layerWhitelist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                    if (!laoDefModExtension.WhitelistGameConditionDef.NullOrEmpty())
                    {
                        foreach (GameConditionDef gameConditionDef in laoDefModExtension.WhitelistGameConditionDef)
                        {
                            if (gameConditionDef.layerWhitelist == null)
                            {
                                gameConditionDef.layerWhitelist = AlwaysGameConditionPlanetLayerDefs.ToList();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                gameConditionDef.layerWhitelist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                    if (!laoDefModExtension.WhitelistQuestScriptDef.NullOrEmpty())
                    {
                        foreach (QuestScriptDef questScriptDef in laoDefModExtension.WhitelistQuestScriptDef)
                        {
                            if (questScriptDef.layerWhitelist == null)
                            {
                                questScriptDef.layerWhitelist = AlwaysQuestScriptPlanetLayerDefs.ToList();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                questScriptDef.layerWhitelist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                    if (!laoDefModExtension.BlacklistQuestScriptDef.NullOrEmpty())
                    {
                        foreach (QuestScriptDef questScriptDef in laoDefModExtension.BlacklistQuestScriptDef)
                        {
                            if (questScriptDef.layerBlacklist == null)
                            {
                                questScriptDef.layerBlacklist = AlwaysQuestScriptPlanetLayerDefs.ToList();
                            }
                            foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers())
                            {
                                questScriptDef.layerBlacklist.AddDistinct(planetLayerDef);
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> WG_GetGizmos_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (codes[i].operand?.ToString().Contains("get_IsSelected") ?? false))
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes[i + 1].opcode = OpCodes.Br;
                }
            }
            return codes.AsEnumerable();
        }

        public static void WG_GetGizmos_Postfix(ref IEnumerable<Gizmo> __result, WorldGrid __instance, Dictionary<int, PlanetLayer> ___planetLayers)
        {
            List<Gizmo> NGizmos = __result.ToList();
            if (Current.ProgramState != ProgramState.Entry && ___planetLayers.Count > 1)
            {
                PlanetLayer currentLayer = PlanetLayer.Selected;
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Label".Translate(),
                    defaultDesc = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Desc".Translate(currentLayer.Def.label, currentLayer.Def.viewGizmoTooltip),
                    icon = currentLayer.Def.ViewGizmoTexture,
                    action = delegate
                    {
                        List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();

                        for (int i = 0; i < ___planetLayers.Count; i++)
                        {
                            PlanetLayer planetLayer = ___planetLayers[i];
                            AcceptanceReport acceptanceReportPL = planetLayer.CanSelectLayer();
                            FloatMenuOption floatMenuOption = new FloatMenuOption("WorldSelectLayer".Translate(planetLayer.Def.Named("LAYER")), delegate
                            {
                                PlanetLayer.Selected = planetLayer;
                            }, planetLayer.Def.ViewGizmoTexture, Color.white, orderInPriority: (int)planetLayer.Def.Elevation());
                            if (!acceptanceReportPL.Accepted)
                            {
                                floatMenuOption.Disabled = true;
                                floatMenuOption.Label += $"[{acceptanceReportPL.Reason}]";
                            }
                            floatMenuOptions.Add(floatMenuOption);
                        }
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                    }
                };
                NGizmos.Insert(LAOMod.Settings.ReplaceAllViewLayerGizmo ? 0 : 1, command_Action);
            }
            __result = NGizmos;
        }

        public static void EWOU_TransitionPct_Postfix(ref float __result, WorldObject wo)
        {
            if ((__result == 1 || wo.def.fullyExpandedInSpace) && wo.Tile.Layer != Find.WorldSelector.SelectedLayer)
            {
                __result = wo.Tile.Layer.Def.VisibleInBackgroundOfCurrentLayer();
            }
        }

        public static void WO_VisibleInBackground_Postfix(ref bool __result, WorldObject __instance)
        {
            __result = __result || __instance.Tile.LayerDef.VisibleInBackgroundOfCurrentLayer() > 0;
        }

        public static void WS_Select_Postfix(WorldSelector __instance, WorldObject obj)
        {
            if (obj.Tile.Layer != __instance.SelectedLayer && __instance.NumSelectedObjects <= 1)
            {
                __instance.SelectedLayer = obj.Tile.Layer;
                if (__instance.SelectedTile == PlanetTile.Invalid)
                {
                    __instance.Select(obj);
                }
            }
        }

        public static void TTCCTT_CalculateOutdoorTemperatureAtTile_Postfix(ref float __result, TileTemperaturesComp.CachedTileTemperatureData __instance, int absTick, bool includeDailyVariations)
        {
            float tempOffest = __instance.tile.LayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.tempOffest ?? 0;
            if (tempOffest != 0)
            {
                __result += tempOffest;
            }
        }

        public static void GT_GetTemperatureFromSeasonAtTile_Postfix(ref float __result, int absTick, PlanetTile tile)
        {
            float tempOffest = tile.LayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.tempOffest ?? 0;
            if (tempOffest != 0)
            {
                __result += tempOffest;
            }
        }

        public static IEnumerable<CodeInstruction> TF_TryFindNewSiteTile_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_1)
                {
                    List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_S, 11));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "CheckSiteLayer")));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Starg_S, 11));
                    codes.InsertRange(i + 1, instructionsToInsert);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        public static PlanetLayer CheckSiteLayer(PlanetLayer layer)
        {
            Slate slate = QuestGen.slate;
            Log.Message($"slate {slate != null} {layer?.Def.defName ?? "---"}");
            if (slate != null)
            {
                List<PlanetLayerGroupDef> planetLayerGroupDefs = slate.Get<List<PlanetLayerGroupDef>>("layerGroupWhitelist");
                PlanetLayerGroupDef planetLayerGroupDef = layer.Def.LayerGroup();
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                Log.Message($"{laoDefModExtension?.isPreventQuestMapIfNotWhitelisted.ToString() ?? "---"} && {planetLayerGroupDef?.defName ?? "---"} !({planetLayerGroupDefs?.Contains(planetLayerGroupDef).ToString() ?? "---"})");
                if ((laoDefModExtension?.isPreventQuestMapIfNotWhitelisted ?? false) && !(planetLayerGroupDefs?.Contains(planetLayerGroupDef) ?? false))
                {
                    Find.WorldGrid.TryGetFirstLayerOfDef(PlanetLayerDefOf.Surface, out layer);
                    Log.Message($"changed to {layer?.Def.defName ?? "---"}");
                }
            }
            return layer;
        }

        public static bool TF_TryFindNewSiteTile_Prefix(ref bool __result, out PlanetTile tile, PlanetTile nearTile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, List<LandmarkDef> allowedLandmarks = null, float selectLandmarkChance = 0.5f, bool canSelectComboLandmarks = true, TileFinderMode tileFinderMode = TileFinderMode.Near, bool exitOnFirstTileFound = false, bool canBeSpace = false, PlanetLayer layer = null, Predicate<PlanetTile> validator = null)
        {
            bool flag = ModsConfig.OdysseyActive && Rand.ChanceSeeded(selectLandmarkChance, Gen.HashCombineInt(Find.TickManager.TicksGame, 18271));
            //Log.Message($"TF_TryFindNewSiteTile_Prefix 0| {flag} {nearTile.tileId}");
            if (!nearTile.Valid && !TileFinder.TryFindRandomPlayerTile(out nearTile, allowCaravans, null, canBeSpace: true))
            {
                tile = PlanetTile.Invalid;
                //Log.Message($"TF_TryFindNewSiteTile_Prefix 1| {tile.tileId}");
                __result = false;
                return false;
            }
            if (layer == null)
            {
                layer = nearTile.Layer;
                //Log.Message($"TF_TryFindNewSiteTile_Prefix 2| {layer?.Def.defName ?? "---"}");
            }
            if (!canBeSpace)
            {
                bool isRequireChangeToSurface = layer.Def.isSpace;
                //Log.Message($"TF_TryFindNewSiteTile_Prefix 3| {QuestGen.slate != null} {layer?.Def.defName ?? "---"}");
                if (!isRequireChangeToSurface && QuestGen.slate != null)
                {
                    PlanetLayerGroupDef planetLayerGroupDef = layer.Def.LayerGroup();
                    LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                    //Log.Message($"TF_TryFindNewSiteTile_Prefix 4| {laoDefModExtension?.isPreventQuestMapIfNotWhitelisted.ToString() ?? "---"}");
                    if ((laoDefModExtension?.isPreventQuestMapIfNotWhitelisted ?? false))
                    {
                        isRequireChangeToSurface = true;
                        List<PlanetLayerGroupDef> planetLayerGroupDefs = QuestGen.slate.Get<List<PlanetLayerGroupDef>>("layerGroupWhitelist");
                        //Log.Message($"TF_TryFindNewSiteTile_Prefix 5| {planetLayerGroupDef?.defName ?? "---"} !({planetLayerGroupDefs?.Contains(planetLayerGroupDef).ToString() ?? "---"})");
                        if ((planetLayerGroupDefs?.Contains(planetLayerGroupDef) ?? false))
                        {
                            isRequireChangeToSurface = false;
                            List<PlanetLayer> planetLayerOptions = new List<PlanetLayer>();
                            foreach (PlanetLayer planetLayer in Find.WorldGrid.PlanetLayers.Values)
                            {
                                if (planetLayerGroupDefs.Any((PlanetLayerGroupDef plgd) => plgd.ContainsLayer(planetLayer.Def)))
                                {
                                    planetLayerOptions.Add(planetLayer);
                                }
                            }
                            layer = planetLayerOptions.RandomElement();
                        }
                    }
                }
                if (isRequireChangeToSurface)
                {
                    //Log.Message($"TF_TryFindNewSiteTile_Prefix 6| {layer?.Def.defName ?? "---"}");
                    if (!Find.WorldGrid.TryGetFirstAdjacentLayerOfDef(nearTile, PlanetLayerDefOf.Surface, out layer))
                    {
                        //Log.Message($"TF_TryFindNewSiteTile_Prefix 7| {layer?.Def.defName ?? "---"}");
                        Find.WorldGrid.TryGetFirstLayerOfDef(DefOfLocal.LAO_Surface.ContainedLayers().RandomElement(), out layer);
                        //Log.Message($"TF_TryFindNewSiteTile_Prefix 8| {layer?.Def.defName ?? "---"}");
                    }
                }
            }
            //Log.Message($"TF_TryFindNewSiteTile_Prefix 9| {layer?.Def.defName ?? "---"}");
            FastTileFinder.LandmarkMode landmarkMode = (flag ? FastTileFinder.LandmarkMode.Required : FastTileFinder.LandmarkMode.Any);
            FastTileFinder.TileQueryParams query = new FastTileFinder.TileQueryParams(nearTile, minDist, maxDist, landmarkMode, reachable: true, Hilliness.Undefined, Hilliness.Undefined, checkBiome: true, validSettlement: true, canSelectComboLandmarks);
            List<PlanetTile> list = layer.FastTileFinder.Query(query, null, allowedLandmarks);
            if (validator != null)
            {
                for (int num2 = list.Count - 1; num2 >= 0; num2--)
                {
                    if (!validator(list[num2]))
                    {
                        list.RemoveAt(num2);
                    }
                }
            }
            //Log.Message($"TF_TryFindNewSiteTile_Prefix 10| {layer?.Def.defName ?? "---"}");
            if (list.Empty())
            {
                if (TileFinder.TryFillFindTile(layer.GetClosestTile_NewTemp(nearTile), out tile, minDist, maxDist, allowedLandmarks, canSelectComboLandmarks, tileFinderMode, exitOnFirstTileFound, validator, flag))
                {
                    //Log.Message($"TF_TryFindNewSiteTile_Prefix 11| {tile.tileId} {layer?.Def.defName ?? "---"}");
                    __result = true;
                    return false;
                }
                tile = PlanetTile.Invalid;
                return false;
            }
            tile = list.RandomElement();
            //Log.Message($"TF_TryFindNewSiteTile_Prefix 12| {tile.tileId} {layer?.Def.defName ?? "---"}");
            __result = true;
            return false;
        }

        public static bool QNRS_TryGetLayer_Prefix(ref bool __result, QuestNode_Root_Site __instance, Slate slate, out PlanetTile source, out PlanetLayer layer)
        {
            layer = null;
            Map map = QuestGen.slate.Get<Map>("map");
            if (map != null && map.Tile.Valid)
            {
                source = map.Tile;
            }
            else if (!TileFinder.TryFindRandomPlayerTile(out source, allowCaravans: false, null, canBeSpace: true))
            {
                source = Find.WorldGrid.Surface.Tiles.RandomElement().tile;
            }
            if (Validator(source, source.Layer))
            {
                layer = source.Layer;
            }
            else
            {
                foreach (var (_, planetLayer2) in Find.WorldGrid.PlanetLayers.InRandomOrder())
                {
                    if (planetLayer2 != source.Layer && Validator(source, planetLayer2))
                    {
                        layer = planetLayer2;
                        break;
                    }
                }
            }
            __result = layer != null;
            Log.Message($"QNRS_TryGetLayer_Prefix 0| {layer?.Def.defName ?? "---"}");
            return false;
            bool Validator(PlanetTile origin, PlanetLayer layerB)
            {
                if (!__instance.canBeSpace.GetValue(slate) && layerB.Def.isSpace)
                {
                    return false;
                }
                List<PlanetLayerDef> value = __instance.layerWhitelist.GetValue(slate);
                List<PlanetLayerDef> value2 = __instance.layerBlacklist.GetValue(slate);
                if (!value.NullOrEmpty() && !value.Contains(layerB.Def))
                {
                    return false;
                }
                if (!value2.NullOrEmpty() && value2.Contains(layerB.Def))
                {
                    return false;
                }
                if (__instance.requireSameOrAdjacentLayer.GetValue(slate) && origin.Valid && origin.Layer != layerB && !layerB.DirectConnectionTo(origin.Layer))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
