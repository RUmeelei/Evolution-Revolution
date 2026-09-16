using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        using Core;
        using Configs;

        public class UnitVisualPoolManager : MonoBehaviour
        {
            public static UnitVisualPoolManager Instance {get; private set;}

            [Header("Main")]
            [SerializeField] private GameObject Prefab;

            private Queue<UnitVisual> Pool = new Queue<UnitVisual>();

            private MainConfig mainConfig;

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

                CoreManager.RegisterUnitVisualPoolManager(this);
            }

            public void Initialize()
            {
                mainConfig = CoreManager.MainConfig;

                for (int i = 0; i < mainConfig.UnitVisualInitialSize; i++)
                {
                    var view = CreateNew();

                    Pool.Enqueue(view);
                }
            }

            private UnitVisual CreateNew()
            {
                GameObject go = Instantiate(Prefab, transform);

                go.SetActive(false);

                return go.GetComponent<UnitVisual>();
            }

            public UnitVisual Get()
            {
                if (Pool.Count > 0)
                {
                    var view = Pool.Dequeue();

                    view.gameObject.SetActive(true);

                    return view;
                }
                
                return CreateNew();
            }

            public void Return(UnitVisual view)
            {
                view.Release();

                view.gameObject.SetActive(false);

                Pool.Enqueue(view);
            }
        }
    }
}