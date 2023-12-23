using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f;
    /// <summary>
    /// 实际速度
    /// </summary> 
    Vector3 velocity;
    /// <summary>
    /// 输入速度
    /// </summary>
    Vector3 desiredVelocity;
    Vector2 playerInput;
    void Update()
    {
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        // playerInput.Normalize();
        // playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        // transform.localPosition = new Vector3(playerInput.x, 0.5f, playerInput.y);
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        //加速度
        float maxSpeedChange = maxAcceleration * Time.deltaTime;
        velocity.x =
            Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z =
            Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
        Vector3 displacement = velocity * Time.deltaTime;
        transform.localPosition += displacement;
    }
}
