using Fusion;
using UnityEngine;

public class PlayerStateManager : NetworkBehaviour {
    /*
    static readonly int Speed = Animator.StringToHash("Speed");
    [SerializeField] Animator animator;
    [SerializeField] NetworkCharacterController ncc;
    [SerializeField] PlayerController playerController;
    [SerializeField] float animDampTime = 0.1f;
    
    [Networked] float PlanarSpeed { get; set; }
    


    public override void Spawned() {
        ncc = GetComponent<NetworkCharacterController>();
        playerController = GetComponent<PlayerController>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator) animator.applyRootMotion = false;
    }
    
    public override void FixedUpdateNetwork() {
        if (!playerController) return;
        if (!Object.HasStateAuthority) return;
        
        Vector3 v = playerController.GetVelocity(); v.y = 0;
        PlanarSpeed = v.magnitude;
    }

    public override void Render() {
        if (!animator) return;
        
        animator.SetFloat(Speed, PlanarSpeed , animDampTime, Runner.DeltaTime);
        
        Debug.Log(" speed:" + PlanarSpeed);
    }
    */
    
}