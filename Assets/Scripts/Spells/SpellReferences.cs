namespace Spells {
    using Fusion;
    using UnityEngine;

    public class SpellReferences : MonoBehaviour {
        [SerializeField] private NetworkPrefabRef fireballPrefab;

        public NetworkPrefabRef Fireball => fireballPrefab;

        public static SpellReferences Instance { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}