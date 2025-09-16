using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GameComponent_LayeredAtmosphereOrbit : GameComponent
    {
        public static GameComponent_LayeredAtmosphereOrbit instance;

        public PlanetDef currentPlanetDef;
        public Dictionary<Gravship, GravshipRoute> gravshipRoutes = new Dictionary<Gravship, GravshipRoute>();
        private List<Gravship> tmpGravshipRoutesGravships = new List<Gravship>();
        private List<GravshipRoute> tmpGravshipRoutesRoutes = new List<GravshipRoute>();

        public GameComponent_LayeredAtmosphereOrbit(Game game)
        {
            instance = this;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            LayeredAtmosphereOrbitUtility.TryAddPlanetLayerts(Find.Scenario);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref gravshipRoutes, "gravshipRoutes", LookMode.Reference, LookMode.Deep, ref tmpGravshipRoutesGravships, ref tmpGravshipRoutesRoutes);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }
            if (gravshipRoutes == null)
            {
                gravshipRoutes = new Dictionary<Gravship, GravshipRoute>();
            }
        }
    }
}