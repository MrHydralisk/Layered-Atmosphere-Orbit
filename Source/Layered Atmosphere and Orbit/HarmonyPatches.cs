using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.LayeredAtmosphereOrbit");

            {
                foreach (Scenario scenario in ScenarioLister.AllScenarios())
                {
                    LayeredAtmosphereOrbitUtility.TryAddPlanetLayerts(scenario);
                }
            }
        }
    }
}
