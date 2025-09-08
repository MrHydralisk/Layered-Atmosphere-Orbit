using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace LayeredAtmosphereOrbit
{
    public class GenStep_FloatingIslandSleepingMechanoids : GenStep
    {
        public FloatRange defaultPointsRange = new FloatRange(340f, 1000f);

        public override int SeedPart => 341176078;

        public static void SendMechanoidsToSleepImmediately(List<Pawn> spawnedMechanoids)
        {
            for (int i = 0; i < spawnedMechanoids.Count; i++)
            {
                spawnedMechanoids[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
                JobDriver curDriver = spawnedMechanoids[i].jobs.curDriver;
                if (curDriver != null)
                {
                    curDriver.asleep = true;
                }
                CompCanBeDormant comp = spawnedMechanoids[i].GetComp<CompCanBeDormant>();
                if (comp != null)
                {
                    comp.ToSleep();
                }
                else
                {
                    Log.ErrorOnce("Tried spawning sleeping mechanoid " + spawnedMechanoids[i]?.ToString() + " without CompCanBeDormant!", 0x12EA9A79 ^ spawnedMechanoids[i].def.defName.GetHashCode());
                }
            }
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            List<IntVec3> spawnArea = map.AllCells.Where((IntVec3 x) => x.Standable(map)).ToList();
            if (spawnArea.NullOrEmpty())
            {
                return;
            }
            List<Pawn> list = new List<Pawn>();
            foreach (Pawn item in GeneratePawns(parms, map))
            {
                if (!CellFinder.TryFindRandomSpawnCellForPawnNear(spawnArea.RandomElement(), map, out IntVec3 spawnCell, 10))
                {
                    Find.WorldPawns.PassToWorld(item);
                    break;
                }
                GenSpawn.Spawn(item, spawnCell, map);
                list.Add(item);
                spawnArea.Remove(spawnCell);
            }
            if (!list.Any())
            {
                return;
            }
            foreach (Pawn item2 in list)
            {
                CompWakeUpDormant comp = item2.GetComp<CompWakeUpDormant>();
                if (comp != null)
                {
                    comp.wakeUpIfTargetClose = true;
                }
            }
            LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_SleepThenAssaultColony(Faction.OfMechanoids), map, list);
            SendMechanoidsToSleepImmediately(list);
        }

        private IEnumerable<Pawn> GeneratePawns(GenStepParams parms, Map map)
        {
            float points = ((parms.sitePart != null) ? parms.sitePart.parms.threatPoints : defaultPointsRange.RandomInRange);
            PawnGroupMakerParms pawnGroupMakerParms = new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = Faction.OfMechanoids,
                points = points
            };
            if (parms.sitePart != null)
            {
                pawnGroupMakerParms.seed = SleepingMechanoidsSitePartUtility.GetPawnGroupMakerSeed(parms.sitePart.parms);
            }
            return PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms);
        }
    }
}