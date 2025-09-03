using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public static class LayeredAtmosphereOrbitUtility
    {
        public static List<ScenPart_PlanetLayer> planetLayersLAO
        {
            get
            {
                if (planetLayersLAOCached == null)
                {
                    planetLayersLAOCached = new List<ScenPart_PlanetLayer>();
                    foreach (string defName in LAOMod.Settings.AutoAddLayersDefNames)
                    {
                        ScenPart_PlanetLayerFixed scenPart_LAOPlanetLayer = new ScenPart_PlanetLayerFixed();
                        scenPart_LAOPlanetLayer.def = DefDatabase<ScenPartDef>.GetNamed(defName);
                        scenPart_LAOPlanetLayer.tag = defName;
                        scenPart_LAOPlanetLayer.layer = DefDatabase<PlanetLayerDef>.GetNamed(defName);
                        scenPart_LAOPlanetLayer.settingsDef = DefDatabase<PlanetLayerSettingsDef>.GetNamed(defName);
                        scenPart_LAOPlanetLayer.hide = true;
                        planetLayersLAOCached.Add(scenPart_LAOPlanetLayer);
                    }
                }
                return planetLayersLAOCached;
            }
        }
        public static List<ScenPart_PlanetLayer> planetLayersLAOCached;

        public static void TryAddPlanetLayerts(Scenario scenario)
        {
            List<ScenPart> parts = AccessTools.Field(typeof(Scenario), "parts").GetValue(scenario) as List<ScenPart>;
            foreach (ScenPart_PlanetLayer sppl in planetLayersLAO)
            {
                if (!parts.Any((ScenPart sp) => sp.def == sppl.def))
                {
                    parts.Add(sppl);
                }
            }
            AccessTools.Field(typeof(Scenario), "parts").SetValue(scenario, parts);
            List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)> planetLayers = new List<(ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension)>();
            foreach (ScenPart scenPart in scenario.AllParts)
            {
                if (scenPart is ScenPart_PlanetLayer scenPart_PlanetLayer)
                {
                    planetLayers.Add((scenPart_PlanetLayer, scenPart_PlanetLayer.layer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>() ?? new LayeredAtmosphereOrbitDefModExtension()));
                }
            }
            planetLayers.SortBy((spLayer) => spLayer.Item2.elevation);
            for (int i = 1; i < planetLayers.Count; i++)
            {
                (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerFrom = planetLayers[i - 1];
                (ScenPart_PlanetLayer, LayeredAtmosphereOrbitDefModExtension) spPlanetLayerTo = planetLayers[i];
                Log.Message($"[{i}] {spPlanetLayerFrom.Item1.Label} [{spPlanetLayerFrom.Item2.elevation}] --- {spPlanetLayerTo.Item1.Label} [{spPlanetLayerTo.Item2.elevation}]");
                if (!spPlanetLayerFrom.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerTo.Item1.tag))
                {
                    spPlanetLayerFrom.Item1.connections.Add(new LayerConnection() { tag = spPlanetLayerTo.Item1.tag, zoomMode = LayerConnection.ZoomMode.ZoomOut });
                }
                if (!spPlanetLayerTo.Item1.connections.Any((LayerConnection lc) => lc.tag == spPlanetLayerFrom.Item1.tag))
                {
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
                                layerConnection.fuelCost = (planetLayers[indexTo].Item2.elevation - spPlanetLayerFrom.Item1.layer.Elevation()) * LAOMod.Settings.FuelPerKm;
                            }
                        }
                    }
                }
            }
        }

        public static float DistanceToReachPlanetLayer(PlanetLayerDef from, PlanetLayerDef to)
        {
            return to.Elevation() - from.Elevation();
        }

        public static float Elevation(this PlanetLayerDef planetLayer)
        {
            return planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.elevation ?? 200;
        }

        public static OrbitType LayerOrbitType(this PlanetLayerDef planetLayer)
        {
            return planetLayer.GetModExtension<LayeredAtmosphereOrbitDefModExtension>()?.layerType ?? OrbitType.unknown;
        }

        public static float VisibleInBackgroundOfCurrentLayer(this PlanetLayerDef planetLayer)
        {
            OrbitType currentOrbitType = Find.WorldSelector.SelectedLayer.Def.LayerOrbitType();
            if (planetLayer.LayerOrbitType() == OrbitType.atmosphere)
            {
                if (currentOrbitType == OrbitType.surface)
                {
                    return 0.3f;
                }
                else if (currentOrbitType == OrbitType.atmosphere)
                {
                    return 0.6f;
                }
            }
            return 0;
        }
    }
}