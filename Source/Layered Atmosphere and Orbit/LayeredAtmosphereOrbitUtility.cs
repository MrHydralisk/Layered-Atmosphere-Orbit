using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public static class LayeredAtmosphereOrbitUtility
    {
        public static List<ScenPart_PlanetLayer> planetLayersLAO;
        public static Dictionary<PlanetLayerGroupDef, List<PlanetLayerDef>> planetLayerGroups;
        public static Dictionary<PlanetDef, List<PlanetLayerDef>> planets;
        public static Dictionary<Map, float> mapVacuum = new Dictionary<Map, float>();

        public static void ResetLayerData()
        {
            planetLayerGroups = new Dictionary<PlanetLayerGroupDef, List<PlanetLayerDef>>();
            planets = new Dictionary<PlanetDef, List<PlanetLayerDef>>();
            List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
            foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            {
                PlanetLayerGroupDef planetLayerGroupDef = planetLayerDef.LayerGroup();
                if (planetLayerGroupDef != null)
                {
                    if (planetLayerGroups.TryGetValue(planetLayerGroupDef, out List<PlanetLayerDef> subPlanetLayers))
                    {
                        subPlanetLayers.Add(planetLayerDef);
                    }
                    else
                    {
                        planetLayerGroups.Add(planetLayerGroupDef, new List<PlanetLayerDef>() { planetLayerDef });
                    }
                }
                PlanetDef planetDef = planetLayerDef.Planet();
                if (planetDef != null)
                {
                    if (planets.TryGetValue(planetDef, out List<PlanetLayerDef> subPlanetLayers))
                    {
                        subPlanetLayers.Add(planetLayerDef);
                    }
                    else
                    {
                        planets.Add(planetDef, new List<PlanetLayerDef>() { planetLayerDef });
                    }
                }
            }
            //Log.Message($"All layer groupds:\n{string.Join("\n", planetLayerGroups.SelectMany(x => x.Value.Select(y => $"- {x.Key.label} > {y.label}")))}");
            planetLayersLAO = new List<ScenPart_PlanetLayer>();
            foreach (string defName in LAOMod.Settings.AutoAddLayersDefNames)
            {
                ScenPart_PlanetLayerFixed scenPart_LAOPlanetLayer = new ScenPart_PlanetLayerFixed();
                scenPart_LAOPlanetLayer.def = DefDatabase<ScenPartDef>.GetNamed(defName, false);
                scenPart_LAOPlanetLayer.tag = defName;
                scenPart_LAOPlanetLayer.layer = DefDatabase<PlanetLayerDef>.GetNamed(defName, false);
                scenPart_LAOPlanetLayer.settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamed(defName, false);
                scenPart_LAOPlanetLayer.hide = true;
                List<string> nullDefs = new List<string>();
                if (scenPart_LAOPlanetLayer.def == null)
                {
                    nullDefs.Add("ScenPartDef");
                }
                if (scenPart_LAOPlanetLayer.layer == null)
                {
                    nullDefs.Add("PlanetLayerDef");
                }
                if (scenPart_LAOPlanetLayer.settingsDef == null)
                {
                    nullDefs.Add("PlanetLayerSettingsDef");
                }
                if (nullDefs.Empty())
                {
                    planetLayersLAO.Add(scenPart_LAOPlanetLayer);
                }
                else if (nullDefs.Count == 3)
                {
                    Log.Error($"Couldn't find defName \"{defName}\" for any ScenPartDef, PlanetLayerDef or PlanetLayerSettingsDef. Seems like related mod was uninstalled, then disable it in LAO mod settings.");
                }
                else
                {
                    Log.Error($"Couldn't find defName \"{defName}\" for {string.Join(", ", nullDefs)}.Seems like there is an issue in defNames, so report to developer.");
                }
            }
        }

        public static void TryAddPlanetLayerts(Scenario scenario)
        {
            List<ScenPart> parts = scenario.parts;
            foreach (ScenPart_PlanetLayer sppl in planetLayersLAO)
            {
                if (!parts.Any((ScenPart sp) => sp.def == sppl.def))
                {
                    parts.Add(sppl);
                }
            }
            scenario.parts = parts;
            List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)> planetLayers = new List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)>();
            foreach (ScenPart scenPart in scenario.AllParts)
            {
                if (scenPart is ScenPart_PlanetLayer scenPart_PlanetLayer)
                {
                    LayeredAtmosphereOrbitDefModExtension laoDefModExtension = scenPart_PlanetLayer.layer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                    if (laoDefModExtension?.planetLayerGroup == null)
                    {
                        Log.Error($"Planet Layer {scenPart_PlanetLayer?.layer?.defName ?? "---"} missing proper LayeredAtmosphereOrbitDefModExtension. Can continue playing without issues, but ask developer to add it with patch. Layer is from mod {scenPart_PlanetLayer?.layer?.modContentPack?.Name ?? "---"} [{scenPart_PlanetLayer?.layer?.modContentPack?.PackageId ?? "---"}].");
                    }
                    planetLayers.Add((scenPart_PlanetLayer, laoDefModExtension));
                }
            }
            //Log.Message($"All layer connections Before:\n{string.Join("\n", planetLayers.SelectMany(x => x.Item1.connections.Select(y => $"- {x.Item1.layer.defName} > {y.tag} = {y.zoomMode}")))}");
            planetLayers.SortBy((spLayer) => spLayer.Item2?.elevation ?? 200);
            for (int i = 0; i < planetLayers.Count; i++)
            {
                (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerFrom = planetLayers[i];
                for (int j = spPlanetLayerFrom.Item1.connections.Count - 1; j > -1; j--)
                {
                    LayerConnection layerConnection = spPlanetLayerFrom.Item1.connections[j];
                    (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerTo = planetLayers.FirstOrDefault(pl => pl.Item1.tag == layerConnection.tag);
                    if ((spPlanetLayerFrom.Item2?.isReplaceConnections ?? false) && (spPlanetLayerTo.Item2?.isReplaceConnections ?? false))
                    {
                        //Log.Message($"A Remove {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} = {layerConnection.zoomMode}");
                        spPlanetLayerFrom.Item1.connections.RemoveAt(j);
                    }
                }
                List<PlanetLayerDef> forcedConnectToPlanetLayer = spPlanetLayerFrom.Item2?.forcedConnectToPlanetLayer;
                if (!forcedConnectToPlanetLayer.NullOrEmpty())
                {
                    for (int j = 0; j < forcedConnectToPlanetLayer.Count; j++)
                    {
                        PlanetLayerDef planetLayerDef = forcedConnectToPlanetLayer[j];
                        if (!spPlanetLayerFrom.Item1.connections.Any((LayerConnection lc) => lc.tag == planetLayerDef.defName))
                        {
                            //Log.Message($"A Add Forced {spPlanetLayerFrom.Item1.layer.defName} > {planetLayerDef.defName} = {LayerConnection.ZoomMode.ZoomOut}");
                            spPlanetLayerFrom.Item1.connections.Add(new LayerConnection() { tag = planetLayerDef.defName, zoomMode = (spPlanetLayerFrom.Item1.layer.DistanceToReachPlanetLayer(planetLayerDef) < 0 ? LayerConnection.ZoomMode.ZoomIn : LayerConnection.ZoomMode.ZoomOut) });
                        }
                    }
                }
            }
            List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)> planetLayersKnown = planetLayers.Where(sppl => sppl.Item2?.planetLayerGroup != null).ToList();
            //Log.Message($"B planetLayersKnown:\n{string.Join("\n", planetLayersKnown.Select(x => x.Item1.layer.defName))}");
            List<(PlanetDef, ScenPart_PlanetLayer)> planetWells = new List<(PlanetDef, ScenPart_PlanetLayer)>();
            foreach (PlanetDef planetDef in planets.Keys)
            {
                List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)> planetKnown = planetLayersKnown.Where(sppl => sppl.Item2?.planetLayerGroup?.planet == planetDef).ToList();
                //Log.Message($"B planetKnown {planetDef.defName}:\n{string.Join("\n", planetKnown.Select(x => x.Item1.layer.defName))}");
                for (int i = 1; i < planetKnown.Count; i++)
                {
                    (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerFrom = planetKnown[i - 1];
                    (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerTo = planetKnown[i];
                    //Log.Message($"B Check {planetDef.defName} {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} =\n{string.Join("\n", spPlanetLayerFrom.Item1.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}\n>\n{string.Join("\n", spPlanetLayerTo.Item1.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}\n\\---/");
                    if (!spPlanetLayerFrom.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerTo.Item1.tag))
                    {
                        //Log.Message($"B Add {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} = {LayerConnection.ZoomMode.ZoomOut}");
                        spPlanetLayerFrom.Item1.connections.Add(new LayerConnection() { tag = spPlanetLayerTo.Item1.tag, zoomMode = LayerConnection.ZoomMode.ZoomOut });
                    }
                    if (!spPlanetLayerTo.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerFrom.Item1.tag))
                    {
                        //Log.Message($"B Add {spPlanetLayerTo.Item1.layer.defName} > {spPlanetLayerFrom.Item1.layer.defName} = {LayerConnection.ZoomMode.ZoomIn}");
                        spPlanetLayerTo.Item1.connections.Add(new LayerConnection() { tag = spPlanetLayerFrom.Item1.tag, zoomMode = LayerConnection.ZoomMode.ZoomIn });
                    }

                }
                ScenPart_PlanetLayer scenPart_PlanetLayer = null;
                float elevationDiff = float.MaxValue;
                for (int i = 0; i < planetKnown.Count; i++)
                {
                    (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayer = planetKnown[i];
                    if (spPlanetLayer.Item1.layer.Planet() != planetDef)
                    {
                        continue;
                    }
                    //Log.Message($"{planetDef.defName} {spPlanetLayer.Item1.layer.defName}: {planetDef.gravityWellExitElevation} - {spPlanetLayer.Item2.elevation} < {elevationDiff}");
                    if (planetDef.gravityWellExitElevation - spPlanetLayer.Item2.elevation < elevationDiff)
                    {
                        elevationDiff = planetDef.gravityWellExitElevation - spPlanetLayer.Item2.elevation;
                        scenPart_PlanetLayer = spPlanetLayer.Item1;
                    }
                }
                if (scenPart_PlanetLayer == null)
                {
                    continue;
                }
                planetWells.Add((planetDef, scenPart_PlanetLayer));
            }
            for (int i = 1; i < planetWells.Count; i++)
            {
                (PlanetDef, ScenPart_PlanetLayer) spPlanetFrom = planetWells[i - 1];
                (PlanetDef, ScenPart_PlanetLayer) spPlanetTo = planetWells[i];
                float fuelBetweenPlanets = 0;
                if (LAOMod.Settings.KmPerFuelSpace > -1)
                {
                    fuelBetweenPlanets = Vector3.Distance(spPlanetTo.Item1.posFromRimworld, spPlanetFrom.Item1.posFromRimworld) / LAOMod.Settings.KmPerFuelSpace;
                }
                //Log.Message($"{spPlanetFrom.Item1.defName} > {spPlanetTo.Item1.defName}: {fuelBetweenPlanets} = {Vector3.Distance(spPlanetTo.Item1.posFromRimworld, spPlanetFrom.Item1.posFromRimworld)} ({spPlanetTo.Item1.posFromRimworld}, {spPlanetFrom.Item1.posFromRimworld}) / {LAOMod.Settings.KmPerFuelSpace}");
                //Log.Message($"C Check {spPlanetFrom.Item2.layer.defName} > {spPlanetTo.Item2.layer.defName} =\n{string.Join("\n", spPlanetFrom.Item2.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}\n>\n{string.Join("\n", spPlanetTo.Item2.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}");
                if (!spPlanetFrom.Item2.connections.Any((LayerConnection lc) => lc.tag == spPlanetTo.Item2.tag))
                {
                    LayerConnection layerConnection = new LayerConnection() { tag = spPlanetTo.Item2.tag, zoomMode = LayerConnection.ZoomMode.None };
                    if (LAOMod.Settings.UseFuelCostBetweenLayers)
                    {
                        layerConnection.fuelCost = fuelBetweenPlanets + (spPlanetFrom.Item1.gravityWellExitElevation - spPlanetFrom.Item2.layer.Elevation()) * LAOMod.Settings.FuelPerKm;
                        //Log.Message($"{layerConnection.fuelCost} = {fuelBetweenPlanets} + {spPlanetFrom.Item1.gravityWellExitElevation - spPlanetFrom.Item2.layer.Elevation()}({spPlanetFrom.Item1.gravityWellExitElevation} - {spPlanetFrom.Item2.layer.Elevation()}) * {LAOMod.Settings.FuelPerKm}");
                    }
                    //Log.Message($"C Add {spPlanetFrom.Item2.layer.defName} > {spPlanetTo.Item2.layer.defName} = {LayerConnection.ZoomMode.ZoomOut} [{layerConnection.fuelCost}]");
                    spPlanetFrom.Item2.connections.Add(layerConnection);
                }
                if (!spPlanetTo.Item2.connections.Any((LayerConnection lc) => lc.tag == spPlanetFrom.Item2.tag))
                {
                    LayerConnection layerConnection = new LayerConnection() { tag = spPlanetFrom.Item2.tag, zoomMode = LayerConnection.ZoomMode.None };
                    if (LAOMod.Settings.UseFuelCostBetweenLayers)
                    {
                        layerConnection.fuelCost = fuelBetweenPlanets + (spPlanetTo.Item1.gravityWellExitElevation - spPlanetTo.Item2.layer.Elevation()) * LAOMod.Settings.FuelPerKm;
                        //Log.Message($"{layerConnection.fuelCost} = {fuelBetweenPlanets} + {spPlanetTo.Item1.gravityWellExitElevation - spPlanetTo.Item2.layer.Elevation()}({spPlanetTo.Item1.gravityWellExitElevation} - {spPlanetTo.Item2.layer.Elevation()}) * {LAOMod.Settings.FuelPerKm}");
                    }
                    //Log.Message($"C Add {spPlanetTo.Item2.layer.defName} > {spPlanetFrom.Item2.layer.defName} = {LayerConnection.ZoomMode.ZoomIn} [{layerConnection.fuelCost}]");
                    spPlanetTo.Item2.connections.Add(layerConnection);
                }
            }
            if (LAOMod.Settings.UseFuelCostBetweenLayers)
            {
                for (int i = 0; i < planetLayers.Count; i++)
                {
                    (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerFrom = planetLayers[i];
                    if (spPlanetLayerFrom.Item1.connections.NullOrEmpty())
                    {
                        continue;
                    }
                    for (int j = 0; j < spPlanetLayerFrom.Item1.connections.Count; j++)
                    {
                        LayerConnection layerConnection = spPlanetLayerFrom.Item1.connections[j];
                        if (layerConnection.zoomMode == LayerConnection.ZoomMode.ZoomOut)
                        {
                            int indexTo = planetLayers.FindIndex(pl => pl.Item1.tag == layerConnection.tag);
                            if (indexTo > -1)
                            {
                                layerConnection.fuelCost = ((planetLayers[indexTo].Item2?.elevation ?? 200) - spPlanetLayerFrom.Item1.layer.Elevation()) * LAOMod.Settings.FuelPerKm;
                            }
                        }
                    }
                }
            }
            //Log.Message($"All layer connections After:\n{string.Join("\n", planetLayers.SelectMany(x => x.Item1.connections.Select(y => $"- {x.Item1.layer.defName} > {y.tag} = {y.zoomMode} [{y.fuelCost}]")))}");
        }

        public static float DistanceToReachPlanetLayer(this PlanetLayerDef from, PlanetLayerDef to)
        {
            return to.Elevation() - from.Elevation();
        }

        public static float Elevation(this PlanetLayerDef planetLayer)
        {
            return planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.elevation ?? 200;
        }

        public static PlanetLayerGroupDef LayerGroup(this PlanetLayerDef planetLayer)
        {
            return planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.planetLayerGroup ?? null;
        }

        public static PlanetDef Planet(this PlanetLayerDef planetLayer)
        {
            return planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.planetLayerGroup?.planet ?? null;
        }

        public static bool ContainsLayer(this PlanetLayerGroupDef planetLayerGroup, PlanetLayerDef planetLayer)
        {
            if (planetLayerGroups.TryGetValue(planetLayerGroup, out List<PlanetLayerDef> subPlanetLayers))
            {
                return subPlanetLayers.Contains(planetLayer);
            }
            return false;
        }

        public static bool ContainsLayerSub(this PlanetLayerGroupDef planetLayerGroup, PlanetLayerDef planetLayer)
        {
            foreach (PlanetLayerGroupDef planetLayerGroupSub in planetLayerGroup.planetLayerGroupsToShowToo)
            {
                return planetLayerGroupSub.ContainsLayer(planetLayer);
            }
            return false;
        }

        public static List<PlanetLayerDef> ContainedLayers(this PlanetLayerGroupDef planetLayerGroup)
        {
            List<PlanetLayerDef> subPlanetLayers = new List<PlanetLayerDef>();
            planetLayerGroups.TryGetValue(planetLayerGroup, out subPlanetLayers);
            return subPlanetLayers;
        }

        public static float VisibleInBackgroundOfCurrentLayer(this PlanetLayerDef planetLayer)
        {
            PlanetLayerGroupDef planetLayerGroup = Find.WorldSelector.SelectedLayer.Def.LayerGroup();
            if (planetLayerGroup != null)
            {
                if (planetLayerGroup.ContainsLayer(planetLayer))
                {
                    return LAOMod.Settings.TransparentInGroup;
                }
                else if (planetLayerGroup.ContainsLayerSub(planetLayer))
                {
                    return LAOMod.Settings.TransparentInGroupSub;
                }
            }
            return 0;
        }

        public static float Vacuum(this PlanetLayerDef planetLayer, BiomeDef biomeDef = null)
        {
            float vacuum = 0;
            LayeredAtmosphereOrbitDefModExtension laoLayerDefModExtension = planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (laoLayerDefModExtension != null && laoLayerDefModExtension.vacuum > -1)
            {
                vacuum = laoLayerDefModExtension.vacuum;
            }
            LayeredAtmosphereOrbitDefModExtension laoGroupDefModExtension = planetLayer.LayerGroup()?.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (laoGroupDefModExtension != null && laoGroupDefModExtension.vacuum > -1)
            {
                vacuum = laoGroupDefModExtension.vacuum;
            }
            LayeredAtmosphereOrbitDefModExtension laoBiomeDefModExtension = biomeDef?.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (laoBiomeDefModExtension != null && laoBiomeDefModExtension.vacuum > -1)
            {
                vacuum = laoBiomeDefModExtension.vacuum;
            }
            return vacuum;
        }



        public static bool TestIncidentDefOnLayerDef(this PlanetLayerDef layer, IncidentDef def)
        {
            if (!def.layerWhitelist.NullOrEmpty() && !def.layerWhitelist.Contains(layer))
            {
                return false;
            }
            if (!def.layerBlacklist.NullOrEmpty() && def.layerBlacklist.Contains(layer))
            {
                return false;
            }
            if (!def.canOccurOnAllPlanetLayers && layer.onlyAllowWhitelistedIncidents && (def.layerWhitelist.NullOrEmpty() || !def.layerWhitelist.Contains(layer)))
            {
                return false;
            }
            return true;
        }

        public static bool TestBiomeDefOnLayerDef(this PlanetLayerDef layer, BiomeDef biome)
        {
            if (!biome.layerWhitelist.NullOrEmpty() && !biome.layerWhitelist.Contains(layer))
            {
                return false;
            }
            if (!biome.layerBlacklist.NullOrEmpty() && biome.layerBlacklist.Contains(layer))
            {
                return false;
            }
            if (layer.onlyAllowWhitelistedBiomes && (biome.layerWhitelist.NullOrEmpty() || !biome.layerWhitelist.Contains(layer)))
            {
                return false;
            }
            return true;
        }

        public static bool TestGameConditionDefOnLayerDef(this PlanetLayerDef layer, GameConditionDef def)
        {
            if (!def.layerWhitelist.NullOrEmpty() && !def.layerWhitelist.Contains(layer))
            {
                return false;
            }
            if (!def.layerBlacklist.NullOrEmpty() && def.layerBlacklist.Contains(layer))
            {
                return false;
            }
            if (!def.canAffectAllPlanetLayers && layer.onlyAllowWhitelistedGameConditions && (def.layerWhitelist.NullOrEmpty() || !def.layerWhitelist.Contains(layer)))
            {
                return false;
            }
            return true;
        }

        public static bool TestQuestScriptDefOnLayerDef(this PlanetLayerDef layer, QuestScriptDef quest)
        {
            if (!quest.layerWhitelist.NullOrEmpty() && !quest.layerWhitelist.Contains(layer))
            {
                return false;
            }
            if (!quest.layerBlacklist.NullOrEmpty() && quest.layerBlacklist.Contains(layer))
            {
                return false;
            }
            if (!quest.canOccurOnAllPlanetLayers && layer.onlyAllowWhitelistedIncidents && (quest.layerWhitelist.NullOrEmpty() || !quest.layerWhitelist.Contains(layer)))
            {
                return false;
            }
            if (!quest.everAcceptableInSpace && layer.isSpace)
            {
                return false;
            }
            return !layer.onlyAllowWhitelistedQuests;
        }

        public static bool TestArrivalFactionDefOnLayerDef(this PlanetLayerDef layer, FactionDef f)
        {
            if (!f.arrivalLayerWhitelist.NullOrEmpty() && !f.arrivalLayerWhitelist.Contains(layer))
            {
                return false;
            }
            if (!f.arrivalLayerBlacklist.NullOrEmpty() && f.arrivalLayerBlacklist.Contains(layer))
            {
                return false;
            }
            if (layer.onlyAllowWhitelistedArrivals && (f.arrivalLayerWhitelist.NullOrEmpty() || !f.arrivalLayerWhitelist.Contains(layer)))
            {
                return false;
            }
            return true;
        }

        public static bool TestFactionDefOnLayerDef(this PlanetLayerDef layer, FactionDef f)
        {
            if (!f.layerBlacklist.NullOrEmpty() && f.layerBlacklist.Contains(layer))
            {
                return false;
            }
            if (!f.layerWhitelist.NullOrEmpty() || layer != PlanetLayerDefOf.Surface)
            {
                return f.layerWhitelist.Contains(layer);
            }
            return true;
        }
    }
}