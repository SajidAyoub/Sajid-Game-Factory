using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float forwardSpeed = 8f;
    [SerializeField, Min(0f)] private float horizontalSpeed = 5f;

    private const float MinX = -4.5f;
    private const float MaxX = 4.5f;

    private void Update()
    {
        Move(ReadHorizontalInput(), Time.deltaTime);
    }

    private static float ReadHorizontalInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        bool leftPressed = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool rightPressed = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        return (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
    }

    private void Move(float horizontalInput, float deltaTime)
    {
        Vector3 position = transform.position;
        float safeHorizontal = float.IsNaN(horizontalSpeed) || float.IsInfinity(horizontalSpeed)
            ? 0f : Mathf.Max(0f, horizontalSpeed);
        float safeForward = float.IsNaN(forwardSpeed) || float.IsInfinity(forwardSpeed)
            ? 0f : Mathf.Max(0f, forwardSpeed);
        position.x = Mathf.Clamp(
            position.x + horizontalInput * safeHorizontal * deltaTime, MinX, MaxX);
        position.z += safeForward * deltaTime;
        transform.position = position;
    }
}
