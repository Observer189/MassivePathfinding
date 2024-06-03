using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locomotion : MonoBehaviour
{
    public float maxSpeed;
    public float acceleration;
    public bool rotateToMovementDir;
    
    protected Vector2 targetSpeed;
    protected Vector2 speed;

    protected Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void FixedUpdate()
    {
        body.angularVelocity = 0;
        UpdateSpeed(Time.fixedDeltaTime);
        Move(Time.fixedDeltaTime);
    }

    protected void Move(float delta)
    {
        body.MovePosition(body.position + speed * delta);
    }

    protected void UpdateSpeed(float delta)
    {

        /*if (speed.sqrMagnitude > movementSpeed.GetCurValue() * movementSpeed.GetCurValue())
        {
            speed = speed.normalized * movementSpeed.GetCurValue();
        }*/
        var diff = targetSpeed - speed;
        var acc = acceleration * delta;
        if (diff.sqrMagnitude < acc * acc)
        {
            speed = targetSpeed;
        }
        else
        {
            speed += diff.normalized * acc;
        }

        if (speed.sqrMagnitude > 0)
        {
            if (rotateToMovementDir)
            {
                transform.up = speed;
            }
        }
    }
    
    public void Stop()
    {
        targetSpeed = Vector2.zero;
        speed = Vector2.zero;
    }
    
    public void SetTargetSpeed(Vector2 targetSpeed)
    {
        this.targetSpeed = targetSpeed;
        if (this.targetSpeed.sqrMagnitude > maxSpeed * maxSpeed)
        {
            this.targetSpeed = targetSpeed.normalized * maxSpeed;
        }
    }
}
