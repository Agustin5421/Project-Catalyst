using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Structs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    
    // NetworkRunner: core object that manages networking, simulation, and callbacks.
    private NetworkRunner _runner;
    
    [SerializeField] private NetworkPrefabRef playerPrefab; // player prefab to spawn
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    
    private LocalInputReader _localInput;  
    
    [SerializeField] private Transform spawnPoint; 

    // Creates and starts a new Fusion session (Host or Client).
    async void StartGame(GameMode mode) {
        try {
            Debug.Log($"[Spawner] StartGame(mode={mode})");
        
            // Create the Fusion runner and let it know that we will be providing user input
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            
            // Note: Fusion supports multiple INetworkRunnerCallbacks registrations.
            // If several objects implement the same callback (e.g., OnInput),
            // the Runner will invoke ALL of them. This lets you split responsibilities
            // across different scripts (Spawner, PlayerInput, UI, etc.) cleanly.
            _runner.AddCallbacks(this);

            // Create the NetworkSceneInfo from the current scene
            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid) {
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
            }

            // Start or join (depends on gamemode) a session with a specific name
            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });
        }
        catch (Exception e) {
            Debug.LogError($"[Spawner] StartGame EXCEPTION: {e}");
        }
    }
    
    private void OnGUI() {
        if (_runner) return;
        if (GUI.Button(new Rect(0,0,200,40), "Host")) {
            StartGame(GameMode.Host);
        }
        if (GUI.Button(new Rect(0,40,200,40), "Join")) {
            StartGame(GameMode.Client);
        }
    }
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (!runner.IsServer) return;

        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;

        NetworkObject networkPlayerObject =
            runner.Spawn(playerPrefab, spawnPosition, spawnRotation, player);

        runner.SetPlayerObject(player, networkPlayerObject);

        _spawnedCharacters.Add(player, networkPlayerObject);

        Debug.Log($"[Spawner] Spawn player object={networkPlayerObject?.name} | for player={player}");
    }



    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (!_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            return;
        runner.Despawn(networkObject);
        _spawnedCharacters.Remove(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {
        Debug.Log($"[Spawner] Shutdown reason={shutdownReason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
        Debug.Log("[Spawner] OnDisconnectedFromServer reason=" + reason); 
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
        Debug.Log("[Spawner] OnConnectRequest from " + request.RemoteAddress);
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
        Debug.Log("[Spawner] OnConnectFailed for player " + remoteAddress + " reason=" + reason);
        
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        if (_localInput is null) {
            if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObj)) {
                _localInput = playerObj.GetComponentInChildren<LocalInputReader>(true);
                Debug.Log($"[Spawner] LocalInputReader asignado en OnInput? {_localInput}");
            }
        }

        if (_localInput is null) return;

        var data = new NetInputData {
            Move   = _localInput.Move,
            Jump   = _localInput.JumpPressedThisFrame,
            Sprint = _localInput.SprintHeld
        };
        
        input.Set(data);
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }
}
