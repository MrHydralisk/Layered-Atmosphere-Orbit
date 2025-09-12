using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIsland : GenStep
    {
        public List<GenStep_Asteroid.MineableCountConfig> mineableCounts;

        public IntRange numChunks;

        public float ruinsChance;

        public float archeanTreeChance;

        public OrbitalDebrisDef orbitalDebris;

        public static SimpleCurve edgeElevationMult = new SimpleCurve
        {
            new CurvePoint(0, 0),
            new CurvePoint(1, 0.2f),
            new CurvePoint(2, 0.6f),
            new CurvePoint(3, 0.8f),
            new CurvePoint(4, 0.9f),
            new CurvePoint(5, 0.95f),
            new CurvePoint(6, 1f)
        };

        public float widthOffsetPerCell = 0.015f;
        public int maxOpenTunnelsPerRockGroup = 2;
        public int maxClosedTunnelsPerRockGroup = 2;
        public float minTunnelWidth = 0.25f;
        public float branchChance = 0.05f;
        public float openTunnelsPer10k = 4f;
        public SimpleCurve tunnelsWidthPerRockCount = new SimpleCurve
        {
            new CurvePoint(100f, 1f),
            new CurvePoint(300f, 1.5f),
            new CurvePoint(3000f, 1.9f)
        };

        public float FloorThreshold = 0.5f;
        public float WallThreshold = 0.7f;
        public float ThickRoofThreshold = float.MaxValue;

        public float radiuPercToStartFlood = 0.05f;

        public FloatRange SoilThreshold = FloatRange.Zero;

        private ModuleBase innerNoise;

        public override int SeedPart => 1929282;

        public float Radius;

        public ThingDef rockDef;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (ModLister.OdysseyInstalled)
            {
                rockDef = Find.World.NaturalRockTypesIn(map.Tile).RandomElement();
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
                if (orbitalDebris != null)
                {
                    map.OrbitalDebris = orbitalDebris;
                }
            }
            map.regionAndRoomUpdater.Enabled = true;
        }

        protected virtual void SpawnFloatingIsland(Map map)
        {
            using (map.pathing.DisableIncrementalScope())
            {
                foreach (IntVec3 allCell in map.AllCells)
                {
                    float num = MapGenerator.Elevation[allCell];
                    float num2 = MapGenerator.Caves[allCell];
                    float fertility = MapGenerator.Fertility[allCell];
                    if (num > FloorThreshold)
                    {
                        TerrainDef terrainDef = rockDef.building.naturalTerrain;
                        if (SoilThreshold.Includes(num) && GenAdjFast.AdjacentCells8Way(allCell).All((IntVec3 adjCell) => MapGenerator.Elevation[adjCell] > FloorThreshold))
                        {
                            terrainDef = TerrainThreshold.TerrainAtValue(map.Biome.terrainsByFertility, fertility);
                        }
                        map.terrainGrid.SetTerrain(allCell, terrainDef);
                    }
                    if (num > WallThreshold && num2 == 0f)
                    {
                        GenSpawn.Spawn(rockDef, allCell, map);
                    }
                    if (num > WallThreshold)
                    {
                        RoofDef roofDef = RoofDefOf.RoofRockThin;
                        if (num > ThickRoofThreshold)
                        {
                            roofDef = RoofDefOf.RoofRockThick;
                        }
                        map.roofGrid.SetRoof(allCell, roofDef);
                    }
                }
                HashSet<IntVec3> mainIsland = new HashSet<IntVec3>();
                float radius = map.Size.x * radiuPercToStartFlood;
                List<IntVec3> centerArea = new CellRect(Mathf.RoundToInt(map.Center.x - radius), Mathf.RoundToInt(map.Center.z - radius), Mathf.RoundToInt(radius * 2), Mathf.RoundToInt(radius * 2)).Where((IntVec3 x) => map.Center.DistanceTo(x) <= radius && x.GetTerrain(map) != DefOfLocal.LAO_Air).ToList();
                int iteration = 0;
                while (!centerArea.NullOrEmpty() && iteration < 1000)
                {
                    iteration++;
                    IntVec3 startingTile = centerArea.First();
                    map.floodFiller.FloodFill(startingTile, (IntVec3 x) => x.GetTerrain(map) != DefOfLocal.LAO_Air, delegate (IntVec3 x)
                    {
                        mainIsland.Add(x);
                        int index = centerArea.IndexOf(x);
                        if (index > -1)
                        {
                            centerArea.RemoveAt(index);
                        }
                    });
                }
                if (iteration >= 1000)
                {
                    Log.Warning("Exceeded iteration limit during SpawnFloatingIsland");
                }
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
                int distToEdge = allCell.DistanceToEdge(map);
                if (MapGenerator.Elevation[allCell] > 0 && distToEdge <= 6)
                {
                    MapGenerator.Elevation[allCell] = MapGenerator.Elevation[allCell] * edgeElevationMult.Evaluate(distToEdge);
                }
            }
        }

        protected virtual ModuleBase ConfigureNoise(Map map, GenStepParams parms)
        {
            ModuleBase input = new DistFromPoint((float)map.Size.x * Radius);
            input = new ScaleBias(-1.0, 1.0, input);
            input = new Scale(0.6499999761581421, 1.0, 1.0, input);
            input = new Rotate(0.0, Rand.Range(0f, 360f), 0.0, input);
            input = new Translate(-map.Center.x, 0.0, -map.Center.z, input);
            NoiseDebugUI.StoreNoiseRender(input, "Base asteroid shape");
            input = new Blend(new Perlin(0.006000000052154064, 2.0, 2.0, 3, Rand.Int, QualityMode.Medium), input, new Const(0.800000011920929));
            input = new Blend(new Perlin(0.05000000074505806, 2.0, 0.5, 6, Rand.Int, QualityMode.Medium), input, new Const(0.8500000238418579));
            input = new Power(input, new Const(0.20000000298023224));
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
            @default.widthOffsetPerCell = widthOffsetPerCell;
            @default.maxOpenTunnelsPerRockGroup = maxOpenTunnelsPerRockGroup;
            @default.maxClosedTunnelsPerRockGroup = maxClosedTunnelsPerRockGroup;
            @default.minTunnelWidth = minTunnelWidth;
            @default.branchChance = branchChance;
            @default.openTunnelsPer10k = openTunnelsPer10k;
            @default.tunnelsWidthPerRockCount = tunnelsWidthPerRockCount;
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
                return elevation[c] > WallThreshold;
            }
            return false;
        }
    }
}