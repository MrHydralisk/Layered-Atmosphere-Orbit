﻿using RimWorld.Planet;
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

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandDebug : GenStep
    {
        public List<GenStep_Asteroid.MineableCountConfig> mineableCounts;

        public IntRange numChunks;

        public float ruinsChance;

        public float archeanTreeChance;

        private ModuleBase innerNoise;

        public override int SeedPart => 1929282;

        public float Radius => LAOMod.Settings.DebugFloatingIslandRadius;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (ModLister.OdysseyInstalled)
            {
                GenerateAsteroidElevation(map, parms);
                GenerateCaveElevation(map, parms);
                SpawnFloatingIsland(map);
                SpawnOres(map, parms);
                if (Rand.Chance(ruinsChance))
                {
                    GenStep_Asteroid.GenerateRuins(map, parms);
                }
                if (Rand.Chance(archeanTreeChance))
                {
                    GenStep_Asteroid.GenerateArcheanTree(map, parms );
                }
                map.OrbitalDebris = OrbitalDebrisDefOf.Asteroid;
            }
        }

        private static void SpawnFloatingIsland(Map map)
        {
            using (map.pathing.DisableIncrementalScope())
            {
                foreach (IntVec3 allCell in map.AllCells)
                {
                    float num = MapGenerator.Elevation[allCell];
                    float num2 = MapGenerator.Caves[allCell];
                    if (num > LAOMod.Settings.DebugFloatingIslandFloorThreshold)
                    {
                        map.terrainGrid.SetTerrain(allCell, ThingDefOf.Slate.building.naturalTerrain);
                    }
                    if (num > LAOMod.Settings.DebugFloatingIslandWallThreshold && num2 == 0f)
                    {
                        GenSpawn.Spawn(ThingDefOf.Slate, allCell, map);
                    }
                    if (num > LAOMod.Settings.DebugFloatingIslandWallThreshold)
                    {
                        map.roofGrid.SetRoof(allCell, RoofDefOf.RoofRockThin);
                    }
                }
                HashSet<IntVec3> mainIsland = new HashSet<IntVec3>();
                map.floodFiller.FloodFill(map.Center, (IntVec3 x) => x.GetTerrain(map) != DefOfLocal.LAO_Air, delegate (IntVec3 x)
                {
                    mainIsland.Add(x);
                });
                foreach (IntVec3 allCell2 in map.AllCells)
                {
                    if (mainIsland.Contains(allCell2))
                    {
                        continue;
                    }
                    map.terrainGrid.SetTerrain(allCell2, DefOfLocal.LAO_Air);
                    map.roofGrid.SetRoof(allCell2, null);
                    foreach (Thing item in allCell2.GetThingList(map).ToList())
                    {
                        item.Destroy();
                    }
                }
            }
        }

        private void GenerateAsteroidElevation(Map map, GenStepParams parms)
        {
            innerNoise = ConfigureNoise(map, parms);
            foreach (IntVec3 allCell in map.AllCells)
            {
                MapGenerator.Elevation[allCell] = innerNoise.GetValue(allCell);
            }
        }

        protected virtual ModuleBase ConfigureNoise(Map map, GenStepParams parms)
        {
            ModuleBase input = new DistFromPoint((float)map.Size.x * Radius);
            input = new ScaleBias(-1.0, 1.0, input);
            if (LAOMod.Settings.is1Scale)
            {
                input = new Scale(LAOMod.Settings.DebugFloatingIsland1Scale, 1.0, 1.0, input);
            }
            if (LAOMod.Settings.is2Rotate)
            {
                input = new Rotate(0.0, LAOMod.Settings.DebugFloatingIslandRotation, 0.0, input);
            }
            if (LAOMod.Settings.is3Translate)
            {
                input = new Translate(-map.Center.x, 0.0, -map.Center.z, input);
            }
            NoiseDebugUI.StoreNoiseRender(input, "Base asteroid shape");
            if (LAOMod.Settings.is4Blend)
            {
                input = new Blend(new Perlin(LAOMod.Settings.DebugFloatingIsland4Perlin, LAOMod.Settings.DebugFloatingIsland4PerlinA, LAOMod.Settings.DebugFloatingIsland4PerlinB, LAOMod.Settings.DebugFloatingIsland4PerlinC, LAOMod.Settings.DebugFloatingIslandPerlinSeedA, QualityMode.Medium), input, new Const(LAOMod.Settings.DebugFloatingIsland4Const));
            }
            if (LAOMod.Settings.is5Blend)
            {
                input = new Blend(new Perlin(LAOMod.Settings.DebugFloatingIsland5Perlin, LAOMod.Settings.DebugFloatingIsland5PerlinA, LAOMod.Settings.DebugFloatingIsland5PerlinB, LAOMod.Settings.DebugFloatingIsland5PerlinC, LAOMod.Settings.DebugFloatingIslandPerlinSeedB, QualityMode.Medium), input, new Const(LAOMod.Settings.DebugFloatingIsland5Const));
            }
            if (LAOMod.Settings.is6Power)
            {
                input = new Power(input, new Const(LAOMod.Settings.DebugFloatingIslandConst));
            }
            NoiseDebugUI.StoreNoiseRender(input, "Asteroid");
            return input;
        }

        private void SpawnOres(Map map, GenStepParams parms)
        {
            ThingDef thingDef = ((SpaceMapParent)map.ParentHolder).preciousResource ?? mineableCounts.RandomElement().mineable;
            int num = 0;
            for (int i = 0; i < mineableCounts.Count; i++)
            {
                if (mineableCounts[i].mineable == thingDef)
                {
                    num = mineableCounts[i].countRange.RandomInRange;
                    break;
                }
            }
            if (num == 0)
            {
                Debug.LogError("No count found for resource " + thingDef);
                return;
            }
            int randomInRange = numChunks.RandomInRange;
            int forcedLumpSize = num / randomInRange;
            GenStep_ScatterLumpsMineable genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable();
            genStep_ScatterLumpsMineable.count = randomInRange;
            genStep_ScatterLumpsMineable.forcedDefToScatter = thingDef;
            genStep_ScatterLumpsMineable.forcedLumpSize = forcedLumpSize;
            genStep_ScatterLumpsMineable.Generate(map, parms);
        }

        private void GenerateCaveElevation(Map map, GenStepParams parms)
        {
            Perlin directionNoise = new Perlin(0.0020000000949949026, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
            MapGenFloatGrid elevation = MapGenerator.Elevation;
            BoolGrid visited = new BoolGrid(map);
            List<IntVec3> group = new List<IntVec3>();
            MapGenCavesUtility.CaveGenParms @default = MapGenCavesUtility.CaveGenParms.Default;
            @default.widthOffsetPerCell = 0.015f;
            @default.minTunnelWidth = 0.5f;
            @default.branchChance = 0.05f;
            @default.maxOpenTunnelsPerRockGroup = 2;
            @default.maxClosedTunnelsPerRockGroup = 2;
            @default.minTunnelWidth = 0.25f;
            @default.branchChance = 0.05f;
            @default.openTunnelsPer10k = 4f;
            @default.tunnelsWidthPerRockCount = new SimpleCurve
        {
            new CurvePoint(100f, 1f),
            new CurvePoint(300f, 1.5f),
            new CurvePoint(3000f, 1.9f)
        };
            MapGenCavesUtility.GenerateCaves(map, visited, group, directionNoise, @default, Rock);
            bool Rock(IntVec3 cell)
            {
                return IsRock(cell, elevation, map);
            }
        }

        private bool IsRock(IntVec3 c, MapGenFloatGrid elevation, Map map)
        {
            if (c.InBounds(map))
            {
                return elevation[c] > 0.7f;
            }
            return false;
        }

        //private static void GenerateArcheanTree(Map map, GenStepParams parms)
        //{
        //    if (!MapGenUtility.TryGetRandomClearRect(23, 23, out var rect, -1, -1, RectValidator, 0.59999996f, float.MaxValue) && !MapGenUtility.TryGetRandomClearRect(23, 23, out rect, -1, -1, RectValidator, -1f, float.MaxValue))
        //    {
        //        return;
        //    }
        //    int num = 5;
        //    foreach (IntVec3 cell in rect.Cells)
        //    {
        //        float magnitude = (cell - rect.CenterCell).Magnitude;
        //        if (magnitude < 8f || (magnitude < 9f && Rand.Chance(0.25f)))
        //        {
        //            cell.GetEdifice(map)?.Destroy();
        //            map.roofGrid.SetRoof(cell, null);
        //        }
        //        if (magnitude < (float)num || (magnitude < (float)num + 1.9f && Rand.Chance(0.25f)))
        //        {
        //            map.terrainGrid.SetTerrain(cell, TerrainDefOf.SoilRich);
        //        }
        //    }
        //    WildPlantSpawner.SpawnPlant(ThingDefOf.Plant_TreeArchean, map, rect.CenterCell, setRandomGrowth: false).Growth = 1f;
        //    RCellFinder.TryFindRandomCellsNear(rect.CenterCell, 6, map, Validator, out var cells, 4, 7);
        //    for (int i = 0; i < cells.Count; i++)
        //    {
        //        IntVec3 intVec = cells[i];
        //        if (i == 0)
        //        {
        //            RoomGenUtility.SpawnCrate(ThingDefOf.SealedCrate, intVec, map, Rot4.North, ThingSetMakerDefOf.Reward_ArcheanSeed);
        //        }
        //        else
        //        {
        //            GenSpawn.Spawn(ThingDefOf.AncientSpacerCrate, intVec, map, Rot4.North);
        //        }
        //    }
        //    bool RectValidator(CellRect r)
        //    {
        //        for (int j = 0; j < map.layoutStructureSketches.Count; j++)
        //        {
        //            CellRect container = map.layoutStructureSketches[j].structureLayout.container;
        //            if (container.ExpandedBy(5).Overlaps(r))
        //            {
        //                return false;
        //            }
        //        }
        //        foreach (IntVec3 cell2 in r.Cells)
        //        {
        //            if (!cell2.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Medium))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    bool Validator(IntVec3 cell)
        //    {
        //        if (rect.Contains(cell) && cell.GetEdifice(map) == null && cell.GetPlant(map) == null)
        //        {
        //            return (cell - rect.CenterCell).Magnitude >= 2.9f;
        //        }
        //        return false;
        //    }
        //}

        //private static void GenerateRuins(Map map, GenStepParams parms)
        //{
        //    IntVec2 size = new IntVec2(RuinsSize.RandomInRange, RuinsSize.RandomInRange);
        //    IntVec3 intVec = IntVec3.Invalid;
        //    Rot4 rot2 = Rot4.North;
        //    for (int i = 0; i < 10; i++)
        //    {
        //        float num = Rand.Range(0f, 0.2f);
        //        Vector3 vect = new Vector3(Mathf.RoundToInt((float)map.Size.x * num), 0f, 0f).RotatedBy(Rand.Range(0f, 360f));
        //        IntVec3 intVec2 = map.Center + vect.ToIntVec3();
        //        rot2 = Rot4.Random;
        //        for (int j = 0; j < 2; j++)
        //        {
        //            if (i == 1)
        //            {
        //                rot2 = rot2.Rotated(RotationDirection.Clockwise);
        //            }
        //            if (IsValidPoint(intVec2, rot2))
        //            {
        //                intVec = intVec2;
        //                break;
        //            }
        //        }
        //    }
        //    if (!(intVec == IntVec3.Invalid))
        //    {
        //        CellRect cellRect = intVec.RectAbout(size, rot2);
        //        StructureGenParams parms2 = new StructureGenParams
        //        {
        //            size = cellRect.Size
        //        };
        //        LayoutWorker worker = LayoutDefOf.SpaceRuins.Worker;
        //        LayoutStructureSketch layoutStructureSketch = worker.GenerateStructureSketch(parms2);
        //        SketchResolveParams parms3 = new SketchResolveParams
        //        {
        //            sketch = layoutStructureSketch.layoutSketch,
        //            destroyChanceExp = 1.5f
        //        };
        //        SketchResolverDefOf.DamageBuildingsLight.Resolve(parms3);
        //        map.layoutStructureSketches.Add(layoutStructureSketch);
        //        worker.Spawn(layoutStructureSketch, map, cellRect.Min);
        //    }
        //    bool IsValidPoint(IntVec3 p, Rot4 rot)
        //    {
        //        foreach (IntVec3 item in p.RectAbout(size, rot))
        //        {
        //            if (MapGenerator.Elevation[item] < 0.7f)
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //}
    }
}