using Fusion;
using UnityEngine;

public class DragonPet : NetworkBehaviour {
    [Header("Flight Settings")]
    [SerializeField] private float flightHeight = 5f; // Height above owner
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float orbitDistance = 2f; // Distance from owner/target horizontally
    [SerializeField] private float positionSmoothing = 10f;

    [Header("Combat Settings")]
    [SerializeField] private float aggroRange = 20f;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackInterval = 4f;
    [SerializeField] private float attackDiveSpeed = 15f;
    [SerializeField] private float attackReturnSpeed = 10f;

    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private float LastAttackTime { get; set; }
    [Networked] private DragonState State { get; set; }
    [Networked] private Vector3 DiveStartPosition { get; set; } // Where we started the dive from

    private enum DragonState {
        Following,
        Diving,
        Returning
    }

    private NetworkObject _targetBoss;

    public void Init(NetworkObject owner) {
        Owner = owner;
        // Snap to start position to avoid long travel time
        transform.position = owner.transform.position + Vector3.up * flightHeight;
    }

    public override void Spawned() {
        if (Object.HasStateAuthority) {
            State = DragonState.Following;
        }
    }

    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority) return;

        // Find Boss if needed
        if (_targetBoss == null) {
            var boss = FindFirstObjectByType<Boss>();
            if (boss != null) _targetBoss = boss.Object;
        }

        switch (State) {
            case DragonState.Following:
                HandleFollowing();
                break;
            case DragonState.Diving:
                HandleDiving();
                break;
            case DragonState.Returning:
                HandleReturning();
                break;
        }
    }

    private void HandleFollowing() {
        if (Owner == null) return;

        // 1. Determine base hover position (Owner position + Height)
        Vector3 baseTargetPos = Owner.transform.position + Vector3.up * flightHeight;
        
        // 2. Add circling/wandering movement
        // Use Time.time to drive the circling animation
        float time = Runner.SimulationTime;
        float circleRadius = orbitDistance;
        
        // Use Sin/Cos for circular motion on X/Z plane
        float offsetX = Mathf.Sin(time * 0.5f) * circleRadius; // Slow circle
        float offsetZ = Mathf.Cos(time * 0.5f) * circleRadius;
        
        // Add perlin noise for height variation to look more "alive"
        float heavyBreathing = Mathf.Sin(time * 2f) * 0.5f; 
        
        Vector3 hoverPosition = baseTargetPos + new Vector3(offsetX, heavyBreathing, offsetZ);

        // Check aggression
        if (_targetBoss != null) {
            float distToBoss = Vector3.Distance(transform.position, _targetBoss.transform.position);
            
            if (distToBoss <= aggroRange) {
                 // Check if time to attack
                 if (Runner.SimulationTime - LastAttackTime >= attackInterval) {
                   StartAttack(); 
                   return;
                }
            }
        }

        // 3. Move towards calculated hover position
        // Smoothly interpolate for natural flying
        transform.position = Vector3.Lerp(transform.position, hoverPosition, Runner.DeltaTime * positionSmoothing / 2f); // Slightly softer smoothing for wandering
        
        // Face movement direction
        Vector3 moveDir = (hoverPosition - transform.position);
        
        // If moving significantly, look ahead
        // If hovering roughly in place, look at boss or owner
        if (moveDir.sqrMagnitude > 0.1f) {
             Quaternion targetRot = Quaternion.LookRotation(moveDir);
             transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Runner.DeltaTime * 2f);
        } else if (_targetBoss != null) {
            // If idle, look at boss to seem "aware"
            Vector3 lookDir = _targetBoss.transform.position - transform.position;
            if (lookDir != Vector3.zero) {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Runner.DeltaTime * 2f);
            }
        }
    }

    private void StartAttack() {
        State = DragonState.Diving;
        DiveStartPosition = transform.position;
        LastAttackTime = Runner.SimulationTime;
        Debug.Log("Dragon started dive attack!");
    }

    private void HandleDiving() {
        if (_targetBoss == null) {
            State = DragonState.Returning;
            return;
        }

        Vector3 targetPos = _targetBoss.transform.position;
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Move fast towards boss
        transform.position += direction * attackDiveSpeed * Runner.DeltaTime;
        transform.LookAt(targetPos);

        // Check if hit
        if (Vector3.Distance(transform.position, targetPos) < 1.5f) { // Impact radius
            // Deal damage
            var bossScript = _targetBoss.GetComponent<Boss>();
            if (bossScript != null) {
                bossScript.TakeDamage(attackDamage);
            }
            
            // Switch to return
            State = DragonState.Returning;
            Debug.Log("Dragon hit boss!");
        }
    }

    private void HandleReturning() {
        if (Owner == null) {
             // Just fly up if no owner
             transform.position += Vector3.up * attackReturnSpeed * Runner.DeltaTime;
             if (transform.position.y > 10f) State = DragonState.Following; // Reset eventually
             return;
        }

        // Return to hover position above Owner
        Vector3 returnTarget = Owner.transform.position + Vector3.up * flightHeight;
        Vector3 direction = (returnTarget - transform.position).normalized;

        transform.position += direction * attackReturnSpeed * Runner.DeltaTime;
        
        // Look up/towards return point
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Runner.DeltaTime * 10f);

        // Check availability to switch back to normal following
        if (Vector3.Distance(transform.position, returnTarget) < 2f) {
            State = DragonState.Following;
        }
    }
}
