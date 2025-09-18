using RimWorld;
using System.Collections.Generic;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LayeredAtmosphereOrbitDefModExtension : DefModExtension
    {
        public bool isReplaceConnections = false;
        public bool isOptionToAutoAdd = false;
        public bool isRockColored = false;
        public bool isPreventQuestMapIfNotWhitelisted = false;
        public float elevation = 200;
        public float tempOffest = 0;
        public PlanetLayerGroupDef planetLayerGroup;
        public List<PlanetLayerDef> forcedConnectToPlanetLayer = new List<PlanetLayerDef>();
        public List<BiomeDef> availableBiomes = new List<BiomeDef>();
        public List<FactionDef> WhitelistArrivalFactionDef = new List<FactionDef>();
        public List<FactionDef> WhitelistFactionDef = new List<FactionDef>();
        public List<BiomeDef> WhitelistBiomeDef = new List<BiomeDef>();
        public List<IncidentDef> WhitelistIncidentDef = new List<IncidentDef>();
        public List<GameConditionDef> WhitelistGameConditionDef = new List<GameConditionDef>();
        public List<QuestScriptDef> WhitelistQuestScriptDef = new List<QuestScriptDef>();
        public List<QuestScriptDef> BlacklistQuestScriptDef = new List<QuestScriptDef>();
    }
}