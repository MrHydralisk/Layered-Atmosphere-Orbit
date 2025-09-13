using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class QuestNode_GetLAOLayers : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> storeAs = "layerGroupWhitelist";

        public SlateRef<List<PlanetLayerGroupDef>> layerGroupWhitelist;

        protected override bool TestRunInt(Slate slate)
        {
            slate.Set(storeAs.GetValue(slate), layerGroupWhitelist);
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestGen.slate.Set(storeAs.GetValue(slate), layerGroupWhitelist);
        }
    }
}

