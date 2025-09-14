using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Structs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    
    private NetworkRunner _runner;
    
    [SerializeField] private NetworkPrefabRef playerPrefab;
    
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private LocalInputReader _localInput; // referencia al lector del jugador local
    
    [SerializeField] private Transform spawnPoint;  // Asignás un Empty en la escena

    async void StartGame(GameMode mode)
    {
        Debug.Log($"[Spawner] StartGame(mode={mode})");
        
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

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
    
    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0,0,200,40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0,40,200,40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        // Usar posición y rotación del spawnPoint fijo
        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;

        NetworkObject networkPlayerObject = runner.Spawn(
            playerPrefab, spawnPosition, spawnRotation, player);

        _spawnedCharacters.Add(player, networkPlayerObject);

        Debug.Log($"[Spawner] Spawn player object={networkPlayerObject?.name} | for player={player}");

        if (player == runner.LocalPlayer)
        {
            _localInput = networkPlayerObject.GetComponentInChildren<LocalInputReader>(true);
            Debug.Log($"[Spawner] LocalInputReader asignado? {_localInput != null}");
        }
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Si no hay lector local, loguéalo igual para detectar el problema
        if (_localInput == null)
        {
            // Evitamos spam logeando cada ~10 ticks
            if (runner.Tick % 10 == 0)
                Debug.Log($"[Spawner] OnInput (Tick={runner.Tick}) → _localInput == NULL. " +
                          $"¿Asignaste LocalInputReader en el prefab y soy LocalPlayer?");
            return;
        }

        var data = new NetInputData
        {
            Move = _localInput.Move
        };

        if (_localInput.SprintHeld)
            data.Buttons.Set(NetInputData.BUTTON_SPRINT, true);

        if (_localInput.JumpPressedThisFrame)
            data.Buttons.Set(NetInputData.BUTTON_JUMP, true);

        input.Set(data);

        // Logs con throttle para no spamear
        if (runner.Tick % 10 == 0 || data.Move.sqrMagnitude > 0.0001f)
        {
            Debug.Log($"[Spawner] OnInput (Tick={runner.Tick}) " +
                      $"Move=({data.Move.x:0.00},{data.Move.y:0.00}) " +
                      $"Sprint={data.Buttons.IsSet(NetInputData.BUTTON_SPRINT)} ");
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}
