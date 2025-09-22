using Fusion;
using UnityEngine;
using Unity.Cinemachine; 

public class ThirdPersonCameraBinder : NetworkBehaviour {
    [Header("Camera Prefab")]
    [SerializeField] CinemachineCamera cameraPrefab;

    [Header("Pivots/Targets")]
    [SerializeField] Transform yawPivot;
    [SerializeField] Transform pitchPivot;
    [SerializeField] Transform headTarget;

    CinemachineCamera vcamInstance;

    public override void Spawned() {
        if (!HasInputAuthority) return;

        vcamInstance = Instantiate(cameraPrefab);

        var main = Camera.main;
        if (main && !main.TryGetComponent<CinemachineBrain>(out _)) {
            main.gameObject.AddComponent<CinemachineBrain>();
        }

        vcamInstance.Follow = pitchPivot;
        vcamInstance.LookAt = headTarget;

        vcamInstance.Priority = 1000;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (vcamInstance != null) Destroy(vcamInstance.gameObject);
    }
}
