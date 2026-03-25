using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Represents a gore area definition from the game data.
    /// Defines which surfaces, bolts, and effects are applied when a specific body part is damaged.
    /// </summary>
    [System.Serializable]
    public class GoreArea
    {
        /// <summary>
        /// The location/name of this gore area (e.g., "hand", "arm_lower", "head").
        /// </summary>
        [SerializeField]
        public string Location;

        /// <summary>
        /// Optional flags that control behavior of this gore area.
        /// </summary>
        [SerializeField]
        public List<string> Flags = new();

        /// <summary>
        /// List of surface names to disable when this gore area is triggered.
        /// </summary>
        [SerializeField]
        public List<string> Surfaces_Off = new();

        /// <summary>
        /// List of bolt names to disable when this gore area is triggered.
        /// </summary>
        [SerializeField]
        public List<string> Bolts_Off = new();

        /// <summary>
        /// List of surface names to enable/activate when this gore area is triggered.
        /// </summary>
        [SerializeField]
        public List<string> Surfaces_On = new();

        /// <summary>
        /// List of child gore area locations that are part of this area hierarchy.
        /// </summary>
        [SerializeField]
        public List<string> Children = new();

        /// <summary>
        /// Optional chunk definition for gore pieces that should fly off.
        /// </summary>
        [SerializeField]
        public GoreChunk Chunk;

        /// <summary>
        /// Optional bolt-on model definition that should be attached to this area.
        /// </summary>
        [SerializeField]
        public GoreBoltOn BoltOn;

        /// <summary>
        /// List of visual effects to spawn when this gore area is triggered.
        /// </summary>
        [SerializeField]
        public List<GoreEffect> FX = new();
    }

    /// <summary>
    /// Defines a chunk that should separate from the character model.
    /// </summary>
    [System.Serializable]
    public class GoreChunk
    {
        /// <summary>
        /// The root bone or surface name for this chunk.
        /// </summary>
        [SerializeField]
        public string root;

        /// <summary>
        /// The bone to apply force to when separating the chunk.
        /// </summary>
        [SerializeField]
        public string bone;

        /// <summary>
        /// Minimum force to apply to the chunk in Newtons.
        /// </summary>
        [SerializeField]
        public int MinForce;

        /// <summary>
        /// Maximum force to apply to the chunk in Newtons.
        /// </summary>
        [SerializeField]
        public int MaxForce;

        /// <summary>
        /// Surfaces to enable when the chunk is separated.
        /// </summary>
        [SerializeField]
        public List<string> Surfaces_On = new();

        /// <summary>
        /// Optional: Explicit list of surface names to include in the chunk.
        /// When set, these surfaces are cloned into the flying chunk instead of matching by root name.
        /// Used for multi-surface body parts (e.g. head) where a single root name cannot match all surfaces.
        /// </summary>
        [SerializeField]
        public List<string> Surfaces = new();

        /// <summary>
        /// Child surfaces to disable when the chunk is separated.
        /// </summary>
        [SerializeField]
        public List<string> Children_Off = new();
    }

    /// <summary>
    /// Defines a visual effect to spawn during gore events.
    /// </summary>
    [System.Serializable]
    public class GoreEffect
    {
        /// <summary>
        /// The name of the gore effect (e.g., "gore_mist_small", "blood_spurt_arterial").
        /// </summary>
        [SerializeField]
        public string Name;

        /// <summary>
        /// The bolt location where this effect should be spawned.
        /// </summary>
        [SerializeField]
        public string Bolt;

        /// <summary>
        /// Optional file path for the effect definition.
        /// </summary>
        [SerializeField]
        public string File;
    }

    /// <summary>
    /// Defines a bolt-on model to attach to the character during gore events.
    /// </summary>
    [System.Serializable]
    public class GoreBoltOn
    {
        /// <summary>
        /// The name of the gore piece to use (e.g., "brain", "bone_long").
        /// </summary>
        [SerializeField]
        public string Name;

        /// <summary>
        /// The bolt location where this model should be attached.
        /// </summary>
        [SerializeField]
        public string Bolt;
    }
}
