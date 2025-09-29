using System;
using Fusion;
using UnityEngine;

namespace Structs {
    [Serializable]
    public struct NetInputData : INetworkInput {
        public Vector2 Move;
        public NetworkBool Jump;
        public NetworkBool Sprint;
    }
}

