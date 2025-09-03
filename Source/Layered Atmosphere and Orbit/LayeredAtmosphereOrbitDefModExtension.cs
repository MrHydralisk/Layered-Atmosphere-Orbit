using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class LayeredAtmosphereOrbitDefModExtension : DefModExtension
    {
        public bool isOptionToAutoAdd = false;
        public OrbitType layerType = OrbitType.unknown;
        public float elevation = 200;
    }
}