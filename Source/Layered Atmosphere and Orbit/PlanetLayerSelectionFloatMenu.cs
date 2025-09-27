using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LayeredAtmosphereOrbit
{
    public class PlanetLayerSelectionFloatMenu : Window
    {
        private static CachedTexture currentIcon = new CachedTexture("UI/Misc/AlertFlashArrow");
        public bool vanishIfMouseDistant = true;

        private string titlePlanet;
        private string titleGroup;
        private string titleLayer;

        protected Dictionary<PlanetDef, (FloatMenuOption, List<PlanetLayerGroupDef>)> planets;
        protected Dictionary<PlanetLayerGroupDef, (FloatMenuOption, List<PlanetLayer>)> groups;
        protected Dictionary<PlanetLayer, FloatMenuOption> layers;

        protected List<(PlanetDef, FloatMenuOption)> planetsOptions;
        protected List<(PlanetLayerGroupDef, FloatMenuOption)> groupsOptions;
        protected List<(PlanetLayer, FloatMenuOption)> layersOptions;

        protected PlanetDef currentPlanet;
        protected PlanetLayerGroupDef currentGroup;
        protected PlanetLayer currentLayer;

        private Color baseColor = Color.white;

        private Vector2 scrollPositionLayer;
        private Vector2 scrollPositionGroup;
        private Vector2 scrollPositionPlanet;

        private Vector2 mouseClickPos;

        private static readonly Vector2 InitialPositionShift = new Vector2(4f, 0f);

        public float DistBetweenType = 1f;

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2((TotalWidthLayers + TotalWidthGroups + TotalWidthPlanets + Margin * 2), TotalWindowHeight);

        private float MaxWindowHeight => UI.screenHeight * 0.27f;

        private float TotalWindowHeight => Mathf.Max(PlanetWindowHeight, GroupWindowHeight, LayerWindowHeight);
        private float PlanetWindowHeight => Mathf.Min(MaxWindowHeight, TotalPlanetsHeight) + 1;
        private float GroupWindowHeight => Mathf.Min(MaxWindowHeight, TotalGroupsHeight) + 1;
        private float LayerWindowHeight => Mathf.Min(MaxWindowHeight, TotalLayersHeight) + 1;


        private float TotalLayersHeight
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < layersOptions.Count; i++)
                {
                    float requiredHeight = layersOptions[i].Item2.RequiredHeight;
                    num += requiredHeight + -1f;
                }
                return num;
            }
        }

        private float TotalGroupsHeight
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < groupsOptions.Count; i++)
                {
                    float requiredHeight = groupsOptions[i].Item2.RequiredHeight;
                    num += requiredHeight + -1f;
                }
                return num;
            }
        }

        private float TotalPlanetsHeight
        {
            get
            {
                float num = 0f;
                for (int i = 0; i < planetsOptions.Count; i++)
                {
                    float requiredHeight = planetsOptions[i].Item2.RequiredHeight;
                    num += requiredHeight + -1f;
                }
                return num;
            }
        }


        private float TotalWidthLayers
        {
            get
            {
                float num = ColumnWidthLayer;
                if (UsingScrollbarLayers)
                {
                    num += 16f;
                }
                return num;
            }
        }

        private float TotalWidthGroups
        {
            get
            {
                float num = ColumnWidthGroup;
                if (UsingScrollbarGroups)
                {
                    num += 16f;
                }
                return num;
            }
        }

        private float TotalWidthPlanets
        {
            get
            {
                float num = ColumnWidthPlanet;
                if (UsingScrollbarPlanets)
                {
                    num += 16f;
                }
                return num;
            }
        }


        private float ColumnWidthLayer
        {
            get
            {
                float num = 75f;
                for (int i = 0; i < layersOptions.Count; i++)
                {
                    float requiredWidth = layersOptions[i].Item2.RequiredWidth;
                    if (requiredWidth >= 235f)
                    {
                        return 235f;
                    }
                    if (requiredWidth > num)
                    {
                        num = requiredWidth;
                    }
                }
                return Mathf.Round(num);
            }
        }

        private float ColumnWidthGroup
        {
            get
            {
                float num = 75f;
                for (int i = 0; i < groupsOptions.Count; i++)
                {
                    float requiredWidth = groupsOptions[i].Item2.RequiredWidth;
                    if (requiredWidth >= 235f)
                    {
                        return 235f;
                    }
                    if (requiredWidth > num)
                    {
                        num = requiredWidth;
                    }
                }
                return Mathf.Round(num);
            }
        }

        private float ColumnWidthPlanet
        {
            get
            {
                float num = 75f;
                for (int i = 0; i < planetsOptions.Count; i++)
                {
                    float requiredWidth = planetsOptions[i].Item2.RequiredWidth;
                    if (requiredWidth >= 235f)
                    {
                        return 235f;
                    }
                    if (requiredWidth > num)
                    {
                        num = requiredWidth;
                    }
                }
                return Mathf.Round(num);
            }
        }


        private bool UsingScrollbarLayers
        {
            get
            {
                if (layersOptions == null)
                {
                    return false;
                }
                Text.Font = GameFont.Small;
                float requiredHeight = 0f;
                float maxWindowHeight = MaxWindowHeight;
                for (int i = 0; i < layersOptions.Count; i++)
                {
                    requiredHeight += layersOptions[i].Item2.RequiredHeight;
                    if (requiredHeight + -1f > maxWindowHeight)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool UsingScrollbarGroups
        {
            get
            {
                if (groupsOptions == null)
                {
                    return false;
                }
                Text.Font = GameFont.Small;
                float requiredHeight = 0f;
                float maxWindowHeight = MaxWindowHeight;
                for (int i = 0; i < groupsOptions.Count; i++)
                {
                    requiredHeight += groupsOptions[i].Item2.RequiredHeight;
                    if (requiredHeight + -1f > maxWindowHeight)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool UsingScrollbarPlanets
        {
            get
            {
                if (planetsOptions == null)
                {
                    return false;
                }
                Text.Font = GameFont.Small;
                float requiredHeight = 0f;
                float maxWindowHeight = MaxWindowHeight;
                for (int i = 0; i < planetsOptions.Count; i++)
                {
                    requiredHeight += planetsOptions[i].Item2.RequiredHeight;
                    if (requiredHeight + -1f > maxWindowHeight)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        public PlanetLayerSelectionFloatMenu(PlanetLayer currentLayer, List<PlanetLayer> allLayers)
        {
            mouseClickPos = UI.MousePositionOnUIInverted;
            layers = new Dictionary<PlanetLayer, FloatMenuOption>();
            groups = new Dictionary<PlanetLayerGroupDef, (FloatMenuOption, List<PlanetLayer>)>();
            planets = new Dictionary<PlanetDef, (FloatMenuOption, List<PlanetLayerGroupDef>)>();
            for (int i = 0; i < allLayers.Count; i++)
            {
                PlanetLayer planetLayer = allLayers[i];
                PlanetLayerGroupDef planetLayerGroup = planetLayer.Def.LayerGroup();
                PlanetDef planet = planetLayerGroup?.planet;
                AcceptanceReport acceptanceReportPL = planetLayer.CanSelectLayer();
                FloatMenuOption layerOption = new FloatMenuOption("WorldSelectLayer".Translate(planetLayer.Def.Named("LAYER")), delegate
                {
                    PlanetLayer.Selected = planetLayer;
                }, planetLayer.Def.ViewGizmoTexture, Color.white, orderInPriority: (int)planetLayer.Def.Elevation())
                {
                    tooltip = new TipSignal(planetLayer.Def.viewGizmoTooltip, planetLayer.Def.index ^ 0x1241961)
                };
                if (!acceptanceReportPL.Accepted)
                {
                    layerOption.Disabled = true;
                    layerOption.Label += $"[{acceptanceReportPL.Reason}]";
                }
                layerOption.SetSizeMode(FloatMenuSizeMode.Normal);
                layers.AddDistinct(planetLayer, layerOption);
                if (groups.TryGetValue(planetLayerGroup, out (FloatMenuOption, List<PlanetLayer>) planetLayerGroupFloat))
                {
                    planetLayerGroupFloat.Item2.AddDistinct(planetLayer);
                }
                else
                {
                    groups.Add(planetLayerGroup, (null, new List<PlanetLayer>() { planetLayer }));
                }
                if (planets.TryGetValue(planet, out (FloatMenuOption, List<PlanetLayerGroupDef>) planetFloat))
                {
                    planetFloat.Item2.AddDistinct(planetLayerGroup);
                }
                else
                {
                    planets.Add(planet, (null, new List<PlanetLayerGroupDef>() { planetLayerGroup }));
                }
                titlePlanet = "Planet";
                titleGroup = "Group";
                titleLayer = "Layer";
            }

            List<PlanetDef> planetKeys = planets.Keys.ToList();
            for (int i = 0; i < planetKeys.Count; i++)
            {
                PlanetDef planet = planetKeys[i];
                List<PlanetLayerGroupDef> groups = planets[planet].Item2;
                FloatMenuOption planetFloatMenuOption = new FloatMenuOption(planet.LabelCap, delegate
                {
                    currentPlanet = planet;
                    if (planets.TryGetValue(currentPlanet, out (FloatMenuOption, List<PlanetLayerGroupDef>) groupValues))
                    {
                        currentGroup = groupValues.Item2.FirstOrDefault();
                    }
                    RefillMenus();
                }, planet.ViewGizmoTexture, Color.white, orderInPriority: planet.GetHashCode())
                {
                    tooltip = new TipSignal(planet.description, planet.index ^ 0x1241961)
                };
                planetFloatMenuOption.SetSizeMode(FloatMenuSizeMode.Normal);
                planets[planet] = (planetFloatMenuOption, groups);
            }

            List<PlanetLayerGroupDef> groupKeys = groups.Keys.ToList();
            for (int i = 0; i < groupKeys.Count; i++)
            {
                PlanetLayerGroupDef group = groupKeys[i];
                List<PlanetLayer> layers = groups[group].Item2;
                FloatMenuOption groupFloatMenuOption = new FloatMenuOption(group.LabelCap, delegate
                {
                    currentGroup = group;
                    RefillMenus();
                }, group.ViewGizmoTexture, Color.white, orderInPriority: group.GetHashCode())
                {
                    tooltip = new TipSignal(group.description, group.index ^ 0x1241961)
                };
                groupFloatMenuOption.SetSizeMode(FloatMenuSizeMode.Normal);
                groups[group] = (groupFloatMenuOption, layers);
            }

            this.currentLayer = currentLayer;
            currentGroup = currentLayer.Def.LayerGroup();
            currentPlanet = currentGroup?.planet;

            RefillMenus();

            titlePlanet = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Grouped.Planet".Translate();
            titleGroup = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Grouped.Group".Translate();
            titleLayer = "LayeredAtmosphereOrbit.WorldGrid.Gizmo.SelectPlanerLayer.Grouped.Layer".Translate();

            layer = WindowLayer.Super;
            closeOnClickedOutside = true;
            doWindowBackground = false;
            drawShadow = false;
            preventCameraMotion = false;
            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
        }

        public void RefillMenus()
        {
            planetsOptions = (from op in planets
                              orderby op.Value.Item1.Priority descending, op.Value.Item1.orderInPriority descending
                              select (op.Key, op.Value.Item1)).ToList();
            if (planets.TryGetValue(currentPlanet, out (FloatMenuOption, List<PlanetLayerGroupDef>) groupValues))
            {
                groupsOptions = new List<(PlanetLayerGroupDef, FloatMenuOption)>();
                foreach (PlanetLayerGroupDef planetLayerGroupDef in groupValues.Item2)
                {
                    if (groups.TryGetValue(planetLayerGroupDef, out (FloatMenuOption, List<PlanetLayer>) groupValue))
                    {
                        groupsOptions.Add((planetLayerGroupDef, groupValue.Item1));
                    }
                }
                groupsOptions = (from op in groupsOptions
                                 orderby op.Item2.Priority descending, op.Item2.orderInPriority descending
                                 select (op.Item1, op.Item2)).ToList();
            }
            else
            {
                groupsOptions = (from op in groups
                                 orderby op.Value.Item1.Priority descending, op.Value.Item1.orderInPriority descending
                                 select (op.Key, op.Value.Item1)).ToList();
            }
            if (groups.TryGetValue(currentGroup, out (FloatMenuOption, List<PlanetLayer>) layerValues))
            {
                layersOptions = new List<(PlanetLayer, FloatMenuOption)>();
                foreach (PlanetLayer planetLayer in layerValues.Item2)
                {
                    if (layers.TryGetValue(planetLayer, out FloatMenuOption layerValue))
                    {
                        layersOptions.Add((planetLayer, layerValue));
                    }
                }
                layersOptions = (from op in layersOptions
                                 orderby op.Item2.Priority descending, op.Item2.orderInPriority descending
                                 select (op.Item1, op.Item2)).ToList();
            }
            else
            {
                layersOptions = (from op in layers
                                 orderby op.Value.Priority descending, op.Value.orderInPriority descending
                                 select (op.Key, op.Value)).ToList();
            }
            SetInitialSizeAndPosition();
        }

        protected override void SetInitialSizeAndPosition()
        {
            Vector2 vector = mouseClickPos + InitialPositionShift;
            Vector2 initialSize = InitialSize;
            vector.y = vector.y - initialSize.y;
            if (vector.x + initialSize.x > (float)UI.screenWidth)
            {
                vector.x = (float)UI.screenWidth - initialSize.x;
            }
            if (vector.y + initialSize.y > (float)UI.screenHeight)
            {
                vector.y = (float)UI.screenHeight - initialSize.y;
            }
            windowRect = new Rect(vector.x, vector.y, initialSize.x, initialSize.y);
        }

        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            GUI.color = baseColor;
            Text.Font = GameFont.Small;
            Vector2 vector = new Vector2(windowRect.x, windowRect.y);
            if (!titlePlanet.NullOrEmpty())
            {
                float height = Text.CalcHeight(titlePlanet, TotalWidthPlanets);
                Rect titleRect = new Rect(vector.x, vector.y + (TotalWindowHeight - PlanetWindowHeight) - height - 2, TotalWidthPlanets, height);
                DrawTitle(titlePlanet, titleRect, 12419610);
                vector.x = titleRect.xMax + Margin;
            }
            if (!titleGroup.NullOrEmpty())
            {
                float height = Text.CalcHeight(titleGroup, TotalWidthGroups);
                Rect titleRect = new Rect(vector.x, vector.y + (TotalWindowHeight - GroupWindowHeight) - height - 2, TotalWidthGroups, height);
                DrawTitle(titleGroup, titleRect, 12419611);
                vector.x = titleRect.xMax + Margin;
            }
            if (!titleLayer.NullOrEmpty())
            {
                float height = Text.CalcHeight(titleLayer, TotalWidthLayers);
                Rect titleRect = new Rect(vector.x, vector.y + (TotalWindowHeight - LayerWindowHeight) - height - 2, TotalWidthLayers, height);
                DrawTitle(titleLayer, titleRect, 12419612);
            }
        }

        public void DrawTitle(string title, Rect titleRect, int ID)
        {
            Find.WindowStack.ImmediateWindow(ID, titleRect, WindowLayer.Super, delegate
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect position = titleRect.AtZero();
                GUI.DrawTexture(position, TexUI.TextBGBlack);
                Rect rect = titleRect.AtZero();
                rect.y += 1f;
                Widgets.Label(rect, title);
                Text.Anchor = TextAnchor.UpperLeft;
            }, doBackground: false, absorbInputAroundWindow: false, 0f);
        }

        public override void DoWindowContents(Rect rect)
        {
            UpdateBaseColor();
            float PlanetsHeight = PlanetWindowHeight;
            Rect rectPlanets = new Rect(rect.x, rect.y + (TotalWindowHeight - PlanetsHeight), TotalWidthPlanets, PlanetsHeight);
            DoWindowPlanetContents(rectPlanets);
            float GroupsHeight = GroupWindowHeight;
            Rect rectGroups = new Rect(rectPlanets.xMax + Margin, rect.y + (TotalWindowHeight - GroupsHeight), TotalWidthGroups, GroupsHeight);
            DoWindowGroupContents(rectGroups);
            float LayersHeight = LayerWindowHeight;
            Rect rectLayers = new Rect(rectGroups.xMax + Margin, rect.y + (TotalWindowHeight - LayersHeight), TotalWidthLayers, LayersHeight);
            DoWindowLayerContents(rectLayers);
        }

        public void DoWindowLayerContents(Rect rect)
        {
            bool usingScrollbar = UsingScrollbarLayers;
            GUI.color = baseColor;
            Text.Font = GameFont.Small;
            Vector2 zero = rect.min;
            float columnWidth = ColumnWidthLayer;
            if (usingScrollbar)
            {
                Widgets.BeginScrollView(rect, ref scrollPositionLayer, new Rect(zero.x, zero.y, TotalWidthLayers - 16f, TotalLayersHeight));
            }
            for (int i = 0; i < layersOptions.Count; i++)
            {
                FloatMenuOption floatMenuOption = layersOptions[i].Item2;
                float requiredHeight = floatMenuOption.RequiredHeight;
                Rect rect2 = new Rect(zero.x, zero.y, columnWidth, requiredHeight);
                zero.y += requiredHeight + -1f;
                if (floatMenuOption.DoGUI(rect2, false, null))
                {
                    Find.WindowStack.TryRemove(this);
                    break;
                }
                if (layersOptions[i].Item1 == currentLayer)
                {
                    Widgets.DrawTextureFitted(new Rect(rect2.xMax - 8, rect2.y + (rect2.height - 8) / 2, 8, 8), currentIcon.Texture, 1f);
                }
            }
            if (usingScrollbar)
            {
                Widgets.EndScrollView();
            }
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Close();
            }
            GUI.color = Color.white;
        }

        public void DoWindowGroupContents(Rect rect)
        {
            bool usingScrollbar = UsingScrollbarGroups;
            GUI.color = baseColor;
            Text.Font = GameFont.Small;
            Vector2 zero = rect.min;
            float columnWidth = ColumnWidthGroup;
            if (usingScrollbar)
            {
                Widgets.BeginScrollView(rect, ref scrollPositionGroup, new Rect(zero.x, zero.y, TotalWidthGroups - 16f, TotalGroupsHeight));
            }
            for (int i = 0; i < groupsOptions.Count; i++)
            {
                FloatMenuOption floatMenuOption = groupsOptions[i].Item2;
                float requiredHeight = floatMenuOption.RequiredHeight;
                Rect rect2 = new Rect(zero.x, zero.y, columnWidth, requiredHeight);
                zero.y += requiredHeight + -1f;
                floatMenuOption.DoGUI(rect2, false, null);
                if (groupsOptions[i].Item1 == currentGroup)
                {
                    Widgets.DrawTextureFitted(new Rect(rect2.xMax - 8, rect2.y + (rect2.height - 8) / 2, 8, 8), currentIcon.Texture, 1f);
                }
            }
            if (usingScrollbar)
            {
                Widgets.EndScrollView();
            }
            GUI.color = Color.white;
        }

        public void DoWindowPlanetContents(Rect rect)
        {
            bool usingScrollbar = UsingScrollbarPlanets;
            GUI.color = baseColor;
            Text.Font = GameFont.Small;
            Vector2 zero = rect.min;
            float columnWidth = ColumnWidthPlanet;
            if (usingScrollbar)
            {
                Widgets.BeginScrollView(rect, ref scrollPositionPlanet, new Rect(zero.x, zero.y, TotalWidthPlanets - 16f, TotalPlanetsHeight));
            }
            for (int i = 0; i < planetsOptions.Count; i++)
            {
                FloatMenuOption floatMenuOption = planetsOptions[i].Item2;
                float requiredHeight = floatMenuOption.RequiredHeight;
                Rect rect2 = new Rect(zero.x, zero.y, columnWidth, requiredHeight);
                zero.y += requiredHeight + -1f;
                floatMenuOption.DoGUI(rect2, false, null);
                if (planetsOptions[i].Item1 == currentPlanet)
                {
                    Widgets.DrawTextureFitted(new Rect(rect2.xMax - 8, rect2.y + (rect2.height - 8) / 2, 8, 8), currentIcon.Texture, 1f);
                }
            }
            if (usingScrollbar)
            {
                Widgets.EndScrollView();
            }
            GUI.color = Color.white;
        }

        public void Cancel()
        {
            SoundDefOf.FloatMenu_Cancel.PlayOneShotOnCamera();
            Find.WindowStack.TryRemove(this);
        }

        public virtual void PreOptionChosen(FloatMenuOption opt)
        {
        }

        private void UpdateBaseColor()
        {
            baseColor = Color.white;
            if (!vanishIfMouseDistant)
            {
                return;
            }
            Rect r = new Rect(0f, 0f, InitialSize.x, InitialSize.y).ContractedBy(-5f);
            if (!r.Contains(Event.current.mousePosition))
            {
                float num = GenUI.DistFromRect(r, Event.current.mousePosition);
                baseColor = new Color(1f, 1f, 1f, 1f - num / 95f);
                if (num > 95f)
                {
                    Close(doCloseSound: false);
                    Cancel();
                }
            }
        }
    }
}

