using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    
    // NetworkRunner: core object that manages networking, simulation, and callbacks.
    private NetworkRunner _runner;
    
    [SerializeField] private NetworkPrefabRef playerPrefab; // player prefab to spawn
    [SerializeField] private NetworkPrefabRef bossPrefab; // boss prefab to spawn
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkObject _spawnedBoss; // Track the spawned boss
    
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform bossSpawnPoint; // Boss spawn position (if null, will use Vector3.zero) 
    
    [SerializeField] private float bossRespawnTime = 10f; // Time to respawn the boss after death
    private bool _bossRespawning;
    private bool _sceneLoaded; 

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
    
    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    /*
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
    */


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    private void Update() {
        if (!_sceneLoaded || _runner == null || !_runner.IsServer || !bossPrefab.IsValid) return;
        
        // Check if boss is dead (null) and not respawning
        // Note: When NetworkObject is despawned, the C# wrapper becomes null (Unity object lifecycle)
        if (_spawnedBoss == null && !_bossRespawning) {
            StartCoroutine(RespawnBoss());
        }
    }

    private System.Collections.IEnumerator RespawnBoss() {
        _bossRespawning = true;
        Debug.Log($"[Spawner] Boss died. Respawning in {bossRespawnTime} seconds...");
        
        yield return new WaitForSeconds(bossRespawnTime);
        
        // One final check to make sure game is still running
        if (_runner != null && _runner.IsRunning) {
            Vector3 bossPosition = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Quaternion bossRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
                
            _spawnedBoss = _runner.Spawn(bossPrefab, bossPosition, bossRotation, null);
            Debug.Log($"[Spawner] Boss respawned at position: {bossPosition}");
        }
        
        _bossRespawning = false;
    }
    
    public void OnSceneLoadDone(NetworkRunner runner) {
        // Spawn the boss when the scene is loaded (only on server)
        if (runner.IsServer && bossPrefab.IsValid) {
            Vector3 bossPosition = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Quaternion bossRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
            
            _spawnedBoss = runner.Spawn(bossPrefab, bossPosition, bossRotation, null);
            Debug.Log($"[Spawner] Boss spawned at position: {bossPosition}");
        }
        
        _sceneLoaded = true;
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }
}
