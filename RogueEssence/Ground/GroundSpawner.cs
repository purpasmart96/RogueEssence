﻿using System;
using System.Collections.Generic;
using RogueEssence.Script;
using RogueEssence.Dungeon;
using Microsoft.Xna.Framework;
using RogueElements;
using System.Runtime.Serialization;

namespace RogueEssence.Ground
{
    /// <summary>
    /// Class representing an NPC spawner on a ground map. Its meant to randomly create an NPC at runtime, and is controlled via script.
    /// </summary>
    [Serializable]
    public class GroundSpawner : GroundEntity
    {
        /// <summary>
        /// Name of the NPC that will be spawned by this spawner
        /// </summary>
        public string NPCName { get; set; }

        /// <summary>
        /// The "Character" entity from which the NPC will be generated.
        /// </summary>
        public CharData NPCChar { get; set; }

        /// <summary>
        /// The NPC that was spawned by this spawner
        /// </summary>
        [NonSerialized]
        private GroundChar curNpc;
        public GroundChar CurrentNPC { get { return curNpc; } internal set { curNpc = value; } }

        /// <summary>
        /// The Callbacks that will be enabled on the spawned entity
        /// </summary>
        public HashSet<LuaEngine.EEntLuaEventTypes> EntityCallbacks;

        /// <summary>
        /// Script events available for this entity's instance.
        /// </summary>
        [NonSerialized]
        public Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent> scriptEvents;

        public override Color DevEntColoring => Color.Salmon;


        public override EEntTypes GetEntityType()
        {
            return EEntTypes.Spawner;
        }

        /// <summary>
        /// Creates a NPC Spawner entity with the given name, NPC name and character entity.
        /// </summary>
        /// <param name="spawnername">name given to the spawner entity</param>
        /// <param name="npcname">name given to the spawned entity</param>
        /// <param name="npcchar">character entity from which the NPC will be generated from</param>
        public GroundSpawner(string spawnername, string npcname, CharData npcchar )
            :base()
        {
            EntName = spawnername;
            NPCName = npcname;
            NPCChar = npcchar;
            Bounds = new Rect(Position.X, Position.Y, GroundAction.HITBOX_WIDTH, GroundAction.HITBOX_HEIGHT); //Static size, so its easier to click on it!
            EntityCallbacks = new HashSet<LuaEngine.EEntLuaEventTypes>();
            scriptEvents = new Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent>();
        }
        protected GroundSpawner(GroundSpawner other) : base(other)
        {
            NPCName = other.NPCName;
            NPCChar = new CharData(other.NPCChar);
            EntityCallbacks = new HashSet<LuaEngine.EEntLuaEventTypes>();
            foreach (LuaEngine.EEntLuaEventTypes ev in other.EntityCallbacks)
                EntityCallbacks.Add(ev);

            scriptEvents = new Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent>();
            foreach (LuaEngine.EEntLuaEventTypes ev in other.scriptEvents.Keys)
                scriptEvents.Add(ev, (ScriptEvent)other.scriptEvents[ev].Clone());
        }

        public override GroundEntity Clone() { return new GroundSpawner(this); }


        /// <summary>
        /// Generates the NPC and place it at the location the spawner is on the map.
        /// </summary>
        public virtual GroundChar Spawn(GroundMap currentmap)
        {
            if (!EntEnabled)
                return null;

            CurrentNPC = new GroundChar(NPCChar, Position, Direction, NPCName);

            //Setup callbacks on the spawned entity
            CurrentNPC.ReloadEvents();

            currentmap.AddTempChar(CurrentNPC);
            CurrentNPC.OnMapInit();

            //Run our script callback after we spawned
            CoroutineManager.Instance.StartCoroutine( RunEvent(LuaEngine.EEntLuaEventTypes.EntSpawned, CurrentNPC) );

            return CurrentNPC;
        }

        /// <summary>
        /// Add a script event to be assigned to the spawned entity.
        /// </summary>
        /// <param name="ev"></param>
        public virtual void AddSpawnedEntScriptEvent(LuaEngine.EEntLuaEventTypes ev)
        {
            EntityCallbacks.Add(ev);
        }

        /// <summary>
        /// Removes a script event from the list of events to be assigned to the spawned entity
        /// </summary>
        /// <param name="ev"></param>
        public virtual void RemoveSpawnedEntScriptEvent(LuaEngine.EEntLuaEventTypes ev)
        {
            EntityCallbacks.Remove(ev);
        }


        /// <summary>
        /// Iterates the list of entity callbacks enabled
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<LuaEngine.EEntLuaEventTypes> IterateSpawnedEntScriptEvents()
        {
            foreach (LuaEngine.EEntLuaEventTypes v in EntityCallbacks)
                yield return v;
        }

        public override void LuaEngineReload()
        {
            ReloadEvents();
        }


        public bool HasSpawnScriptEvent(LuaEngine.EEntLuaEventTypes ev)
        {
            return EntityCallbacks.Contains(ev);
        }

        public override bool HasScriptEvent(LuaEngine.EEntLuaEventTypes ev)
        {
            return scriptEvents.ContainsKey(ev);
        }

        public override void ReloadEvents()
        {
            scriptEvents = new Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent>();
            foreach (LuaEngine.EEntLuaEventTypes ev in LuaEngine.IterateLuaEntityEvents())
            {
                if (!IsEventSupported(ev))
                    continue;
                string callback = LuaEngine.MakeLuaEntityCallbackName(EntName, ev);
                if (!LuaEngine.Instance.DoesFunctionExists(callback))
                    continue;
                DiagManager.Instance.LogInfo(String.Format("GroundSpawner.ReloadEvents(): Added event {0} to entity {1}!", ev.ToString(), EntName));
                scriptEvents[ev] = new ScriptEvent(callback);
            }
        }

        public override void DoCleanup()
        {
            foreach (var entry in scriptEvents)
                entry.Value.DoCleanup();
            scriptEvents.Clear();
        }

        public override IEnumerator<YieldInstruction> RunEvent(LuaEngine.EEntLuaEventTypes ev, params object[] parameters)
        {
            if (scriptEvents.ContainsKey(ev))
            {
                //Since ScriptEvent.Apply takes a single variadic table, we have to concatenate our current variadic argument table
                // with the extra parameter we want to pass. Otherwise "parameters" will be passed as a table instead of its
                // individual elements, and things will crash left and right.
                List<object> partopass = new List<object>();
                partopass.Add(this);
                partopass.AddRange(parameters);
                yield return CoroutineManager.Instance.StartCoroutine(scriptEvents[ev].Apply(partopass.ToArray()));
            }
            else
                yield break;
        }

        public override bool IsEventSupported(LuaEngine.EEntLuaEventTypes ev)
        {
            return ev == LuaEngine.EEntLuaEventTypes.EntSpawned;
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext cntxt)
        {
            scriptEvents = new Dictionary<LuaEngine.EEntLuaEventTypes, ScriptEvent>();
            Collider.Width = GroundAction.HITBOX_WIDTH;
            Collider.Height = GroundAction.HITBOX_HEIGHT;
        }
    }
}
