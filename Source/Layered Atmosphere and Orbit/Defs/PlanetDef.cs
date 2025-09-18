using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class PlanetDef : Def
    {
        public Vector3 posFromRimworld = Vector3.zero;
        public float gravityWellExitElevation = 1000;
        public float gravityWellRadius = 200;
        public List<GameConditionDef> permamentGameConditionDefs = new List<GameConditionDef>();
    }
}