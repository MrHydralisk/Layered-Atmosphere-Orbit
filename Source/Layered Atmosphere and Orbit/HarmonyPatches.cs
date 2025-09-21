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
using Verse;

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
            val.Patch(AccessTools.Method(typeof(StorytellerComp), "IncidentChanceFinal"), transpiler: new HarmonyMethod(patchType, "SC_IncidentChanceFinal_Transpiler"));
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
            val.Patch(AccessTools.FirstMethod(typeof(TileFinder), (MethodInfo mi) => mi.Name == "TryFindNewSiteTile" && mi.GetParameters().Count((ParameterInfo PI) => PI.ParameterType.Name.Contains(typeof(PlanetTile).Name)) > 1), prefix: new HarmonyMethod(patchType, "TF_TryFindNewSiteTile_Prefix"));
            val.Patch(AccessTools.Method(typeof(PlanetLayer), "DirectConnectionTo"), prefix: new HarmonyMethod(patchType, "PL_DirectConnectionTo_Prefix"));
            if (LAOMod.Settings.PlanetPatches)
            {
                if (LAOMod.Settings.HideOtherPlanets)
                {
                    val.Patch(AccessTools.Property(typeof(PlanetLayer), "Visible").GetGetMethod(), prefix: new HarmonyMethod(patchType, "PL_Visible_Prefix"));
                    val.Patch(AccessTools.Property(typeof(PlanetLayer), "Raycastable").GetGetMethod(), postfix: new HarmonyMethod(patchType, "PL_Raycastable_Postfix"));
                    val.Patch(AccessTools.Property(typeof(WorldDrawLayer), "Visible").GetGetMethod(), postfix: new HarmonyMethod(patchType, "WDL_Visible_Postfix"));
                    val.Patch(AccessTools.Property(typeof(WorldDrawLayer), "Raycastable").GetGetMethod(), postfix: new HarmonyMethod(patchType, "WDL_Raycastable_Postfix"));
                }
                val.Patch(AccessTools.Property(typeof(WorldSelector), "SelectedLayer").GetSetMethod(), postfix: new HarmonyMethod(patchType, "WS_SelectedLayer_Postfix"));
                val.Patch(AccessTools.Method(typeof(Map), "FinalizeInit"), postfix: new HarmonyMethod(patchType, "M_FinalizeInit_Postfix"));
                val.Patch(AccessTools.Method(typeof(CameraJumper), "TryHideWorld"), postfix: new HarmonyMethod(patchType, "CJ_TryHideWorld_Postfix"));
                val.Patch(AccessTools.Method(typeof(Page_SelectStartingSite), "PreOpen"), postfix: new HarmonyMethod(patchType, "PSSS_PreOpen_Postfix"));
            }
            if (LAOMod.Settings.GravshipRoute)
            {
                val.Patch(AccessTools.Method(typeof(GravshipUtility), "TravelTo"), prefix: new HarmonyMethod(patchType, "GU_TravelTo_Prefix"));
                val.Patch(AccessTools.Property(typeof(Gravship), "DrawPos").GetGetMethod(true), prefix: new HarmonyMethod(patchType, "G_DrawPos_Prefix"));
                val.Patch(AccessTools.Method(typeof(GravshipUtility), "ArriveExistingMap"), postfix: new HarmonyMethod(patchType, "GU_ArriveExistingMap_Postfix"));
                val.Patch(AccessTools.Method(typeof(GravshipUtility), "ArriveNewMap"), postfix: new HarmonyMethod(patchType, "GU_ArriveExistingMap_Postfix"));
                val.Patch(AccessTools.Property(typeof(Gravship), "TraveledPctStepPerTick").GetGetMethod(true), prefix: new HarmonyMethod(patchType, "G_TraveledPctStepPerTick_Prefix"));
            }
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
            List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
            List<IncidentDef> AllIncidentDefs = DefDatabase<IncidentDef>.AllDefs.ToList();
            List<BiomeDef> AllBiomeDefs = DefDatabase<BiomeDef>.AllDefs.ToList();
            List<GameConditionDef> AllGameConditionDefs = DefDatabase<GameConditionDef>.AllDefs.ToList();
            List<QuestScriptDef> AllQuestScriptDefs = DefDatabase<QuestScriptDef>.AllDefs.ToList();
            List<FactionDef> AllFactionDefs = DefDatabase<FactionDef>.AllDefs.ToList();

            Dictionary<PlanetLayerDef, (LayeredAtmosphereOrbitDefModExtension, LayeredAtmosphereOrbitDefModExtension)> PlanetLayerDefMods = AllPlanetLayerDefs.ToDictionary((PlanetLayerDef pld) => pld, (PlanetLayerDef pld) => (pld.GetModExtension<LayeredAtmosphereOrbitDefModExtension>(), pld.LayerGroup()?.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()));

            Log.Message($"Allow QuestScriptDef A:\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"   {qsd.defName} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}\n{string.Join("\n", qsd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", qsd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestQuestScriptDefOnLayerDef(qsd)}"))}"))}");
            foreach (QuestScriptDef questScriptDef in AllQuestScriptDefs)
            {
                List<PlanetLayerDef> AllowInQuestScriptDef = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInQuestScriptDef = new List<PlanetLayerDef>();
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    if (questScriptDef.layerWhitelist.NullOrEmpty())
                    {
                        if (planetLayerDef.onlyAllowWhitelistedQuests)
                        {
                            ForbidInQuestScriptDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            AllowInQuestScriptDef.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (questScriptDef.layerWhitelist.Contains(planetLayerDef))
                        {
                            AllowInQuestScriptDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            ForbidInQuestScriptDef.AddUnique(planetLayerDef);
                        }
                    }
                }
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInQuestScriptDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInQuestScriptDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (questScriptDef.layerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInQuestScriptDef.AddRangeUnique(questScriptDef.layerBlacklist);
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInQuestScriptDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInQuestScriptDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistQuestScriptDef?.Contains(questScriptDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistQuestScriptDef?.Contains(questScriptDef) ?? false;
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistQuestScriptDef?.Contains(questScriptDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistQuestScriptDef?.Contains(questScriptDef) ?? false);
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInQuestScriptDef.AddDistinct(planetLayerDef);
                        int index = ForbidInQuestScriptDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInQuestScriptDef.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInQuestScriptDef.AddDistinct(planetLayerDef);
                        int index = AllowInQuestScriptDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInQuestScriptDef.RemoveAt(index);
                        }
                    }
                }
                questScriptDef.layerWhitelist = AllowInQuestScriptDef;
                questScriptDef.layerBlacklist = ForbidInQuestScriptDef;
                
            }
            Log.Message($"Allow QuestScriptDef B:\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"   {qsd.defName} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}\n{string.Join("\n", qsd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", qsd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestQuestScriptDefOnLayerDef(qsd)}"))}"))}");
            //Log.Message($"Allow arrival FactionDef A:\n{string.Join("\n", AllFactionDefs.Select((fd) => $"   {fd.defName}\n{string.Join("\n", fd.arrivalLayerWhitelist.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", fd.arrivalLayerBlacklist.Select((pld) => $"      -{pld.defName}"))}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestArrivalFactionDefOnLayerDef(fd)}"))}"))}");
            //Log.Message($"Allow FactionDef A:\n{string.Join("\n", AllFactionDefs.Select((fd) => $"   {fd.defName}\n{string.Join("\n", fd.layerWhitelist.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", fd.layerBlacklist.Select((pld) => $"      -{pld.defName}"))}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestFactionDefOnLayerDef(fd)}"))}"))}");
            foreach (FactionDef factionDef in AllFactionDefs)
            {
                List<PlanetLayerDef> AllowInArrivalFactionDefs = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInArrivalFactionDefs = new List<PlanetLayerDef>();
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    if (factionDef.arrivalLayerWhitelist.NullOrEmpty())
                    {
                        if (planetLayerDef.onlyAllowWhitelistedArrivals)
                        {
                            ForbidInArrivalFactionDefs.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            AllowInArrivalFactionDefs.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (factionDef.arrivalLayerWhitelist.Contains(planetLayerDef))
                        {
                            AllowInArrivalFactionDefs.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            ForbidInArrivalFactionDefs.AddUnique(planetLayerDef);
                        }
                    }
                }
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInArrivalFactionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInArrivalFactionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (factionDef.arrivalLayerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInArrivalFactionDefs.AddRangeUnique(factionDef.arrivalLayerBlacklist);
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInArrivalFactionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInArrivalFactionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    TechLevel techLevelMin = TechLevel.Undefined;
                    TechLevel techLevelMax = TechLevel.Undefined;
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistArrivalFactionDef?.Contains(factionDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistArrivalFactionDef?.Contains(factionDef) ?? false;
                        if (LayerGroupDefModExtension.minArrivalFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMin = LayerGroupDefModExtension.minArrivalFactionTechLevel;
                        }
                        if (LayerGroupDefModExtension.maxArrivalFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMax = LayerGroupDefModExtension.maxArrivalFactionTechLevel;
                        }
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistArrivalFactionDef?.Contains(factionDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistArrivalFactionDef?.Contains(factionDef) ?? false);
                        if (LayerDefModExtension.minArrivalFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMin = LayerDefModExtension.minArrivalFactionTechLevel;
                        }
                        if (LayerDefModExtension.maxArrivalFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMax = LayerDefModExtension.maxArrivalFactionTechLevel;
                        }
                    }
                    if (techLevelMin != TechLevel.Undefined)
                    {
                        if (factionDef.techLevel >= techLevelMin)
                        {
                            AllowInArrivalFactionDefs.AddDistinct(planetLayerDef);
                            int index = ForbidInArrivalFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                ForbidInArrivalFactionDefs.RemoveAt(index);
                            }
                        }
                        else
                        {
                            ForbidInArrivalFactionDefs.AddDistinct(planetLayerDef);
                            int index = AllowInArrivalFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                AllowInArrivalFactionDefs.RemoveAt(index);
                            }
                        }
                    }
                    if (techLevelMax != TechLevel.Undefined)
                    {
                        if (factionDef.techLevel <= techLevelMax)
                        {
                            AllowInArrivalFactionDefs.AddDistinct(planetLayerDef);
                            int index = ForbidInArrivalFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                ForbidInArrivalFactionDefs.RemoveAt(index);
                            }
                        }
                        else
                        {
                            ForbidInArrivalFactionDefs.AddDistinct(planetLayerDef);
                            int index = AllowInArrivalFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                AllowInArrivalFactionDefs.RemoveAt(index);
                            }
                        }
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInArrivalFactionDefs.AddDistinct(planetLayerDef);
                        int index = ForbidInArrivalFactionDefs.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInArrivalFactionDefs.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInArrivalFactionDefs.AddDistinct(planetLayerDef);
                        int index = AllowInArrivalFactionDefs.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInArrivalFactionDefs.RemoveAt(index);
                        }
                    }
                }
                factionDef.arrivalLayerWhitelist = AllowInArrivalFactionDefs;
                factionDef.arrivalLayerBlacklist = ForbidInArrivalFactionDefs;
                List<PlanetLayerDef> AllowInFactionDefs = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInFactionDefs = new List<PlanetLayerDef>();
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInFactionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInFactionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    //Log.Message($"|||A {planetLayerDef.defName}");
                    if (factionDef.layerWhitelist.NullOrEmpty())
                    {
                        if ((PlanetLayerDefMods[planetLayerDef].Item1?.onlyAllowWhitelistedFactions ?? false) || (PlanetLayerDefMods[planetLayerDef].Item2?.onlyAllowWhitelistedFactions ?? false))
                        {
                            //Log.Message($"|||B");
                            ForbidInFactionDefs.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            //Log.Message($"|||C");
                            AllowInFactionDefs.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (factionDef.layerWhitelist.Contains(planetLayerDef))
                        {
                            //Log.Message($"|||D");
                            AllowInFactionDefs.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            //Log.Message($"|||E");
                            ForbidInFactionDefs.AddUnique(planetLayerDef);
                        }
                    }
                    //Log.Message($"|||F {planetLayerDef.defName}");
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInFactionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInFactionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (factionDef.layerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInFactionDefs.AddRangeUnique(factionDef.layerBlacklist);
                }
                //Log.Message($"|||3 {factionDef.defName}:\n{string.Join("\n", AllowInFactionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInFactionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    TechLevel techLevelMin = TechLevel.Undefined;
                    TechLevel techLevelMax = TechLevel.Undefined;
                    bool onlyAllowWhitelistedFactions = false;
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        onlyAllowWhitelistedFactions = LayerGroupDefModExtension.onlyAllowWhitelistedFactions;
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistFactionDef?.Contains(factionDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistFactionDef?.Contains(factionDef) ?? false;
                        if (LayerGroupDefModExtension.minFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMin = LayerGroupDefModExtension.minFactionTechLevel;
                        }
                        if (LayerGroupDefModExtension.maxFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMax = LayerGroupDefModExtension.maxFactionTechLevel;
                        }
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        onlyAllowWhitelistedFactions = onlyAllowWhitelistedFactions || LayerDefModExtension.onlyAllowWhitelistedFactions;
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistFactionDef?.Contains(factionDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistFactionDef?.Contains(factionDef) ?? false);
                        if (LayerDefModExtension.minFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMin = LayerDefModExtension.minFactionTechLevel;
                        }
                        if (LayerDefModExtension.maxFactionTechLevel != TechLevel.Undefined)
                        {
                            techLevelMax = LayerDefModExtension.maxFactionTechLevel;
                        }
                    }
                    if (techLevelMin != TechLevel.Undefined)
                    {
                        if (factionDef.techLevel >= techLevelMin)
                        {
                            AllowInFactionDefs.AddDistinct(planetLayerDef);
                            int index = ForbidInFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                ForbidInFactionDefs.RemoveAt(index);
                            }
                        }
                        else
                        {
                            ForbidInFactionDefs.AddDistinct(planetLayerDef);
                            int index = AllowInFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                AllowInFactionDefs.RemoveAt(index);
                            }
                        }
                    }
                    if (techLevelMax != TechLevel.Undefined)
                    {
                        if (factionDef.techLevel <= techLevelMax)
                        {
                            AllowInFactionDefs.AddDistinct(planetLayerDef);
                            int index = ForbidInFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                ForbidInFactionDefs.RemoveAt(index);
                            }
                        }
                        else
                        {
                            ForbidInFactionDefs.AddDistinct(planetLayerDef);
                            int index = AllowInFactionDefs.IndexOf(planetLayerDef);
                            if (index > -1)
                            {
                                AllowInFactionDefs.RemoveAt(index);
                            }
                        }
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInFactionDefs.AddDistinct(planetLayerDef);
                        int index = ForbidInFactionDefs.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInFactionDefs.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInFactionDefs.AddDistinct(planetLayerDef);
                        int index = AllowInFactionDefs.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInFactionDefs.RemoveAt(index);
                        }
                    }
                }
                factionDef.layerWhitelist = AllowInFactionDefs;
                factionDef.layerBlacklist = ForbidInFactionDefs;
            }
            //Log.Message($"Allow arrival FactionDef B:\n{string.Join("\n", AllFactionDefs.Select((fd) => $"   {fd.defName} {fd.techLevel}\n{string.Join("\n", fd.arrivalLayerWhitelist.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", fd.arrivalLayerBlacklist.Select((pld) => $"      -{pld.defName}"))}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestArrivalFactionDefOnLayerDef(fd)}"))}"))}");
            //Log.Message($"Allow FactionDef B:\n{string.Join("\n", AllFactionDefs.Select((fd) => $"   {fd.defName} {fd.techLevel}\n{string.Join("\n", fd.layerWhitelist.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", fd.layerBlacklist.Select((pld) => $"      -{pld.defName}"))}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestFactionDefOnLayerDef(fd)}"))}"))}");
            return;

            //foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            //{
            //    //PlanetLayerArrivalFactionDefs.Add(planetLayerDef, (
            //    //    AllFactionDefs.Where((FactionDef fd) => fd.arrivalLayerWhitelist.NullOrEmpty() ? true : fd.arrivalLayerWhitelist.Contains(planetLayerDef)).ToList(),
            //    //    AllFactionDefs.Where((FactionDef fd) => fd.arrivalLayerBlacklist.NullOrEmpty() ? false : fd.arrivalLayerBlacklist.Contains(planetLayerDef)).ToList()));



            //    PlanetLayerFactionDefs.Add(planetLayerDef, (
            //        AllFactionDefs.Where((FactionDef fd) => fd.layerWhitelist.NullOrEmpty() ? true : fd.layerWhitelist.Contains(planetLayerDef)).ToList(),
            //        AllFactionDefs.Where((FactionDef fd) => fd.layerBlacklist.NullOrEmpty() ? false : fd.layerBlacklist.Contains(planetLayerDef)).ToList(), 
            //        false));
            //}
            //Log.Message($"Available FactionDef S:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", AllFactionDefs.Select((FactionDef fd) => $"      {fd.defName}\n      +{(fd.layerWhitelist.NullOrEmpty() ? true : fd.layerWhitelist.Contains(pl))} = {fd.layerWhitelist.NullOrEmpty()} ? true : {fd.layerWhitelist.Contains(pl)}\n      -{(fd.layerBlacklist.NullOrEmpty() ? false : fd.layerBlacklist.Contains(pl))} = {fd.layerBlacklist.NullOrEmpty()} ? false : {fd.layerBlacklist.Contains(pl)}"))}"))}");
            ////Log.Message($"Available arrival FactionDef A:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //Log.Message($"Available FactionDef A:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //List<PlanetLayerGroupDef> AllPlanetLayerGroupDefs = DefDatabase<PlanetLayerGroupDef>.AllDefs.ToList();
            //foreach (PlanetLayerGroupDef planetLayerGroupDef in AllPlanetLayerGroupDefs)
            //{
            //    LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            //    foreach (PlanetLayerDef planetLayerDef in planetLayerGroupDef.ContainedLayers().Where(p => AllPlanetLayerDefs.Contains(p)))
            //    {
            //        ////IncidentDefs
            //        //List<IncidentDef> WhitelistIncidentDefs = AllIncidentDefs.Where((IncidentDef id) => id.canOccurOnAllPlanetLayers || (id.layerWhitelist?.Contains(planetLayerDef) ?? false)).ToList();
            //        //List<IncidentDef> BlacklistIncidentDefs = AllIncidentDefs.Where((IncidentDef id) => id.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //        //if (laoDefModExtension != null)
            //        //{
            //        //    if (laoDefModExtension.BlacklistIncidentDef != null)
            //        //    {
            //        //        BlacklistIncidentDefs.AddRangeUnique(laoDefModExtension.BlacklistIncidentDef);
            //        //    }
            //        //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //        //    {
            //        //        WhitelistIncidentDefs.RemoveAll((IncidentDef id) => BlacklistIncidentDefs.Contains(id));
            //        //    }
            //        //    if (laoDefModExtension.WhitelistIncidentDef != null)
            //        //    {
            //        //        WhitelistIncidentDefs.AddRangeUnique(laoDefModExtension.WhitelistIncidentDef);
            //        //    }
            //        //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //        //    {
            //        //        BlacklistIncidentDefs.RemoveAll((IncidentDef id) => WhitelistIncidentDefs.Contains(id));
            //        //    }
            //        //}
            //        //if (!WhitelistIncidentDefs.NullOrEmpty())
            //        //{
            //        //    foreach (IncidentDef incidentDef in WhitelistIncidentDefs)
            //        //    {
            //        //        //Log.Message($"{planetLayerDef.defName} WhitelistIncidentDefs {incidentDef.defName} {incidentDef.canOccurOnAllPlanetLayers}");
            //        //        if (!incidentDef.canOccurOnAllPlanetLayers)
            //        //        {
            //        //            if (incidentDef.layerWhitelist == null)
            //        //            {
            //        //                incidentDef.layerWhitelist = new List<PlanetLayerDef>();
            //        //            }
            //        //            incidentDef.layerWhitelist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        //if (!planetLayerDef.onlyAllowWhitelistedIncidents)
            //        //{
            //        //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //        //    {
            //        //        foreach (IncidentDef incidentDef in BlacklistIncidentDefs)
            //        //        {
            //        //            //Log.Message($"{planetLayerDef.defName} BlacklistIncidentDefs {incidentDef.defName}");
            //        //            if (incidentDef.layerBlacklist == null)
            //        //            {
            //        //                incidentDef.layerBlacklist = new List<PlanetLayerDef>();
            //        //            }
            //        //            incidentDef.layerBlacklist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        ////BiomeDefs
            //        //List<BiomeDef> WhitelistBiomeDefs = AllBiomeDefs.Where((BiomeDef bd) => bd.layerWhitelist?.Contains(planetLayerDef) ?? false).ToList();
            //        //List<BiomeDef> BlacklistBiomeDefs = AllBiomeDefs.Where((BiomeDef bd) => bd.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //        //if (laoDefModExtension != null)
            //        //{
            //        //    if (laoDefModExtension.BlacklistBiomeDef != null)
            //        //    {
            //        //        BlacklistBiomeDefs.AddRangeUnique(laoDefModExtension.BlacklistBiomeDef);
            //        //    }
            //        //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //        //    {
            //        //        WhitelistBiomeDefs.RemoveAll((BiomeDef bd) => BlacklistBiomeDefs.Contains(bd));
            //        //    }
            //        //    if (laoDefModExtension.WhitelistBiomeDef != null)
            //        //    {
            //        //        WhitelistBiomeDefs.AddRangeUnique(laoDefModExtension.WhitelistBiomeDef);
            //        //    }
            //        //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //        //    {
            //        //        BlacklistBiomeDefs.RemoveAll((BiomeDef id) => WhitelistBiomeDefs.Contains(id));
            //        //    }
            //        //}
            //        //if (!WhitelistBiomeDefs.NullOrEmpty())
            //        //{
            //        //    foreach (BiomeDef biomeDef in WhitelistBiomeDefs)
            //        //    {
            //        //        //Log.Message($"{planetLayerDef.defName} WhitelistBiomeDefs {biomeDef.defName}");
            //        //        if (biomeDef.layerWhitelist == null)
            //        //        {
            //        //            biomeDef.layerWhitelist = new List<PlanetLayerDef>();
            //        //        }
            //        //        biomeDef.layerWhitelist.AddDistinct(planetLayerDef);
            //        //    }
            //        //}
            //        //if (!planetLayerDef.onlyAllowWhitelistedBiomes)
            //        //{
            //        //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //        //    {
            //        //        foreach (BiomeDef biomeDef in BlacklistBiomeDefs)
            //        //        {
            //        //            //Log.Message($"{planetLayerDef.defName} BlacklistBiomeDefs {biomeDef.defName}");
            //        //            if (biomeDef.layerBlacklist == null)
            //        //            {
            //        //                biomeDef.layerBlacklist = new List<PlanetLayerDef>();
            //        //            }
            //        //            biomeDef.layerBlacklist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        ////GameConditionDefs
            //        //List<GameConditionDef> WhitelistGameConditionDefs = AllGameConditionDefs.Where((GameConditionDef bd) => bd.canAffectAllPlanetLayers || (bd.layerWhitelist?.Contains(planetLayerDef) ?? false)).ToList();
            //        //List<GameConditionDef> BlacklistGameConditionDefs = AllGameConditionDefs.Where((GameConditionDef bd) => bd.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //        //if (laoDefModExtension != null)
            //        //{
            //        //    if (laoDefModExtension.BlacklistGameConditionDef != null)
            //        //    {
            //        //        BlacklistGameConditionDefs.AddRangeUnique(laoDefModExtension.BlacklistGameConditionDef);
            //        //    }
            //        //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //        //    {
            //        //        WhitelistGameConditionDefs.RemoveAll((GameConditionDef bd) => BlacklistGameConditionDefs.Contains(bd));
            //        //    }
            //        //    if (laoDefModExtension.WhitelistGameConditionDef != null)
            //        //    {
            //        //        WhitelistGameConditionDefs.AddRangeUnique(laoDefModExtension.WhitelistGameConditionDef);
            //        //    }
            //        //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //        //    {
            //        //        BlacklistGameConditionDefs.RemoveAll((GameConditionDef id) => WhitelistGameConditionDefs.Contains(id));
            //        //    }
            //        //}
            //        //if (!WhitelistGameConditionDefs.NullOrEmpty())
            //        //{
            //        //    foreach (GameConditionDef gameConditionDef in WhitelistGameConditionDefs)
            //        //    {
            //        //        //Log.Message($"{planetLayerDef.defName} WhitelistGameConditionDefs {gameConditionDef.defName} {gameConditionDef.canAffectAllPlanetLayers}");
            //        //        if (!gameConditionDef.canAffectAllPlanetLayers)
            //        //        {
            //        //            if (gameConditionDef.layerWhitelist == null)
            //        //            {
            //        //                gameConditionDef.layerWhitelist = new List<PlanetLayerDef>();
            //        //            }
            //        //            gameConditionDef.layerWhitelist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        //if (!planetLayerDef.onlyAllowWhitelistedGameConditions)
            //        //{
            //        //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //        //    {
            //        //        foreach (GameConditionDef gameConditionDef in BlacklistGameConditionDefs)
            //        //        {
            //        //            //Log.Message($"{planetLayerDef.defName} BlacklistGameConditionDefs {gameConditionDef.defName}");
            //        //            if (gameConditionDef.layerBlacklist == null)
            //        //            {
            //        //                gameConditionDef.layerBlacklist = new List<PlanetLayerDef>();
            //        //            }
            //        //            gameConditionDef.layerBlacklist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        ////QuestScriptDefs
            //        //List<QuestScriptDef> WhitelistQuestScriptDefs = AllQuestScriptDefs.Where((QuestScriptDef qsd) => qsd.canOccurOnAllPlanetLayers || (qsd.layerWhitelist?.Contains(planetLayerDef) ?? false) || (qsd.everAcceptableInSpace && planetLayerDef.isSpace)).ToList();
            //        //List<QuestScriptDef> BlacklistQuestScriptDefs = AllQuestScriptDefs.Where((QuestScriptDef qsd) => (qsd.layerBlacklist?.Contains(planetLayerDef) ?? false) || (qsd.everAcceptableInSpace && planetLayerDef.isSpace)).ToList();
            //        //if (laoDefModExtension != null)
            //        //{
            //        //    if (laoDefModExtension.BlacklistQuestScriptDef != null)
            //        //    {
            //        //        BlacklistQuestScriptDefs.AddRangeUnique(laoDefModExtension.BlacklistQuestScriptDef);
            //        //    }
            //        //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //        //    {
            //        //        WhitelistQuestScriptDefs.RemoveAll((QuestScriptDef bd) => BlacklistQuestScriptDefs.Contains(bd));
            //        //    }
            //        //    if (laoDefModExtension.WhitelistQuestScriptDef != null)
            //        //    {
            //        //        WhitelistQuestScriptDefs.AddRangeUnique(laoDefModExtension.WhitelistQuestScriptDef);
            //        //    }
            //        //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //        //    {
            //        //        BlacklistQuestScriptDefs.RemoveAll((QuestScriptDef id) => WhitelistQuestScriptDefs.Contains(id));
            //        //    }
            //        //}
            //        //if (!WhitelistQuestScriptDefs.NullOrEmpty())
            //        //{
            //        //    foreach (QuestScriptDef questScriptDef in WhitelistQuestScriptDefs)
            //        //    {
            //        //        //Log.Message($"{planetLayerDef.defName} WhitelistQuestScriptDefs {questScriptDef.defName} {questScriptDef.canOccurOnAllPlanetLayers} || {questScriptDef.everAcceptableInSpace} && {planetLayerDef.isSpace}");
            //        //        if (!(questScriptDef.canOccurOnAllPlanetLayers || (questScriptDef.everAcceptableInSpace && planetLayerDef.isSpace)))
            //        //        {
            //        //            if (questScriptDef.layerWhitelist == null)
            //        //            {
            //        //                questScriptDef.layerWhitelist = new List<PlanetLayerDef>();
            //        //            }
            //        //            questScriptDef.layerWhitelist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        //if (!planetLayerDef.onlyAllowWhitelistedQuests)
            //        //{
            //        //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //        //    {
            //        //        foreach (QuestScriptDef questScriptDef in BlacklistQuestScriptDefs)
            //        //        {
            //        //            //Log.Message($"{planetLayerDef.defName} BlacklistQuestScriptDefs {questScriptDef.defName}");
            //        //            if (questScriptDef.layerBlacklist == null)
            //        //            {
            //        //                questScriptDef.layerBlacklist = new List<PlanetLayerDef>();
            //        //            }
            //        //            questScriptDef.layerBlacklist.AddDistinct(planetLayerDef);
            //        //        }
            //        //    }
            //        //}
            //        ////ArrivalFactionDefs
            //        //List<FactionDef> WhitelistArrivalFactionDefs = PlanetLayerArrivalFactionDefs[planetLayerDef].Item1;
            //        //List<FactionDef> BlacklistArrivalFactionDefs = PlanetLayerArrivalFactionDefs[planetLayerDef].Item2;
            //        //if (laoDefModExtension != null)
            //        //{
            //        //    IEnumerable<FactionDef> WithinFactionDefs = AllFactionDefs.ToList();
            //        //    IEnumerable<FactionDef> BellowFactionDefs = AllFactionDefs.ToList();
            //        //    IEnumerable<FactionDef> AboveFactionDefs = AllFactionDefs.ToList();
            //        //    if (laoDefModExtension.minArrivalFactionTechLevel != TechLevel.Undefined)
            //        //    {
            //        //        WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel >= laoDefModExtension.minArrivalFactionTechLevel);
            //        //        BellowFactionDefs = BellowFactionDefs.Where((FactionDef fd) => fd.techLevel < laoDefModExtension.minArrivalFactionTechLevel);
            //        //    }
            //        //    if (laoDefModExtension.maxArrivalFactionTechLevel != TechLevel.Undefined)
            //        //    {
            //        //        WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel <= laoDefModExtension.maxArrivalFactionTechLevel);
            //        //        AboveFactionDefs = AboveFactionDefs.Where((FactionDef fd) => fd.techLevel > laoDefModExtension.maxArrivalFactionTechLevel);
            //        //    }
            //        //    if (WithinFactionDefs != null)
            //        //    {
            //        //        WhitelistArrivalFactionDefs.AddRangeUnique(WithinFactionDefs);
            //        //    }
            //        //    if (BellowFactionDefs != null)
            //        //    {
            //        //        BlacklistArrivalFactionDefs.AddRangeUnique(BellowFactionDefs);
            //        //    }
            //        //    if (AboveFactionDefs != null)
            //        //    {
            //        //        BlacklistArrivalFactionDefs.AddRangeUnique(AboveFactionDefs);
            //        //    }
            //        //    if (laoDefModExtension.BlacklistArrivalFactionDef != null)
            //        //    {
            //        //        BlacklistArrivalFactionDefs.AddRangeUnique(laoDefModExtension.BlacklistArrivalFactionDef);
            //        //    }
            //        //    if (!BlacklistArrivalFactionDefs.NullOrEmpty())
            //        //    {
            //        //        WhitelistArrivalFactionDefs.RemoveAll((FactionDef fd) => BlacklistArrivalFactionDefs.Contains(fd));
            //        //    }
            //        //    if (laoDefModExtension.WhitelistArrivalFactionDef != null)
            //        //    {
            //        //        WhitelistArrivalFactionDefs.AddRangeUnique(laoDefModExtension.WhitelistArrivalFactionDef);
            //        //    }
            //        //    if (!BlacklistArrivalFactionDefs.NullOrEmpty())
            //        //    {
            //        //        BlacklistArrivalFactionDefs.RemoveAll((FactionDef fd) => WhitelistArrivalFactionDefs.Contains(fd));
            //        //    }
            //        //}
            //        //PlanetLayerArrivalFactionDefs[planetLayerDef] = (WhitelistArrivalFactionDefs, BlacklistArrivalFactionDefs);
            //        //FactionDefs
            //        List<FactionDef> WhitelistFactionDefs = PlanetLayerFactionDefs[planetLayerDef].Item1;
            //        List<FactionDef> BlacklistFactionDefs = PlanetLayerFactionDefs[planetLayerDef].Item2;
            //        Log.Message($"Available FactionDef B0:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (laoDefModExtension != null)
            //        {
            //            IEnumerable<FactionDef> WithinFactionDefs = AllFactionDefs.ToList();
            //            IEnumerable<FactionDef> BellowFactionDefs = AllFactionDefs.ToList();
            //            IEnumerable<FactionDef> AboveFactionDefs = AllFactionDefs.ToList();
            //            bool isTechLevelLimit = false;
            //            if (laoDefModExtension.minFactionTechLevel != TechLevel.Undefined)
            //            {
            //                WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel >= laoDefModExtension.minFactionTechLevel);
            //                BellowFactionDefs = BellowFactionDefs.Where((FactionDef fd) => fd.techLevel < laoDefModExtension.minFactionTechLevel);
            //                isTechLevelLimit = true;
            //            }
            //            if (laoDefModExtension.maxFactionTechLevel != TechLevel.Undefined)
            //            {
            //                WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel <= laoDefModExtension.maxFactionTechLevel);
            //                AboveFactionDefs = AboveFactionDefs.Where((FactionDef fd) => fd.techLevel > laoDefModExtension.maxFactionTechLevel);
            //                isTechLevelLimit = true;
            //            }
            //            Log.Message($"Available FactionDef B1 ({laoDefModExtension.minFactionTechLevel} {laoDefModExtension.maxFactionTechLevel}) {isTechLevelLimit}:\n   {planetLayerDef.defName}:\n{string.Join("\n", WithinFactionDefs.Select((fd) => $"++++++{fd.defName}"))}\n/---\\\n{string.Join("\n", BellowFactionDefs.Select((fd) => $"******{fd.defName}"))}\n|---|\n{string.Join("\n", AboveFactionDefs.Select((fd) => $"------{fd.defName}"))}\n\\---/\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //            if (isTechLevelLimit)
            //            {
            //                if (WithinFactionDefs != null)
            //                {
            //                    WhitelistFactionDefs.AddRangeUnique(WithinFactionDefs);
            //                }
            //                if (BellowFactionDefs != null)
            //                {
            //                    BlacklistFactionDefs.AddRangeUnique(BellowFactionDefs);
            //                }
            //                if (AboveFactionDefs != null)
            //                {
            //                    BlacklistFactionDefs.AddRangeUnique(AboveFactionDefs);
            //                }
            //            }
            //            Log.Message($"Available FactionDef B2:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //            if (laoDefModExtension.BlacklistFactionDef != null)
            //            {
            //                Log.Message($"Available FactionDef B2+:\n   {planetLayerDef.defName}\n{string.Join("\n", laoDefModExtension.BlacklistFactionDef.Select((fd) => $"------{fd.defName}"))}");
            //                BlacklistFactionDefs.AddRangeUnique(laoDefModExtension.BlacklistFactionDef);
            //            }
            //            Log.Message($"Available FactionDef B3:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //            if (!BlacklistFactionDefs.NullOrEmpty())
            //            {
            //                WhitelistFactionDefs.RemoveAll((FactionDef fd) => BlacklistFactionDefs.Contains(fd));
            //            }
            //            Log.Message($"Available FactionDef B4:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //            if (laoDefModExtension.WhitelistFactionDef != null)
            //            {
            //                Log.Message($"Available FactionDef B4+:\n   {planetLayerDef.defName}\n{string.Join("\n", laoDefModExtension.WhitelistFactionDef.Select((fd) => $"++++++{fd.defName}"))}");
            //                WhitelistFactionDefs.AddRangeUnique(laoDefModExtension.WhitelistFactionDef);
            //            }
            //            Log.Message($"Available FactionDef B5:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //            if (!BlacklistFactionDefs.NullOrEmpty())
            //            {
            //                BlacklistFactionDefs.RemoveAll((FactionDef fd) => WhitelistFactionDefs.Contains(fd));
            //            }
            //            Log.Message($"Available FactionDef B6:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        }
            //        PlanetLayerFactionDefs[planetLayerDef] = (WhitelistFactionDefs, BlacklistFactionDefs, PlanetLayerFactionDefs[planetLayerDef].Item3 || (laoDefModExtension?.onlyAllowWhitelistedFactions ?? false));
            //    }
            //}
            ////Log.Message($"Available arrival FactionDef B:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //Log.Message($"Available FactionDef B:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            //{
            //    LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            //    ////IncidentDefs
            //    //List<IncidentDef> WhitelistIncidentDefs = AllIncidentDefs.Where((IncidentDef id) => id.canOccurOnAllPlanetLayers || (id.layerWhitelist?.Contains(planetLayerDef) ?? false)).ToList();
            //    //List<IncidentDef> BlacklistIncidentDefs = AllIncidentDefs.Where((IncidentDef id) => id.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //    //if (laoDefModExtension != null)
            //    //{
            //    //    if (laoDefModExtension.BlacklistIncidentDef != null)
            //    //    {
            //    //        BlacklistIncidentDefs.AddRangeUnique(laoDefModExtension.BlacklistIncidentDef);
            //    //    }
            //    //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //    //    {
            //    //        WhitelistIncidentDefs.RemoveAll((IncidentDef id) => BlacklistIncidentDefs.Contains(id));
            //    //    }
            //    //    if (laoDefModExtension.WhitelistIncidentDef != null)
            //    //    {
            //    //        WhitelistIncidentDefs.AddRangeUnique(laoDefModExtension.WhitelistIncidentDef);
            //    //    }
            //    //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //    //    {
            //    //        BlacklistIncidentDefs.RemoveAll((IncidentDef id) => WhitelistIncidentDefs.Contains(id));
            //    //    }
            //    //}
            //    //if (!WhitelistIncidentDefs.NullOrEmpty())
            //    //{
            //    //    foreach (IncidentDef incidentDef in WhitelistIncidentDefs)
            //    //    {
            //    //        //Log.Message($"{planetLayerDef.defName} WhitelistIncidentDefs {incidentDef.defName} {incidentDef.canOccurOnAllPlanetLayers}");
            //    //        if (!incidentDef.canOccurOnAllPlanetLayers)
            //    //        {
            //    //            if (incidentDef.layerWhitelist == null)
            //    //            {
            //    //                incidentDef.layerWhitelist = new List<PlanetLayerDef>();
            //    //            }
            //    //            incidentDef.layerWhitelist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    //if (!planetLayerDef.onlyAllowWhitelistedIncidents)
            //    //{
            //    //    if (!BlacklistIncidentDefs.NullOrEmpty())
            //    //    {
            //    //        foreach (IncidentDef incidentDef in BlacklistIncidentDefs)
            //    //        {
            //    //            //Log.Message($"{planetLayerDef.defName} BlacklistIncidentDefs {incidentDef.defName}");
            //    //            if (incidentDef.layerBlacklist == null)
            //    //            {
            //    //                incidentDef.layerBlacklist = new List<PlanetLayerDef>();
            //    //            }
            //    //            incidentDef.layerBlacklist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    ////BiomeDefs
            //    //List<BiomeDef> WhitelistBiomeDefs = AllBiomeDefs.Where((BiomeDef bd) => bd.layerWhitelist?.Contains(planetLayerDef) ?? false).ToList();
            //    //List<BiomeDef> BlacklistBiomeDefs = AllBiomeDefs.Where((BiomeDef bd) => bd.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //    //if (laoDefModExtension != null)
            //    //{
            //    //    if (laoDefModExtension.BlacklistBiomeDef != null)
            //    //    {
            //    //        BlacklistBiomeDefs.AddRangeUnique(laoDefModExtension.BlacklistBiomeDef);
            //    //    }
            //    //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //    //    {
            //    //        WhitelistBiomeDefs.RemoveAll((BiomeDef bd) => BlacklistBiomeDefs.Contains(bd));
            //    //    }
            //    //    if (laoDefModExtension.WhitelistBiomeDef != null)
            //    //    {
            //    //        WhitelistBiomeDefs.AddRangeUnique(laoDefModExtension.WhitelistBiomeDef);
            //    //    }
            //    //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //    //    {
            //    //        BlacklistBiomeDefs.RemoveAll((BiomeDef id) => WhitelistBiomeDefs.Contains(id));
            //    //    }
            //    //}
            //    //if (!WhitelistBiomeDefs.NullOrEmpty())
            //    //{
            //    //    foreach (BiomeDef biomeDef in WhitelistBiomeDefs)
            //    //    {
            //    //        //Log.Message($"{planetLayerDef.defName} WhitelistBiomeDefs {biomeDef.defName}");
            //    //        if (biomeDef.layerWhitelist == null)
            //    //        {
            //    //            biomeDef.layerWhitelist = new List<PlanetLayerDef>();
            //    //        }
            //    //        biomeDef.layerWhitelist.AddDistinct(planetLayerDef);
            //    //    }
            //    //}
            //    //if (!planetLayerDef.onlyAllowWhitelistedBiomes)
            //    //{
            //    //    if (!BlacklistBiomeDefs.NullOrEmpty())
            //    //    {
            //    //        foreach (BiomeDef biomeDef in BlacklistBiomeDefs)
            //    //        {
            //    //            //Log.Message($"{planetLayerDef.defName} BlacklistBiomeDefs {biomeDef.defName}");
            //    //            if (biomeDef.layerBlacklist == null)
            //    //            {
            //    //                biomeDef.layerBlacklist = new List<PlanetLayerDef>();
            //    //            }
            //    //            biomeDef.layerBlacklist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    ////GameConditionDefs
            //    //List<GameConditionDef> WhitelistGameConditionDefs = AllGameConditionDefs.Where((GameConditionDef bd) => bd.canAffectAllPlanetLayers || (bd.layerWhitelist?.Contains(planetLayerDef) ?? false)).ToList();
            //    //List<GameConditionDef> BlacklistGameConditionDefs = AllGameConditionDefs.Where((GameConditionDef bd) => bd.layerBlacklist?.Contains(planetLayerDef) ?? false).ToList();
            //    //if (laoDefModExtension != null)
            //    //{
            //    //    if (laoDefModExtension.BlacklistGameConditionDef != null)
            //    //    {
            //    //        BlacklistGameConditionDefs.AddRangeUnique(laoDefModExtension.BlacklistGameConditionDef);
            //    //    }
            //    //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //    //    {
            //    //        WhitelistGameConditionDefs.RemoveAll((GameConditionDef bd) => BlacklistGameConditionDefs.Contains(bd));
            //    //    }
            //    //    if (laoDefModExtension.WhitelistGameConditionDef != null)
            //    //    {
            //    //        WhitelistGameConditionDefs.AddRangeUnique(laoDefModExtension.WhitelistGameConditionDef);
            //    //    }
            //    //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //    //    {
            //    //        BlacklistGameConditionDefs.RemoveAll((GameConditionDef id) => WhitelistGameConditionDefs.Contains(id));
            //    //    }
            //    //}
            //    //if (!WhitelistGameConditionDefs.NullOrEmpty())
            //    //{
            //    //    foreach (GameConditionDef gameConditionDef in WhitelistGameConditionDefs)
            //    //    {
            //    //        //Log.Message($"{planetLayerDef.defName} WhitelistGameConditionDefs {gameConditionDef.defName} {gameConditionDef.canAffectAllPlanetLayers}");
            //    //        if (!gameConditionDef.canAffectAllPlanetLayers)
            //    //        {
            //    //            if (gameConditionDef.layerWhitelist == null)
            //    //            {
            //    //                gameConditionDef.layerWhitelist = new List<PlanetLayerDef>();
            //    //            }
            //    //            gameConditionDef.layerWhitelist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    //if (!planetLayerDef.onlyAllowWhitelistedGameConditions)
            //    //{
            //    //    if (!BlacklistGameConditionDefs.NullOrEmpty())
            //    //    {
            //    //        foreach (GameConditionDef gameConditionDef in BlacklistGameConditionDefs)
            //    //        {
            //    //            //Log.Message($"{planetLayerDef.defName} BlacklistGameConditionDefs {gameConditionDef.defName}");
            //    //            if (gameConditionDef.layerBlacklist == null)
            //    //            {
            //    //                gameConditionDef.layerBlacklist = new List<PlanetLayerDef>();
            //    //            }
            //    //            gameConditionDef.layerBlacklist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    ////QuestScriptDefs
            //    //List<QuestScriptDef> WhitelistQuestScriptDefs = AllQuestScriptDefs.Where((QuestScriptDef qsd) => qsd.canOccurOnAllPlanetLayers || (qsd.layerWhitelist?.Contains(planetLayerDef) ?? false) || (qsd.everAcceptableInSpace && planetLayerDef.isSpace)).ToList();
            //    //List<QuestScriptDef> BlacklistQuestScriptDefs = AllQuestScriptDefs.Where((QuestScriptDef qsd) => (qsd.layerBlacklist?.Contains(planetLayerDef) ?? false) || (qsd.everAcceptableInSpace && planetLayerDef.isSpace)).ToList();
            //    //if (laoDefModExtension != null)
            //    //{
            //    //    if (laoDefModExtension.BlacklistQuestScriptDef != null)
            //    //    {
            //    //        BlacklistQuestScriptDefs.AddRangeUnique(laoDefModExtension.BlacklistQuestScriptDef);
            //    //    }
            //    //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //    //    {
            //    //        WhitelistQuestScriptDefs.RemoveAll((QuestScriptDef bd) => BlacklistQuestScriptDefs.Contains(bd));
            //    //    }
            //    //    if (laoDefModExtension.WhitelistQuestScriptDef != null)
            //    //    {
            //    //        WhitelistQuestScriptDefs.AddRangeUnique(laoDefModExtension.WhitelistQuestScriptDef);
            //    //    }
            //    //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //    //    {
            //    //        BlacklistQuestScriptDefs.RemoveAll((QuestScriptDef id) => WhitelistQuestScriptDefs.Contains(id));
            //    //    }
            //    //}
            //    //if (!WhitelistQuestScriptDefs.NullOrEmpty())
            //    //{
            //    //    foreach (QuestScriptDef questScriptDef in WhitelistQuestScriptDefs)
            //    //    {
            //    //        //Log.Message($"{planetLayerDef.defName} WhitelistQuestScriptDefs {questScriptDef.defName} {questScriptDef.canOccurOnAllPlanetLayers} || {questScriptDef.everAcceptableInSpace} && {planetLayerDef.isSpace}");
            //    //        if (!(questScriptDef.canOccurOnAllPlanetLayers || (questScriptDef.everAcceptableInSpace && planetLayerDef.isSpace)))
            //    //        {
            //    //            if (questScriptDef.layerWhitelist == null)
            //    //            {
            //    //                questScriptDef.layerWhitelist = new List<PlanetLayerDef>();
            //    //            }
            //    //            questScriptDef.layerWhitelist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    //if (!planetLayerDef.onlyAllowWhitelistedQuests)
            //    //{
            //    //    if (!BlacklistQuestScriptDefs.NullOrEmpty())
            //    //    {
            //    //        foreach (QuestScriptDef questScriptDef in BlacklistQuestScriptDefs)
            //    //        {
            //    //            //Log.Message($"{planetLayerDef.defName} BlacklistQuestScriptDefs {questScriptDef.defName}");
            //    //            if (questScriptDef.layerBlacklist == null)
            //    //            {
            //    //                questScriptDef.layerBlacklist = new List<PlanetLayerDef>();
            //    //            }
            //    //            questScriptDef.layerBlacklist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    ////ArrivalFactionDefs
            //    //List<FactionDef> WhitelistArrivalFactionDefs = PlanetLayerArrivalFactionDefs[planetLayerDef].Item1;
            //    //List<FactionDef> BlacklistArrivalFactionDefs = PlanetLayerArrivalFactionDefs[planetLayerDef].Item2;
            //    //if (laoDefModExtension != null)
            //    //{
            //    //    IEnumerable<FactionDef> WithinFactionDefs = AllFactionDefs.ToList();
            //    //    IEnumerable<FactionDef> BellowFactionDefs = AllFactionDefs.ToList();
            //    //    IEnumerable<FactionDef> AboveFactionDefs = AllFactionDefs.ToList();
            //    //    if (laoDefModExtension.minArrivalFactionTechLevel != TechLevel.Undefined)
            //    //    {
            //    //        WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel >= laoDefModExtension.minArrivalFactionTechLevel);
            //    //        BellowFactionDefs = BellowFactionDefs.Where((FactionDef fd) => fd.techLevel < laoDefModExtension.minArrivalFactionTechLevel);
            //    //    }
            //    //    if (laoDefModExtension.maxArrivalFactionTechLevel != TechLevel.Undefined)
            //    //    {
            //    //        WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel <= laoDefModExtension.maxArrivalFactionTechLevel);
            //    //        AboveFactionDefs = AboveFactionDefs.Where((FactionDef fd) => fd.techLevel > laoDefModExtension.maxArrivalFactionTechLevel);
            //    //    }
            //    //    if (WithinFactionDefs != null)
            //    //    {
            //    //        WhitelistArrivalFactionDefs.AddRangeUnique(WithinFactionDefs);
            //    //    }
            //    //    if (BellowFactionDefs != null)
            //    //    {
            //    //        BlacklistArrivalFactionDefs.AddRangeUnique(BellowFactionDefs);
            //    //    }
            //    //    if (AboveFactionDefs != null)
            //    //    {
            //    //        BlacklistArrivalFactionDefs.AddRangeUnique(AboveFactionDefs);
            //    //    }
            //    //    if (laoDefModExtension.BlacklistArrivalFactionDef != null)
            //    //    {
            //    //        BlacklistArrivalFactionDefs.AddRangeUnique(laoDefModExtension.BlacklistArrivalFactionDef);
            //    //    }
            //    //    if (!BlacklistArrivalFactionDefs.NullOrEmpty())
            //    //    {
            //    //        WhitelistArrivalFactionDefs.RemoveAll((FactionDef fd) => BlacklistArrivalFactionDefs.Contains(fd));
            //    //    }
            //    //    if (laoDefModExtension.WhitelistArrivalFactionDef != null)
            //    //    {
            //    //        WhitelistArrivalFactionDefs.AddRangeUnique(laoDefModExtension.WhitelistArrivalFactionDef);
            //    //    }
            //    //    if (!BlacklistArrivalFactionDefs.NullOrEmpty())
            //    //    {
            //    //        BlacklistArrivalFactionDefs.RemoveAll((FactionDef fd) => WhitelistArrivalFactionDefs.Contains(fd));
            //    //    }
            //    //}
            //    //PlanetLayerArrivalFactionDefs[planetLayerDef] = (WhitelistArrivalFactionDefs, BlacklistArrivalFactionDefs);
            //    //FactionDefs
            //    List<FactionDef> WhitelistFactionDefs = PlanetLayerFactionDefs[planetLayerDef].Item1;
            //    List<FactionDef> BlacklistFactionDefs = PlanetLayerFactionDefs[planetLayerDef].Item2;
            //    Log.Message($"Available FactionDef C0:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //    if (laoDefModExtension != null)
            //    {
            //        IEnumerable<FactionDef> WithinFactionDefs = AllFactionDefs.ToList();
            //        IEnumerable<FactionDef> BellowFactionDefs = AllFactionDefs.ToList();
            //        IEnumerable<FactionDef> AboveFactionDefs = AllFactionDefs.ToList();
            //        bool isTechLevelLimit = false;
            //        if (laoDefModExtension.minFactionTechLevel != TechLevel.Undefined)
            //        {
            //            WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel >= laoDefModExtension.minFactionTechLevel);
            //            BellowFactionDefs = BellowFactionDefs.Where((FactionDef fd) => fd.techLevel < laoDefModExtension.minFactionTechLevel);
            //            isTechLevelLimit = true;
            //        }
            //        if (laoDefModExtension.maxFactionTechLevel != TechLevel.Undefined)
            //        {
            //            WithinFactionDefs = WithinFactionDefs.Where((FactionDef fd) => fd.techLevel <= laoDefModExtension.maxFactionTechLevel);
            //            AboveFactionDefs = AboveFactionDefs.Where((FactionDef fd) => fd.techLevel > laoDefModExtension.maxFactionTechLevel);
            //            isTechLevelLimit = true;
            //        }
            //        Log.Message($"Available FactionDef C1 ({laoDefModExtension.minFactionTechLevel} {laoDefModExtension.maxFactionTechLevel}) {isTechLevelLimit}:\n   {planetLayerDef.defName}:\n{string.Join("\n", WithinFactionDefs.Select((fd) => $"++++++{fd.defName}"))}\n/---\\\n{string.Join("\n", BellowFactionDefs.Select((fd) => $"******{fd.defName}"))}\n|---|\n{string.Join("\n", AboveFactionDefs.Select((fd) => $"------{fd.defName}"))}\n\\---/\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (isTechLevelLimit)
            //        {
            //            if (WithinFactionDefs != null)
            //            {
            //                WhitelistFactionDefs.AddRangeUnique(WithinFactionDefs);
            //            }
            //            if (BellowFactionDefs != null)
            //            {
            //                BlacklistFactionDefs.AddRangeUnique(BellowFactionDefs);
            //            }
            //            if (AboveFactionDefs != null)
            //            {
            //                BlacklistFactionDefs.AddRangeUnique(AboveFactionDefs);
            //            }
            //        }
            //        Log.Message($"Available FactionDef C2:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (laoDefModExtension.BlacklistFactionDef != null)
            //        {
            //            Log.Message($"Available FactionDef C2+:\n   {planetLayerDef.defName}\n{string.Join("\n", laoDefModExtension.BlacklistFactionDef.Select((fd) => $"------{fd.defName}"))}");
            //            BlacklistFactionDefs.AddRangeUnique(laoDefModExtension.BlacklistFactionDef);
            //        }
            //        Log.Message($"Available FactionDef C3:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (!BlacklistFactionDefs.NullOrEmpty())
            //        {
            //            WhitelistFactionDefs.RemoveAll((FactionDef fd) => BlacklistFactionDefs.Contains(fd));
            //        }
            //        Log.Message($"Available FactionDef C4:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (laoDefModExtension.WhitelistFactionDef != null)
            //        {
            //            Log.Message($"Available FactionDef C4+:\n   {planetLayerDef.defName}\n{string.Join("\n", laoDefModExtension.WhitelistFactionDef.Select((fd) => $"++++++{fd.defName}"))}");
            //            WhitelistFactionDefs.AddRangeUnique(laoDefModExtension.WhitelistFactionDef);
            //        }
            //        Log.Message($"Available FactionDef C5:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //        if (!BlacklistFactionDefs.NullOrEmpty())
            //        {
            //            BlacklistFactionDefs.RemoveAll((FactionDef fd) => WhitelistFactionDefs.Contains(fd));
            //        }
            //        Log.Message($"Available FactionDef C6:\n   {planetLayerDef.defName}\n{string.Join("\n", WhitelistFactionDefs.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", BlacklistFactionDefs.Select((fd) => $"      {fd.defName}"))}");
            //    }
            //    PlanetLayerFactionDefs[planetLayerDef] = (WhitelistFactionDefs, BlacklistFactionDefs, PlanetLayerFactionDefs[planetLayerDef].Item3 || (laoDefModExtension?.onlyAllowWhitelistedFactions ?? false));
            //}
            ////Log.Message($"Available arrival FactionDef C:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerArrivalFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //Log.Message($"Available FactionDef C:\n{string.Join("\n", AllPlanetLayerDefs.Select((pl) => $"   {pl.defName}\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item1.Select((fd) => $"      {fd.defName}"))}\n<--->\n\n{string.Join("\n", PlanetLayerFactionDefs[pl].Item2.Select((fd) => $"      {fd.defName}"))}"))}");
            //foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            //{
            //    //(List<FactionDef> WhitelistArrivalFactionDefs, List<FactionDef> BlacklistArrivalFactionDefs) = PlanetLayerArrivalFactionDefs[planetLayerDef];
            //    //if (!WhitelistArrivalFactionDefs.NullOrEmpty())
            //    //{
            //    //    foreach (FactionDef factionDef in WhitelistArrivalFactionDefs)
            //    //    {
            //    //        Log.Message($"{planetLayerDef.defName} WhitelistArrivalFactionDefs {factionDef.defName} {factionDef.arrivalLayerWhitelist.Contains(planetLayerDef)}");
            //    //        if (factionDef.arrivalLayerWhitelist == null)
            //    //        {
            //    //            factionDef.arrivalLayerWhitelist = new List<PlanetLayerDef>();
            //    //        }
            //    //        factionDef.arrivalLayerWhitelist.AddDistinct(planetLayerDef);
            //    //    }
            //    //}
            //    //if (!planetLayerDef.onlyAllowWhitelistedArrivalModes)
            //    //{
            //    //    if (!BlacklistArrivalFactionDefs.NullOrEmpty())
            //    //    {
            //    //        foreach (FactionDef factionDef in BlacklistArrivalFactionDefs)
            //    //        {
            //    //            Log.Message($"{planetLayerDef.defName} BlacklistArrivalFactionDefs {factionDef.defName} {factionDef.arrivalLayerBlacklist.Contains(planetLayerDef)}");
            //    //            if (factionDef.arrivalLayerBlacklist == null)
            //    //            {
            //    //                factionDef.arrivalLayerBlacklist = new List<PlanetLayerDef>();
            //    //            }
            //    //            factionDef.arrivalLayerBlacklist.AddDistinct(planetLayerDef);
            //    //        }
            //    //    }
            //    //}
            //    (List<FactionDef> WhitelistFactionDefs, List<FactionDef> BlacklistFactionDefs, bool onlyAllowWhitelistedFactions) = PlanetLayerFactionDefs[planetLayerDef];
            //    if (!WhitelistFactionDefs.NullOrEmpty())
            //    {
            //        foreach (FactionDef factionDef in WhitelistFactionDefs)
            //        {
            //            Log.Message($"{planetLayerDef.defName} WhitelistFactionDefs {factionDef.defName} {factionDef.layerWhitelist.Contains(planetLayerDef)}");
            //            if (factionDef.layerWhitelist == null)
            //            {
            //                factionDef.layerWhitelist = new List<PlanetLayerDef>();
            //            }
            //            factionDef.layerWhitelist.AddDistinct(planetLayerDef);
            //        }
            //    }
            //    if (!(onlyAllowWhitelistedFactions))
            //    {
            //        if (!BlacklistFactionDefs.NullOrEmpty())
            //        {
            //            foreach (FactionDef factionDef in BlacklistFactionDefs)
            //            {
            //                Log.Message($"{planetLayerDef.defName} BlacklistFactionDefs {factionDef.defName} {factionDef.layerBlacklist.Contains(planetLayerDef)}");
            //                if (factionDef.layerBlacklist == null)
            //                {
            //                    factionDef.layerBlacklist = new List<PlanetLayerDef>();
            //                }
            //                factionDef.layerBlacklist.AddDistinct(planetLayerDef);
            //            }
            //        }
            //    }
            //}
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

        public static IEnumerable<CodeInstruction> SC_IncidentChanceFinal_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int countStloc0 = 0;
            int startIndex = -1;
            int endIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_0)
                {
                    countStloc0++;
                    if (countStloc0 == 2)
                    {
                        startIndex = i + 1;
                    }
                    if (countStloc0 == 3 && startIndex > -1)
                    {
                        endIndex = i + 1;
                        List<CodeInstruction> instructionsToInsert = codes.GetRange(startIndex, endIndex - startIndex);
                        instructionsToInsert.RemoveAt(1);
                        instructionsToInsert.Insert(2, new CodeInstruction(OpCodes.Ldarg_2));
                        instructionsToInsert[3] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "IncidentChanceFactor_PlanetLayer"));
                        codes.InsertRange(endIndex, instructionsToInsert);
                        break;
                    }
                }
            }
            return codes.AsEnumerable();
        }

        public static float IncidentChanceFactor_PlanetLayer(IncidentDef def, IIncidentTarget target)
        {
            LayeredAtmosphereOrbitDefModExtension layeredAtmosphereOrbitDefModExtension = target?.Tile.LayerDef?.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (!(layeredAtmosphereOrbitDefModExtension?.IncidentChanceMultipliers.NullOrEmpty() ?? true))
            {
                int index = layeredAtmosphereOrbitDefModExtension.IncidentChanceMultipliers.FindIndex((IncidentChanceMultiplier icm) => icm.incident == def);
                if (index > -1)
                {
                    return layeredAtmosphereOrbitDefModExtension.IncidentChanceMultipliers[index].multiplier;
                }
            }
            return 1f;
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

        public static bool TF_TryFindNewSiteTile_Prefix(ref bool __result, out PlanetTile tile, PlanetTile nearTile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, List<LandmarkDef> allowedLandmarks = null, float selectLandmarkChance = 0.5f, bool canSelectComboLandmarks = true, TileFinderMode tileFinderMode = TileFinderMode.Near, bool exitOnFirstTileFound = false, bool canBeSpace = false, PlanetLayer layer = null, Predicate<PlanetTile> validator = null)
        {
            bool flag = ModsConfig.OdysseyActive && Rand.ChanceSeeded(selectLandmarkChance, Gen.HashCombineInt(Find.TickManager.TicksGame, 18271));
            if (!nearTile.Valid && !TileFinder.TryFindRandomPlayerTile(out nearTile, allowCaravans, null, canBeSpace: true))
            {
                tile = PlanetTile.Invalid;
                __result = false;
                return false;
            }
            if (layer == null)
            {
                layer = nearTile.Layer;
            }
            if (!canBeSpace)
            {
                bool isRequireChangeToSurface = layer.Def.isSpace;
                if (!isRequireChangeToSurface && QuestGen.slate != null)
                {
                    PlanetLayerGroupDef planetLayerGroupDef = layer.Def.LayerGroup();
                    LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerGroupDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                    if ((laoDefModExtension?.isPreventQuestMapIfNotWhitelisted ?? false))
                    {
                        isRequireChangeToSurface = true;
                        List<PlanetLayerGroupDef> planetLayerGroupDefs = QuestGen.slate.Get<List<PlanetLayerGroupDef>>("layerGroupWhitelist");
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
                    if (!Find.WorldGrid.TryGetFirstAdjacentLayerOfDef(nearTile, PlanetLayerDefOf.Surface, out layer))
                    {
                        Find.WorldGrid.TryGetFirstLayerOfDef(DefOfLocal.LAO_Surface.ContainedLayers().RandomElement(), out layer);
                    }
                }
            }
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
            if (list.Empty())
            {
                if (TileFinder.TryFillFindTile(layer.GetClosestTile_NewTemp(nearTile), out tile, minDist, maxDist, allowedLandmarks, canSelectComboLandmarks, tileFinderMode, exitOnFirstTileFound, validator, flag))
                {
                    __result = true;
                    return false;
                }
                tile = PlanetTile.Invalid;
                return false;
            }
            tile = list.RandomElement();
            __result = true;
            return false;
        }

        public static bool PL_DirectConnectionTo_Prefix(ref bool __result, PlanetLayer __instance, PlanetLayer other)
        {
            PlanetLayerGroupDef planetLayerGroupDef = __instance.Def.LayerGroup();
            if (planetLayerGroupDef != null)
            {
                __result = planetLayerGroupDef.ContainsLayer(other.Def) || planetLayerGroupDef.planetLayerGroupsDirectConnection.Any((PlanetLayerGroupDef plgd) => plgd.ContainsLayer(other.Def));
                return false;
            }
            return true;
        }

        //Planet

        public static bool PL_Visible_Prefix(ref bool __result, PlanetLayer __instance)
        {
            if (__instance.Def.Planet() != GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static void PL_Raycastable_Postfix(ref bool __result, PlanetLayer __instance)
        {
            __result = __result && __instance.Def.Planet() == GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef;
        }

        public static void WDL_Visible_Postfix(ref bool __result, WorldDrawLayer __instance)
        {
            __result = __result && __instance.planetLayer.Def.Planet() == GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef;
        }

        public static void WDL_Raycastable_Postfix(ref bool __result, WorldDrawLayer __instance)
        {
            __result = __result && __instance.planetLayer.Def.Planet() == GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef;
        }

        public static void WS_SelectedLayer_Postfix(WorldSelector __instance, PlanetLayer value)
        {
            GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef = value.Def.Planet();
        }

        public static void M_FinalizeInit_Postfix(Map __instance)
        {
            List<GameConditionDef> permamentGameConditionDefs = __instance.Tile.LayerDef.Planet()?.permamentGameConditionDefs;
            foreach (GameConditionDef gameConditionDef in permamentGameConditionDefs)
            {
                __instance.GameConditionManager.RegisterCondition(GameConditionMaker.MakeConditionPermanent(gameConditionDef));
            }
        }

        public static void CJ_TryHideWorld_Postfix()
        {
            PlanetLayer planetLayer = Find.CurrentMap?.Tile.Layer;
            if (planetLayer != null && GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef != planetLayer.Def.Planet())
            {
                Find.WorldSelector.SelectedLayer = planetLayer;
            }
        }

        public static void PSSS_PreOpen_Postfix()
        {
            GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef = Find.GameInitData.startingTile.LayerDef.Planet();
        }

        //Gravship Route

        public static bool GU_TravelTo_Prefix(Gravship gravship, PlanetTile oldTile, PlanetTile newTile)
        {
            if (ModsConfig.OdysseyActive)
            {
                gravship.SetFaction(gravship.Engine.Faction);
                gravship.Tile = oldTile;
                gravship.destinationTile = newTile;
                GravshipRoute route = new GravshipRoute();
                List<PlanetLayerConnection> path = new List<PlanetLayerConnection>();
                if (oldTile.Layer != newTile.Layer && PlanetLayer.TryGetPath(oldTile.Layer, newTile.Layer, path, out _))
                {
                    Vector3 oldTileVerticalTrajectory = Find.WorldGrid.GetTileCenter(oldTile).normalized;
                    for (int i = 0; i < path.Count; i++)
                    {
                        PlanetLayerConnection planetLayerConnection = path[i];
                        PlanetDef targetPlanet = planetLayerConnection.target.Def.Planet();
                        PlanetDef originPlanet = planetLayerConnection.origin.Def.Planet();
                        bool isInterplanetary = targetPlanet != null && originPlanet != null && targetPlanet != originPlanet;
                        if (isInterplanetary)
                        {
                            route.AddRoutePoint(planetLayerConnection.origin.Radius * oldTileVerticalTrajectory, planetLayerConnection.origin.Def);
                            route.AddInterplanetaryJumpPoint(originPlanet.gravityWellRadius * oldTileVerticalTrajectory, planetLayerConnection.origin.Def);
                            route.AddRoutePoint(targetPlanet.gravityWellRadius * oldTileVerticalTrajectory, planetLayerConnection.target.Def);
                            route.AddRoutePoint(planetLayerConnection.target.Radius * oldTileVerticalTrajectory, planetLayerConnection.target.Def);
                        }
                        else
                        {
                            PlanetLayer planetLayer = planetLayerConnection.target;
                            if (planetLayerConnection.target.Def.Elevation() > planetLayerConnection.origin.Def.Elevation())
                            {
                                planetLayer = planetLayerConnection.origin;
                            }
                            route.AddRoutePoint(planetLayerConnection.origin.Radius * oldTileVerticalTrajectory, planetLayer.Def);
                            route.AddRoutePoint(planetLayerConnection.origin.Radius * oldTileVerticalTrajectory, planetLayer.Def);
                        }
                    }
                    route.AddRoutePoint(newTile.Layer.Radius * oldTileVerticalTrajectory, newTile.LayerDef);
                    route.AddRoutePoint(Find.WorldGrid.GetTileCenter(newTile), newTile.LayerDef);
                }
                else
                {
                    route.AddRoutePoint(Find.WorldGrid.GetTileCenter(oldTile), oldTile.LayerDef);
                    if (oldTile.Layer != newTile.Layer)
                    {
                        route.AddRoutePoint(Find.WorldGrid.GetTileCenter(gravship.Tile.Layer.GetClosestTile_NewTemp(gravship.Tile)), gravship.Tile.LayerDef);
                    }
                    route.AddRoutePoint(Find.WorldGrid.GetTileCenter(newTile), newTile.LayerDef);
                }
                GameComponent_LayeredAtmosphereOrbit.instance.gravshipRoutes.SetOrAdd(gravship, route);
                Find.WorldObjects.Add(gravship);
                CameraJumper.TryJump(gravship);
            }
            return false;
        }

        public static bool G_DrawPos_Prefix(ref Vector3 __result, Gravship __instance, float ___traveledPct)
        {
            if (GameComponent_LayeredAtmosphereOrbit.instance.gravshipRoutes.TryGetValue(__instance, out GravshipRoute gravshipRoute))
            {
                __result = gravshipRoute.Evaluate(___traveledPct, out PlanetLayerDef planetLayerDef);
                if (__instance.Tile.LayerDef != planetLayerDef && Find.WorldGrid.TryGetFirstLayerOfDef(planetLayerDef, out PlanetLayer layer))
                {
                    bool isLooking = Find.WorldSelector.SelectedLayer == __instance.Tile.Layer && WorldRendererUtility.WorldSelected && new Rect(0f, 0f, UI.screenWidth, UI.screenHeight).Contains(GenWorldUI.WorldToUIPosition(__instance.Tile.Layer.Origin + Find.WorldGrid.GetTileCenter(__instance.Tile)));
                    if (__instance.Tile.LayerDef.Planet() != planetLayerDef.Planet())
                    {
                        Messages.Message("LayeredAtmosphereOrbit.Gravship.GravityJumpPerformed".Translate(__instance.LabelCap, __instance.Tile.LayerDef.Planet().LabelCap, planetLayerDef.Planet().LabelCap).RawText, __instance, MessageTypeDefOf.TaskCompletion);
                    }
                    __instance.Tile = layer.GetClosestTile_NewTemp(__instance.initialTile);
                    if (isLooking)
                    {
                        if (Find.WorldSelector.IsSelected(__instance))
                        {
                            CameraJumper.TryJumpAndSelect(__instance);
                        }
                        else
                        {
                            Find.WorldSelector.SelectedLayer = __instance.Tile.Layer;
                        }
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void GU_ArriveExistingMap_Postfix(Gravship gravship)
        {
            GameComponent_LayeredAtmosphereOrbit.instance.gravshipRoutes.Remove(gravship);
        }

        public static bool G_TraveledPctStepPerTick_Prefix(ref float __result, Gravship __instance, float ___traveledPct)
        {
            if (GameComponent_LayeredAtmosphereOrbit.instance.gravshipRoutes.TryGetValue(__instance, out GravshipRoute gravshipRoute))
            {
                gravshipRoute.TryCache();
                __result = 0.00025f / gravshipRoute.routeLength;
                if (__result <= 0)
                {
                    __result = 1;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
