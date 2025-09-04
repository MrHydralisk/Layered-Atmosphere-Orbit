using System.Collections.Generic;
using Verse;
using Verse.Noise;

namespace LayeredAtmosphereOrbit
{
    public class LAOSettings : ModSettings
    {
        public bool UseFuelCostBetweenLayers = false;
        public float FuelPerKm = 1;
        public bool ShowLayerInGroup = true;
        public bool AutoSwapLayerOnSelection = true;
        public float TransparentInGroup = 0.6f;
        public float TransparentInGroupSub = 0.3f;
        public List<string> AutoAddLayersDefNames = new List<string>();
        //debug
        public bool isOpenDebugFloatingIslandMapGen = false;
        public float DebugFloatingIslandRotation = Rand.Range(0f, 360f);
        public int DebugFloatingIslandPerlinSeedA = Rand.Int;
        public int DebugFloatingIslandPerlinSeedB = Rand.Int;
        public float DebugFloatingIslandRadius = 0.2f;
        public bool is1Scale = false;
        public float DebugFloatingIsland1Scale = 0.6499999761581421f;
        public bool is2Rotate = false;
        public bool is3Translate = false;
        public bool is4Blend = false;
        public float DebugFloatingIsland4Perlin = 0.006000000052154064f;
        public float DebugFloatingIsland4PerlinA = 2.0f;
        public float DebugFloatingIsland4PerlinB = 2.0f;
        public int DebugFloatingIsland4PerlinC = 3;
        public float DebugFloatingIsland4Const = 0.800000011920929f;
        public bool is5Blend = false;
        public float DebugFloatingIsland5Perlin = 0.05000000074505806f;
        public float DebugFloatingIsland5PerlinA = 2.0f;
        public float DebugFloatingIsland5PerlinB = 0.5f;
        public int DebugFloatingIsland5PerlinC = 6;
        public float DebugFloatingIsland5Const = 0.8500000238418579f;
        public bool is6Power = false;
        public float DebugFloatingIslandConst = 0.20000000298023224f;
        public float DebugFloatingIslandFloorThreshold = 0.5f;
        public float DebugFloatingIslandWallThreshold = 0.7f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UseFuelCostBetweenLayers, "UseFuelCostBetweenLayers", defaultValue: false);
            Scribe_Values.Look(ref FuelPerKm, "FuelPerKm", defaultValue: 1);
            Scribe_Values.Look(ref ShowLayerInGroup, "ShowLayerInGroup", defaultValue: true);
            Scribe_Values.Look(ref AutoSwapLayerOnSelection, "AutoSwapLayerOnSelection", defaultValue: true);
            Scribe_Values.Look(ref TransparentInGroup, "TransparentInGroup", defaultValue: 0.6f);
            Scribe_Values.Look(ref TransparentInGroupSub, "TransparentInGroupSub", defaultValue: 0.3f);
            Scribe_Collections.Look(ref AutoAddLayersDefNames, "AutoAddLayersDefNames", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (AutoAddLayersDefNames == null)
                {
                    AutoAddLayersDefNames = new List<string>();
                }
            }
            //debug
            Scribe_Values.Look(ref isOpenDebugFloatingIslandMapGen, "isOpenDebugFloatingIslandMapGen", defaultValue: false);
            Scribe_Values.Look(ref DebugFloatingIslandRadius, "DebugFloatingIslandRadius", defaultValue: 0.2f);
            Scribe_Values.Look(ref is1Scale, "is1Scale");
            Scribe_Values.Look(ref DebugFloatingIsland1Scale, "DebugFloatingIsland1Scale", 0.6499999761581421f);
            Scribe_Values.Look(ref is2Rotate, "is2Rotate");
            Scribe_Values.Look(ref is3Translate, "is3Translate");
            Scribe_Values.Look(ref is4Blend, "is4Blend");
            Scribe_Values.Look(ref DebugFloatingIsland4Perlin, "DebugFloatingIsland4Perlin", 0.006000000052154064f);
            Scribe_Values.Look(ref DebugFloatingIsland4PerlinA, "DebugFloatingIsland4PerlinA", 2.0f);
            Scribe_Values.Look(ref DebugFloatingIsland4PerlinB, "DebugFloatingIsland4PerlinB", 2.0f);
            Scribe_Values.Look(ref DebugFloatingIsland4PerlinC, "DebugFloatingIsland4PerlinC", 3);
            Scribe_Values.Look(ref DebugFloatingIsland4Const, "DebugFloatingIsland4Const", 0.800000011920929f);
            Scribe_Values.Look(ref is5Blend, "is5Blend");
            Scribe_Values.Look(ref DebugFloatingIsland5Perlin, "DebugFloatingIsland5Perlin", 0.05000000074505806f);
            Scribe_Values.Look(ref DebugFloatingIsland5PerlinA, "DebugFloatingIsland5PerlinA", 2.0f);
            Scribe_Values.Look(ref DebugFloatingIsland5PerlinB, "DebugFloatingIsland5PerlinB", 0.5f);
            Scribe_Values.Look(ref DebugFloatingIsland5PerlinC, "DebugFloatingIsland5PerlinC", 6);
            Scribe_Values.Look(ref DebugFloatingIsland5Const, "DebugFloatingIsland5Const", 0.8500000238418579f);
            Scribe_Values.Look(ref is6Power, "is6Power");
            Scribe_Values.Look(ref DebugFloatingIslandConst, "DebugFloatingIslandConst", 0.20000000298023224f);
            Scribe_Values.Look(ref DebugFloatingIslandFloorThreshold, "DebugFloatingIslandFloorThreshold", 0.5f);
            Scribe_Values.Look(ref DebugFloatingIslandWallThreshold, "DebugFloatingIslandWallThreshold", 0.7f);
        }
    }
}

