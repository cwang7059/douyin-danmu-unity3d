using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed partial class ApocalypseKingUnityGame
{
    private readonly struct ResolutionPreset
    {
        public readonly string Label;
        public readonly int Width;
        public readonly int Height;

        public ResolutionPreset(string label, int width, int height)
        {
            Label = label;
            Width = width;
            Height = height;
        }
    }

    private enum TankModelVariant
    {
        None,
        T55A,
        T55AK,
    }

    private enum TeamKind
    {
        Human,
        Giant,
    }

    private enum ProjectileKind
    {
        Bullet,
        Shell,
        Rocket,
        Rock,
        Bomb,
    }

    private enum ProjectileTarget
    {
        Giant,
        Human,
    }

    private enum EffectKind
    {
        Fireball,
        Smoke,
    }

    private sealed class ModelPose
    {
        public readonly float TargetHeight;
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly float Roll;
        public readonly float BodyOffset;
        public readonly bool MirrorWithFacing;

        public ModelPose(float targetHeight, float pitch, float yaw, float roll, float bodyOffset, bool mirrorWithFacing)
        {
            TargetHeight = targetHeight;
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
            BodyOffset = bodyOffset;
            MirrorWithFacing = mirrorWithFacing;
        }
    }

    private sealed class BattleUnit
    {
        public int id;
        public UnitKind kind;
        public TankModelVariant tankModel;
        public TeamKind team;
        public GameObject root;
        public Transform body;
        public GameObject modelInstance;
        public Animation[] animations;
        public Animator animator;
        public AnimationClip[] animatorClips;
        public PlayableGraph animationGraph;
        public AnimationClipPlayable animationPlayable;
        public AnimationPlayableOutput animationOutput;
        public string currentAnimation;
        public string currentAnimatorClip;
        public bool active;
        public Transform tankAimRoot;
        public Transform tankTurretVisual;
        public Transform tankBarrelVisual;
        public Transform tankMuzzleVisual;
        public float x;
        public float z;
        public float baseZ;
        public float altitude;
        public float hp;
        public float maxHp;
        public float damage;
        public float baseSpeed;
        public float speed;
        public float radius;
        public float attackRange;
        public float attackInterval;
        public float attackCooldown;
        public float attackVisualTimer;
        public float hitFlashTimer;
        public float seed;
        public int rank;
        public int facing;
        public float headingDegrees;
        public float turretYawDegrees;
        public float modelYawOffset;
        public float animTimer;
        public float moveSpeed;
        public float rotorSpinDegrees;
        public float wheelSpinDegrees;
        public float trackScroll;
        public Vector3 baseModelScale;
        public Vector3 baseModelLocalPosition;
        public Transform aircraftRotorRoot;
        public Quaternion aircraftRotorBaseLocalRotation;
        public readonly List<AircraftRotorRig> aircraftRotorRigs = new List<AircraftRotorRig>(2);
        public TankMotionRig tankMotionRig;
        public GameObject motionAccessoryRoot;
    }

    private sealed class AircraftRotorRig
    {
        public Transform rotor;
        public Quaternion baseLocalRotation;
        public Vector3 localAxis;
        public float speedMultiplier;
    }

    private sealed class TankMotionRig
    {
        public readonly List<Transform> wheelTransforms = new List<Transform>();
        public readonly List<Quaternion> wheelBaseRotations = new List<Quaternion>();
        public readonly List<Material> trackMaterials = new List<Material>();
        public readonly List<Transform> aimTransforms = new List<Transform>();
        public readonly List<Quaternion> aimBaseRotations = new List<Quaternion>();
        public Transform helperRoot;
    }

    private sealed class BuildingObstacle
    {
        public readonly GameObject Root;
        public readonly string Name;
        public readonly float CenterX;
        public readonly float CenterZ;
        public readonly float HalfX;
        public readonly float HalfZ;
        public readonly float Height;
        public readonly float Padding;
        public float Hp;
        public bool Destroyed;

        public BuildingObstacle(GameObject root, string name, float centerX, float centerZ, float halfX, float halfZ, float height, float padding, float hp)
        {
            Root = root;
            Name = name;
            CenterX = centerX;
            CenterZ = centerZ;
            HalfX = halfX;
            HalfZ = halfZ;
            Height = height;
            Padding = padding;
            Hp = hp;
            Destroyed = false;
        }
    }

    private sealed class RoadCorridor
    {
        public readonly string Name;
        public readonly float CenterX;
        public readonly float CenterZ;
        public readonly float HalfX;
        public readonly float HalfZ;
        public readonly float Priority;

        public bool Horizontal => HalfX >= HalfZ;

        public RoadCorridor(string name, float centerX, float centerZ, float halfX, float halfZ, float priority)
        {
            Name = name;
            CenterX = centerX;
            CenterZ = centerZ;
            HalfX = halfX;
            HalfZ = halfZ;
            Priority = priority;
        }

        public float DistanceToPoint(Vector2 point)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - CenterX) - HalfX);
            float dz = Mathf.Max(0f, Mathf.Abs(point.y - CenterZ) - HalfZ);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    private sealed class ProjectileView
    {
        public ProjectileKind kind;
        public ProjectileTarget target;
        public GameObject root;
        public LineRenderer line;
        public Transform head;
        public bool active;
        public float fromX;
        public float fromZ;
        public float toX;
        public float toZ;
        public float fromHeight;
        public float toHeight;
        public float progress;
        public float duration;
        public float trailTimer;
        public float damage;
        public float radius;
        public float speed;
        public Color color;
        public Vector3 lastWorldPosition;
        public Vector3 worldPosition;
    }

    private sealed class EffectView
    {
        public bool active;
        public GameObject root;
        public Transform orb;
        public Light light;
        public EffectKind kind;
        public float baseScale;
        public float life;
        public float maxLife;
    }

    private sealed class DeathVisual
    {
        public UnitKind kind;
        public GameObject root;
        public bool active;
        public float life;
        public float maxLife;
        public float smokeTimer;
        public bool crashTriggered;
        public Vector3 velocity;
    }
}
