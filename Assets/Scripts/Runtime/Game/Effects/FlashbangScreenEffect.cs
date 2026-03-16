using Tolik.RemakeSoF.Runtime.Game.Camera;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// SoF2-authentischer Flashbang-Bildschirmeffekt (M84 Stun Grenade).
    /// Erzeugt ein Fullscreen-Weiss-Overlay das ueber Zeit ausblendet.
    /// Intensitaet basiert auf Distanz zur Explosion und Blickrichtung.
    /// In SoF2 (CG_Flashbang): damageType "flash" → weisser Screen + HighPitch-Tinnitus.
    /// </summary>
    public class FlashbangScreenEffect : MonoBehaviour
    {
        /// <summary>Maximaler Wirkungsradius in Metern (SoF2: 800 QU × 0.0254 = 20.32m).</summary>
        private const float FLASH_RADIUS = 20.32f;

        /// <summary>Dauer des vollen Weiss-Overlays in Sekunden bevor Fade beginnt.</summary>
        private const float HOLD_DURATION = 0.3f;

        /// <summary>Dauer des Fade-Outs von Weiss zu Transparent in Sekunden.</summary>
        private const float FADE_DURATION = 2.5f;

        /// <summary>Mindest-Intensitaet wenn Spieler direkt in die Explosion schaut (0-1).</summary>
        private const float MIN_DIRECT_LOOK_INTENSITY = 0.3f;

        /// <summary>Aktuelle Overlay-Intensitaet (0 = aus, 1 = volles Weiss).</summary>
        private float m_Intensity;

        /// <summary>Vergangene Zeit seit Flash-Ausloesung.</summary>
        private float m_ElapsedTime;

        /// <summary>Ob ein Flash gerade aktiv ist.</summary>
        private bool m_IsActive;

        /// <summary>OnGUI-Textur fuer Fullscreen-Overlay (1x1 weisser Pixel).</summary>
        private Texture2D m_WhiteTexture;

        /// <summary>
        /// Singleton-Instanz — es gibt nur einen Bildschirm-Flash gleichzeitig.
        /// </summary>
        private static FlashbangScreenEffect s_Instance;

        /// <summary>
        /// Triggert den Flashbang-Effekt fuer den lokalen Spieler.
        /// Berechnet Intensitaet basierend auf Distanz und Blickrichtung.
        /// </summary>
        public static void TriggerFlash(Vector3 explosionPosition)
        {
            // Cinemachine: AimCameraController sitzt auf dem Kamera-Rig,
            // nicht Camera.main verwenden da Cinemachine virtuelle Kameras nutzt.
            AimCameraController controller = Object.FindAnyObjectByType<AimCameraController>();
            if (controller == null)
            {
                return;
            }

            Transform camTransform = controller.transform;
            float distance = Vector3.Distance(camTransform.position, explosionPosition);
            if (distance > FLASH_RADIUS)
            {
                return;
            }

            // Distanz-Falloff (linear): naeher = staerker
            float distanceFactor = 1f - (distance / FLASH_RADIUS);

            // Blickrichtungs-Faktor: SoF2 prueft ob Spieler in Richtung Explosion schaut
            // Dot-Product: 1.0 = direkt hinschauen, 0.0 = seitlich, -1.0 = wegschauen
            Vector3 toExplosion = (explosionPosition - camTransform.position).normalized;
            float dot = Vector3.Dot(camTransform.forward, toExplosion);

            // Nur Flash wenn Spieler grob in Richtung Explosion schaut (dot > 0 = vordere Hemisphäre)
            // Aber auch seitlich/hinter gibt Mini-Flash wenn sehr nah (SoF2: reduzierter Effekt)
            float lookFactor;
            if (dot > 0f)
            {
                // Vorwaerts: voller Effekt skaliert mit dot
                lookFactor = Mathf.Lerp(MIN_DIRECT_LOOK_INTENSITY, 1f, dot);
            }
            else
            {
                // Abgewandt: stark reduziert, nur bei sehr kurzer Distanz
                lookFactor = Mathf.Lerp(0f, MIN_DIRECT_LOOK_INTENSITY, distanceFactor);
            }

            float intensity = distanceFactor * lookFactor;
            if (intensity < 0.05f)
            {
                return;
            }

            // Instanz erstellen oder vorhandene nutzen
            if (s_Instance == null)
            {
                GameObject flashObj = new("FlashbangScreenEffect");
                DontDestroyOnLoad(flashObj);
                s_Instance = flashObj.AddComponent<FlashbangScreenEffect>();
            }

            s_Instance.ActivateFlash(intensity);
        }

        /// <summary>
        /// Aktiviert den Flash mit der angegebenen Intensitaet.
        /// Ueberschreibt einen laufenden schwächeren Flash.
        /// </summary>
        private void ActivateFlash(float intensity)
        {
            // Nur ueberschreiben wenn neuer Flash staerker ist
            if (m_IsActive && intensity <= m_Intensity)
            {
                return;
            }

            m_Intensity = Mathf.Clamp01(intensity);
            m_ElapsedTime = 0f;
            m_IsActive = true;
        }

        private void Awake()
        {
            m_WhiteTexture = new Texture2D(1, 1);
            m_WhiteTexture.SetPixel(0, 0, Color.white);
            m_WhiteTexture.Apply();
        }

        private void Update()
        {
            if (!m_IsActive)
            {
                return;
            }

            m_ElapsedTime += Time.deltaTime;

            float totalDuration = HOLD_DURATION + FADE_DURATION;
            if (m_ElapsedTime >= totalDuration)
            {
                m_IsActive = false;
                m_Intensity = 0f;
                return;
            }
        }

        private void OnGUI()
        {
            if (!m_IsActive || m_Intensity <= 0f)
            {
                return;
            }

            // Alpha berechnen: volle Hold-Phase → linearer Fade-Out
            float alpha;
            if (m_ElapsedTime < HOLD_DURATION)
            {
                alpha = m_Intensity;
            }
            else
            {
                float fadeProgress = (m_ElapsedTime - HOLD_DURATION) / FADE_DURATION;
                alpha = m_Intensity * (1f - fadeProgress);
            }

            if (alpha <= 0.01f)
            {
                return;
            }

            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), m_WhiteTexture);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (m_WhiteTexture != null)
            {
                Destroy(m_WhiteTexture);
            }

            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }
    }
}
