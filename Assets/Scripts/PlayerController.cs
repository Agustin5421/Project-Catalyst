using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour {
    private Rigidbody _rb; // Reference to the Rigidbody component
    private float _movementX;
    private float _movementY;
    [SerializeField] private float speed = 10f; // Speed multiplier for movement
    
    
    [Header("Salto")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private Transform groundCheck;  // un empty en los pies
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;   // capa del suelo
    
    private bool _isGrounded;
    
    void Start() {
        _rb = GetComponent<Rigidbody>(); // Initialize the Rigidbody reference
    }
    
    void Update() {
        _isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask); // Check if the player is grounded
    }

    void OnMove(InputValue movementValue) {
    
        Vector2 movementVector = movementValue.Get<Vector2>();
        
        _movementX = movementVector.x;
        _movementY = movementVector.y;
    }

    void OnJump(InputValue jumpValue) {
        if (jumpValue.isPressed && _isGrounded)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        
    }

    void FixedUpdate() {
        Vector3 movement = new Vector3(_movementX, 0, _movementY);
        _rb.AddForce(movement * speed); // Apply force based on input
    }

}