using HarmonyLib;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public static class LayeredAtmosphereOrbitUtility
    {
        public static List<ScenPart_PlanetLayer> planetLayersLAO;
        public static Dictionary<PlanetLayerGroupDef, List<PlanetLayerDef>> planetLayerGroups;

        public static void ResetLayerData()
        {
            planetLayerGroups = new Dictionary<PlanetLayerGroupDef, List<PlanetLayerDef>>();
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
            }
            Log.Message($"All layer groupds:\n{string.Join("\n", planetLayerGroups.SelectMany(x => x.Value.Select(y => $"- {x.Key.label} > {y.label}")))}");
            planetLayersLAO = new List<ScenPart_PlanetLayer>();
            foreach (string defName in LAOMod.Settings.AutoAddLayersDefNames)
            {
                ScenPart_PlanetLayerFixed scenPart_LAOPlanetLayer = new ScenPart_PlanetLayerFixed();
                scenPart_LAOPlanetLayer.def = DefDatabase<ScenPartDef>.GetNamed(defName);
                scenPart_LAOPlanetLayer.tag = defName;
                scenPart_LAOPlanetLayer.layer = DefDatabase<PlanetLayerDef>.GetNamed(defName);
                scenPart_LAOPlanetLayer.settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamed(defName);
                scenPart_LAOPlanetLayer.hide = true;
                planetLayersLAO.Add(scenPart_LAOPlanetLayer);
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
                    if ((spPlanetLayerFrom.Item2?.isReplaceConnections ?? false) || (spPlanetLayerTo.Item2?.isReplaceConnections ?? false))
                    {
                        //Log.Message($"Remove {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} = {layerConnection.zoomMode}");
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
                            //Log.Message($"Add Forced {spPlanetLayerFrom.Item1.layer.defName} > {planetLayerDef.defName} = {LayerConnection.ZoomMode.ZoomOut}");
                            spPlanetLayerFrom.Item1.connections.Add(new LayerConnection() { tag = planetLayerDef.defName, zoomMode = (spPlanetLayerFrom.Item1.layer.DistanceToReachPlanetLayer(planetLayerDef) < 0 ? LayerConnection.ZoomMode.ZoomIn : LayerConnection.ZoomMode.ZoomOut) });
                        }
                    }
                }
            }
            List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)> planetLayersKnown = planetLayers.Where(sppl => sppl.Item2?.planetLayerGroup != null).ToList();
            for (int i = 1; i < planetLayersKnown.Count; i++)
            {
                (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerFrom = planetLayersKnown[i - 1];
                (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerTo = planetLayersKnown[i];
                //Log.Message($"Check {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} =\n{string.Join("\n", spPlanetLayerFrom.Item1.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}\n>\n{string.Join("\n", spPlanetLayerFrom.Item1.connections.Select(y => $"- {y.tag} = {y.zoomMode}"))}");
                if (!spPlanetLayerFrom.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerTo.Item1.tag))
                {
                    //Log.Message($"Add {spPlanetLayerFrom.Item1.layer.defName} > {spPlanetLayerTo.Item1.layer.defName} = {LayerConnection.ZoomMode.ZoomOut}");
                    spPlanetLayerFrom.Item1.connections.Add(new LayerConnection() { tag = spPlanetLayerTo.Item1.tag, zoomMode = LayerConnection.ZoomMode.ZoomOut });
                }
                if (!spPlanetLayerTo.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerFrom.Item1.tag))
                {
                    //Log.Message($"Add {spPlanetLayerTo.Item1.layer.defName} > {spPlanetLayerFrom.Item1.layer.defName} = {LayerConnection.ZoomMode.ZoomIn}");
                    spPlanetLayerTo.Item1.connections.Add(new LayerConnection() { tag = spPlanetLayerFrom.Item1.tag, zoomMode = LayerConnection.ZoomMode.ZoomIn });
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
            //Log.Message($"All layer connections After:\n{string.Join("\n", planetLayers.SelectMany(x => x.Item1.connections.Select(y => $"- {x.Item1.layer.defName} > {y.tag} = {y.zoomMode}")))}");
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
    }
}