using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Resources
    {
        using Core;
        using Simulation;

        public class ResourceManager : MonoBehaviour
        {
            public static ResourceManager Instance {get; private set;}

            [Header("Main")]
            public bool EnableLogging;

            public List<ResourceData> Resources = new List<ResourceData>();
            private Dictionary<string, ResourceData> ResourcesDictionary = new Dictionary<string, ResourceData>();

            private SimulationManager simulationManager;

            public event Action<float> OnResourceTick;

            void Awake()
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }

                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject); return;
                }

                Instance = this;

                DontDestroyOnLoad(gameObject);

                CoreManager.RegisterResourceManager(this);
            }

            void Start()
            {
                simulationManager = CoreManager.SimulationManager;

                simulationManager.OnSlowTick += ProcessResourceTick;
            }

            void OnDestroy()
            {
                if (simulationManager != null) simulationManager.OnSlowTick -= ProcessResourceTick;
            }

            private void ProcessResourceTick(float delta)
            {
                OnResourceTick?.Invoke(delta);
            }
        }
    }
}
