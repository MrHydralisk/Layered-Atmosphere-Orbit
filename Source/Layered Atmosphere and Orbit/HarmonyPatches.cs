using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
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
            
            val.Patch(AccessTools.Method(typeof(WorldGrid), "GetGizmos"), postfix: new HarmonyMethod(patchType, "WG_GetGizmos_Postfix"));
        }

        public static void WG_GetGizmos_Postfix(ref IEnumerable<Gizmo> __result, WorldGrid __instance, Dictionary<int, PlanetLayer> ___planetLayers)
        {
            List<Gizmo> NGizmos = __result.ToList();
            if (___planetLayers.Count > 1)
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "Layer",
                    hotKey = KeyBindingDefOf.Misc1,
                    defaultDesc = "All layers",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ViewPlanet"),
                    action = delegate
                    {
                        List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();

                        for (int i = 0; i < ___planetLayers.Count; i++)
                        {
                            PlanetLayer planetLayer = ___planetLayers[i];
                            AcceptanceReport acceptanceReportPL = planetLayer.CanSelectLayer();
                            FloatMenuOption floatMenuOption = new FloatMenuOption("WorldSelectLayer".Translate(planetLayer.Def.Named("LAYER")), delegate
                            {
                                PlanetLayer.Selected = planetLayer;
                            }, orderInPriority: (int)planetLayer.Def.Elevation());
                            if (!acceptanceReportPL.Accepted)
                            {
                                floatMenuOption.Disabled = true;
                                floatMenuOption.Label += $"[{acceptanceReportPL.Reason}]";
                            }
                            floatMenuOptions.Add(floatMenuOption);
                        }
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                    }
                };
                NGizmos.Insert(1, command_Action);
            }
            __result = NGizmos;
        }
    }
}
