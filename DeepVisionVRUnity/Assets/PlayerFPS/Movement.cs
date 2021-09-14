using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 10f;
    private float speedFactor = 1f;
    private Vector2 horizontalInput;

    public float jumpHeigth = 3f;
    private bool jump;
    private bool sprint;
    public float sprintSpeedFactor = 2f;
    public float gravity = -9.81f;
    private Vector3 verticalVelocity = Vector3.zero;
    public LayerMask groundMask;
    private bool isGrounded;

    private void Update()
    {
        isGrounded = Physics.CheckSphere(transform.position, 0.1f, groundMask);
        if (isGrounded)
        {
            verticalVelocity.y = 0;
        }

        if (sprint)
        {
            speedFactor = sprintSpeedFactor;
            if (horizontalInput.magnitude < 0.001f)
            {
                sprint = false;
            }
        }
        else
        {
            speedFactor = 1f;
        }
        
        Vector3 horizontalVelocity = (transform.right * horizontalInput.x + transform.forward * horizontalInput.y) * speed * speedFactor;
        controller.Move(horizontalVelocity * Time.deltaTime);

        if (jump)
        {
            if (isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(-2f * jumpHeigth * gravity);
            }
            jump = false;
        }
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }


    public void ReceiveHorizontalInput(Vector2 _horizontalInput)
    {
        horizontalInput = _horizontalInput;
    }


    public void OnJumpPressed()
    {
        jump = true;
    }


    public void OnSprintPressed()
    {
        sprint = true;
    }
}
