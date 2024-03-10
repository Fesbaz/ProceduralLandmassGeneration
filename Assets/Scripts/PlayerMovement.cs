using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    
    public CharacterController controller;

    public float moveSpeed = 20f;
    public float g = -9.81f;
    public float jumpHeight = 10f;

    // Floor collision
    public Transform groundCheck;
    public float groundDistance = 0.4f; // Radius of sphere under player
    public LayerMask groundMask; // Which objects sphere should check for, REMEMBER TO SET CORRECT LAYERS IN INSPECTOR

    Vector3 velocity;
    [SerializeField]
    bool isGrounded;

    void Start() {
        
    }

    void Update() {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); // Creates sphere at groundCheck.position, groundDistance as radius

        // isGrounded might register before we are fully grounded, so set velocity.y to low value;
        if (isGrounded && velocity.y < 0) { // Is velocity.y needed?
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump")) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * g);
        }

        velocity.y += g * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
