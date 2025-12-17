using Fusion;
using UnityEngine;

namespace Spells {
    public class Fireball : ISpell {
        private float _lifeTime = 2f;
        private AudioClip _castSound;
        private string _name = "Fireball";
        private NetworkPrefabRef _prefab;
        
        /// <summary>
        /// Constructor that accepts a prefab for skin customization
        /// </summary>
        /// <param name="prefab">The NetworkPrefabRef of the fireball prefab to use</param>
        public Fireball(NetworkPrefabRef prefab) {
            _prefab = prefab;
            if (SpellReferences.Instance != null) {
                _castSound = SpellReferences.Instance.FireballCastSound;
            }
        }
        
        /// <summary>
        /// Default constructor that falls back to SpellReferences if available
        /// </summary>
        public Fireball() {
            // Fallback to SpellReferences for backward compatibility
            if (SpellReferences.Instance != null) {
                _prefab = SpellReferences.Instance.Fireball;
                _castSound = SpellReferences.Instance.FireballCastSound;
            } else {
                Debug.LogWarning("Fireball created without prefab and SpellReferences.Instance is null. Prefab must be set manually.");
            }
        }
        
        public void CastSpell() {
            Debug.Log("Casting fireball spell!!");
        }

        public string GetName() {
            return _name;
        }

        public NetworkPrefabRef GetPrefab() {
            return _prefab;
        }
        
        /// <summary>
        /// Sets the prefab for this fireball instance (allows runtime skin changes)
        /// </summary>
        /// <param name="prefab">The NetworkPrefabRef of the fireball prefab to use</param>
        public void SetPrefab(NetworkPrefabRef prefab) {
            _prefab = prefab;
        }
        
        // Interface implementation - must match exactly
        public void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection) {
            // Call the overloaded version with default values
            Cast(runner, caster, spawnPosition, forwardDirection, -1f, -1f);
        }
        
        // Overloaded method with speed and scale parameters
        public void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection, float speed, float scale) {
            if (!_prefab.IsValid) {
                Debug.LogError($"Fireball spell '{_name}' has no prefab assigned!");
                return;
            }

            runner.Spawn(
                _prefab,
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
            
            if (_castSound != null) {
                AudioSource.PlayClipAtPoint(_castSound, spawnPosition);
            }

            Debug.Log($"{caster.InputAuthority} cast {_name}!");
        }
    }
}