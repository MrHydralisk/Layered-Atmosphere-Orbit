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
            val.Patch(AccessTools.Method(typeof(PawnApparelGenerator), "NeedVacuumResistance"), transpiler: new HarmonyMethod(patchType, "PAG_NeedVacuumResistance_Transpiler"));
            //val.Patch(AccessTools.Method(typeof(TemperatureVacuumSaveLoad), "DoExposeWork"), transpiler: new HarmonyMethod(patchType, "TVSL_DoExposeWork_Transpiler"));
            //val.Patch(AccessTools.Property(typeof(Room), "Vacuum").GetGetMethod(), transpiler: new HarmonyMethod(patchType, "Room_Vacuum_Transpiler"));
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
                val.Patch(AccessTools.Property(typeof(Game), "CurrentMap").GetSetMethod(), postfix: new HarmonyMethod(patchType, "G_CurrentMap_Postfix"));
                val.Patch(AccessTools.Method(typeof(CameraJumper), "TryHideWorld"), postfix: new HarmonyMethod(patchType, "CJ_TryHideWorld_Postfix"));
                val.Patch(AccessTools.Method(typeof(WorldInterface), "Reset"), postfix: new HarmonyMethod(patchType, "WI_Reset_Postfix"));
                val.Patch(AccessTools.Method(typeof(Page_SelectStartingSite), "PreOpen"), postfix: new HarmonyMethod(patchType, "PSSS_PreOpen_Postfix"));
                val.Patch(AccessTools.Property(typeof(Tile), "OnSurface").GetGetMethod(), postfix: new HarmonyMethod(patchType, "T_OnSurface_Postfix"));
                val.Patch(AccessTools.Property(typeof(WITab_Terrain), "IsVisible").GetGetMethod(), prefix: new HarmonyMethod(patchType, "WITT_IsVisible_Prefix"));
                val.Patch(AccessTools.Property(typeof(WITab_Planet), "IsVisible").GetGetMethod(), prefix: new HarmonyMethod(patchType, "WITT_IsVisible_Prefix"));
                val.Patch(AccessTools.Property(typeof(WITab_Orbit), "IsVisible").GetGetMethod(), prefix: new HarmonyMethod(patchType, "WITT_IsVisible_Prefix"));
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
                if (generatedLocationDef.LayerDefs.Any((PlanetLayerDef pld) => pld.LayerGroup() != null))
                {
                    if (generatedLocationDef.worldObjectDef == null)
                    {
                        continue;
                    }
                    if (generatedLocationDef.worldObjectDef.inspectorTabsResolved == null)
                    {
                        generatedLocationDef.worldObjectDef.inspectorTabsResolved = new List<InspectTabBase>() { (InspectTabBase)Activator.CreateInstance(typeof(WITab_PlanetLayer)) };
                    }
                    else if(!generatedLocationDef.worldObjectDef.inspectorTabsResolved.Any((InspectTabBase itb) => itb is WITab_PlanetLayer))
                    {
                        generatedLocationDef.worldObjectDef.inspectorTabsResolved.Add((InspectTabBase)Activator.CreateInstance(typeof(WITab_PlanetLayer)));
                    }
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

            //Log.Message($"Allow IncidentDef A:\n{string.Join("\n", AllIncidentDefs.Select((id) => $"   {id.defName} {id.canOccurOnAllPlanetLayers}\n{string.Join("\n", id.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", id.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestIncidentDefOnLayerDef(id)} {pld.onlyAllowWhitelistedIncidents}"))}"))}");
            foreach (IncidentDef incidentDef in AllIncidentDefs)
            {
                List<PlanetLayerDef> AllowInIncidentDef = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInIncidentDef = new List<PlanetLayerDef>();
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    if (incidentDef.layerWhitelist.NullOrEmpty())
                    {
                        if (incidentDef.canOccurOnAllPlanetLayers)
                        {
                            AllowInIncidentDef.AddUnique(planetLayerDef);
                            continue;
                        }
                        if (planetLayerDef.onlyAllowWhitelistedIncidents)
                        {
                            ForbidInIncidentDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            AllowInIncidentDef.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (incidentDef.layerWhitelist.Contains(planetLayerDef))
                        {
                            AllowInIncidentDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            ForbidInIncidentDef.AddUnique(planetLayerDef);
                        }
                    }
                }
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInIncidentDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInIncidentDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (incidentDef.layerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInIncidentDef.AddRangeUnique(incidentDef.layerBlacklist);
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInIncidentDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInIncidentDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistIncidentDef?.Contains(incidentDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistIncidentDef?.Contains(incidentDef) ?? false;
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistIncidentDef?.Contains(incidentDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistIncidentDef?.Contains(incidentDef) ?? false);
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInIncidentDef.AddDistinct(planetLayerDef);
                        int index = ForbidInIncidentDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInIncidentDef.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInIncidentDef.AddDistinct(planetLayerDef);
                        int index = AllowInIncidentDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInIncidentDef.RemoveAt(index);
                        }
                    }
                }
                incidentDef.layerWhitelist = AllowInIncidentDef;
                incidentDef.layerBlacklist = ForbidInIncidentDef;
            }
            //Log.Message($"Allow IncidentDef B:\n{string.Join("\n", AllIncidentDefs.Select((id) => $"   {id.defName} {id.canOccurOnAllPlanetLayers}\n{string.Join("\n", id.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", id.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestIncidentDefOnLayerDef(id)} {pld.onlyAllowWhitelistedIncidents}"))}"))}");
            //Log.Message($"Allow BiomeDef A:\n{string.Join("\n", AllBiomeDefs.Select((bd) => $"   {bd.defName}\n{string.Join("\n", bd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", bd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestBiomeDefOnLayerDef(bd)} {pld.onlyAllowWhitelistedBiomes}"))}"))}");
            foreach (BiomeDef biomeDef in AllBiomeDefs)
            {
                List<PlanetLayerDef> AllowInBiomeDef = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInBiomeDef = new List<PlanetLayerDef>();
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    if (biomeDef.layerWhitelist.NullOrEmpty())
                    {
                        if (planetLayerDef.onlyAllowWhitelistedBiomes)
                        {
                            ForbidInBiomeDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            AllowInBiomeDef.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (biomeDef.layerWhitelist.Contains(planetLayerDef))
                        {
                            AllowInBiomeDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            ForbidInBiomeDef.AddUnique(planetLayerDef);
                        }
                    }
                }
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInBiomeDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInBiomeDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (biomeDef.layerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInBiomeDef.AddRangeUnique(biomeDef.layerBlacklist);
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInBiomeDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInBiomeDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistBiomeDef?.Contains(biomeDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistBiomeDef?.Contains(biomeDef) ?? false;
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistBiomeDef?.Contains(biomeDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistBiomeDef?.Contains(biomeDef) ?? false);
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInBiomeDef.AddDistinct(planetLayerDef);
                        int index = ForbidInBiomeDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInBiomeDef.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInBiomeDef.AddDistinct(planetLayerDef);
                        int index = AllowInBiomeDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInBiomeDef.RemoveAt(index);
                        }
                    }
                }
                biomeDef.layerWhitelist = AllowInBiomeDef;
                biomeDef.layerBlacklist = ForbidInBiomeDef;
            }
            //Log.Message($"Allow BiomeDef B:\n{string.Join("\n", AllBiomeDefs.Select((bd) => $"   {bd.defName}\n{string.Join("\n", bd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", bd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestBiomeDefOnLayerDef(bd)} {pld.onlyAllowWhitelistedBiomes}"))}"))}");
            //Log.Message($"Allow GameConditionDef A:\n{string.Join("\n", AllGameConditionDefs.Select((gcd) => $"   {gcd.defName} {gcd.canAffectAllPlanetLayers}\n{string.Join("\n", gcd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", gcd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestGameConditionDefOnLayerDef(gcd)} {pld.onlyAllowWhitelistedGameConditions}"))}"))}");
            foreach (GameConditionDef gameConditionDef in AllGameConditionDefs)
            {
                List<PlanetLayerDef> AllowInGameConditionDef = new List<PlanetLayerDef>();
                List<PlanetLayerDef> ForbidInGameConditionDef = new List<PlanetLayerDef>();
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    if (gameConditionDef.canAffectAllPlanetLayers)
                    {
                        AllowInGameConditionDef.AddUnique(planetLayerDef);
                        continue;
                    }
                    if (gameConditionDef.layerWhitelist.NullOrEmpty())
                    {
                        if (planetLayerDef.onlyAllowWhitelistedGameConditions)
                        {
                            ForbidInGameConditionDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            AllowInGameConditionDef.AddUnique(planetLayerDef);
                        }
                    }
                    else
                    {
                        if (gameConditionDef.layerWhitelist.Contains(planetLayerDef))
                        {
                            AllowInGameConditionDef.AddUnique(planetLayerDef);
                        }
                        else
                        {
                            ForbidInGameConditionDef.AddUnique(planetLayerDef);
                        }
                    }
                }
                //Log.Message($"|||1 {factionDef.defName}:\n{string.Join("\n", AllowInGameConditionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInGameConditionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                if (gameConditionDef.layerBlacklist.NullOrEmpty())
                {

                }
                else
                {
                    ForbidInGameConditionDef.AddRangeUnique(gameConditionDef.layerBlacklist);
                }
                //Log.Message($"|||2 {factionDef.defName}:\n{string.Join("\n", AllowInGameConditionDefs.Select((pld) => $"      +{pld.defName}"))}\n\\---/\n{string.Join("\n", ForbidInGameConditionDefs.Select((pld) => $"      -{pld.defName}"))}\n");
                foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
                {
                    bool isAdditionallyWhitelist = false;
                    bool isAdditionallyBlacklist = false;
                    LayeredAtmosphereOrbitDefModExtension LayerGroupDefModExtension = PlanetLayerDefMods[planetLayerDef].Item2;
                    if (LayerGroupDefModExtension != null)
                    {
                        isAdditionallyWhitelist = LayerGroupDefModExtension.WhitelistGameConditionDef?.Contains(gameConditionDef) ?? false;
                        isAdditionallyBlacklist = LayerGroupDefModExtension.BlacklistGameConditionDef?.Contains(gameConditionDef) ?? false;
                    }
                    LayeredAtmosphereOrbitDefModExtension LayerDefModExtension = PlanetLayerDefMods[planetLayerDef].Item1;
                    if (LayerDefModExtension != null)
                    {
                        isAdditionallyWhitelist = isAdditionallyWhitelist || (LayerDefModExtension.WhitelistGameConditionDef?.Contains(gameConditionDef) ?? false);
                        isAdditionallyBlacklist = isAdditionallyBlacklist || (LayerDefModExtension.BlacklistGameConditionDef?.Contains(gameConditionDef) ?? false);
                    }
                    if (isAdditionallyWhitelist)
                    {
                        AllowInGameConditionDef.AddDistinct(planetLayerDef);
                        int index = ForbidInGameConditionDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            ForbidInGameConditionDef.RemoveAt(index);
                        }
                    }
                    if (isAdditionallyBlacklist)
                    {
                        ForbidInGameConditionDef.AddDistinct(planetLayerDef);
                        int index = AllowInGameConditionDef.IndexOf(planetLayerDef);
                        if (index > -1)
                        {
                            AllowInGameConditionDef.RemoveAt(index);
                        }
                    }
                }
                gameConditionDef.layerWhitelist = AllowInGameConditionDef;
                gameConditionDef.layerBlacklist = ForbidInGameConditionDef;
            }
            //Log.Message($"Allow GameConditionDef B:\n{string.Join("\n", AllGameConditionDefs.Select((gcd) => $"   {gcd.defName} {gcd.canAffectAllPlanetLayers}\n{string.Join("\n", gcd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", gcd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestGameConditionDefOnLayerDef(gcd)} {pld.onlyAllowWhitelistedGameConditions}"))}"))}");
            //Log.Message($"Allow QuestScriptDef A:\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"   {qsd.defName} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}\n{string.Join("\n", qsd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", qsd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestQuestScriptDefOnLayerDef(qsd)}"))}"))}");
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
            //Log.Message($"Allow QuestScriptDef B:\n{string.Join("\n", AllQuestScriptDefs.Select((qsd) => $"   {qsd.defName} {qsd.everAcceptableInSpace} {qsd.neverPossibleInSpace}\n{string.Join("\n", qsd.layerWhitelist?.Select((pld) => $"      +{pld.defName}") ?? new List<string>() { "---" })}\n\\---/\n{string.Join("\n", qsd.layerBlacklist?.Select((pld) => $"      -{pld.defName}") ?? new List<string>() { "---" })}\n/---\\\n{string.Join("\n", AllPlanetLayerDefs.Select((pld) => $"      {pld.defName} {pld.TestQuestScriptDefOnLayerDef(qsd)}"))}"))}");
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

        public static IEnumerable<CodeInstruction> PAG_NeedVacuumResistance_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int startIndex = -1;
            int endIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse_S)
                {
                    startIndex = i;
                }
                if (codes[i].opcode == OpCodes.Brtrue_S && startIndex > -1)
                {
                    endIndex = i;
                    break;
                }
            }
            if (startIndex > -1 && endIndex > -1)
            {
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, (Label)codes[startIndex].operand));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "NeedVacuumResistance_PlanetLayer")));
                codes.InsertRange(endIndex, instructionsToInsert);
            }
            return codes.AsEnumerable();
        }

        public static bool NeedVacuumResistance_PlanetLayer(PawnGenerationRequest request)
        {
            return request.Tile.LayerDef.Vacuum(request.Tile.Tile?.PrimaryBiome) >= 0.5f;
        }

        //public static IEnumerable<CodeInstruction> TVSL_DoExposeWork_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        //{
        //    List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Call && (codes[i].operand?.ToString().Contains("VacuumFloatToByte") ?? false))
        //        {
        //            List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TemperatureVacuumSaveLoad), "map")));
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "Vacuum_DoExposeWork_PlanetLayer")));
        //            codes.InsertRange(i, instructionsToInsert);
        //            codes.RemoveAt(i - 1);
        //            break;
        //        }
        //    }
        //    return codes.AsEnumerable();
        //}

        //public static float Vacuum_DoExposeWork_PlanetLayer(Map map)
        //{
        //    float vacuum = 1;
        //    if (!LayeredAtmosphereOrbitUtility.mapVacuum.TryGetValue(map, out vacuum))
        //    {
        //        vacuum = map.Tile.LayerDef.Vacuum(map.Biome);
        //        LayeredAtmosphereOrbitUtility.mapVacuum.Add(map, vacuum);
        //    }
        //    return vacuum;
        //}

        //public static IEnumerable<CodeInstruction> Room_Vacuum_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        //{
        //    List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Stfld && (codes[i].operand?.ToString().Contains("vacuum") ?? false))
        //        {
        //            List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Room), "Map").GetGetMethod()));
        //            instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "Vacuum_DoExposeWork_PlanetLayer")));
        //            codes.InsertRange(i - 2, instructionsToInsert);
        //            codes.RemoveAt(i - 3);
        //            break;
        //        }
        //    }
        //    return codes.AsEnumerable();
        //}

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

        public static void G_CurrentMap_Postfix(Map value)
        {
            PlanetLayer planetLayer = value?.Tile.Layer;
            if (planetLayer != null && GameComponent_LayeredAtmosphereOrbit.instance.currentPlanetDef != planetLayer.Def.Planet())
            {
                Find.WorldSelector.SelectedLayer = planetLayer;
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

        public static void WI_Reset_Postfix()
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

        public static void T_OnSurface_Postfix(ref bool __result, Tile __instance)
        {
            __result = __result ||( __instance.Layer.Def.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.isSurface ?? false);
        }

        public static bool WITT_IsVisible_Prefix(ref bool __result)
        {
            __result = !LAOMod.Settings.HideWorldTabs;
            return __result;
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
