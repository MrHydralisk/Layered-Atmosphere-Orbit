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
        public float cratersPercent = 0.005f;
        public FloatRange cratersRadiusRange = new FloatRange(4, 6);

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            SetupLunaElevationNoise(layer);
            SetupTemperatureOffsetNoise();
            SetupRainfallNoise();
            SetupLunaHillinessNoise(layer);
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
            float freqMultiplier = FreqMultiplier;
            ModuleBase lhs = new Perlin(0.035f * freqMultiplier, 2.0, 0.4000000059604645, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
            ModuleBase lhs2 = new RidgedMultifractal(0.012f * freqMultiplier, 2.0, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
            ModuleBase input = new Perlin(0.12f * freqMultiplier, 2.0, 0.5, 5, Rand.Range(0, int.MaxValue), QualityMode.High);
            ModuleBase moduleBase = new Perlin(0.01f * freqMultiplier, 2.0, 0.5, 5, Rand.Range(0, int.MaxValue), QualityMode.High);
            float num;
            if (Find.World.PlanetCoverage < 0.55f)
            {
                ModuleBase input2 = new DistanceFromPlanetViewCenter(layer.ViewCenter, Find.WorldGrid.SurfaceViewAngle, invert: true);
                input2 = new ScaleBias(2.0, -1.0, input2);
                moduleBase = new Blend(moduleBase, input2, new Const(0.4000000059604645));
                num = Rand.Range(-0.4f, -0.35f);
            }
            else
            {
                num = Rand.Range(0.15f, 0.25f);
            }
            NoiseDebugUI.StorePlanetNoise(moduleBase, "elevContinents");
            input = new ScaleBias(0.5, 0.5, input);
            lhs2 = new Multiply(lhs2, input);
            float num2 = Rand.Range(0.4f, 0.6f);
            noiseElevation = new Blend(lhs, lhs2, new Const(num2));
            noiseElevation = new Blend(noiseElevation, moduleBase, new Const(num));
            if (Find.World.PlanetCoverage < 0.9999f)
            {
                noiseElevation = new ConvertToIsland(Find.WorldGrid.SurfaceViewCenter, Find.WorldGrid.SurfaceViewAngle, noiseElevation);
            }
            noiseElevation = new ScaleBias(0.5, 0.5, noiseElevation);
            noiseElevation = new Power(noiseElevation, new Const(3.0));
            NoiseDebugUI.StorePlanetNoise(noiseElevation, "noiseElevation");
            noiseElevation = new ScaleBias(ElevationRange.Span, ElevationRange.min, noiseElevation);
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