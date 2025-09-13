using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class FloatingIslandMapParent : AtmosphereMapParent
    {
        public PlanetTile parentPlanetTile = PlanetTile.Invalid;

        public ThingDef rockDef;
        private Color cachedRockColor = Color.white;
        public override Color ExpandingIconColor => cachedRockColor;

        public override void PostMake()
        {
            base.PostMake();
            rockDef = Find.World.NaturalRockTypesIn(Tile).RandomElement();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            PlanetLayer SurfacePlanetLayer = Find.WorldGrid.PlanetLayers.Values.FirstOrDefault((PlanetLayer pl) => pl.Def == PlanetLayerDefOf.Surface);
            LayeredAtmosphereOrbitDefModExtension laoDefModExtension = def.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
            if (laoDefModExtension != null)
            {
                if (SurfacePlanetLayer != null && !laoDefModExtension.availableBiomes.NullOrEmpty() && GenWorldClosest.TryFindClosestTile(SurfacePlanetLayer[Tile].tile, (PlanetTile x) => laoDefModExtension.availableBiomes.Contains(x.Tile.PrimaryBiome), out PlanetTile foundTile))
                {
                    parentPlanetTile = foundTile;
                    Tile.Tile.PrimaryBiome = parentPlanetTile.Tile.PrimaryBiome;
                    Tile.Tile.hilliness = (Hilliness)Mathf.Min((int)Hilliness.LargeHills, (int)parentPlanetTile.Tile.hilliness);
                    Tile.Tile.rainfall = parentPlanetTile.Tile.rainfall;
                    Tile.Tile.feature = parentPlanetTile.Tile.feature;
                }
                if (laoDefModExtension.isRockColored)
                {
                    cachedRockColor = rockDef?.graphicData?.color ?? Color.white;
                }
            }
        }

        public override string GetExtraInspectString()
        {
            if (preciousResource != null)
            {
                return "LayeredAtmosphereOrbit.FloatingIslandMapParent.TracesOfPreciousResource".Translate((rockDef ?? ThingDefOf.Vacstone).LabelCap, preciousResource.label);
            }
            return "";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref parentPlanetTile, "parentPlanetTile");
            Scribe_Defs.Look(ref rockDef, "rockDef");
        }
    }
}

