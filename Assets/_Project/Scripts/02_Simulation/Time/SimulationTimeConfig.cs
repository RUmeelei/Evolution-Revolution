using UnityEngine;

namespace ER.Simulation.Time
{
    [CreateAssetMenu(fileName = "SimulationTimeConfig", menuName = "ER/02_Simulation/Time/Time Config")]
    public class SimulationTimeConfig : ScriptableObject
    {
        [Header("Base Tick Duration")]
        [SerializeField] private float baseSecondsPerTick = 2.5f;
        [SerializeField] private float maxFrameAccumulationTime = 1.0f;

        [Header("Speed Multipliers")]
        [SerializeField] private float speed1 = 1.0f;
        [SerializeField] private float speed2 = 2.0f;
        [SerializeField] private float speed3 = 4.0f;
        [SerializeField] private float speed4 = 20.0f;
        [SerializeField] private float speed5 = 90.0f;

        public float BaseSecondsPerTick => baseSecondsPerTick;
        public float MaxFrameAccumulationTime => maxFrameAccumulationTime;

        public float Speed1 => speed1;
        public float Speed2 => speed2;
        public float Speed3 => speed3;
        public float Speed4 => speed4;
        public float Speed5 => speed5;
    }
}