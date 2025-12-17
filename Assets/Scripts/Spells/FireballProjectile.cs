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
        [Networked] private bool IsCasterBoss { get; set; } // True if the caster is a Boss, false if it's a Player
        [Networked] private float CustomSpeed { get; set; } // Custom speed override (-1 means use default)
        [Networked] private float CustomScale { get; set; } // Custom scale override (-1 means use default)
        private bool _hasHitSomething = false; // Prevent multiple hits
        
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
        
        public void Init(Vector3 direction, bool isCasterBoss = false, float customSpeed = -1f, float customScale = -1f) {
            if (Object.HasStateAuthority) {
                Direction = direction.normalized;
                SpawnTime = Runner.SimulationTime;
                IsCasterBoss = isCasterBoss;
                CustomSpeed = customSpeed;
                CustomScale = customScale;
                IsInitialized = true;
                
                // Force player fireballs to do 100 damage for testing (overriding Inspector value)
                if (!IsCasterBoss) {
                    damage = 100f;
                }
                
                // Apply scale immediately if specified
                if (customScale > 0f) {
                    transform.localScale = Vector3.one * customScale;
                }
                
                Debug.Log($"FireballProjectile Init called with direction: {Direction}, IsCasterBoss: {IsCasterBoss}, Speed: {customSpeed}, Scale: {customScale}, Damage: {damage}");
            }
        }
        
        public override void Render() {
            // Apply scale on all clients (in case NetworkTransform doesn't sync it)
            if (CustomScale > 0f && transform.localScale != Vector3.one * CustomScale) {
                transform.localScale = Vector3.one * CustomScale;
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
            // Use custom speed if specified, otherwise use default speed
            float currentSpeed = CustomSpeed > 0f ? CustomSpeed : speed;
            Vector3 movement = Direction * currentSpeed * deltaTime;
            Vector3 previousPosition = transform.position;
            transform.position += movement;
            
            // Manual collision check using Physics.OverlapSphere
            // This is a fallback if OnTriggerEnter doesn't work
            // If collision detected and object despawned, return early
            if (CheckCollisions(previousPosition, transform.position)) {
                return; // Object was despawned, exit early
            }
            
            // Safety check again after collision check (in case object was despawned)
            if (Runner == null || Object == null) return;
            
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
                return; // Exit after despawning
            }
        }
        
        /// <summary>
        /// Manually checks for collisions using Physics.OverlapSphere.
        /// This is a fallback method since OnTriggerEnter might not work reliably with NetworkBehaviour.
        /// </summary>
        /// <returns>True if a collision was detected and the object was despawned, false otherwise</returns>
        private bool CheckCollisions(Vector3 from, Vector3 to) {
            if (!Object.HasStateAuthority) return false;
            if (_hasHitSomething) return false; // Already hit something, don't check again
            
            // Safety check
            if (Runner == null || Object == null) return false;
            
            // Get the collider radius (assuming sphere collider)
            Collider fireballCollider = GetComponent<Collider>();
            float radius = 0.5f; // Default radius
            
            if (fireballCollider is SphereCollider sphereCollider) {
                radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            } else if (fireballCollider is CapsuleCollider capsuleCollider) {
                radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            } else if (fireballCollider is BoxCollider boxCollider) {
                radius = Mathf.Max(boxCollider.size.x, boxCollider.size.y, boxCollider.size.z) * 0.5f * 
                         Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            }
            
            // Use OverlapSphere to check for collisions at current position
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            
            foreach (Collider hit in hits) {
                if (hit == null || hit.transform == null) continue;
                
                // Don't collide with self
                if (hit.transform.root == transform.root) continue;
                
                Debug.Log($"Manual collision check detected: {hit.name}");
                
                // Check what we hit based on who cast the fireball
                if (IsCasterBoss) {
                    // Boss cast this fireball - damage players (anything that's NOT a boss)
                    Boss boss = hit.GetComponent<Boss>();
                    if (boss == null) {
                        boss = hit.GetComponentInParent<Boss>();
                    }
                    if (boss == null) {
                        boss = hit.GetComponentInChildren<Boss>();
                    }
                    
                    // If it's NOT a boss, it's a player - damage them
                    if (boss == null) {
                        // Check if it's a player (has PlayerStateMachine or is a player NetworkObject)
                        PlayerStateMachine.PlayerStateMachine player = hit.GetComponent<PlayerStateMachine.PlayerStateMachine>();
                        if (player == null) {
                            player = hit.GetComponentInParent<PlayerStateMachine.PlayerStateMachine>();
                        }
                        if (player == null) {
                            player = hit.GetComponentInChildren<PlayerStateMachine.PlayerStateMachine>();
                        }
                        
                        if (player != null) {
                            // Get PlayerHealth component and deal damage
                            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                            if (playerHealth == null) {
                                playerHealth = player.GetComponentInParent<PlayerHealth>();
                            }
                            if (playerHealth == null) {
                                playerHealth = player.GetComponentInChildren<PlayerHealth>();
                            }
                            
                            if (playerHealth != null) {
                                playerHealth.TakeDamage(damage);
                                Debug.Log($"Boss fireball hit player {hit.name} for {damage} damage!");
                            } else {
                                Debug.LogWarning($"Boss fireball hit player {hit.name} but no PlayerHealth component found!");
                            }
                            _hasHitSomething = true;
                            
                            // Despawn the fireball after hitting player
                            if (Runner != null && Object != null) {
                                Runner.Despawn(Object);
                            }
                            return true;
                        }
                    }
                } else {
                    // Player cast this fireball - damage bosses
                    Boss boss = hit.GetComponent<Boss>();
                    if (boss == null) {
                        boss = hit.GetComponentInParent<Boss>();
                    }
                    if (boss == null) {
                        boss = hit.GetComponentInChildren<Boss>();
                    }
                    
                    if (boss != null) {
                        Debug.Log($"Player fireball hit boss {hit.name}! Dealing {damage} damage.");
                        _hasHitSomething = true; // Mark as hit to prevent multiple hits
                        boss.TakeDamage(damage);
                        Debug.Log($"Fireball hit boss {hit.name} for {damage} damage!");
                        
                        // Despawn the fireball after hitting boss
                        if (Runner != null && Object != null) {
                            Runner.Despawn(Object);
                        }
                        return true; // Return true to indicate object was despawned
                    }
                }
            }
            
            return false; // No collision detected
        }
        
        [SerializeField] private float damage = 100f; // Damage dealt by fireball
        
        private void OnTriggerEnter(Collider other) {
            Debug.Log($"OnTriggerEnter called! Collider: {other?.name}, HasStateAuthority: {Object?.HasStateAuthority}");
            
            // Safety checks
            if (Runner == null || Object == null) {
                Debug.LogWarning("Fireball collision: Runner or Object is null");
                return;
            }
            
            if (!Object.HasStateAuthority) {
                Debug.LogWarning($"Fireball collision: No state authority. HasStateAuthority: {Object.HasStateAuthority}");
                return;
            }
            
            // Don't collide with the caster
            if (other == null || other.transform == null) {
                Debug.LogWarning("Fireball collision: other or transform is null");
                return;
            }
            
            if (other.transform.root == transform.root) {
                Debug.Log($"Fireball collision: Ignoring collision with caster ({other.name})");
                return;
            }
            
            Debug.Log($"Fireball collision detected with: {other.name}");
            
            // Check what we hit based on who cast the fireball
            bool shouldDespawn = false;
            
            if (IsCasterBoss) {
                // Boss cast this fireball - damage players (anything that's NOT a boss)
                Boss boss = other.GetComponent<Boss>();
                if (boss == null) {
                    boss = other.GetComponentInParent<Boss>();
                }
                if (boss == null) {
                    boss = other.GetComponentInChildren<Boss>();
                }
                
                // If it's NOT a boss, it's a player - damage them
                if (boss == null) {
                    // Check if it's a player (has PlayerStateMachine or is a player NetworkObject)
                    PlayerStateMachine.PlayerStateMachine player = other.GetComponent<PlayerStateMachine.PlayerStateMachine>();
                    if (player == null) {
                        player = other.GetComponentInParent<PlayerStateMachine.PlayerStateMachine>();
                    }
                    if (player == null) {
                        player = other.GetComponentInChildren<PlayerStateMachine.PlayerStateMachine>();
                    }
                    
                    if (player != null) {
                        // Get PlayerHealth component and deal damage
                        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                        if (playerHealth == null) {
                            playerHealth = player.GetComponentInParent<PlayerHealth>();
                        }
                        if (playerHealth == null) {
                            playerHealth = player.GetComponentInChildren<PlayerHealth>();
                        }
                        
                        if (playerHealth != null) {
                            playerHealth.TakeDamage(damage);
                            Debug.Log($"Boss fireball hit player {other.name} for {damage} damage!");
                        } else {
                            Debug.LogWarning($"Boss fireball hit player {other.name} but no PlayerHealth component found!");
                        }
                        shouldDespawn = true;
                    }
                }
            } else {
                // Player cast this fireball - damage bosses
                Boss boss = other.GetComponent<Boss>();
                if (boss == null) {
                    boss = other.GetComponentInParent<Boss>();
                }
                if (boss == null) {
                    boss = other.GetComponentInChildren<Boss>();
                }
                
                if (boss != null) {
                    Debug.Log($"Player fireball hit boss {other.name}! Dealing {damage} damage.");
                    // Deal damage to the boss
                    boss.TakeDamage(damage);
                    Debug.Log($"Fireball hit boss {other.name} for {damage} damage!");
                    shouldDespawn = true;
                }
            }
            
            // Despawn the fireball after collision
            if (shouldDespawn) {
                Debug.Log($"Fireball collided with {other.name}, despawning");
                if (Runner != null && Object != null) {
                    Runner.Despawn(Object);
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision) {
            // Also handle regular collisions (non-trigger)
            if (collision != null && collision.collider != null) {
                Debug.Log($"OnCollisionEnter called! Collider: {collision.collider.name}");
                HandleCollision(collision.collider);
            }
        }
        
        private void HandleCollision(Collider other) {
            // Safety checks
            if (Runner == null || Object == null) {
                Debug.LogWarning("Fireball collision: Runner or Object is null");
                return;
            }
            
            if (!Object.HasStateAuthority) {
                Debug.LogWarning($"Fireball collision: No state authority. HasStateAuthority: {Object.HasStateAuthority}");
                return;
            }
            
            // Don't collide with the caster
            if (other == null || other.transform == null) {
                Debug.LogWarning("Fireball collision: other or transform is null");
                return;
            }
            
            if (other.transform.root == transform.root) {
                Debug.Log($"Fireball collision: Ignoring collision with caster ({other.name})");
                return;
            }
            
            Debug.Log($"Fireball collision detected with: {other.name}");
            
            // Check what we hit based on who cast the fireball
            bool shouldDespawn = false;
            
            if (IsCasterBoss) {
                // Boss cast this fireball - damage players (anything that's NOT a boss)
                Boss boss = other.GetComponent<Boss>();
                if (boss == null) {
                    boss = other.GetComponentInParent<Boss>();
                }
                if (boss == null) {
                    boss = other.GetComponentInChildren<Boss>();
                }
                
                // If it's NOT a boss, it's a player - damage them
                if (boss == null) {
                    // Check if it's a player (has PlayerStateMachine or is a player NetworkObject)
                    PlayerStateMachine.PlayerStateMachine player = other.GetComponent<PlayerStateMachine.PlayerStateMachine>();
                    if (player == null) {
                        player = other.GetComponentInParent<PlayerStateMachine.PlayerStateMachine>();
                    }
                    if (player == null) {
                        player = other.GetComponentInChildren<PlayerStateMachine.PlayerStateMachine>();
                    }
                    
                    if (player != null) {
                        // Get PlayerHealth component and deal damage
                        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                        if (playerHealth == null) {
                            playerHealth = player.GetComponentInParent<PlayerHealth>();
                        }
                        if (playerHealth == null) {
                            playerHealth = player.GetComponentInChildren<PlayerHealth>();
                        }
                        
                        if (playerHealth != null) {
                            playerHealth.TakeDamage(damage);
                            Debug.Log($"Boss fireball hit player {other.name} for {damage} damage!");
                        } else {
                            Debug.LogWarning($"Boss fireball hit player {other.name} but no PlayerHealth component found!");
                        }
                        shouldDespawn = true;
                    }
                }
            } else {
                // Player cast this fireball - damage bosses
                Boss boss = other.GetComponent<Boss>();
                if (boss == null) {
                    boss = other.GetComponentInParent<Boss>();
                }
                if (boss == null) {
                    boss = other.GetComponentInChildren<Boss>();
                }
                
                if (boss != null) {
                    Debug.Log($"Player fireball hit boss {other.name}! Dealing {damage} damage.");
                    // Deal damage to the boss
                    boss.TakeDamage(damage);
                    Debug.Log($"Fireball hit boss {other.name} for {damage} damage!");
                    shouldDespawn = true;
                }
            }
            
            // Despawn the fireball after collision
            if (shouldDespawn) {
                Debug.Log($"Fireball collided with {other.name}, despawning");
                if (Runner != null && Object != null) {
                    Runner.Despawn(Object);
                }
            }
        }
    }
}

