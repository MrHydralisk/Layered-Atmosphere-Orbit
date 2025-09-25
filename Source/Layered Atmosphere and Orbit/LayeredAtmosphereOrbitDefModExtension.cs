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
        public bool isSurface = false;
        public bool isPreventQuestMapIfNotWhitelisted = false;
        public bool onlyAllowWhitelistedFactions = false;
        public bool isAvoidFactionDuplication = true;
        public float elevation = 200;
        public float tempOffest = 0;
        public float vacuum = -1;
        public PlanetLayerGroupDef planetLayerGroup;
        public List<PlanetLayerDef> forcedConnectToPlanetLayer = new List<PlanetLayerDef>();
        public List<BiomeDef> availableBiomes = new List<BiomeDef>();
        public List<IncidentChanceMultiplier> IncidentChanceMultipliers = new List<IncidentChanceMultiplier>();
        public List<IncidentDef> WhitelistIncidentDef = new List<IncidentDef>();
        public List<IncidentDef> BlacklistIncidentDef = new List<IncidentDef>();
        public List<BiomeDef> WhitelistBiomeDef = new List<BiomeDef>();
        public List<BiomeDef> BlacklistBiomeDef = new List<BiomeDef>();
        public List<GameConditionDef> WhitelistGameConditionDef = new List<GameConditionDef>();
        public List<GameConditionDef> BlacklistGameConditionDef = new List<GameConditionDef>();
        public List<QuestScriptDef> WhitelistQuestScriptDef = new List<QuestScriptDef>();
        public List<QuestScriptDef> BlacklistQuestScriptDef = new List<QuestScriptDef>();
        public TechLevel minArrivalFactionTechLevel = TechLevel.Undefined;
        public TechLevel maxArrivalFactionTechLevel = TechLevel.Undefined;
        public List<FactionDef> WhitelistArrivalFactionDef = new List<FactionDef>();
        public List<FactionDef> BlacklistArrivalFactionDef = new List<FactionDef>();
        public TechLevel minFactionTechLevel = TechLevel.Undefined;
        public TechLevel maxFactionTechLevel = TechLevel.Undefined;
        public List<FactionDef> WhitelistFactionDef = new List<FactionDef>();
        public List<FactionDef> BlacklistFactionDef = new List<FactionDef>();
    }
}