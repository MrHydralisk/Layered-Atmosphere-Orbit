using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class FloatingIslandMapParent : AtmosphereMapParent
    {
        public PlanetTile parentPlanetTile = PlanetTile.Invalid;

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            PlanetLayer SurfacePlanetLayer = Find.WorldGrid.PlanetLayers.Values.FirstOrDefault((PlanetLayer pl) => pl.Def == PlanetLayerDefOf.Surface);
            LayeredAtmosphereOrbitDefModExtension laoDefModExtension = def.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (SurfacePlanetLayer != null && laoDefModExtension != null && !laoDefModExtension.availableBiomes.NullOrEmpty() && GenWorldClosest.TryFindClosestTile(SurfacePlanetLayer[Tile].tile, (PlanetTile x) => laoDefModExtension.availableBiomes.Contains(x.Tile.PrimaryBiome), out PlanetTile foundTile))
            {
                parentPlanetTile = foundTile;
                Tile.Tile.PrimaryBiome = parentPlanetTile.Tile.PrimaryBiome;
                Tile.Tile.hilliness = parentPlanetTile.Tile.hilliness;
                Tile.Tile.rainfall = parentPlanetTile.Tile.rainfall;
                Tile.Tile.feature = parentPlanetTile.Tile.feature;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref parentPlanetTile, "parentPlanetTile");
        }
    }
}

