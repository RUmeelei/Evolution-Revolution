using UnityEngine;

namespace ER
{
    namespace Controls
    {
        using Core;
        
        public class SelectionManager : MonoBehaviour
        {
            public static SelectionManager Instance {get; private set;}

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

                CoreManager.RegisterSelectionManager(this);
            }
        }
    }
}