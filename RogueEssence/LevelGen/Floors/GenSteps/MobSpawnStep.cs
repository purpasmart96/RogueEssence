﻿using System;
using RogueElements;
using RogueEssence.Dev;

namespace RogueEssence.LevelGen
{
    /// <summary>
    /// Generates the encounter table of enemies to spawn on a floor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class MobSpawnStep<T> : GenStep<T> where T : BaseMapGenContext
    {
        [SubGroup]
        public SpawnList<TeamSpawner> Spawns;

        public MobSpawnStep()
        {
            Spawns = new SpawnList<TeamSpawner>();
        }

        public override void Apply(T map)
        {
            for(int ii = 0; ii < Spawns.Count; ii++)
                map.TeamSpawns.Add(Spawns.GetSpawn(ii).Clone(), Spawns.GetSpawnRate(ii));//Clone Use Case; convert to Instantiate?
        }
    }
}
