using Tolik.RemakeSoF.Runtime.Game.Camera;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// SoF2-authentischer Flashbang-Bildschirmeffekt (M84 Stun Grenade).
    /// Erzeugt ein Fullscreen-Weiss-Overlay das ueber Zeit ausblendet.
    /// Intensitaet basiert auf Distanz zur Explosion und Blickrichtung.
    /// In SoF2 (CG_Flashbang): damageType "flash" → weisser Screen mit distanzbasiertem Fade.
    /// SoF2 Referenz: cg_weapons.c CG_FlashBang(), cg_draw.c CG_DrawFlashBang().
    /// MAX_FLASHBANG_AFFECT_DISTANCE = 1750 QU, MAX_FLASHBANG_DISTANCE = 3000 QU,
    /// MAX_FLASHBANG_TIME = 11000ms.
    /// </summary>
    public class FlashbangScreenEffect : MonoBehaviour
    {
        /// <summary>
        /// SoF2 MAX_FLASHBANG_AFFECT_DISTANCE: Voller Effektradius (1750 QU × 0.0254 = 44.45m).
        /// Innerhalb dieses Radius skaliert die Intensitaet linear.
        /// </summary>
        private const float FLASH_AFFECT_RADIUS = 44.45f;

        /// <summary>
        /// SoF2 MAX_FLASHBANG_DISTANCE: Maximaler Radius ueberhaupt (3000 QU × 0.0254 = 76.2m).
        /// Ausserhalb dieses Radius kein Effekt.
        /// </summary>
        private const float FLASH_MAX_RADIUS = 76.2f;

        /// <summary>
        /// SoF2 MAX_FLASHBANG_TIME: Maximale Fade-Dauer in Sekunden (11000ms = 11s).
        /// Bei voller Intensitaet (Direkttreffer, hinschauen) betraegt der Fade 11 Sekunden.
        /// </summary>
        private const float MAX_FADE_DURATION = 11f;

        /// <summary>
        /// Maximale Hold-Dauer (komplett weiss) in Sekunden bei voller Intensitaet.
        /// SoF2 hat keine Hold-Phase (sofortiger Fade), aber fuer authentisches
        /// "hart geblendet"-Feeling halten wir bei hoher Intensitaet bis zu 1.5s voll weiss.
        /// </summary>
        private const float MAX_HOLD_DURATION = 1.5f;

        /// <summary>
        /// Hold-Phase setzt erst ab dieser Intensitaet ein.
        /// Unter diesem Wert gibt es keinen harten Blind, nur Fade.
        /// </summary>
        private const float HOLD_INTENSITY_THRESHOLD = 0.5f;

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

            // SoF2 MAX_FLASHBANG_DISTANCE: Ausserhalb komplett ignoriert
            if (distance > FLASH_MAX_RADIUS)
            {
                return;
            }

            // Line-of-Sight Check: Flash darf nicht durch Waende wirken.
            // Raycast von Kamera zur Explosion — wenn blockiert, kein Flash.
            int losMask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("Hitbox"));
            if (Physics.Linecast(camTransform.position, explosionPosition, losMask))
            {
                return;
            }

            // SoF2 cg_weapons.c:1236-1250: Blickrichtung als Distanz-Strafe
            // Wer nicht hinschaut bekommt virtuell mehr Distanz aufaddiert
            Vector3 toExplosion = (explosionPosition - camTransform.position).normalized;
            float dot = Vector3.Dot(camTransform.forward, toExplosion);

            // SoF2: Wenn nicht hinschauen, wird eine Distanz-Strafe addiert
            // dot < 0 = wegschauen, dot > 0 = hinschauen
            float effectiveDistance = distance;
            if (dot < 0.5f)
            {
                // Je mehr wegschauen, desto mehr Distanzstrafe (bis +50% des Affect-Radius)
                float lookPenalty = Mathf.Lerp(FLASH_AFFECT_RADIUS * 0.5f, 0f, Mathf.InverseLerp(-1f, 0.5f, dot));
                effectiveDistance += lookPenalty;
            }

            // SoF2 cg_weapons.c:1260: effectiveDistance = MAX_AFFECT - distance (invertiert)
            float invertedDistance = FLASH_AFFECT_RADIUS - effectiveDistance;
            if (invertedDistance <= 0f)
            {
                return;
            }

            // SoF2 cg_weapons.c:1265: alpha und fadeTime basieren auf invertierter Distanz
            // alpha = invertedDistance / MAX_AFFECT_DISTANCE (0.0-1.0)
            float intensity = Mathf.Clamp01(invertedDistance / FLASH_AFFECT_RADIUS);

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

            // SoF2 MAX_FLASHBANG_TIME: Fade-Dauer skaliert mit Intensitaet
            // Volle Intensitaet = MAX_FADE_DURATION (11s), halbe = 5.5s
            float fadeDuration = MAX_FADE_DURATION * m_Intensity;

            // Hold-Phase: Nur bei hoher Intensitaet (hart geblendet)
            // Skaliert von 0 bis MAX_HOLD_DURATION basierend auf Intensitaet ueber Threshold
            float holdDuration = 0f;
            if (m_Intensity > HOLD_INTENSITY_THRESHOLD)
            {
                float holdFactor = (m_Intensity - HOLD_INTENSITY_THRESHOLD) / (1f - HOLD_INTENSITY_THRESHOLD);
                holdDuration = MAX_HOLD_DURATION * holdFactor;
            }

            float totalDuration = holdDuration + fadeDuration;
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

            // Fade-Dauer und Hold-Dauer identisch wie in Update berechnen
            float fadeDuration = MAX_FADE_DURATION * m_Intensity;
            float holdDuration = 0f;
            if (m_Intensity > HOLD_INTENSITY_THRESHOLD)
            {
                float holdFactor = (m_Intensity - HOLD_INTENSITY_THRESHOLD) / (1f - HOLD_INTENSITY_THRESHOLD);
                holdDuration = MAX_HOLD_DURATION * holdFactor;
            }

            // Alpha berechnen: Hold-Phase → linearer Fade-Out (SoF2 cg_draw.c:1680)
            float alpha;
            if (m_ElapsedTime < holdDuration)
            {
                // Hart geblendet: volles Weiss
                alpha = m_Intensity;
            }
            else
            {
                // SoF2: alpha = flashbangAlpha * (1.0 - elapsed / fadeTime)
                float fadeProgress = (m_ElapsedTime - holdDuration) / fadeDuration;
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
