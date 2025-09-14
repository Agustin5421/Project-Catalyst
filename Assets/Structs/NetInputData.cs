using System;
using Fusion;

namespace Structs
{
    [Serializable]
    public struct NetInputData : INetworkInput {
        public UnityEngine.Vector2 Move;   
        public NetworkButtons Buttons;       

        public const int BUTTON_JUMP = 0;
        public const int BUTTON_SPRINT = 1;
    }
}

