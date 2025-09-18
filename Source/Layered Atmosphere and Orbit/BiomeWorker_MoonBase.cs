using RimWorld;
using RimWorld.Planet;
using System.Collections;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class BiomeWorker_MoonBase : BiomeWorker
    {
        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            return 0;
        }
    }
}

