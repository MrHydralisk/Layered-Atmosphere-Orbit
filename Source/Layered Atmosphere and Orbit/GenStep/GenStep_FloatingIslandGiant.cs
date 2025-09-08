using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandGiant : GenStep_FloatingIsland
    {
        protected override ModuleBase ConfigureNoise(Map map, GenStepParams parms)
        {
            ModuleBase input = new DistFromPoint((float)map.Size.x * Radius);
            input = new ScaleBias(-1.0, 1.0, input);
            input = new Scale(0.5499999761581421, 1.0, 0.9, input);
            input = new Rotate(0.0, Rand.Range(0f, 360f), 0.0, input);
            input = new Translate(-map.Center.x, 0.0, -map.Center.z, input);
            NoiseDebugUI.StoreNoiseRender(input, "Base asteroid shape");
            input = new Blend(new Perlin(0.006000000052154064, 2.0, 2.0, 3, Rand.Int, QualityMode.Medium), input, new Const(0.800000011920929));
            input = new Blend(new Perlin(0.05000000074505806, 2.0, 0.5, 6, Rand.Int, QualityMode.Medium), input, new Const(0.8500000238418579));
            input = new Power(input, new Const(0.20000000298023224));
            NoiseDebugUI.StoreNoiseRender(input, "Asteroid");
            return input;
        }
    }
}