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
    public class GenStep_FloatingIslandCollapsing : GenStep_FloatingIsland
    {
        public override float WallThreshold => 1;

        protected override ModuleBase ConfigureNoise(Map map, GenStepParams parms)
        {
            ModuleBase input = new DistFromPoint((float)map.Size.x * Radius);
            input = new ScaleBias(-1.0, 1.0, input);
            input = new Scale(0.7999999761581421, 1.0, 0.7999999761581421, input);
            input = new Rotate(0.0, Rand.Range(0f, 360f), 0.0, input);
            input = new Translate(-map.Center.x, 0.0, -map.Center.z, input);
            NoiseDebugUI.StoreNoiseRender(input, "Base asteroid shape");
            input = new Blend(new Perlin(0.006000000052154064, 4.0, 4.0, 3, Rand.Int, QualityMode.Medium), input, new Const(0.800000011920929));
            input = new Blend(new Perlin(0.02000000074505806, 2.0, 0.5, 6, Rand.Int, QualityMode.Medium), input, new Const(0.8500000238418579));
            input = new Power(input, new Const(0.35000000298023224));
            NoiseDebugUI.StoreNoiseRender(input, "Asteroid");
            return input;
        }
    }
}