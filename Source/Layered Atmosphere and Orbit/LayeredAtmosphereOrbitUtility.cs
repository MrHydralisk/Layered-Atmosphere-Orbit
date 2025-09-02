using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
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
                    string[] planetLayerDefNames = { "LAO_Troposphere", "LAO_Stratosphere", "LAO_Mesosphere", "LAO_HighOrbit" };
                    planetLayersLAOCached = new List<ScenPart_PlanetLayer>();
                    foreach (string defName in planetLayerDefNames)
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
            List<ScenPart_PlanetLayer> planetLayers = new List<ScenPart_PlanetLayer>();
            foreach (ScenPart scenPart in scenario.AllParts)
            {
                if (scenPart is ScenPart_PlanetLayer scenPart_PlanetLayer)
                {
                    planetLayers.Add(scenPart_PlanetLayer);
                }
            }
            planetLayers.SortBy((ScenPart_PlanetLayer sppl) => sppl.Settings.radius);
            for (int i = 1; i < planetLayers.Count; i++)
            {
                ScenPart_PlanetLayer scenPart_PlanetLayerFrom = planetLayers[i - 1];
                ScenPart_PlanetLayer scenPart_PlanetLayerTo = planetLayers[i];
                Log.Message($"[{i}] {scenPart_PlanetLayerFrom.Label} [{scenPart_PlanetLayerFrom.Settings.radius}] --- {scenPart_PlanetLayerTo.Label} [{scenPart_PlanetLayerTo.Settings.radius}]");
                if (!scenPart_PlanetLayerFrom.connections.Any((LayerConnection lc) => lc.tag == scenPart_PlanetLayerTo.tag))
                {
                    scenPart_PlanetLayerFrom.connections.Add(new LayerConnection() { tag = scenPart_PlanetLayerTo.tag, zoomMode = LayerConnection.ZoomMode.ZoomOut });
                }
                if (!scenPart_PlanetLayerTo.connections.Any((LayerConnection lc) => lc.tag == scenPart_PlanetLayerFrom.tag))
                {
                    scenPart_PlanetLayerTo.connections.Add(new LayerConnection() { tag = scenPart_PlanetLayerFrom.tag, zoomMode = LayerConnection.ZoomMode.ZoomIn });
                }
            }
        }
    }
}