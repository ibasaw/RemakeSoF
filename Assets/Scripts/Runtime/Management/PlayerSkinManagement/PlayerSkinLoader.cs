using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.PlayerSkinManagement
{
    /// <summary>
    /// Internal service responsible for loading skin assets (prefabs, animators)
    /// </summary>
    internal class PlayerSkinLoader
    {
        private readonly PlayerSkinDataRegistry m_Registry;

        public PlayerSkinLoader(PlayerSkinDataRegistry registry)
        {
            m_Registry = registry;
        }

        public SkinDefinition GetSkinByName(string skinName)
        {
            return m_Registry.GetSkinByName(skinName);
        }

        public GameObject LoadPrefabForModel(string prefabPath)
        {
            return ServiceLocator.Get<PrefabManager>().LoadPrefab<GameObject>(prefabPath);
        }

        public RuntimeAnimatorController LoadAnimatorForModel(string prefabPath)
        {
            return ServiceLocator.Get<PrefabManager>().LoadPrefab<RuntimeAnimatorController>(prefabPath);
        }

        public RuntimeAnimatorController CreateAnimatorController(string name)
        {
            var controller = new AnimatorOverrideController();
            RuntimeAnimatorController baseController = LoadAnimatorForModel(name);
            controller.runtimeAnimatorController = baseController;
            return controller;
        }
    }
}
