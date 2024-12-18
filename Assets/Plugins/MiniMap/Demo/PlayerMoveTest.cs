using System;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerMoveTest : MonoBehaviour
{
    public float moveSpeed = 5f;        // 移动速度
    public float rotationSpeed = 200f; // 旋转速度

    private CharacterController characterController;

    private bool isCursorLocked = true; // 鼠标锁定状态

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        LockCursor(true);
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleCursorLock();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // 获取水平输入
        float moveZ = Input.GetAxis("Vertical");   // 获取垂直输入

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.SimpleMove(move * moveSpeed);
    }

    private void HandleRotation()
    {
        if (isCursorLocked)
        {
            float mouseX = Input.GetAxis("Mouse X");

            // 根据鼠标移动旋转角色
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        else if (Input.GetMouseButtonDown(0)) // 点击鼠标左键重新锁定
        {
            LockCursor(true);
        }
    }

    private void LockCursor(bool lockCursor)
    {
        isCursorLocked = lockCursor;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }
}