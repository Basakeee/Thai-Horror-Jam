using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorJam
{
    // Top-down แบบ Twelve Minutes: กล้องอยู่ทิศเดียวตลอด เลยใช้ WASD ตรงๆ ตามแกนโลก
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.2f;
        public float runSpeed = 5.6f;
        public bool frozen;
        public bool IsRunning { get; private set; }

        CharacterController controller;
        float verticalSpeed;

        void Awake() => controller = GetComponent<CharacterController>();

        public void ResetState()
        {
            frozen = false;
            IsRunning = false;
            verticalSpeed = 0f;
        }

        void Update()
        {
            var kb = Keyboard.current;
            IsRunning = false;
            if (frozen || kb == null) return;

            Vector3 input = Vector3.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            input = Vector3.ClampMagnitude(input, 1f);

            IsRunning = kb.leftShiftKey.isPressed && input.sqrMagnitude > 0.01f;
            float speed = IsRunning ? runSpeed : walkSpeed;
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - 20f * Time.deltaTime;

            controller.Move((input * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (input.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(input);
        }
    }
}
