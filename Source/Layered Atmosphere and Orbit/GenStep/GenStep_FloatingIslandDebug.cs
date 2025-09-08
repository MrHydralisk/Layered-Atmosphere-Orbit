using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

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
                    GenStep_Asteroid.GenerateArcheanTree(map, parms);
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
            @default.widthOffsetPerCell = LAOMod.Settings.DebugFloatingIslandwidthOffsetPerCell;
            @default.maxOpenTunnelsPerRockGroup = LAOMod.Settings.DebugFloatingIslandmaxOpenTunnelsPerRockGroup;
            @default.maxClosedTunnelsPerRockGroup = LAOMod.Settings.DebugFloatingIslandmaxClosedTunnelsPerRockGroup;
            @default.minTunnelWidth = LAOMod.Settings.DebugFloatingIslandminTunnelWidth;
            @default.branchChance = LAOMod.Settings.DebugFloatingIslandbranchChance;
            @default.openTunnelsPer10k = LAOMod.Settings.DebugFloatingIslandopenTunnelsPer10k;
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
    }
}