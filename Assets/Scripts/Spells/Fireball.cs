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
        
        // Interface implementation - must match exactly
        public void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection) {
            // Call the overloaded version with default values
            Cast(runner, caster, spawnPosition, forwardDirection, -1f, -1f);
        }
        
        // Overloaded method with speed and scale parameters
        public void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection, float speed, float scale) {
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
                        // Check if caster is a Boss
                        bool isCasterBoss = caster.GetComponent<Boss>() != null;
                        // Use provided speed/scale if specified, otherwise use defaults
                        fireballProjectile.Init(forwardDirection, isCasterBoss, speed, scale);
                    }
                }
            );
            
            Debug.Log($"{caster.InputAuthority} cast {_name}!");
        }
    }
}