using RimWorld.Planet;
using System.Collections;
using UnityEngine;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class WorldDrawLayer_UngeneratedPlanetPartsBackgroundBiome : WorldDrawLayer
    {
        private const int SubdivisionsCount = 4;

        private const float ViewAngleOffset = 10f;

        public override IEnumerable Regenerate()
        {
            foreach (object item in base.Regenerate())
            {
                yield return item;
            }
            Vector3 surfaceViewCenter = Find.WorldGrid.SurfaceViewCenter;
            float surfaceViewAngle = Find.WorldGrid.SurfaceViewAngle;
            if (surfaceViewAngle < 180f)
            {
                SphereGenerator.Generate(SubdivisionsCount, planetLayer.Radius + -0.16f, -surfaceViewCenter, 180f - Mathf.Min(surfaceViewAngle, 180f) + ViewAngleOffset, out var outVerts, out var outIndices);
                LayerSubMesh subMesh = GetSubMesh(planetLayer.Def.backgroundBiome.DrawMaterial);
                subMesh.verts.AddRange(outVerts);
                subMesh.tris.AddRange(outIndices);
            }
            FinalizeMesh(MeshParts.All);
        }
    }
}

