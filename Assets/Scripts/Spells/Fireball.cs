using Fusion;
using UnityEngine;

namespace Spells {
    public class Fireball : ISpell {
        private float _lifeTime = 2f;
        private string _name = "Fireball";
        private NetworkPrefabRef Prefab => SpellReferences.Instance.Fireball;        
        
        public void CastSpell() {
            Debug.Log("Casting fireball spell!!");
        }

        public string GetName() {
            return _name;
        }

        public NetworkPrefabRef GetPrefab() {
            return Prefab;
        }
        
        public void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection) {
            if (!Prefab.IsValid) {
                Debug.LogError($"Fireball spell '{_name}' has no prefab assigned!");
                return;
            }

            runner.Spawn(
                Prefab,
                spawnPosition,
                Quaternion.LookRotation(forwardDirection),
                caster.InputAuthority,
                (r, obj) => {
                    Fireball fireball = obj.GetComponent<Fireball>();
                    if (fireball != null) {
                        fireball.Init(forwardDirection);
                    }
                }
            );
            
            Debug.Log($"{caster.InputAuthority} cast {_name}!");
        }
        
        public void Init(Vector3 direction) {
            Debug.Log($"Fireball initialized with direction: {direction}");
        }
    }
}