namespace Spells {
    using Fusion;
    using UnityEngine;

    public class SpellReferences : MonoBehaviour {
        [SerializeField] private NetworkPrefabRef fireballPrefab;

        public NetworkPrefabRef Fireball => fireballPrefab;
        
        [SerializeField] private AudioClip fireballCastSound;
        public AudioClip FireballCastSound => fireballCastSound;

        [SerializeField] private NetworkPrefabRef golemPrefab;
        public NetworkPrefabRef Golem => golemPrefab;

        [SerializeField] private NetworkPrefabRef dragonPetPrefab;
        public NetworkPrefabRef DragonPet => dragonPetPrefab;

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