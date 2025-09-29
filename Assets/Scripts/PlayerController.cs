using Fusion;
using Structs;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour {
    NetworkCharacterController _ncc;
    CharacterController _cc; 
    NetworkButtons _lastBtns;

    [SerializeField] float walkSpeed = 6f;
    [SerializeField] float sprintSpeed = 10f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float rotationSpeed = 15f;
    
    [Networked] Vector3 Velocity { get; set; }
    
    public Vector3 GetVelocity() => Velocity;

    public override void Spawned() {
        _ncc = GetComponent<NetworkCharacterController>();
        _cc = GetComponent<CharacterController>();
        _ncc.Velocity = Vector3.zero;
        Velocity = Vector3.zero;
        Debug.Log($"[CC] Spawned | StateAuth={Object.HasStateAuthority} | InputAuth={HasInputAuthority} | IsServer={Runner.IsServer}");
    }

    /*
    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetInputData input)) return;
        
        var speed = input.Buttons.IsSet(NetInputData.BUTTON_SPRINT) ? sprintSpeed : walkSpeed;
        var inputDirection = new Vector3(input.Move.x, 0, input.Move.y);
        if (inputDirection.sqrMagnitude > 1f) inputDirection.Normalize();
        
        var velocity = Velocity;
        
        if (inputDirection.magnitude > 0.1f) {
            velocity.x = inputDirection.x * speed;
            velocity.z = inputDirection.z * speed;
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                Quaternion.LookRotation(inputDirection), 
                rotationSpeed * Runner.DeltaTime
            );
        } else {
            velocity.x = 0f;
            velocity.z = 0f;
        }
        
        if (_cc.isGrounded && velocity.y < 0) {
            velocity.y = 0f;
        }
        velocity.y += gravity * Runner.DeltaTime;
        
        if (input.Buttons.WasPressed(_lastBtns, NetInputData.BUTTON_JUMP) && _cc.isGrounded) {
            velocity.y = _ncc.jumpImpulse;
        }
        
        _cc.Move(velocity * Runner.DeltaTime);
        
        Velocity = velocity;
        
        _lastBtns = input.Buttons;
    }
    */
}
