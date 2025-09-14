using Fusion;
using Structs;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour {
    private NetworkCharacterController _ncc;
    private NetworkButtons _lastBtns;

    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;

    public override void Spawned() {
        _ncc = GetComponent<NetworkCharacterController>();
        _ncc.Velocity = Vector3.zero; 
        Debug.Log($"[CC] Spawned | StateAuth={Object.HasStateAuthority} | InputAuth={HasInputAuthority} | IsServer={Runner.IsServer}");
    }

    public override void FixedUpdateNetwork() {
        if (!GetInput(out NetInputData input))
            return;
        var speed = input.Buttons.IsSet(NetInputData.BUTTON_SPRINT) ? sprintSpeed : walkSpeed;
        var dir = new Vector3(input.Move.x, 0, input.Move.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        dir *= speed;

        _ncc.Move(dir * Runner.DeltaTime);

        if (input.Buttons.WasPressed(_lastBtns, NetInputData.BUTTON_JUMP)) {
            _ncc.Jump(); 
        }

        _lastBtns = input.Buttons;
    }
}