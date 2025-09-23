using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class WorldGenStep_LunaTerrain : WorldGenStep_Terrain
    {
        public override int SeedPart => 17111970;
        public float cratersPercent = 0.01f;
        public FloatRange cratersRadiusRange = new FloatRange(4, 8);

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            SetupLunaHillinessNoise(layer);
            SetupLunaElevationNoise(layer);
            SetupTemperatureOffsetNoise();
            SetupRainfallNoise();
            SetupSwampinessNoise();
            layer.Tiles.Clear();
            for (int i = 0; i < layer.TilesCount; i++)
            {
                Tile item = GenerateTileFor(new PlanetTile(i, layer), layer);
                Vector3 tileCenter = layer.GetTileCenter(item.tile);
                layer.Tiles.Add(item);
            }
        }

        private void SetupLunaElevationNoise(PlanetLayer layer)
        {
            noiseElevation = new ScaleBias(ElevationRange.Span, ElevationRange.min, noiseMountainLines);
            NoiseDebugUI.StorePlanetNoise(noiseElevation, "noiseElevation");
        }

        private void SetupLunaHillinessNoise(PlanetLayer layer)
        {
            List<(Vector3, float)> impactPoints = new List<(Vector3, float)>();
            float tileSize = layer.AverageTileSize;
            for (int i = 0; i < layer.TilesCount * cratersPercent; i++)
            {
                int index = Rand.Range(0, layer.TilesCount);
                Vector3 tileCenter = layer.GetTileCenter(index);
                impactPoints.Add((tileCenter, tileSize * cratersRadiusRange.RandomInRange));
            }
            noiseMountainLines = new Crators(impactPoints, 0, 0.2f, 3 * tileSize, 6 * tileSize);
            NoiseDebugUI.StorePlanetNoise(noiseMountainLines, "noiseMountainLines");
        }

        public override Tile GenerateTileFor(PlanetTile tile, PlanetLayer layer)
        {
            SurfaceTile surfaceTile = new SurfaceTile(tile);
            Vector3 tileCenter = layer.GetTileCenter(tile);
            surfaceTile.elevation = noiseElevation.GetValue(tileCenter);
            float value = noiseMountainLines.GetValue(tileCenter);
            if (value > 0.8)
            {
                if (Rand.Chance(0.2f))
                {
                    surfaceTile.hilliness = Hilliness.Impassable;
                }
                else
                {
                    surfaceTile.hilliness = Hilliness.Mountainous;
                }
            }
            else if (value > 0.6)
            {
                surfaceTile.hilliness = Hilliness.Mountainous;
            }
            else if (value > 0.4)
            {
                surfaceTile.hilliness = Hilliness.LargeHills;
            }
            else if (value > 0.2)
            {
                surfaceTile.hilliness = Hilliness.SmallHills;
            }
            else
            {
                surfaceTile.hilliness = Hilliness.Flat;
            }
            float num = BaseTemperatureAtLatitude(layer.LongLatOf(tile).y);
            num -= TemperatureReductionAtElevation(surfaceTile.elevation);
            num += noiseTemperatureOffset.GetValue(tileCenter);
            SimpleCurve temperatureCurve = Find.World.info.overallTemperature.GetTemperatureCurve();
            if (temperatureCurve != null)
            {
                num = temperatureCurve.Evaluate(num);
            }
            surfaceTile.temperature = num;
            surfaceTile.rainfall = 0;
            surfaceTile.PrimaryBiome = BiomeFrom(surfaceTile, tile, layer);
            return surfaceTile;
        }
    }
}