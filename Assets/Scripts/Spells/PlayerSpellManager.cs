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
        
        [Header("Golem Ability")]
        [SerializeField] private float golemCooldown = 0f;
        [SerializeField] private float golemManaCost = 50f;
        [SerializeField] private float golemSpawnHeightOffset = 2f;
        [Networked] private float LastGolemCastTime { get; set; }
        private List<NetworkObject> _activeGolems = new List<NetworkObject>();
        
        [Header("Dragon Pet (Passive)")]
        private NetworkObject _activeDragonPet;
        
        private bool _hasLoggedCooldownReady = false;

        private PlayerStateMachine.PlayerStateMachine _playerStateMachine;
        private PlayerMana _playerMana;

        public override void Spawned() {
            _playerStateMachine = GetComponent<PlayerStateMachine.PlayerStateMachine>();
            _playerMana = GetComponent<PlayerMana>();
            
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
            // EquippedSpellPrefabs.Set(0, yourFireballPrefabRef);
            
            // Spawn Dragon Pet (Server Only) automatically on recruit
            if (Object.HasStateAuthority && _activeDragonPet == null) {
               SpawnDragonPet();
            }
        }
        
        private void SpawnDragonPet() {
             NetworkPrefabRef petPrefab = SpellReferences.Instance.DragonPet;
             if (!petPrefab.IsValid) {
                 Debug.LogWarning("DragonPet prefab not valid in SpellReferences!");
                 return;
             }
             
             // Spawn above player
             Vector3 spawnPos = transform.position + Vector3.up * 5f;
             _activeDragonPet = Runner.Spawn(petPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
             
             var petScript = _activeDragonPet.GetComponent<DragonPet>();
             if (petScript != null) {
                 petScript.Init(Object);
             }
             Debug.Log("Dragon Pet Summoned along with Player!");
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
            
            if (data.CastSlot2) {
                Debug.Log("DEBUG: PlayerSpellManager detected CastSlot2 input!");
                CastGolems();
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

            // Check Mana (Client Prediction)
            if (_playerMana != null && !_playerMana.TryConsumeMana(20f)) {
                 Debug.Log("Not enough mana for Fireball!");
                 return;
            } else if (_playerMana == null) {
                 Debug.LogWarning("PlayerMana component missing!");
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
            Vector3 forwardDirection = transform.forward;
            
            // Use camera forward direction if available (aim where the camera is looking)
            var cameraBinder = GetComponent<ThirdPersonCameraBinder>();
            if (cameraBinder != null) {
                forwardDirection = cameraBinder.CameraForward;
            } else if (Camera.main != null) {
                forwardDirection = Camera.main.transform.forward;
            }
            
            Vector3 spawnPosition = transform.position + forwardDirection * 1.5f + transform.up * 1.0f;

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
            
            // Check Mana (Server Authority)
            // Note: Since we predicted on client, we should theoretically be good, but server is authority.
            // However, since we reduced it on client (prediction), the server needs to reduce it too?
            // Wait, TryConsumeMana only reduces if HasStateAuthority.
            // On Client (InputAuthority), TryConsumeMana returns true check but doesn't reduce networked var.
            // On Server (StateAuthority), TryConsumeMana reduces the actual var.
            // So we call it on both.
            
            if (_playerMana != null && !_playerMana.TryConsumeMana(20f)) {
                 Debug.Log("Server: Not enough mana for Fireball!");
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
        
        public void CastGolems() {
            if (!Object.HasInputAuthority) return;
            
            float timeSinceLastCast = Runner.SimulationTime - LastGolemCastTime;
            if (timeSinceLastCast < golemCooldown) {
                // Log cooldown to see if that's the blocker
                Debug.Log($"DEBUG: CastGolems blocked by cooldown. Remaining: {golemCooldown - timeSinceLastCast}");
                return; 
            }
            
            // Check Mana (Client Prediction)
            if (_playerMana != null && !_playerMana.TryConsumeMana(golemManaCost)) {
                 Debug.Log("Not enough mana for Golems!");
                 return;
            }
            
             Debug.Log("DEBUG: CastGolems Requesting RPC...");
             // Request server to spawn golems
             RPC_RequestCastGolems();
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestCastGolems() {
             Debug.Log("DEBUG: RPC_RequestCastGolems received on Server!");
             float timeSinceLastCast = Runner.SimulationTime - LastGolemCastTime;
             if (timeSinceLastCast < golemCooldown) {
                 Debug.LogWarning("DEBUG: Server blocked CastGolems due to cooldown.");
                 return;
             }
             
             // Check Mana (Server Authority)
             if (_playerMana != null && !_playerMana.TryConsumeMana(golemManaCost)) {
                  Debug.Log("Server: Not enough mana for Golems!");
                  return;
             }
             
             LastGolemCastTime = Runner.SimulationTime;
             
             // Clear existing golems (limit 3 active implies batch replacement or cap)
             // User said "player can summon up to 3... all 3 are summoned at the same time"
             // Using logic: Despawn old ones
             for (int i = _activeGolems.Count - 1; i >= 0; i--) {
                 if (_activeGolems[i] != null && _activeGolems[i].IsValid) {
                     Runner.Despawn(_activeGolems[i]);
                 }
             }
             _activeGolems.Clear();
             
             NetworkPrefabRef golemPrefab = SpellReferences.Instance.Golem;
             if (!golemPrefab.IsValid) {
                 Debug.LogError("DEBUG: Golem prefab INVALID in SpellReferences!");
                 return;
             }
             
             Debug.Log($"DEBUG: Spawning 3 Golems with prefab {golemPrefab}");
             
             for (int i = 0; i < 3; i++) {
                 // Random position around player, further away (radius 3 to 6)
                 Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(3f, 6f);
                 Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
                 Vector3 spawnPos = transform.position + offset;
                 
                 // Snap to ground
                 if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f)) {
                     spawnPos = hit.point + Vector3.up * golemSpawnHeightOffset; // Add offset up to avoid pivot clipping if centered
                 } else {  // Fallback
                     spawnPos.y = transform.position.y;
                 }
                 
                 // Spawn with Identity rotation so they stand upright
                 var golemObj = Runner.Spawn(golemPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                 var golemScript = golemObj.GetComponent<Golem>();
                 if (golemScript != null) {
                     golemScript.Init(Object);
                 }
                 _activeGolems.Add(golemObj);
             }
             
             Debug.Log("Summoned 3 Golems!");
        }
    }
}