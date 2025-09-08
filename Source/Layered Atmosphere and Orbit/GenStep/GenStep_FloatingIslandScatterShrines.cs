using RimWorld.Planet;
using RimWorld.SketchGen;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.Noise;
using HarmonyLib;
using System.Reflection;
using Verse.AI.Group;
using Verse.AI;
using RimWorld.BaseGen;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandScatterShrines : GenStep_Scatterer
    {
        private IntVec2 Size;

        public IntRange SizeRange = new IntRange(15, 20);

        private int usedRectsPadding = 2;

        public override int SeedPart => 1801222485;

        protected override bool TryFindScatterCell(Map map, out IntVec3 result)
        {
            Size.x = SizeRange.RandomInRange;
            Size.z = SizeRange.RandomInRange;
            return base.TryFindScatterCell(map, out result);
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (!base.CanScatterAt(c, map))
            {
                return false;
            }
            if (!c.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy))
            {
                return false;
            }
            if (MapGenerator.TryGetVar<List<CellRect>>("UsedRects", out var var))
            {
                CellRect cellRect = EffectiveRectAt(c);
                foreach (CellRect item in var)
                {
                    if (cellRect.Overlaps(item.ExpandedBy(usedRectsPadding)))
                    {
                        return false;
                    }
                }
            }
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c2 = c + GenAdj.AdjacentCellsAndInside[i];
                if (c2.InBounds(map))
                {
                    Building edifice = c2.GetEdifice(map);
                    if (edifice != null && edifice.def.building.isNaturalRock)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected CellRect EffectiveRectAt(IntVec3 c)
        {
            return CellRect.CenteredOn(c, Size.x, Size.z);
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1)
        {
            List<CellRect> orGenerateVar = MapGenerator.GetOrGenerateVar<List<CellRect>>("UsedRects");
            CellRect cellRect = EffectiveRectAt(loc);
            CellRect cellRect2 = cellRect.ClipInsideMap(map);
            if (cellRect2.Width != cellRect.Width || cellRect2.Height != cellRect.Height)
            {
                return;
            }
            int index = map.AllCells.FirstIndexOf((IntVec3 iv3) => iv3.GetTerrain(map)?.passability == Traversability.Standable);
            if (index > -1)
            {
                TerrainDef terrainDef = map.AllCells.ElementAt(index).GetTerrain(map);
                foreach (IntVec3 cell in cellRect.Cells)
                {
                    if (cell.GetTerrain(map) == DefOfLocal.LAO_Air)
                    {
                        map.terrainGrid.SetTerrain(cell, terrainDef);
                    }
                }
            }
            foreach (IntVec3 cell in cellRect.Cells)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(cell);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def == ThingDefOf.AncientCryptosleepCasket)
                    {
                        return;
                    }
                }
            }
            if (!orGenerateVar.Contains(cellRect))
            {
                orGenerateVar.Add(cellRect);
                ResolveParams resolveParams = default(ResolveParams);
                resolveParams.rect = cellRect;
                resolveParams.disableSinglePawn = true;
                resolveParams.disableHives = true;
                resolveParams.makeWarningLetter = true;
                if (Find.Storyteller.difficulty.peacefulTemples)
                {
                    resolveParams.podContentsType = PodContentsType.AncientFriendly;
                }
                BaseGen.globalSettings.map = map;
                BaseGen.symbolStack.Push("ancientTemple", resolveParams);
                BaseGen.Generate();
            }
        }
    }
}