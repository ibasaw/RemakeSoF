using System;
using UnityEngine.UIElements;
namespace Unity.DedicatedGameServerSample.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    internal class LogoutView : View<MetagameApplication>
    {
        UIDocument m_UIDocument;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            var root = m_UIDocument.rootVisualElement;
        }
    }
}