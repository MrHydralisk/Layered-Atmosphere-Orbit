using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GameComponent_LayeredAtmosphereOrbit : GameComponent
    {
        public static GameComponent_LayeredAtmosphereOrbit instance;

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