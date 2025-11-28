using System.Collections.Generic;
using System.Linq;
using Fusion;
using Structs;
using UnityEngine;

namespace Spells {
    public class PlayerSpellManager : NetworkBehaviour {
        [SerializeField] private List<ISpell> _availableSpells = new List<ISpell>(); // All spells the player *could* have
        [Networked, Capacity(5)] // Example: player can have 5 spells equipped
        public NetworkArray<NetworkPrefabRef> EquippedSpellPrefabs => default; // Store references to spell prefabs

        [SerializeField] private NetworkPrefabRef fireballPrefab; // Fireball prefab for skin customization
        [SerializeField] private float fireballCooldown = 5f; // Cooldown in seconds
        [Networked] private float LastFireballCastTime { get; set; }
        private bool _hasLoggedCooldownReady = false;

        private PlayerStateMachine.PlayerStateMachine _playerStateMachine;

        public override void Spawned() {
            _playerStateMachine = GetComponent<PlayerStateMachine.PlayerStateMachine>();
            
            // Initialize spells for all instances (both client and server need access)
            // This ensures the server can spawn spells when clients request them via RPC
            if (_availableSpells.Count == 0) {
                // Create fireball spell with the specified prefab for skin customization
                NetworkPrefabRef prefabToUse = fireballPrefab.IsValid 
                    ? fireballPrefab 
                    : (SpellReferences.Instance != null ? SpellReferences.Instance.Fireball : default);
                
                ISpell fireballSpell = new Fireball(prefabToUse); 
                _availableSpells.Add(fireballSpell);
            }
            
            // TODO: You need to assign the actual NetworkPrefabRef for your fireball prefab
            // This should be done through the inspector or by getting it from a prefab registry
            // EquippedSpellPrefabs.Set(0, yourFireballPrefabRef);
        }

        public override void FixedUpdateNetwork() {
            // Check cooldown status and log when it becomes available (on both client and server)
            float timeSinceLastCast = Runner.SimulationTime - LastFireballCastTime;
            bool isOnCooldown = timeSinceLastCast < fireballCooldown;
            
            if (Object.HasInputAuthority) {
                if (isOnCooldown) {
                    _hasLoggedCooldownReady = false;
                } else if (!_hasLoggedCooldownReady && LastFireballCastTime > 0) {
                    // Cooldown just became available
                    Debug.Log("Fireball spell is ready to cast!");
                    _hasLoggedCooldownReady = true;
                }
            }
            
            if (!GetInput(out NetInputData data)) return;
            // Check if the cast button was pressed
            if (data.CastSlot1) {
                // Cast the fireball spell (slot 0)
                CastSpell(0);
            }
        }

        public void CastSpell(int spellSlotIndex) {
            // Only the input authority can initiate casting
            if (!Object.HasInputAuthority) return;

            // Check cooldown
            float timeSinceLastCast = Runner.SimulationTime - LastFireballCastTime;
            if (timeSinceLastCast < fireballCooldown) {
                float remainingCooldown = fireballCooldown - timeSinceLastCast;
                Debug.Log($"Fireball is on cooldown. {remainingCooldown:F1} seconds remaining.");
                return;
            }

            // For now, let's simplify this and just cast the first available spell
            // In a more robust system, you'd have proper spell slot management
            if (_availableSpells.Count == 0) {
                Debug.LogWarning("No spells available!");
                return;
            }

            // Get the first spell (fireball) for now
            ISpell spellToCast = _availableSpells[0];

            if (spellToCast == null) {
                Debug.LogWarning($"No spell found for slot {spellSlotIndex}");
                return;
            }

            // Calculate spawn position and direction
            Vector3 spawnPosition = transform.position + transform.forward * 1.5f + transform.up * 1.0f;
            Vector3 forwardDirection = transform.forward;

            // Request server to spawn the spell via RPC
            // This ensures only the server spawns network objects
            // The server will set the cooldown when the spell is actually cast
            RPC_RequestCastSpell(spellSlotIndex, spawnPosition, forwardDirection);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestCastSpell(int spellSlotIndex, Vector3 spawnPosition, Vector3 forwardDirection) {
            // This RPC is executed on the server/host
            // Only the server can spawn network objects, so we do it here
            
            // Check cooldown on server side as well (in case client check was bypassed)
            float timeSinceLastCast = Runner.SimulationTime - LastFireballCastTime;
            if (timeSinceLastCast < fireballCooldown) {
                float remainingCooldown = fireballCooldown - timeSinceLastCast;
                Debug.Log($"Server: Fireball is on cooldown. {remainingCooldown:F1} seconds remaining.");
                return;
            }
            
            if (_availableSpells.Count == 0) {
                Debug.LogWarning("No spells available on server!");
                return;
            }

            ISpell spellToCast = _availableSpells[0];
            if (spellToCast == null) {
                Debug.LogWarning($"No spell found for slot {spellSlotIndex} on server");
                return;
            }

            // Set cooldown time on server (this will sync to all clients)
            LastFireballCastTime = Runner.SimulationTime;

            // Server spawns the spell
            spellToCast.Cast(Runner, Object, spawnPosition, forwardDirection);
        }
    }
}