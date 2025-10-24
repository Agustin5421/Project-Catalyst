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

        private PlayerStateMachine.PlayerStateMachine _playerStateMachine;

        public override void Spawned() {
            _playerStateMachine = GetComponent<PlayerStateMachine.PlayerStateMachine>();
            
            if (!Object.HasInputAuthority) return;

            // Create fireball spell - you'll need to assign the actual prefab reference
            // For now, we'll create it without a prefab and set it later
            ISpell fireballSpell = new Fireball(); 
            _availableSpells.Add(fireballSpell);
            
            // TODO: You need to assign the actual NetworkPrefabRef for your fireball prefab
            // This should be done through the inspector or by getting it from a prefab registry
            // EquippedSpellPrefabs.Set(0, yourFireballPrefabRef);
        }

        public override void FixedUpdateNetwork() {
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

            // Let the spell handle its own casting logic
            spellToCast.Cast(Runner, Object, spawnPosition, forwardDirection);
        }
    }
}