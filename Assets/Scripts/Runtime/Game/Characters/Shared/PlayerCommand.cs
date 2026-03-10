using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
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

        /// <summary>Jump in diesem Frame angefordert.</summary>
        public bool Jump;

        /// <summary>Walk-Taste gedrückt (Shift).</summary>
        public bool Walk;

        /// <summary>Crouch-Taste gedrückt.</summary>
        public bool Crouch;

        /// <summary>DeltaTime des Client-Frames (Sekunden).</summary>
        public float DeltaTime;

        /// <summary>Sequenznummer für Client-Side Prediction Reconciliation.</summary>
        public uint SequenceNumber;

        /// <summary>
        /// Serialisiert den Command für Netcode RPC-Transport.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MoveInput);
            serializer.SerializeValue(ref YawAngle);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Walk);
            serializer.SerializeValue(ref Crouch);
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
        }
    }
}
