using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    internal class LoadoutController : Controller<MetagameApplication>
    {
        LoadoutView View => App.View.LoadoutView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;

        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);

            PlayerSkinManager.EventManager.AddListener<PlayerSkinChangedEvent>(OnPlayerSkinChanged);
            AddListener<LoadNextSkinEvent>(OnClickLoadNextSkin);
            AddListener<LoadPreviousSkinEvent>(OnClickLoadPreviousSkin);
            Debug.Log("[LoadoutController] Awake - LoadoutController initialized and listeners added");
        }


        void OnClickLoadNextSkin(LoadNextSkinEvent evt)
        {
            Debug.Log("[LoadoutController] OnClickLoadNextSkin - Requesting next skin from PlayerSkinManager");
            PlayerSkinManager.LoadNextSkin();
        }
        
        void OnClickLoadPreviousSkin(LoadPreviousSkinEvent evt)
        {
            Debug.Log("[LoadoutController] OnClickLoadPreviousSkin - Requesting previous skin from PlayerSkinManager");
            PlayerSkinManager.LoadPreviousSkin();
        }

        void OnPlayerSkinChanged(PlayerSkinChangedEvent evt)
        {
            var prefab = PlayerSkinManager.GetCurrentPlayerPrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"[LoadoutController] OnPlayerSkinChanged - Current prefab is null for skin '{evt.skinName}'");
                return;
            }

            Debug.Log($"[LoadoutController] OnPlayerSkinChanged - Setting character prefab in LoadoutView: {prefab.name}");
            View.SetCharacterPrefab(prefab);
        }

        void OnDestroy()
        {
            RemoveListeners();
            Debug.Log("[LoadoutController] OnDestroy - LoadoutController destroyed and listeners removed");
        }

        internal override void RemoveListeners()
        {
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);

            PlayerSkinManager.EventManager.RemoveListener<PlayerSkinChangedEvent>(OnPlayerSkinChanged);
            RemoveListener<LoadNextSkinEvent>(OnClickLoadNextSkin);
            RemoveListener<LoadPreviousSkinEvent>(OnClickLoadPreviousSkin);
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (evt.status == ConnectStatus.Connecting)
            {
                View.Hide();
            }
        }
    }
}
