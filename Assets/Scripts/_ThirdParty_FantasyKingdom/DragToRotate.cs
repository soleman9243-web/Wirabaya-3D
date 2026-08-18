using UnityEngine;

// 确保这里的类名 DragToRotate 和你的文件名一致
public class DragToRotate : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotateSpeed = 5.0f;     // 旋转速度

    private bool _isDragging = false;    // 是否正在拖拽

    void Update()
    {
        // 1. 检测鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
        }

        // 2. 检测鼠标松开
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        // 3. 处理旋转逻辑
        if (_isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed;

            // 应用旋转
            transform.Rotate(Vector3.up, -mouseX, Space.World);
            transform.Rotate(Vector3.right, mouseY, Space.World);
        }
    }
}