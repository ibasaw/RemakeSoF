using System;
using UnityEngine.UIElements;
namespace Unity.DedicatedGameServerSample.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]
    internal class OptionsView : View<MetagameApplication>
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

        public VisualElement LoadRootElement()
        {
            return m_UIDocument.rootVisualElement;
        }
    }
}