using System;
using Fusion;
using UnityEngine;

namespace Structs {
    [Serializable]
    public struct NetInputData : INetworkInput {
        public Vector2 Move;
        public NetworkBool Jump;
        public NetworkBool Sprint;
        public NetworkBool Cast;
        public NetworkBool CastSlot1; // For casting fireball spell
        public byte SpellIndex; // 0 for fireball
    }
}

