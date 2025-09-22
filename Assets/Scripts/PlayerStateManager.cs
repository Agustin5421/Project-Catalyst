using Fusion;
using UnityEngine;

public class PlayerStateManager : NetworkBehaviour {
    public PlayerState State { get; private set; } = PlayerState.Idle;

    [SerializeField] float walkEnterSpeed = 0.15f;
    [SerializeField] float walkExitSpeed  = 0.10f;
    [SerializeField] Animator animator;

    NetworkCharacterController ncc;
    CharacterController cc;
    float speedSmoothed;
    Vector3 _lastPos;

    public override void Spawned() {
        ncc = GetComponent<NetworkCharacterController>();
        cc  = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator) animator.applyRootMotion = false;
        _lastPos = transform.position;   // evitar pico de velocidad el primer frame
    }

    public override void Render() {
        // 1) Velocidad plana (m/s)
        float planar;
        if (Object.HasStateAuthority && ncc != null) {
            // La autoridad sí tiene Velocity válido
            Vector3 v = ncc.Velocity;
            planar = new Vector2(v.x, v.z).magnitude;
        } else {
            // Proxy: derivar de la posición interpolada
            Vector3 delta = transform.position - _lastPos;
            planar = new Vector2(delta.x, delta.z).magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
        }
        _lastPos = transform.position;

        // Suavizado
        speedSmoothed = Mathf.Lerp(speedSmoothed, planar, 1f - Mathf.Exp(-10f * Time.deltaTime));

        // FSM mínima con histéresis
        if (State == PlayerState.Idle) {
            if (speedSmoothed > walkEnterSpeed) State = PlayerState.Walking;
        } else if (State == PlayerState.Walking) {
            if (speedSmoothed < walkExitSpeed) State = PlayerState.Idle;
        }

        // Animator (opcional)
        if (animator) {
            animator.SetFloat("Speed", speedSmoothed);
        }
        
        Debug.Log("state:" + State + " speed:" + speedSmoothed);
    }
}