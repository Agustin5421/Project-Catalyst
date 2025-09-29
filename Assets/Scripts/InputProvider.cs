using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Structs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerStateMachine
{
    /// <summary>
    /// Lee el Input local (nuevo Input System) y lo entrega al Runner (Fusion 2)
    /// Debe existir en la escena del CLIENTE. No depende del Player.
    /// </summary>
    public class InputProvider : MonoBehaviour, INetworkRunnerCallbacks {
        [SerializeField] NetworkRunner _runner; // opcional: si no lo asignas, lo buscamos

        // Si usas acciones generadas por el Input System (InputSystem_Actions)
        private InputSystem_Actions _actions;

        // cache local de entradas
        private Vector2 _move;
        private bool _jump;
        private bool _sprint;

        void OnEnable() {
            // preparar acciones del nuevo Input System
            _actions = new InputSystem_Actions();
            _actions.Enable();

            // leer con callbacks o en Update. Aquí, callbacks:
            _actions.Player.Move.performed  += ctx => _move = ctx.ReadValue<Vector2>();
            _actions.Player.Move.canceled   += ctx => _move = Vector2.zero;
            _actions.Player.Jump.performed  += _ => _jump = true;
            _actions.Player.Jump.canceled   += _ => _jump = false;
            _actions.Player.Sprint.performed+= _ => _sprint = true;
            _actions.Player.Sprint.canceled += _ => _sprint = false;

            // registrar callbacks en el Runner
            if (_runner == null) _runner = FindObjectOfType<NetworkRunner>();
            if (_runner != null) _runner.AddCallbacks(this);
        }

        void OnDisable()
        {
            if (_runner != null) _runner.RemoveCallbacks(this);
            if (_actions != null) _actions.Disable();
        }

        // -------- INetworkRunnerCallbacks --------

        // ¡Este es el único que nos importa para input!
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Solo proveemos input si este cliente tiene autoridad de entrada sobre ALGO.
            // (Fusion igual lo invoca por tick; enviar el paquete es barato)
            var data = new NetInputData
            {
                Move   = _move,
                Jump   = _jump,
                Sprint = _sprint
            };
            input.Set(data);
        }

        // Los demás callbacks pueden quedar vacíos si no los usas
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {
            throw new System.NotImplementedException();
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {
            throw new System.NotImplementedException();
        }

        public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
        public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
        public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
        public void OnShutdown(NetworkRunner r, ShutdownReason reason) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {
            throw new System.NotImplementedException();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {
            throw new System.NotImplementedException();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {
            throw new System.NotImplementedException();
        }

        public void OnConnectedToServer(NetworkRunner r) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {
            throw new System.NotImplementedException();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {
            throw new System.NotImplementedException();
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
}
