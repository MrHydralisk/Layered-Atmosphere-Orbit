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

            LayeredAtmosphereOrbitUtility.ResetLayerData();
            InjectScenarios();

            if (LAOMod.Settings.ReplaceAllViewLayerGizmo)
            {
                val.Patch(AccessTools.Method(typeof(WorldGrid).GetNestedTypes(AccessTools.all).First((Type t) => t.Name.Contains("<GetGizmos>d__103")), "MoveNext"), transpiler: new HarmonyMethod(patchType, "WG_GetGizmos_Transpiler"));
            }
            val.Patch(AccessTools.Method(typeof(WorldGrid), "GetGizmos"), postfix: new HarmonyMethod(patchType, "WG_GetGizmos_Postfix"));
            if (LAOMod.Settings.ShowLayerInGroup)
            {
                val.Patch(AccessTools.Method(typeof(ExpandableWorldObjectsUtility), "TransitionPct"), postfix: new HarmonyMethod(patchType, "EWOU_TransitionPct_Postfix"));
                val.Patch(AccessTools.Property(typeof(WorldObject), "VisibleInBackground").GetGetMethod(), postfix: new HarmonyMethod(patchType, "WO_VisibleInBackground_Postfix"));
            }
            if (LAOMod.Settings.AutoSwapLayerOnSelection)
            {
                val.Patch(AccessTools.Method(typeof(WorldSelector), "Select"), postfix: new HarmonyMethod(patchType, "WS_Select_Postfix"));
            }
        }

        public static void InjectScenarios()
        {
            List<PlanetLayerDef> AllPlanetLayerDefs = DefDatabase<PlanetLayerDef>.AllDefs.ToList();
            foreach (PlanetLayerDef planetLayerDef in AllPlanetLayerDefs)
            {
                LayeredAtmosphereOrbitDefModExtension laoDefModExtension = planetLayerDef.GetModExtension<LayeredAtmosphereOrbitDefModExtension>();
                if (laoDefModExtension?.isOptionToAutoAdd ?? false)
                {
                    LAOMod.AutoAddLayerOptions.Add(planetLayerDef);
                }
            }
            foreach (Scenario scenario in ScenarioLister.AllScenarios())
            {
                LayeredAtmosphereOrbitUtility.TryAddPlanetLayerts(scenario);
            }
        }

        public static IEnumerable<CodeInstruction> WG_GetGizmos_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (codes[i].operand?.ToString().Contains("get_IsSelected") ?? false))
                {
                    codes[i] = new CodeInstruction(OpCodes.Pop);
                    codes[i + 1].opcode = OpCodes.Br;
                }
            }
            return codes.AsEnumerable();
        }

        public static void WG_GetGizmos_Postfix(ref IEnumerable<Gizmo> __result, WorldGrid __instance, Dictionary<int, PlanetLayer> ___planetLayers)
        {
            List<Gizmo> NGizmos = __result.ToList();
            if (Current.ProgramState != ProgramState.Entry && ___planetLayers.Count > 1)
            {
                PlanetLayer currentLayer = PlanetLayer.Selected;
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Label".Translate(),
                    defaultDesc = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Desc".Translate(currentLayer.Def.label, currentLayer.Def.viewGizmoTooltip),
                    icon = currentLayer.Def.ViewGizmoTexture,
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
                            }, planetLayer.Def.ViewGizmoTexture, Color.white, orderInPriority: (int)planetLayer.Def.Elevation());
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
                NGizmos.Insert(LAOMod.Settings.ReplaceAllViewLayerGizmo ? 0 : 1, command_Action);
            }
            __result = NGizmos;
        }

        public static void EWOU_TransitionPct_Postfix(ref float __result, WorldObject wo)
        {
            if ((__result == 1 || wo.def.fullyExpandedInSpace) && wo.Tile.Layer != Find.WorldSelector.SelectedLayer)
            {
                __result = wo.Tile.Layer.Def.VisibleInBackgroundOfCurrentLayer();
            }
        }

        public static void WO_VisibleInBackground_Postfix(ref bool __result, WorldObject __instance)
        {
            __result = __result || __instance.Tile.LayerDef.VisibleInBackgroundOfCurrentLayer() > 0;
        }

        public static void WS_Select_Postfix(WorldSelector __instance, WorldObject obj)
        {
            if (obj.Tile.Layer != __instance.SelectedLayer && __instance.NumSelectedObjects <= 1)
            {
                __instance.SelectedLayer = obj.Tile.Layer;
                if (__instance.SelectedTile == PlanetTile.Invalid)
                {
                    __instance.Select(obj);
                }
            }
        }
    }
}
