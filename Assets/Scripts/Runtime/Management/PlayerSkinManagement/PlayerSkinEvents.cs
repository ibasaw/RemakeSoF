using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    public enum PlayerSkinStatus
    {
        /// <summary>
        /// Status is not defined. This likely means an unexpected error occurred.
        /// </summary>
        Undefined,
        /// <summary>
        /// Skin is being loaded.
        /// </summary>
        Loading,
        /// <summary>
        /// Skin successfully loaded and applied.
        /// </summary>
        Success,
        /// <summary>
        /// Skin data file not found in resources.
        /// </summary>
        SkinNotFound,
        /// <summary>
        /// Failed to parse skin data.
        /// </summary>
        ModelNameParseError,
        /// <summary>
        /// Prefab for the model not found.
        /// </summary>
        PrefabNotFound,
        /// <summary>
        /// Failed to parse shader definition file.
        /// </summary>
        ShaderFileParseError,
        /// <summary>
        /// Shader definition file not found.
        /// </summary>
        ShaderFileNotFound,
        /// <summary>
        /// Invalid skin definition data.
        /// </summary>
        InvalidSkinDefinition,


        /// <summary>
        /// No MaterialAssigner component found on character.
        /// </summary>
        NoAssignerFound,
        /// <summary>
        /// Invalid skin name provided.
        /// </summary>
        InvalidSkinName,
        /// <summary>
        /// Generic error occurred during skin loading.
        /// </summary>
        GenericError
    }

    public class PlayerSkinEvent : AppEvent
    {
        public PlayerSkinStatus status;
    }

    public class PlayerSkinChangedEvent : AppEvent
    {
        public string skinName;
        public GameObject playerPrefab;
    }

    public class PlayerSkinLoadingEvent : AppEvent
    {
        public string skinName;
    }

    public class PlayerSkinErrorEvent : AppEvent
    {
        public string error;
        public PlayerSkinStatus status;
    }
}
