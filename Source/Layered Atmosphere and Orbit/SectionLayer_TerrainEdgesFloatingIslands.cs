using RimWorld;
using Verse;

namespace LayeredAtmosphereOrbit
{
    public class SectionLayer_TerrainEdgesFloatingIslands : SectionLayer_TerrainEdges
    {
        public bool isFloatingIslands;

        public SectionLayer_TerrainEdgesFloatingIslands(Section section) : base(section)
        {
            isFloatingIslands = Map.Tile.Layer.Def.LayerGroup() == PlanetLayerGroupDefOfLocal.LAO_Atmosphere;
        }

        public override void Regenerate()
        {
            if (base.Map.Tile.Valid && !isFloatingIslands)
            {
                return;
            }
            ClearSubMeshes(MeshParts.All);
            TerrainGrid terrainGrid = base.Map.terrainGrid;
            CellRect cellRect = section.CellRect;
            float altitude = AltitudeLayer.TerrainScatter.AltitudeFor();
            float altitude2 = AltitudeLayer.TerrainEdges.AltitudeFor();
            foreach (IntVec3 item in cellRect)
            {
                if (ShouldDrawRockEdges(item, terrainGrid, out var edges, out var corners))
                {
                    TerrainDef terrain = terrainGrid.BaseTerrainAt(item);
                    DrawEdges(terrain, item, edges, altitude);
                    DrawCorners(terrain, item, edges, corners, altitude);
                    if (ShouldDrawPassthrough(item, terrainGrid, out edges, out corners))
                    {
                        DrawLoop(item + IntVec3.South, terrainGrid, edges, corners, altitude2);
                    }
                }
                else if (ShouldDrawLoop(item, terrainGrid, out edges, out corners))
                {
                    DrawLoop(item, terrainGrid, edges, corners, altitude2);
                }
            }
            FinalizeMesh(MeshParts.All);
        }
    }
}