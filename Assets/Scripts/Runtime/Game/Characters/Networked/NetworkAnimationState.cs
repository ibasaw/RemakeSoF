using System;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Kompaktes Paket für netzwerk-synchronisierte Animator-Parameter.
    /// Boolean-Werte werden als Bitflags in einem Byte gespeichert um Bandbreite zu sparen.
    /// Owner schreibt, alle Clients lesen und treiben damit ihren lokalen Animator.
    /// </summary>
    public struct NetworkAnimationState : INetworkSerializable, IEquatable<NetworkAnimationState>
    {
        /// <summary>
        /// Horizontale Geschwindigkeit (Magnitude) des Characters.
        /// </summary>
        public float Speed;

        /// <summary>
        /// Geglätteter Input-Wert auf der Horizontal-Achse (links/rechts).
        /// </summary>
        public float Horizontal;

        /// <summary>
        /// Geglätteter Input-Wert auf der Vertical-Achse (vorwärts/rückwärts).
        /// </summary>
        public float Vertical;

        /// <summary>
        /// Roher MoveInput X-Wert (links/rechts) für Remote-Bone-Rotation.
        /// </summary>
        public float MoveInputX;

        /// <summary>
        /// Roher MoveInput Y-Wert (vorwärts/rückwärts) für Remote-Bone-Rotation.
        /// </summary>
        public float MoveInputY;

        /// <summary>
        /// Pitch-Winkel (Grad) für Remote-Lumbar-Rotation.
        /// </summary>
        public float PitchAngle;

        /// <summary>
        /// Bitflags für boolesche Animator-Parameter.
        /// Bit 0 (0x01): IsMoving, Bit 1 (0x02): IsGrounded,
        /// Bit 2 (0x04): IsWalking, Bit 3 (0x08): IsAttacking,
        /// Bit 4 (0x10): IsCrouching.
        /// </summary>
        public byte Flags;

        /// <summary>
        /// Index der aktuellen Waffe fuer Animator (0 = knife, 1 = rpg7).
        /// </summary>
        public byte CurrentWeapon;

        /// <summary>
        /// Ob der Character sich bewegt (horizontale Geschwindigkeit > Schwellwert).
        /// </summary>
        public bool IsMoving
        {
            readonly get => (Flags & 0x01) != 0;
            set => Flags = (byte)(value ? Flags | 0x01 : Flags & ~0x01);
        }

        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        public bool IsGrounded
        {
            readonly get => (Flags & 0x02) != 0;
            set => Flags = (byte)(value ? Flags | 0x02 : Flags & ~0x02);
        }

        /// <summary>
        /// Ob der Character im Walk-Modus ist (langsamer).
        /// </summary>
        public bool IsWalking
        {
            readonly get => (Flags & 0x04) != 0;
            set => Flags = (byte)(value ? Flags | 0x04 : Flags & ~0x04);
        }

        /// <summary>
        /// Ob der Character gerade angreift.
        /// </summary>
        public bool IsAttacking
        {
            readonly get => (Flags & 0x08) != 0;
            set => Flags = (byte)(value ? Flags | 0x08 : Flags & ~0x08);
        }

        /// <summary>
        /// Ob der Character sich duckt.
        /// </summary>
        public bool IsCrouching
        {
            readonly get => (Flags & 0x10) != 0;
            set => Flags = (byte)(value ? Flags | 0x10 : Flags & ~0x10);
        }

        /// <inheritdoc/>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Speed);
            serializer.SerializeValue(ref Horizontal);
            serializer.SerializeValue(ref Vertical);
            serializer.SerializeValue(ref MoveInputX);
            serializer.SerializeValue(ref MoveInputY);
            serializer.SerializeValue(ref PitchAngle);
            serializer.SerializeValue(ref Flags);
            serializer.SerializeValue(ref CurrentWeapon);
        }

        /// <inheritdoc/>
        public readonly bool Equals(NetworkAnimationState other)
        {
            return Mathf.Approximately(Speed, other.Speed) &&
                   Mathf.Approximately(Horizontal, other.Horizontal) &&
                   Mathf.Approximately(Vertical, other.Vertical) &&
                   Mathf.Approximately(MoveInputX, other.MoveInputX) &&
                   Mathf.Approximately(MoveInputY, other.MoveInputY) &&
                   Mathf.Approximately(PitchAngle, other.PitchAngle) &&
                   Flags == other.Flags &&
                   CurrentWeapon == other.CurrentWeapon;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is NetworkAnimationState other && Equals(other);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Speed, Horizontal, Vertical, MoveInputX, MoveInputY, PitchAngle, Flags, CurrentWeapon);
        }
    }
}
