﻿using Verse;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_Atmosphere : GenStep
    {
        public override int SeedPart => 196743;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!ModsConfig.OdysseyActive)
            {
                return;
            }
            map.regionAndRoomUpdater.Enabled = false;
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (IntVec3 allCell in map.AllCells)
            {
                terrainGrid.SetTerrain(allCell, DefOfLocal.LAO_Air);
            }
        }
    }
}