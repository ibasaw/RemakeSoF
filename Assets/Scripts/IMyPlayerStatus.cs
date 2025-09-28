using UnityEngine;

public interface IMyPlayerStatus
{
    // Core Status
    float HealthNormalized { get; }
    float StaminaNormalized { get; }
    Vector2 VelocityNormalized { get; }
    //float VelocityMagnitudeNormalized { get; }
    //Vector2 MoveInputNormalized { get; }

    // Orientation
    float HeadingSin { get; }
    float HeadingCos { get; }

    // Actions / State
    //float IsGroundedNormalized { get; }
    //float IsJumpingNormalized { get; }
    //float IsAttackingNormalized { get; }
    //float IsAimingNormalized { get; }
    //float LastActionNormalized { get; }

    // Memory / Target info
    float TimeSinceLastSeenTargetNorm { get; }
    //float DistanceToTargetNormalized { get; }
    //float NearbyTargetCountNormalized { get; }
}
