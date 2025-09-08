﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandHives : GenStep
    {
        private List<IntVec3> rockCells = new List<IntVec3>();

        private List<IntVec3> possibleSpawnCells = new List<IntVec3>();

        private List<Hive> spawnedHives = new List<Hive>();

        public int MinDistToOpenSpace = 10;

        public int MinDistFromFactionBase = 50;

        public float CaveCellsPerHive = 1000f;

        public override int SeedPart => 349641510;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!Find.Storyteller.difficulty.allowCaveHives || Faction.OfInsects == null)
            {
                return;
            }
            MapGenFloatGrid caves = MapGenerator.Caves;
            MapGenFloatGrid elevation = MapGenerator.Elevation;
            float num = 0.7f;
            int num2 = 0;
            rockCells.Clear();
            foreach (IntVec3 allCell in map.AllCells)
            {
                if (elevation[allCell] > num)
                {
                    rockCells.Add(allCell);
                }
                if (caves[allCell] > 0f)
                {
                    num2++;
                }
            }
            List<IntVec3> list = map.AllCells.Where((IntVec3 c) => map.thingGrid.ThingsAt(c).Any((Thing thing) => thing.Faction != null)).ToList();
            GenMorphology.Dilate(list, MinDistFromFactionBase, map);
            HashSet<IntVec3> hashSet = new HashSet<IntVec3>(list);
            int num3 = GenMath.RoundRandom((float)num2 / CaveCellsPerHive);
            GenMorphology.Erode(rockCells, MinDistToOpenSpace, map);
            possibleSpawnCells.Clear();
            for (int i = 0; i < rockCells.Count; i++)
            {
                if (caves[rockCells[i]] > 0f && !hashSet.Contains(rockCells[i]))
                {
                    possibleSpawnCells.Add(rockCells[i]);
                }
            }
            spawnedHives.Clear();
            for (int j = 0; j < num3; j++)
            {
                TrySpawnHive(map);
            }
            spawnedHives.Clear();
        }

        private void TrySpawnHive(Map map)
        {
            if (TryFindHiveSpawnCell(map, out var spawnCell))
            {
                possibleSpawnCells.Remove(spawnCell);
                Hive item = HiveUtility.SpawnHive(spawnCell, map, WipeMode.VanishOrMoveAside, spawnInsectsImmediately: true, canSpawnHives: false, canSpawnInsects: false, dormant: false, aggressive: false);
                spawnedHives.Add(item);
            }
        }

        private bool TryFindHiveSpawnCell(Map map, out IntVec3 spawnCell)
        {
            float num = -1f;
            IntVec3 intVec = IntVec3.Invalid;
            for (int i = 0; i < 3; i++)
            {
                if (!possibleSpawnCells.Where((IntVec3 x) => x.Standable(map) && x.GetFirstItem(map) == null && x.GetFirstBuilding(map) == null && x.GetFirstPawn(map) == null).TryRandomElement(out var result))
                {
                    break;
                }
                float num2 = -1f;
                for (int j = 0; j < spawnedHives.Count; j++)
                {
                    float num3 = result.DistanceToSquared(spawnedHives[j].Position);
                    if (num2 < 0f || num3 < num2)
                    {
                        num2 = num3;
                    }
                }
                if (!intVec.IsValid || num2 > num)
                {
                    intVec = result;
                    num = num2;
                }
            }
            spawnCell = intVec;
            return spawnCell.IsValid;
        }
    }
}