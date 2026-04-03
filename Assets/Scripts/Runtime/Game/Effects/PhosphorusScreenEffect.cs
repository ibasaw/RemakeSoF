using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// Sichtbehinderung wenn der lokale Spieler sich innerhalb einer Phosphor-Wolke befindet.
    /// Zeichnet ein gelblich-gruenes Fullscreen-Overlay, dessen Intensitaet
    /// distanzbasiert zum Wolkenzentrum skaliert (staerker in der Mitte).
    /// Folgt dem FlashbangScreenEffect-Singleton-Pattern mit OnGUI-Rendering.
    /// </summary>
    public class PhosphorusScreenEffect : MonoBehaviour
    {
        /// <summary>Maximale Overlay-Intensitaet wenn Spieler exakt im Wolkenzentrum steht.</summary>
        private const float MAX_INTENSITY = 0.8f;

        /// <summary>Interpolarionsgeschwindigkeit fuer sanfte Uebergaenge (ein/aus).</summary>
        private const float LERP_SPEED = 6f;

        /// <summary>Fade-Dauer am Ende der Wolken-Lebenszeit in Sekunden.</summary>
        private const float END_OF_LIFE_FADE = 2f;

        /// <summary>Singleton-Instanz — nur ein Phosphor-Screen-Effekt gleichzeitig.</summary>
        private static PhosphorusScreenEffect s_Instance;

        /// <summary>Aktive Phosphor-Wolken mit Position, Radius und Restlebenszeit.</summary>
        private List<CloudData> m_ActiveClouds = new();

        /// <summary>OnGUI-Textur fuer Fullscreen-Overlay (1x1 Pixel, gelblich-gruen).</summary>
        private Texture2D m_TintTexture;

        /// <summary>Aktuelle gerenderte Intensitaet (geglättet).</summary>
        private float m_CurrentIntensity;

        /// <summary>
        /// Daten einer aktiven Phosphor-Wolke.
        /// </summary>
        private struct CloudData
        {
            /// <summary>Welt-Position des Wolkenzentrums.</summary>
            public Vector3 Position;

            /// <summary>Wirkungsradius fuer Sichtbehinderung in Metern.</summary>
            public float Radius;

            /// <summary>Verbleibende Lebenszeit in Sekunden.</summary>
            public float RemainingLifetime;
        }

        /// <summary>
        /// Registriert eine neue Phosphor-Wolke fuer den Sichtbehinderungs-Effekt.
        /// Wird von EffectFactory.SpawnExplosion() beim Erzeugen einer Phosphor-Explosion aufgerufen.
        /// </summary>
        public static void RegisterCloud(Vector3 position, float radius, float duration)
        {
            EnsureInstance();
            s_Instance.m_ActiveClouds.Add(new CloudData
            {
                Position = position,
                Radius = radius,
                RemainingLifetime = duration,
            });
        }

        /// <summary>
        /// Erstellt die Singleton-Instanz falls noch nicht vorhanden.
        /// </summary>
        private static void EnsureInstance()
        {
            if (s_Instance != null)
            {
                return;
            }

            GameObject effectObj = new("PhosphorusScreenEffect");
            DontDestroyOnLoad(effectObj);
            s_Instance = effectObj.AddComponent<PhosphorusScreenEffect>();
        }

        private void Awake()
        {
            m_TintTexture = new Texture2D(1, 1);
            // Gelblich-gruener Phosphor-Rauch-Ton
            m_TintTexture.SetPixel(0, 0, new Color(0.75f, 0.68f, 0.15f, 1f));
            m_TintTexture.Apply();
        }

        private void Update()
        {
            AimCameraController controller = Object.FindAnyObjectByType<AimCameraController>();
            if (controller == null)
            {
                m_CurrentIntensity = Mathf.Lerp(m_CurrentIntensity, 0f, Time.deltaTime * LERP_SPEED);
                return;
            }

            Vector3 camPos = controller.transform.position;
            float targetIntensity = 0f;

            for (int i = m_ActiveClouds.Count - 1; i >= 0; i--)
            {
                CloudData cloud = m_ActiveClouds[i];
                cloud.RemainingLifetime -= Time.deltaTime;

                if (cloud.RemainingLifetime <= 0f)
                {
                    m_ActiveClouds.RemoveAt(i);
                    continue;
                }

                m_ActiveClouds[i] = cloud;

                float distance = Vector3.Distance(camPos, cloud.Position);
                if (distance >= cloud.Radius)
                {
                    continue;
                }

                // Intensitaet: voll im Zentrum, linear abfallend zum Rand
                float distanceFactor = 1f - (distance / cloud.Radius);

                // Am Ende der Lebenszeit sanft ausfaden
                float lifeFade = Mathf.Clamp01(cloud.RemainingLifetime / END_OF_LIFE_FADE);

                float intensity = MAX_INTENSITY * distanceFactor * lifeFade;
                targetIntensity = Mathf.Max(targetIntensity, intensity);
            }

            // Sanfter Uebergang zur Ziel-Intensitaet
            m_CurrentIntensity = Mathf.Lerp(m_CurrentIntensity, targetIntensity, Time.deltaTime * LERP_SPEED);
        }

        private void OnGUI()
        {
            if (m_CurrentIntensity <= 0.01f)
            {
                return;
            }

            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(m_CurrentIntensity));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), m_TintTexture);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (m_TintTexture != null)
            {
                Destroy(m_TintTexture);
            }

            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }
    }
}
