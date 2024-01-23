using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveComponent : MonoBehaviour
{
    [SerializeField, Range(0f, 100f), Header("最大速度")]
    float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f), Header("最大空中加速度")]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;
    [SerializeField, Range(0f, 100f), Header("最大捕捉速度")]
    float maxSnapSpeed = 100f;
    [SerializeField, Min(0f), Header("探测距离")]
    float probeDistance = 1f;
    [SerializeField, Range(0f, 10f), Header("跳跃高度")]
    float jumpHeight = 2f;
    [SerializeField, Range(0, 5), Header("空中跳跃次数限制")]
    int maxAirJumps = 0;
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;
    [SerializeField]
    LayerMask probeMask = -1;
    float minGroundDotProduct;
    /// <summary>
    /// 跳跃技术
    /// </summary> 
    int jumpPhase;
    /// <summary>
    /// 跳跃状态
    /// </summary>
    bool desiredJump = false;
    /// <summary>
    /// 地面数量
    /// </summary>
    int groundContactCount;
    bool OnGround => groundContactCount > 0;
    /// <summary>
    /// 实际速度
    /// </summary> 
    Vector3 velocity;
    /// <summary>
    /// 输入速度
    /// </summary>
    Vector3 desiredVelocity;
    /// <summary>
    /// 玩家输入方向
    /// </summary>
    Vector2 playerInput;
    /// <summary>
    /// 接触法线
    /// </summary> 
    Vector3 contactNormal;
    int stepsSinceLastGrounded, stepsSinceLastJump;
    Rigidbody body;
    RaycastHit hit;
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }
    void Update()
    {
        GetInputSpeed();
        desiredJump |= Input.GetKeyDown(KeyCode.Space);

        GetComponent<Renderer>().material.SetColor(
            "_Color", OnGround ? Color.black : Color.white
        );
    }
    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        body.velocity = velocity;
        ClearState();
    }
    void OnCollisionEnter(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }
    /// <summary>
    /// 地面捕捉
    /// </summary>
    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        //光线投射
        if (!Physics.Raycast(body.position, Vector3.down,
        out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }
        if (hit.normal.y < minGroundDotProduct)
        {
            return false;
        }

        //没有中止与地面的联系
        groundContactCount = 1;
        contactNormal = hit.normal;
        //速度与地面对齐
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }
    /// <summary>
    /// 获取输入速度：输入向量*最大速度
    /// </summary>
    void GetInputSpeed()
    {
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
    }
    /// <summary>
    /// 更新状态
    /// </summary>
    void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        velocity = body.velocity;
        if (OnGround || SnapToGround())
        {
            stepsSinceLastGrounded = 0;
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }
    /// <summary>
    /// 状态重置
    /// </summary> 
    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    /// <summary>
    /// 碰撞处理
    /// </summary>
    void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }

    /// <summary>
    /// 跳跃处理
    /// </summary>
    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            stepsSinceLastJump = 0;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            velocity += contactNormal * jumpSpeed;
        }
    }
    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX =
            Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);
        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }
}
