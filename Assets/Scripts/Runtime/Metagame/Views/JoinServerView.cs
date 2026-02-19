using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    internal class JoinServerView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        Button m_JoinServerButton;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
            m_JoinServerButton = root.Q<Button>("joinServerButton");
            m_JoinServerButton.RegisterCallback<ClickEvent>(OnClickJoinServer);
        }

        void OnDisable()
        {
            m_JoinServerButton.UnregisterCallback<ClickEvent>(OnClickJoinServer);
        }

        void OnClickJoinServer(ClickEvent evt)
        {
            Debug.Log("Join Server button clicked");
            Broadcast(new JoinServerClickEvent());
        }

    }
}