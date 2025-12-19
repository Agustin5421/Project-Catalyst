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
            
            if (Object.HasStateAuthority) {
                SpawnTime = Runner.SimulationTime;
                // Initialize direction from transform.forward (set by Quaternion.LookRotation during spawn)
                Direction = transform.forward.normalized;
                IsInitialized = true;
                
            } else {
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
                
                // Apply scale immediately if specified
                if (customScale > 0f) {
                    transform.localScale = Vector3.one * customScale;
                }
                
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

            
            // Only the server/host (state authority) should move and manage the fireball
            if (!Object.HasStateAuthority) {
                return;
            }
            
            if (!IsInitialized) {
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

            
            // Destroy after lifetime expires
            float elapsed = Runner.SimulationTime - SpawnTime;
            if (elapsed >= lifetime) {
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
                        _hasHitSomething = true; // Mark as hit to prevent multiple hits
                        boss.TakeDamage(damage);
                        
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
            
            // Safety checks
            if (Runner == null || Object == null) {
                return;
            }
            
            if (!Object.HasStateAuthority) {
                return;
            }
            
            // Don't collide with the caster
            if (other == null || other.transform == null) {
                return;
            }
            
            if (other.transform.root == transform.root) {
                return;
            }
            
            
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
                    // Deal damage to the boss
                    boss.TakeDamage(damage);
                    shouldDespawn = true;
                }
            }
            
            // Despawn the fireball after collision
            if (shouldDespawn) {
                if (Runner != null && Object != null) {
                    Runner.Despawn(Object);
                }
            }
        }
        
        private void OnCollisionEnter(Collision collision) {
            // Also handle regular collisions (non-trigger)
            if (collision != null && collision.collider != null) {
                HandleCollision(collision.collider);
            }
        }
        
        private void HandleCollision(Collider other) {
            // Safety checks
            if (Runner == null || Object == null) {
                return;
            }
            
            if (!Object.HasStateAuthority) {
                return;
            }
            
            // Don't collide with the caster
            if (other == null || other.transform == null) {
                return;
            }
            
            if (other.transform.root == transform.root) {
                return;
            }
            
            
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
                    // Deal damage to the boss
                    boss.TakeDamage(damage);
                    shouldDespawn = true;
                }
            }
            
            // Despawn the fireball after collision
            if (shouldDespawn) {
                if (Runner != null && Object != null) {
                    Runner.Despawn(Object);
                }
            }
        }
    }
}

