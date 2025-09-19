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
        public bool onlyAllowWhitelistedFactions = false;
        public float elevation = 200;
        public float tempOffest = 0;
        public PlanetLayerGroupDef planetLayerGroup;
        public List<PlanetLayerDef> forcedConnectToPlanetLayer = new List<PlanetLayerDef>();
        public List<BiomeDef> availableBiomes = new List<BiomeDef>();
        public TechLevel minFactionTechLevel = TechLevel.Undefined;
        public List<IncidentChanceMultiplier> IncidentChanceMultipliers = new List<IncidentChanceMultiplier>();
        public List<IncidentDef> WhitelistIncidentDef = new List<IncidentDef>();
        public List<IncidentDef> BlacklistIncidentDef = new List<IncidentDef>();
        public List<BiomeDef> WhitelistBiomeDef = new List<BiomeDef>();
        public List<BiomeDef> BlacklistBiomeDef = new List<BiomeDef>();
        public List<GameConditionDef> WhitelistGameConditionDef = new List<GameConditionDef>();
        public List<GameConditionDef> BlacklistGameConditionDef = new List<GameConditionDef>();
        public List<QuestScriptDef> WhitelistQuestScriptDef = new List<QuestScriptDef>();
        public List<QuestScriptDef> BlacklistQuestScriptDef = new List<QuestScriptDef>();
        public List<FactionDef> WhitelistArrivalFactionDef = new List<FactionDef>();
        public List<FactionDef> BlacklistArrivalFactionDef = new List<FactionDef>();
        public List<FactionDef> WhitelistFactionDef = new List<FactionDef>();
        public List<FactionDef> BlacklistFactionDef = new List<FactionDef>();
    }
}