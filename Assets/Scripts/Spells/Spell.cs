using Fusion;
using UnityEngine;

namespace Spells {
    public interface ISpell {
        void CastSpell();

        string GetName();
        
        NetworkPrefabRef GetPrefab();
        
        void Cast(NetworkRunner runner, NetworkObject caster, Vector3 spawnPosition, Vector3 forwardDirection);
    }
}