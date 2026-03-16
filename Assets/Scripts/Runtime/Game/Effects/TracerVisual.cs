using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// Visueller Hitscan-Tracer der von Start nach End fliegt (SoF2 tracerTest2 Tail-Effekt).
    /// Bewegt sich mit der definierten Geschwindigkeit und hinterlaesst einen TrailRenderer.
    /// Wird vom Server-RPC fuer jedes Hitscan-Pellet gespawnt.
    /// Datengetrieben via EffectDefinition aus den JSON-Dateien im Effects/-Unterordner.
    /// </summary>
    public class TracerVisual : MonoBehaviour
    {
        /// <summary>Fallback-Geschwindigkeit falls keine Definition vorhanden (m/s).</summary>
        private const float FALLBACK_SPEED = 300f;

        /// <summary>Fallback-Lebensdauer in Sekunden.</summary>
        private const float FALLBACK_LIFETIME = 0.5f;

        /// <summary>Safety-Timeout: maximale Lebensdauer bevor auto-destroy.</summary>
        private const float MAX_LIFETIME = 3f;

        private Vector3 m_Target;
        private float m_Speed;
        private float m_Lifetime;
        private bool m_Arrived;

        /// <summary>
        /// Erstellt und initialisiert einen TracerVisual anhand der EffectDefinition.
        /// Nutzt EffectFactory fuer Material-Caching und TrailRenderer-Konfiguration.
        /// </summary>
        public static TracerVisual Create(Vector3 start, Vector3 end, string effectId)
        {
            EffectFactory factory = ServiceLocator.Get<EffectFactory>();
            EffectDefinition definition = factory?.GetDefinition(effectId);

            GameObject tracerObj = new("Tracer");
            tracerObj.transform.position = start;

            TracerVisual tracer = tracerObj.AddComponent<TracerVisual>();
            tracer.m_Target = end;
            tracer.m_Lifetime = 0f;
            tracer.m_Arrived = false;

            // Tail-Segment aus Definition suchen
            EffectSegment tailSegment = null;
            float speed = FALLBACK_SPEED;
            float lifetime = FALLBACK_LIFETIME;

            if (definition?.Segments != null)
            {
                foreach (EffectSegment segment in definition.Segments)
                {
                    if (segment.Type == "tail")
                    {
                        tailSegment = segment;

                        if (segment.Trail != null)
                        {
                            speed = segment.Trail.Speed > 0f ? segment.Trail.Speed : FALLBACK_SPEED;
                            lifetime = segment.Trail.Lifetime > 0f ? segment.Trail.Lifetime : FALLBACK_LIFETIME;
                        }

                        break;
                    }
                }
            }

            tracer.m_Speed = speed;

            // TrailRenderer konfigurieren (datengetrieben oder Fallback)
            TrailRenderer trail = tracerObj.AddComponent<TrailRenderer>();

            if (tailSegment != null && factory != null)
            {
                factory.ConfigureTrailRenderer(trail, tailSegment);

                // trail.time bestimmt sichtbare Trail-Laenge (= time × speed).
                // SoF2 length.end definiert gewuenschte Trail-Laenge, nicht trail.Lifetime.
                if (tailSegment.Trail != null)
                {
                    float avgLength = (tailSegment.Trail.LengthMin + tailSegment.Trail.LengthMax) * 0.5f;
                    if (avgLength > 0f && speed > 0f)
                    {
                        trail.time = avgLength / speed;
                    }
                }
            }
            else
            {
                // Fallback: SoF2-typischer gelb-oranger Tracer
                trail.time = 0.08f;
                trail.startWidth = 0.02f;
                trail.endWidth = 0.005f;
                trail.material = new Material(Shader.Find("Sprites/Default"));

                Gradient gradient = new();
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new(new Color(1f, 0.85f, 0.2f), 0f),
                        new(new Color(1f, 0.5f, 0.1f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new(1f, 0f),
                        new(0f, 1f)
                    }
                );
                trail.colorGradient = gradient;
            }

            trail.minVertexDistance = 0.1f;

            // Rotation in Flugrichtung
            Vector3 dir = end - start;
            if (dir.sqrMagnitude > 0.001f)
            {
                tracerObj.transform.rotation = Quaternion.LookRotation(dir);
            }

            return tracer;
        }

        /// <summary>
        /// Bewegt den Tracer mit konstanter Geschwindigkeit zum Zielpunkt.
        /// Zerstoert sich automatisch nach Ankunft + Trail-Fadeout.
        /// </summary>
        private void Update()
        {
            if (m_Arrived)
            {
                return;
            }

            m_Lifetime += Time.deltaTime;

            // Safety-Timeout
            if (m_Lifetime > MAX_LIFETIME)
            {
                m_Arrived = true;
                Destroy(gameObject, 0.1f);
                return;
            }

            // Zum Zielpunkt bewegen
            Vector3 currentPos = transform.position;
            float step = m_Speed * Time.deltaTime;
            float remainingDistance = Vector3.Distance(currentPos, m_Target);

            if (step >= remainingDistance)
            {
                // Angekommen
                transform.position = m_Target;
                m_Arrived = true;

                // Trail-Fadeout abwarten, dann zerstoeren
                TrailRenderer trail = GetComponent<TrailRenderer>();
                float fadeTime = trail != null ? trail.time : FALLBACK_LIFETIME;
                Destroy(gameObject, fadeTime);
            }
            else
            {
                // Weiterfliegen
                Vector3 direction = (m_Target - currentPos).normalized;
                transform.position = currentPos + direction * step;
            }
        }
    }
}