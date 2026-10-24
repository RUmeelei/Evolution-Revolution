using UnityEngine;

namespace ER.World.Population
{
    [CreateAssetMenu(fileName = "PopLocomotionConfig", menuName = "ER/06_Population/Locomotion Config")]
    public class PopLocomotionConfig : ScriptableObject
    {
        [Header("Kinematics Speeds (World Units/Sec)")]
        [SerializeField] private float baseWalkSpeed = 2.4f;

        [SerializeField] private float runSpeed = 4.8f;

        [SerializeField] private float accelerationRate = 7.5f;

        [SerializeField] private float dragDamping = 4.0f;


        [Header("Local Avoidance & Steering")]
        [SerializeField] private float separationRadius = 0.95f;

        [SerializeField] private float separationWeight = 3.2f;

        [SerializeField] private float arrivalDistance = 0.35f;

        [SerializeField] private float spatialCellSize = 2.0f;


        [Header("Autonomous Wandering")]
        [SerializeField] private float wanderRadius = 14.0f;

        [SerializeField] private uint minIdleTicks = 10;

        [SerializeField] private uint maxIdleTicks = 45;


        public float BaseWalkSpeed => baseWalkSpeed;

        public float RunSpeed => runSpeed;

        public float AccelerationRate => accelerationRate;

        public float DragDamping => dragDamping;

        public float SeparationRadius => separationRadius;

        public float SeparationWeight => separationWeight;

        public float ArrivalDistance => arrivalDistance;

        public float SpatialCellSize => spatialCellSize;

        public float WanderRadius => wanderRadius;

        public uint MinIdleTicks => minIdleTicks;

        public uint MaxIdleTicks => maxIdleTicks;
    }
}