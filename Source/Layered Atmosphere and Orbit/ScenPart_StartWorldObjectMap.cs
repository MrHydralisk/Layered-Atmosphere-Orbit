using RimWorld.Planet;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using UnityEngine.Tilemaps;

namespace LayeredAtmosphereOrbit
{
    public class ScenPart_StartWorldObjectMap : ScenPart
    {
        public MapGeneratorDef mapGenerator;
        public WorldObjectDef worldObjectDef;
        public PlanetLayerDef layerDef;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref mapGenerator, "mapGenerator");
            Scribe_Defs.Look(ref worldObjectDef, "worldObjectDef");
            Scribe_Defs.Look(ref layerDef, "layerDef");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
            if (worldObjectDef != null)
            {
                mapGenerator = worldObjectDef.mapGenerator;
            }
            scenPartRect.height = ScenPart.RowHeight;
            Text.Anchor = TextAnchor.UpperRight;
            Rect rect = new Rect(scenPartRect.x - 200f, scenPartRect.y + ScenPart.RowHeight * 2, 200f, ScenPart.RowHeight);
            rect.xMax -= 4f;
            Widgets.Label(rect, "ScenPart_ForcedMapPlanetLayer".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(scenPartRect, worldObjectDef.LabelCap))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (WorldObjectDef item in DefDatabase<WorldObjectDef>.AllDefs)
                {
                    WorldObjectDef localFd3 = item;
                    list.Add(new FloatMenuOption(localFd3.LabelCap, delegate
                    {
                        worldObjectDef = localFd3;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            scenPartRect.y += ScenPart.RowHeight;
            if (Widgets.ButtonText(scenPartRect, mapGenerator?.LabelCap ?? "---"))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (MapGeneratorDef item in DefDatabase<MapGeneratorDef>.AllDefs.Where((MapGeneratorDef d) => d.validScenarioMap).Append(null))
                {
                    MapGeneratorDef localFd2 = item;
                    list.Add(new FloatMenuOption(localFd2?.LabelCap ?? "---", delegate
                    {
                        mapGenerator = localFd2;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            scenPartRect.y += ScenPart.RowHeight;
            if (Widgets.ButtonText(scenPartRect, layerDef.LabelCap))
            {
                List<FloatMenuOption> list2 = new List<FloatMenuOption>();
                foreach (PlanetLayerDef allDef in DefDatabase<PlanetLayerDef>.AllDefs)
                {
                    PlanetLayerDef localFd = allDef;
                    list2.Add(new FloatMenuOption(localFd.LabelCap, delegate
                    {
                        layerDef = localFd;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list2));
            }
        }
    }
}

