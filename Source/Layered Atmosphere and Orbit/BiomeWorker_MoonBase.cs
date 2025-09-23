using RimWorld;
using RimWorld.Planet;

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

