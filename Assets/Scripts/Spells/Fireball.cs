using Fusion;
using UnityEngine;

namespace Spells {
    public class Fireball : ISpell {
        private float _lifeTime = 2f;
        private string _name = "Fireball";
        private NetworkPrefabRef Prefab {
            get {
                if (SpellReferences.Instance == null) {
                    Debug.LogError("SpellReferences.Instance is null! Make sure SpellReferences is in the scene.");
                    return default;
                }
                return SpellReferences.Instance.Fireball;
            }
        }        
        
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
                    FireballProjectile fireballProjectile = obj.GetComponent<FireballProjectile>();
                    if (fireballProjectile != null) {
                        fireballProjectile.Init(forwardDirection);
                    }
                }
            );
            
            Debug.Log($"{caster.InputAuthority} cast {_name}!");
        }
    }
}