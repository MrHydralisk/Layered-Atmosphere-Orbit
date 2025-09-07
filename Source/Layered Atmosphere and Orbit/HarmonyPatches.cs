using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using UnityEngine;
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
            //val.Patch(AccessTools.Method(typeof(QuestNode_GetMap), "IsAcceptableMap"), postfix: new HarmonyMethod(patchType, "QNGM_IsAcceptableMap_Postfix"));
            val.Patch(AccessTools.Method(typeof(QuestNode_GetSiteTile), "TryFindTile"), transpiler: new HarmonyMethod(patchType, "QNGST_TryFindTile_Transpiler"));
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

        public static void QNGM_IsAcceptableMap_Postfix(ref bool __result, QuestNode_GetMap __instance, Map map, Slate slate)
        {
            Log.Message($"{__result} && {(map?.Tile.LayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.isPreventQuestIfNotWhitelisted ?? false)} | {map?.Tile.LayerDef.ToString() ?? "---"}");
            if (__result && (map?.Tile.LayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.isPreventQuestIfNotWhitelisted ?? false))
            {                
                List<PlanetLayerDef> value = __instance.layerWhitelist.GetValue(slate);
                Log.Message($"{value == null} {value.NullOrEmpty()} || !{value?.Contains(map.Tile.LayerDef).ToString() ?? "---"}");
                if (value.NullOrEmpty() || !value.Contains(map.Tile.LayerDef))
                {
                    __result = false;
                    return;
                }
            }
        }

        public static IEnumerable<CodeInstruction> QNGST_TryFindTile_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && (codes[i].operand?.ToString().Contains("TryFindNewSiteTile") ?? false))
                {

                    Label labelSkipIn = il.DefineLabel();
                    Label labelSkipOut = il.DefineLabel();
                    codes[codes.Count - 2].labels.Add(labelSkipIn);
                    codes[codes.Count - 1].labels.Add(labelSkipOut);
                    List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloc_1));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "IsLAOLayer")));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Brtrue, labelSkipIn));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "TryFindNewSiteTile")));
                    instructionsToInsert.Add(new CodeInstruction(OpCodes.Br, labelSkipOut));
                    codes.InsertRange(codes.Count - 2, instructionsToInsert);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        public static bool IsLAOLayer(PlanetTile nearTile)
        {
            if (nearTile.LayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.isPreventQuestIfNotWhitelisted ?? false)
            {
                return false;
            }
            return true;
        }

        public static bool TryFindNewSiteTile(out PlanetTile tile, PlanetTile nearTile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, List<LandmarkDef> allowedLandmarks = null, float selectLandmarkChance = 0.5f, bool canSelectComboLandmarks = true, TileFinderMode tileFinderMode = TileFinderMode.Near, bool exitOnFirstTileFound = false, bool canBeSpace = false, PlanetLayer layer = null, Predicate<PlanetTile> validator = null, Slate slate = null)
        {
            List<PlanetLayerDef> value = slate?.Get<List<PlanetLayerDef>>("layerWhitelist");
            if (value.NullOrEmpty() || !value.Contains(nearTile.LayerDef))
            {
                if (!Find.WorldGrid.TryGetFirstLayerOfDef(PlanetLayerDefOf.Surface, out layer))
                {
                    tile = PlanetTile.Invalid;
                    return false;
                }
            }
            return TileFinder.TryFindNewSiteTile(out tile, nearTile, minDist, maxDist, allowCaravans, allowedLandmarks, selectLandmarkChance, canSelectComboLandmarks, tileFinderMode, exitOnFirstTileFound: exitOnFirstTileFound, canBeSpace, layer);
        }
    }
}
