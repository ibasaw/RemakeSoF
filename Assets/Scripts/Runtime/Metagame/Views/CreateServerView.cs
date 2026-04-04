using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tolik.RemakeSoF.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    internal class CreateServerView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        Button m_CreateServerButton;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_CreateServerButton = root.Q<Button>("createServerButton");
            m_CreateServerButton.RegisterCallback<ClickEvent>(OnClickCreateServer);
            m_CreateServerButton.RegisterCallback<PointerEnterEvent>(_ => UIMenuSoundPlayer.Play(UIMenuSoundPlayer.Hilite));
        }

        void OnDisable()
        {
            m_CreateServerButton.UnregisterCallback<ClickEvent>(OnClickCreateServer);
        }

        void OnClickCreateServer(ClickEvent evt)
        {
            UIMenuSoundPlayer.Play(UIMenuSoundPlayer.ApplyChanges);
            Debug.Log("Create Server button clicked");
            Broadcast(new CreateServerClickEvent());
        }

    }
}