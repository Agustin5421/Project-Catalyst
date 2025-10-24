using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Structs;
using UnityEngine;

/// <summary>
/// Read local input (new Input System) and deliver it to the Runner (Fusion 2)
/// Must exist in the CLIENT's scene. It does not depend on the Player.
/// </summary>
public class InputProvider : MonoBehaviour, INetworkRunnerCallbacks {
    [SerializeField] NetworkRunner _runner; 
    
    InputSystem_Actions _actions;

    Vector2 _move;
    bool _jump;
    bool _sprint;
    bool _castSlot1;

    void OnEnable() {
        _actions = new InputSystem_Actions();
        _actions.Enable();

        _actions.Player.Move.performed  += ctx => _move = ctx.ReadValue<Vector2>();
        _actions.Player.Move.canceled   += ctx => _move = Vector2.zero;
        _actions.Player.Jump.performed  += _ => _jump = true;
        _actions.Player.Jump.canceled   += _ => _jump = false;
        _actions.Player.Sprint.performed+= _ => _sprint = true;
        _actions.Player.Sprint.canceled += _ => _sprint = false;
        _actions.Player.CastSlot1.performed += _ => _castSlot1 = true;
        _actions.Player.CastSlot1.canceled += _ => _castSlot1 = false;

        if (!_runner) _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner) _runner.AddCallbacks(this);
    }

    void OnDisable() {
        if (_runner != null) _runner.RemoveCallbacks(this);
        if (_actions != null) _actions.Disable();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        var data = new NetInputData {
            Move   = _move,
            Jump   = _jump,
            Sprint = _sprint,
            CastSlot1 = _castSlot1,
            SpellIndex = 0 // Fireball is in slot 0
        };
        input.Set(data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {
        Debug.Log("Not implemented yet: OnObjectExitAOI");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {
        Debug.Log("Not implemented yet: OnObjectEnterAOI");
    }

    public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
    public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason reason) { }
    
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
        Debug.Log("Not implemented yet: OnDisconnectedFromServer");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
        Debug.Log("Not implemented yet: OnConnectRequest");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
        Debug.Log("Not implemented yet: OnConnectFailed ");
    }

    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {
        Debug.Log("Not implemented yet: OnSessionListUpdated ");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {
        Debug.Log("Not implemented yet: OnCustomAuthenticationResponse ");
    }

    public void OnDisconnectedFromServer(NetworkRunner r) { }
    public void OnConnectedToServer(NetworkRunner r, NetAddress address) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
}