using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// SoF2-Style Button-Flags fuer usercmd_t.buttons.
    /// Entspricht den BUTTON_* Definitionen aus bg_public.h.
    /// </summary>
    public static class CommandButtons
    {
        /// <summary>Angriff / Feuer (BUTTON_ATTACK).</summary>
        public const int Attack = 1 << 0;

        /// <summary>Springen (BUTTON_JUMP).</summary>
        public const int Jump = 1 << 1;

        /// <summary>Walk / langsam (BUTTON_WALKING).</summary>
        public const int Walk = 1 << 2;

        /// <summary>Ducken (BUTTON_CROUCH).</summary>
        public const int Crouch = 1 << 3;

        /// <summary>Use / Interaktion (BUTTON_USE).</summary>
        public const int Use = 1 << 4;

        /// <summary>Alt-Attack / Sekundaerfeuer (BUTTON_ALT_ATTACK).</summary>
        public const int AltAttack = 1 << 5;

        /// <summary>Zoom / Scopeview.</summary>
        public const int Zoom = 1 << 6;

        /// <summary>Nachladen (BUTTON_RELOAD).</summary>
        public const int Reload = 1 << 7;
    }

    /// <summary>
    /// SoF2-Style Player Command: Enthält den Input eines einzelnen Frames.
    /// Wird vom Client an den Server gesendet anstelle der fertigen Position.
    /// Server re-simuliert den Command mit identischer Physik für autoritative Bewegung.
    /// Entspricht SoF2 usercmd_t aus bg_public.h.
    /// </summary>
    public struct PlayerCommand : INetworkSerializable
    {
        /// <summary>Move-Input (WASD / Stick). X = Strafe, Y = Forward/Back.</summary>
        public Vector2 MoveInput;

        /// <summary>Yaw-Winkel des Charakters in Grad (Kamera-Blickrichtung Y-Rotation).</summary>
        public float YawAngle;

        /// <summary>Pitch-Winkel des Charakters in Grad (Kamera-Blickrichtung X-Rotation). Benötigt fuer server-seitige Hitscan-Richtung.</summary>
        public float PitchAngle;

        /// <summary>
        /// </summary>
        /// <summary>
        /// Button-Bitfield (SoF2 usercmd_t.buttons).
        /// Verwendet CommandButtons-Konstanten fuer Attack, Jump, Walk, Crouch, etc.
        /// </summary>
        public int Buttons;

        /// <summary>DeltaTime des Client-Frames (Sekunden).</summary>
        public float DeltaTime;

        /// <summary>Sequenznummer für Client-Side Prediction Reconciliation.</summary>
        public uint SequenceNumber;

        /// <summary>Prueft ob ein Button-Flag gesetzt ist.</summary>
        public readonly bool HasButton(int button) => (Buttons & button) != 0;

        /// <summary>
        /// Serialisiert den Command für Netcode RPC-Transport.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MoveInput);
            serializer.SerializeValue(ref YawAngle);
            serializer.SerializeValue(ref PitchAngle);
            serializer.SerializeValue(ref Buttons);
            serializer.SerializeValue(ref DeltaTime);
            serializer.SerializeValue(ref SequenceNumber);
        }
    }

    /// <summary>
    /// Server → Client Acknowledgement: autoritative Position nach Verarbeitung eines Commands.
    /// Wird für Client-Side Prediction Reconciliation verwendet.
    /// </summary>
    public struct ServerMovementAck : INetworkSerializable
    {
        /// <summary>Sequenznummer des zuletzt verarbeiteten Commands.</summary>
        public uint LastProcessedSequence;

        /// <summary>Autoritative Position vom Server.</summary>
        public Vector3 Position;

        /// <summary>Autoritative Velocity vom Server.</summary>
        public Vector3 Velocity;

        /// <summary>Autoritative Grounded-State vom Server.</summary>
        public bool IsGrounded;

        /// <summary>Autoritative Jump-State vom Server.</summary>
        public bool IsJumping;

        /// <summary>Autoritativer Crouch-State vom Server.</summary>
        public bool IsCrouching;

        /// <summary>SoF2 PMF_TIME_KNOCKBACK Timer (Sekunden). Verhindert Friction waehrend Knockback.</summary>
        public float KnockbackTime;

        /// <summary>
        /// Serialisiert das Acknowledgement für Netcode RPC-Transport.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref LastProcessedSequence);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref IsJumping);
            serializer.SerializeValue(ref IsCrouching);
            serializer.SerializeValue(ref KnockbackTime);
        }
    }
}
