﻿using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class WorldGenStep_LunaTerrain : WorldGenStep_Terrain
    {
        public override int SeedPart => 17111970;
        public float cratersPercent = 0.01f;
        public FloatRange cratersRadiusRange = new FloatRange(4,8);

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            SetupLunaElevationNoise(layer);
            SetupTemperatureOffsetNoise();
            SetupRainfallNoise();
            SetupLunaHillinessNoise(layer);
            SetupSwampinessNoise();
            for (int i = 0; i < layer.TilesCount * cratersPercent; i++)
            {
                int index = Rand.Range(0, layer.TilesCount);
                Vector3 tileCenter = layer.GetTileCenter(index);

            }
            layer.Tiles.Clear();
            List<string> strings = new List<string>() { $"GenerateFresh {layer.TilesCount}" };
            for (int i = 0; i < layer.TilesCount; i++)
            {
                Tile item = GenerateTileFor(new PlanetTile(i, layer), layer);
                Vector3 tileCenter = layer.GetTileCenter(item.tile);
                strings.Add($"{tileCenter}: {noiseElevation.GetValue(tileCenter)} {noiseMountainLines.GetValue(tileCenter)}");
                layer.Tiles.Add(item);
            }
            Log.Message(string.Join("\n", strings));
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

        //private void SetupTemperatureOffsetNoise()
        //{
        //    float freqMultiplier = FreqMultiplier;
        //    noiseTemperatureOffset = new Perlin(0.018f * freqMultiplier, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        //    noiseTemperatureOffset = new Multiply(noiseTemperatureOffset, new Const(4.0));
        //}

        //private void SetupRainfallNoise()
        //{
        //    float freqMultiplier = FreqMultiplier;
        //    ModuleBase input = new Perlin(0.015f * freqMultiplier, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        //    input = new ScaleBias(0.5, 0.5, input);
        //    NoiseDebugUI.StorePlanetNoise(input, "basePerlin");
        //    ModuleBase moduleBase = new AbsLatitudeCurve(new SimpleCurve
        //{
        //    { 0f, 1.12f },
        //    { 25f, 0.94f },
        //    { 45f, 0.7f },
        //    { 70f, 0.3f },
        //    { 80f, 0.05f },
        //    { 90f, 0.05f }
        //}, 100f);
        //    NoiseDebugUI.StorePlanetNoise(moduleBase, "latCurve");
        //    noiseRainfall = new Multiply(input, moduleBase);
        //    float num = 0.00022222222f;
        //    float num2 = -500f * num;
        //    ModuleBase input2 = new ScaleBias(num, num2, noiseElevation);
        //    input2 = new ScaleBias(-1.0, 1.0, input2);
        //    input2 = new Clamp(0.0, 1.0, input2);
        //    NoiseDebugUI.StorePlanetNoise(input2, "elevationRainfallEffect");
        //    noiseRainfall = new Multiply(noiseRainfall, input2);
        //    Func<double, double> processor = delegate (double val)
        //    {
        //        if (val < 0.0)
        //        {
        //            val = 0.0;
        //        }
        //        if (val < 0.12)
        //        {
        //            val = (val + 0.12) / 2.0;
        //            if (val < 0.03)
        //            {
        //                val = (val + 0.03) / 2.0;
        //            }
        //        }
        //        return val;
        //    };
        //    noiseRainfall = new Arbitrary(noiseRainfall, processor);
        //    noiseRainfall = new Power(noiseRainfall, new Const(1.5));
        //    noiseRainfall = new Clamp(0.0, 999.0, noiseRainfall);
        //    NoiseDebugUI.StorePlanetNoise(noiseRainfall, "noiseRainfall before mm");
        //    noiseRainfall = new ScaleBias(4000.0, 0.0, noiseRainfall);
        //    SimpleCurve rainfallCurve = Find.World.info.overallRainfall.GetRainfallCurve();
        //    if (rainfallCurve != null)
        //    {
        //        noiseRainfall = new CurveSimple(noiseRainfall, rainfallCurve);
        //    }
        //}

        private void SetupLunaHillinessNoise(PlanetLayer layer)
        {
            float freqMultiplier = FreqMultiplier;
            noiseMountainLines = new Perlin(0.025f * freqMultiplier, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
            ModuleBase module = new Perlin(0.06f * freqMultiplier, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
            noiseMountainLines = new Abs(noiseMountainLines);
            noiseMountainLines = new OneMinus(noiseMountainLines);
            module = new Filter(module, -0.3f, 1f);
            noiseMountainLines = new Multiply(noiseMountainLines, module);
            noiseMountainLines = new OneMinus(noiseMountainLines);
            NoiseDebugUI.StorePlanetNoise(noiseMountainLines, "noiseMountainLines");
            noiseHillsPatchesMacro = new Perlin(0.032f * freqMultiplier, 2.0, 0.5, 5, Rand.Range(0, int.MaxValue), QualityMode.Medium);
            noiseHillsPatchesMicro = new Perlin(0.19f * freqMultiplier, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        }

        //private void SetupSwampinessNoise()
        //{
        //    float freqMultiplier = FreqMultiplier;
        //    ModuleBase input = new Perlin(0.09f * freqMultiplier, 2.0, 0.4000000059604645, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        //    ModuleBase input2 = new RidgedMultifractal(0.025f * freqMultiplier, 2.0, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
        //    input = new ScaleBias(0.5, 0.5, input);
        //    input2 = new ScaleBias(0.5, 0.5, input2);
        //    noiseSwampiness = new Multiply(input, input2);
        //    InverseLerp rhs = new InverseLerp(noiseElevation, SwampinessMaxElevation.max, SwampinessMaxElevation.min);
        //    noiseSwampiness = new Multiply(noiseSwampiness, rhs);
        //    InverseLerp rhs2 = new InverseLerp(noiseRainfall, SwampinessMinRainfall.min, SwampinessMinRainfall.max);
        //    noiseSwampiness = new Multiply(noiseSwampiness, rhs2);
        //    NoiseDebugUI.StorePlanetNoise(noiseSwampiness, "noiseSwampiness");
        //}

        public override Tile GenerateTileFor(PlanetTile tile, PlanetLayer layer)
        {
            SurfaceTile surfaceTile = new SurfaceTile(tile);
            Vector3 tileCenter = layer.GetTileCenter(tile);
            surfaceTile.elevation = noiseElevation.GetValue(tileCenter);
            float value = noiseMountainLines.GetValue(tileCenter);
            if (value > 0.235f || surfaceTile.elevation <= 0f)
            {
                if (surfaceTile.elevation > 0f && noiseHillsPatchesMicro.GetValue(tileCenter) > 0.46f && noiseHillsPatchesMacro.GetValue(tileCenter) > -0.3f)
                {
                    if (Rand.Bool)
                    {
                        surfaceTile.hilliness = Hilliness.SmallHills;
                    }
                    else
                    {
                        surfaceTile.hilliness = Hilliness.LargeHills;
                    }
                }
                else
                {
                    surfaceTile.hilliness = Hilliness.Flat;
                }
            }
            else if (value > 0.12f)
            {
                switch (Rand.Range(0, 4))
                {
                    case 0:
                        surfaceTile.hilliness = Hilliness.Flat;
                        break;
                    case 1:
                        surfaceTile.hilliness = Hilliness.SmallHills;
                        break;
                    case 2:
                        surfaceTile.hilliness = Hilliness.LargeHills;
                        break;
                    case 3:
                        surfaceTile.hilliness = Hilliness.Mountainous;
                        break;
                }
            }
            else if (value > 0.0363f)
            {
                surfaceTile.hilliness = Hilliness.Mountainous;
            }
            else
            {
                surfaceTile.hilliness = Hilliness.Impassable;
            }
            for (int i = 0; i < layer.TilesCount; i++)
            {
                Tile item = GenerateTileFor(new PlanetTile(i, layer), layer);
                Vector3 tileCenter = layer.GetTileCenter(item.tile);
                layer.Tiles.Add(item);
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
            surfaceTile.rainfall = noiseRainfall.GetValue(tileCenter);
            if (float.IsNaN(surfaceTile.rainfall))
            {
                float value2 = noiseRainfall.GetValue(tileCenter);
                Log.ErrorOnce($"{value2} rain bad at {tile}", 694822);
            }
            if (surfaceTile.hilliness == Hilliness.Flat || surfaceTile.hilliness == Hilliness.SmallHills)
            {
                surfaceTile.swampiness = noiseSwampiness.GetValue(tileCenter);
            }
            surfaceTile.PrimaryBiome = BiomeFrom(surfaceTile, tile, layer);
            return surfaceTile;
        }

        //private BiomeDef BiomeFrom(Tile ws, PlanetTile tile, PlanetLayer layer)
        //{
        //    List<BiomeDef> allDefsListForReading = DefDatabase<BiomeDef>.AllDefsListForReading;
        //    BiomeDef biomeDef = null;
        //    float num = 0f;
        //    for (int i = 0; i < allDefsListForReading.Count; i++)
        //    {
        //        BiomeDef biomeDef2 = allDefsListForReading[i];
        //        if (biomeDef2.implemented && biomeDef2.generatesNaturally && biomeDef2.Worker.CanPlaceOnLayer(biomeDef2, layer))
        //        {
        //            float score = biomeDef2.Worker.GetScore(biomeDef2, ws, tile);
        //            if (score > num || biomeDef == null)
        //            {
        //                biomeDef = biomeDef2;
        //                num = score;
        //            }
        //        }
        //    }
        //    return biomeDef;
        //}

        //private static float BaseTemperatureAtLatitude(float lat)
        //{
        //    float x = Mathf.Abs(lat) / 90f;
        //    return AvgTempByLatitudeCurve.Evaluate(x);
        //}

        //private static float TemperatureReductionAtElevation(float elev)
        //{
        //    if (elev < 250f)
        //    {
        //        return 0f;
        //    }
        //    float t = (elev - 250f) / 4750f;
        //    return Mathf.Lerp(0f, 40f, t);
        //}
    }
}