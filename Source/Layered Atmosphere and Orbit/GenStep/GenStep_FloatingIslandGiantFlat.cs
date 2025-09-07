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

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandGiantFlat : GenStep_FloatingIslandGiant
    {
        public override float WallThreshold => 0.95f;
        public FloatRange SoilThreshold = new FloatRange (0.6f, 0.92f);

        protected override void SpawnFloatingIsland(Map map)
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
    }
}