/**
 *  the author: D2og
 *  date: 2019-03-06
 *  what it does: camera control (mimic the Unity editor)
 *  how to use it: just put the script on the camera
 *  operation method:   1. Right click and press + mouse to move so that the camera to rotate
 *                      2. Press the mouse wheel + mouse to move so that the camera to translation
 *                      3. Right mouse button + keyboard w s a d (+leftShift) so that the camera to move
 *                      4. the mouse wheel rolling so that the camera forward and backward                  
 */
// Luke Clemens - I modified this so that it supports either input system.


using UnityEngine;

#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.EnhancedTouch;
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace AnimationCookerExample
{

public class CameraOperate : MonoBehaviour
{
    [Tooltip("Mouse wheel rolling control camera please enter, the speed of the back")]
    [Range(0.5f, 2f)] public float ScrollSpeed = 1f;
    [Tooltip("Right mouse button control camera X axis rotation speed")]
    [Range(0.5f, 2f)] public float RotateXSpeed = 1f;
    [Tooltip("Right mouse button control camera Y axis rotation speed")]
    [Range(0.5f, 2f)] public float RotateYSpeed = 1f;
    [Tooltip("Mouse wheel press, camera translation speed")]
    [Range(0.5f, 2f)] public float MoveSpeed = 1f;
    [Tooltip("The keyboard controls how fast the camera moves")]
    [Range(0.5f, 2f)] public float KeyMoveSpeed = 1f;
    [Tooltip("Enable/Disable the camera")]
    public bool Enable = true;
    [Tooltip("Enable/Disable keyboard control")]
    public bool EnableKeyboard = true;

    bool m_isRotating = false; // is currently in rotation
    bool m_isScrolling = false; // is currently in panning
    Transform m_transform; // camera transform component cache
    Vector3 m_startPos; // the initial position of the camera at the beginning of the operation
    bool m_cameraIsDown = false; // is the camera facing down

    // the initial position of the mouse as the camera begins to operate
    #if ENABLE_INPUT_SYSTEM
        Vector2 m_mouseStart;
    #else
        Vector3 m_mouseStart;
    #endif

    // Start is called before the first frame update
    void Start()
    {
        m_transform = transform;
    }

#if ENABLE_INPUT_SYSTEM
    // Update is called once per frame
    void Update()
    {
        if (!Enable) { return; }

        // When in the rotation state, and the right mouse button is released, then exit the rotation state
        if (m_isRotating && Mouse.current.rightButton.wasReleasedThisFrame) { m_isRotating = false; }

        //When it is in the translation state, and the mouse wheel is released, it will exit the translation state
        if (m_isScrolling && Mouse.current.middleButton.wasReleasedThisFrame) { m_isScrolling = false; }

        //Whether it's in a rotational state
        if (m_isRotating) {
            // Get the offset of the mouse on the screen
            Vector3 offset = Mouse.current.position.ReadValue() - m_mouseStart;

            // whether the camera is facing down
            if (m_cameraIsDown) {
                // the final rotation Angle = initial Angle + offset, 0.3f coefficient makes the rotation speed normal when rotateYSpeed, rotateXSpeed is 1
                m_transform.rotation = Quaternion.Euler(m_startPos + new Vector3(offset.y * 0.3f * RotateYSpeed, -offset.x * 0.3f * RotateXSpeed, 0));
            } else {
                // final rotation Angle = initial Angle + offset
                m_transform.rotation = Quaternion.Euler(m_startPos + new Vector3(-offset.y * 0.3f * RotateYSpeed, offset.x * 0.3f * RotateXSpeed, 0));
            }

            // simulate the unity editor operation: right click, the keyboard can control the camera movement
            if (EnableKeyboard) {
                float speed = KeyMoveSpeed;
                // press LeftShift to make speed *2
                if (Keyboard.current.leftShiftKey.isPressed) {
                    speed = 2f * speed;
                }
                // press W on the keyboard to move the camera forward
                if (Keyboard.current.wKey.isPressed) {
                    m_transform.position += m_transform.forward * Time.deltaTime * 10f * speed;
                }
                // press the S key on the keyboard to back up the camera
                if (Keyboard.current.sKey.isPressed) {
                    m_transform.position -= m_transform.forward * Time.deltaTime * 10f * speed;
                }
                // press A on the keyboard and the camera will turn left
                if (Keyboard.current.aKey.isPressed) {
                    m_transform.position -= m_transform.right * Time.deltaTime * 10f * speed;
                }
                // press D on the keyboard to turn the camera to the right
                if (Keyboard.current.dKey.isPressed) {
                    m_transform.position += m_transform.right * Time.deltaTime * 10f * speed;
                }
            }

        } else if (Mouse.current.rightButton.wasPressedThisFrame) {
            // press the right mouse button to enter the rotation state
            // enter the rotation state
            m_isRotating = true;
            // record the initial position of the mouse in order to calculate the offset
            m_mouseStart = Mouse.current.position.ReadValue();
            // record the initial mouse Angle
            m_startPos = m_transform.rotation.eulerAngles;
            // to determine whether the camera is facing down (the Y-axis is <0 according to the position of the object facing up),-0.0001f is a special case when x rotates 90
            m_cameraIsDown = m_transform.up.y < -0.0001f ? true : false;
        }

        // handle translation state
        if (m_isScrolling) {
            // mouse offset on the screen
            Vector3 offset = Mouse.current.position.ReadValue() - m_mouseStart;
            // final position = initial position + offset
            m_transform.position = m_startPos + m_transform.up * -offset.y * 0.1f * MoveSpeed + m_transform.right * -offset.x * 0.1f * MoveSpeed;
        } else if (Mouse.current.middleButton.wasPressedThisFrame) {
            // click the mouse wheel to enter translation mode
            // translation begins
            m_isScrolling = true;
            // record the initial position of the mouse
            m_mouseStart = Mouse.current.position.ReadValue();
            // record the initial position of the camera
            m_startPos = m_transform.position;
        }

        // how much did the roller roll
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        // scroll to scroll or not
        if (scroll.y != 0) {
            // position = current position + scroll amount
            m_transform.position += m_transform.forward * scroll.y * 1000f * Time.deltaTime * ScrollSpeed;
        }
    }
#else
    // Update is called once per frame
    void Update()
    {
        if (!Enable) { return; }

        // When in the rotation state, and the right mouse button is released, then exit the rotation state
        if (m_isRotating && Input.GetMouseButtonUp(1)) { m_isRotating = false; }

        // When it is in the translation state, and the mouse wheel is released, it will exit the translation state
        if (m_isScrolling && Input.GetMouseButtonUp(2)) { m_isScrolling = false; }

        //Whether it's in a rotational state
        if (m_isRotating) {
            // Get the offset of the mouse on the screen
            Vector3 offset = Input.mousePosition - m_mouseStart;

            // whether the camera is facing down
            if (m_cameraIsDown) {
                // the final rotation Angle = initial Angle + offset, 0.3f coefficient makes the rotation speed normal when rotateYSpeed, rotateXSpeed is 1
                m_transform.rotation = Quaternion.Euler(m_startPos + new Vector3(offset.y * 0.3f * RotateYSpeed, -offset.x * 0.3f * RotateXSpeed, 0));
            } else {
                // final rotation Angle = initial Angle + offset
                m_transform.rotation = Quaternion.Euler(m_startPos + new Vector3(-offset.y * 0.3f * RotateYSpeed, offset.x * 0.3f * RotateXSpeed, 0));
            }

            // simulate the unity editor operation: right click, the keyboard can control the camera movement
            if (EnableKeyboard) {
                float speed = KeyMoveSpeed;
                // press LeftShift to make speed *2
                if (Input.GetKey(KeyCode.LeftShift)) {
                    speed = 2f * speed;
                }
                // press W on the keyboard to move the camera forward
                if (Input.GetKey(KeyCode.W)) {
                    m_transform.position += m_transform.forward * Time.deltaTime * 10f * speed;
                }
                // press the S key on the keyboard to back up the camera
                if (Input.GetKey(KeyCode.S)) {
                    m_transform.position -= m_transform.forward * Time.deltaTime * 10f * speed;
                }
                // press A on the keyboard and the camera will turn left
                if (Input.GetKey(KeyCode.A)) {
                    m_transform.position -= m_transform.right * Time.deltaTime * 10f * speed;
                }
                // press D on the keyboard to turn the camera to the right
                if (Input.GetKey(KeyCode.D)) {
                    m_transform.position += m_transform.right * Time.deltaTime * 10f * speed;
                }
            }
        } else if (Input.GetMouseButtonDown(1)) {
            // press the right mouse button to enter the rotation state
            // enter the rotation state
            m_isRotating = true;
            // record the initial position of the mouse in order to calculate the offset
            m_mouseStart = Input.mousePosition;
            // record the initial mouse Angle
            m_startPos = m_transform.rotation.eulerAngles;
            // to determine whether the camera is facing down (the Y-axis is <0 according to the position of the object facing up),-0.0001f is a special case when x rotates 90
            m_cameraIsDown = m_transform.up.y < -0.0001f ? true : false;
        }

        // whether it is in the translation state
        if (m_isScrolling) {
            // mouse offset on the screen
            Vector3 offset = Input.mousePosition - m_mouseStart;
            // final position = initial position + offset
            m_transform.position = m_startPos + m_transform.up * -offset.y * 0.1f * MoveSpeed + m_transform.right * -offset.x * 0.1f * MoveSpeed;
        } else if (Input.GetMouseButtonDown(2)) {
            // click the mouse wheel to enter translation mode
            // translation begins
            m_isScrolling = true;
            // record the initial position of the mouse
            m_mouseStart = Input.mousePosition;
            // record the initial position of the camera
            m_startPos = m_transform.position;
        }

        // how much did the roller roll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        // scroll to scroll or not
        if (scroll != 0) {
            // position = current position + scroll amount
            m_transform.position += m_transform.forward * scroll * 1000f * Time.deltaTime * ScrollSpeed;
        }
    }
#endif
}

} // namespace