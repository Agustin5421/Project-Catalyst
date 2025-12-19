using Fusion;
using UnityEngine;

public class Golem : NetworkBehaviour {
    [SerializeField] private float maxHealth = 40f;
    [SerializeField] private float speed = 6f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackRate = 1.5f;
    [SerializeField] private float aggroRange = 15f;
    
    [Networked] private float CurrentHealth { get; set; }
    [Networked] private float LastAttackTime { get; set; }
    
    // We need to sync the owner so the golem knows who to follow
    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private int RoamSeed { get; set; } // Random seed for unique movement
    private NetworkObject _targetBoss;
    
    public void Init(NetworkObject owner) {
        Owner = owner;
        CurrentHealth = maxHealth;
    }
    
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            CurrentHealth = maxHealth;
            // Initialize seed based on network ID so it's consistent but unique per object
            RoamSeed = Object.Id.GetHashCode(); 
        }
    }
    
    public void TakeDamage(float damage) {
        if (!Object.HasStateAuthority) return;
        
        CurrentHealth -= damage;
        if (CurrentHealth <= 0) {
            Runner.Despawn(Object);
        }
    }
    
    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority) return;
        
        // Find Boss if we don't have one (simple polling)
        if (_targetBoss == null) {
            // Optimization: In a real game, use a global manager. Here FindObjects is ok if infrequent.
            // But doing it every tick is bad. We'll rely on the boss being there.
            var boss = FindFirstObjectByType<Boss>();
            if (boss != null) _targetBoss = boss.Object;
        }
        
        Vector3 targetPos = transform.position;
        bool isAttacking = false;
        
        // Check Aggro
        if (_targetBoss != null) {
            float distanceToBoss = Vector3.Distance(transform.position, _targetBoss.transform.position);
            
            if (distanceToBoss <= aggroRange) {
                // Move to boss
                targetPos = _targetBoss.transform.position;
                
                // Attack if close enough
                if (distanceToBoss <= attackRange) {
                    if (Runner.SimulationTime - LastAttackTime >= attackRate) {
                        AttackBoss();
                    }
                    isAttacking = true; 
                }
            } else {
                // Return to roaming around owner
                 if (Owner != null) {
                    targetPos = GetRoamingPosition(Owner.transform.position);
                 }
            }
        } else {
             // Return to roaming around owner
             if (Owner != null) {
                targetPos = GetRoamingPosition(Owner.transform.position);
             }
        }
        
        // Movement Logic
        if (!isAttacking && targetPos != transform.position) {
             Vector3 direction = (targetPos - transform.position);
             direction.y = 0; // Keep on ground
             
             // Move only if target is far enough (prevent jitter)
             if (direction.magnitude > 0.5f) {
                 transform.position += direction.normalized * speed * Runner.DeltaTime;
                 
                 // Re-enable rotation for roaming so they look where they are going
                 if (direction != Vector3.zero) {
                    // Force zero Y direction to prevent tilting up/down
                    Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
                    if (flatDirection != Vector3.zero) {
                        // Calculate facing direction (Y axis)
                        Quaternion faceRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                        
                        // Combine FIXED -80 degree X tilt with the dynamic Y rotation
                        Quaternion finalTarget = Quaternion.Euler(-80f, faceRotation.eulerAngles.y, 0f);
                        
                        transform.rotation = Quaternion.Slerp(transform.rotation, finalTarget, Runner.DeltaTime * 5f);
                    }
                 }
             }
        }
    }
    
    private void AttackBoss() {
        LastAttackTime = Runner.SimulationTime;
        if (_targetBoss != null) {
            var bossScript = _targetBoss.GetComponent<Boss>();
            if (bossScript != null) {
                bossScript.TakeDamage(attackDamage);
            }
        }
    }

    private Vector3 GetRoamingPosition(Vector3 center) {
        // Create unique movement pattern for each golem
        float time = Runner.SimulationTime * 0.5f; // Global time
        
        // Use Seed to offset phase
        float phase = RoamSeed % 100f; 
        
        // Combine Sin/Cos with Perlin noise for "organic" wandering
        // We want them to generally circle loosely but drift
        
        float baseRadius = 5f;
        float radiusVar = Mathf.Sin(time * 0.3f + phase) * 2f; // Radius breathes between 3 and 7
        float finalRadius = baseRadius + radiusVar;
        
        float angleSpeed = 0.5f + ((RoamSeed % 10) / 20f); // Different speeds
        float angle = time * angleSpeed + phase;
        
        float offsetX = Mathf.Sin(angle) * finalRadius;
        float offsetZ = Mathf.Cos(angle) * finalRadius;
        
        return center + new Vector3(offsetX, 0, offsetZ);
    }
}
