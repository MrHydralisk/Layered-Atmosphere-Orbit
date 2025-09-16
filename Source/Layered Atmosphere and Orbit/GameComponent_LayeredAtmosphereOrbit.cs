using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GameComponent_LayeredAtmosphereOrbit : GameComponent
    {
        public static GameComponent_LayeredAtmosphereOrbit instance;

        public PlanetDef currentPlanetDef;

        public GameComponent_LayeredAtmosphereOrbit(Game game)
        {
            instance = this;
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            LayeredAtmosphereOrbitUtility.TryAddPlanetLayerts(Find.Scenario);
        }
    }
}