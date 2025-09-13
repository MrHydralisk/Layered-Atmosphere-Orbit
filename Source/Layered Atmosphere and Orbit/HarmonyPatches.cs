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
            val.Patch(AccessTools.FirstMethod(typeof(TileFinder), (MethodInfo mi) => mi.Name == "TryFindNewSiteTile" && mi.GetParameters().Count((ParameterInfo PI) => PI.ParameterType.Name.Contains(typeof(PlanetTile).Name)) > 1), transpiler: new HarmonyMethod(patchType, "TF_TryFindNewSiteTile_Transpiler"));
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
            //Log.Message($"slate {slate != null}");
            if (slate != null)
            {
                List<PlanetLayerGroupDef> planetLayerGroupDefs = slate.Get<List<PlanetLayerGroupDef>>("layerGroupWhitelist");
                PlanetLayerGroupDef planetLayerGroupDef = layer.Def.LayerGroup();
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                //Log.Message($"{laoDefModExtension?.isPreventQuestMapIfNotWhitelisted.ToString() ?? "---"} && !({planetLayerGroupDefs != null}|{planetLayerGroupDefs?.Contains(planetLayerGroupDef).ToString() ?? "---"})");
                if ((laoDefModExtension?.isPreventQuestMapIfNotWhitelisted ?? false) && !(planetLayerGroupDefs?.Contains(planetLayerGroupDef) ?? false))
                {
                    Find.WorldGrid.TryGetFirstLayerOfDef(PlanetLayerDefOf.Surface, out layer);
                    //Log.Message($"changed to {layer?.Def.defName ?? "---"}");
                }
            }
            return layer;
        }
    }
}
