using Fusion;
using UnityEngine;

namespace Spells {
    /// <summary>
    /// MonoBehaviour component attached to the Fireball prefab GameObject.
    /// Handles the actual fireball projectile behavior in Unity.
    /// </summary>
    public class FireballProjectile : NetworkBehaviour {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 2f;
        
        [Networked] private Vector3 Direction { get; set; }
        [Networked] private float SpawnTime { get; set; }
        [Networked] private bool IsInitialized { get; set; }
        
        public override void Spawned() {
            Debug.Log($"FireballProjectile.Spawned() called - HasStateAuthority: {Object.HasStateAuthority}, IsServer: {Runner.IsServer}, IsClient: {Runner.IsClient}");
            
            if (Object.HasStateAuthority) {
                SpawnTime = Runner.SimulationTime;
                // Initialize direction from transform.forward (set by Quaternion.LookRotation during spawn)
                Direction = transform.forward.normalized;
                IsInitialized = true;
                
                Debug.Log($"FireballProjectile initialized on server - Direction: {Direction}, Speed: {speed}, Lifetime: {lifetime}, Position: {transform.position}");
            } else {
                Debug.LogWarning($"FireballProjectile spawned but does NOT have state authority! This fireball will not move.");
            }
        }
        
        public void Init(Vector3 direction) {
            if (Object.HasStateAuthority) {
                Direction = direction.normalized;
                SpawnTime = Runner.SimulationTime;
                IsInitialized = true;
                Debug.Log($"FireballProjectile Init called with direction: {Direction}");
            }
        }
        
        public override void FixedUpdateNetwork() {
            // Safety check - ensure Runner and Object are valid
            if (Runner == null || Object == null) return;
            
            // Debug every 30 ticks to avoid spam
            if (Runner.Tick % 30 == 0) {
                Debug.Log($"FixedUpdateNetwork - HasStateAuthority: {Object.HasStateAuthority}, IsInitialized: {IsInitialized}, Direction: {Direction}");
            }
            
            // Only the server/host (state authority) should move and manage the fireball
            if (!Object.HasStateAuthority) {
                if (Runner.Tick % 60 == 0) {
                    Debug.LogWarning("FireballProjectile FixedUpdateNetwork called but no state authority!");
                }
                return;
            }
            
            if (!IsInitialized) {
                Debug.LogWarning("FireballProjectile not initialized yet!");
                return;
            }
            
            // Move the fireball forward
            // NetworkTransform will sync the position to all clients
            float deltaTime = Runner.DeltaTime;
            Vector3 movement = Direction * speed * deltaTime;
            transform.position += movement;
            
            if (Runner.Tick % 30 == 0) {
                Debug.Log($"Fireball moving - Position: {transform.position}, Movement: {movement}, DeltaTime: {deltaTime}");
            }
            
            // Destroy after lifetime expires
            float elapsed = Runner.SimulationTime - SpawnTime;
            if (elapsed >= lifetime) {
                Debug.Log($"Fireball lifetime expired ({elapsed} >= {lifetime}), despawning");
                if (Runner != null && Object != null) {
                    Runner.Despawn(Object);
                }
            }
        }
        
        private void OnTriggerEnter(Collider other) {
            // Safety checks
            if (Runner == null || Object == null) return;
            if (!Object.HasStateAuthority) return;
            
            // Don't collide with the caster
            if (other == null || other.transform == null) return;
            if (other.transform.root == transform.root) return;
            
            // Handle collision logic here
            // For now, just despawn on any collision
            Debug.Log($"Fireball collided with {other.name}, despawning");
            if (Runner != null && Object != null) {
                Runner.Despawn(Object);
            }
        }
    }
}

