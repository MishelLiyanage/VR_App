using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CSIVR.Input
{
    /// <summary>
    /// Headset-free rig: mouse look, WASD/Q-E movement with collision, and a non-tracked XRI ray interactor
    /// driven by named desktop actions (Select, Activate, Rotate, Teleport, Pause). The ray interactor feeds
    /// the same XR Grab Interactables, UI canvases and teleport anchors the XR rig uses.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopRig : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera m_Camera;
        [SerializeField] XRRayInteractor m_Hand;
        [SerializeField] Transform m_HandAttach;

        [Header("Tuning")]
        [SerializeField] float m_MoveSpeed = 2.0f;
        [SerializeField] float m_TurnSpeed = 90f;
        [SerializeField] float m_LookSensitivity = 0.1f;
        [SerializeField] float m_RotateSensitivity = 0.4f;
        [SerializeField] float m_EyeHeight = 1.6f;
        [SerializeField] Vector2 m_HoldDistanceRange = new Vector2(0.4f, 1.5f);
        [SerializeField] float m_TeleportRange = 20f;

        InputAction m_Move, m_Turn, m_Look, m_Select, m_Activate, m_Rotate, m_Teleport, m_Pause, m_Scroll;
        CharacterController m_Controller;
        float m_Pitch;
        float m_VerticalVelocity;
        bool m_CursorMode;
        bool m_TeleportAiming;
        Vector3 m_HandAttachStartLocalPos;

        public bool CursorMode => m_CursorMode;

        void Awake()
        {
            m_Controller = GetComponent<CharacterController>();
            m_Camera.transform.localPosition = new Vector3(0f, m_EyeHeight, 0f);
            m_HandAttachStartLocalPos = m_HandAttach.localPosition;

            m_Move = new InputAction("Move", InputActionType.Value);
            m_Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            m_Turn = new InputAction("Turn", InputActionType.Value);
            m_Turn.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            m_Look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            m_Select = new InputAction("Select", InputActionType.Button, "<Mouse>/rightButton");
            m_Activate = new InputAction("Activate", InputActionType.Button, "<Mouse>/leftButton");
            m_Rotate = new InputAction("Rotate", InputActionType.Button, "<Keyboard>/r");
            m_Teleport = new InputAction("Teleport", InputActionType.Button, "<Keyboard>/t");
            m_Pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            m_Scroll = new InputAction("HoldDistance", InputActionType.Value, "<Mouse>/scroll/y");

            // The ray interactor reads its buttons from this script instead of from tracked controllers.
            m_Hand.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            m_Hand.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            m_Hand.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
        }

        void OnEnable()
        {
            foreach (var a in AllActions()) a.Enable();
            m_Hand.selectExited.AddListener(OnHandSelectExited);
            SetCursorMode(false);
        }

        void OnDisable()
        {
            foreach (var a in AllActions()) a.Disable();
            m_Hand.selectExited.RemoveListener(OnHandSelectExited);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDestroy()
        {
            foreach (var a in AllActions()) a.Dispose();
        }

        InputAction[] AllActions() =>
            new[] { m_Move, m_Turn, m_Look, m_Select, m_Activate, m_Rotate, m_Teleport, m_Pause, m_Scroll };

        void Update()
        {
            if (m_Pause.WasPressedThisFrame())
                SetCursorMode(!m_CursorMode);

            bool rotating = m_Rotate.IsPressed() && m_Hand.hasSelection;

            if (!m_CursorMode)
            {
                if (rotating) RotateHeldObject(m_Look.ReadValue<Vector2>());
                else Look(m_Look.ReadValue<Vector2>());
                Move();
            }

            PoseHand();
            DriveHandInputs();
            UpdateHoldDistance();
            UpdateTeleport();
        }

        public void SetCursorMode(bool cursorMode)
        {
            m_CursorMode = cursorMode;
            Cursor.lockState = cursorMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorMode;
        }

        void Look(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * m_LookSensitivity, 0f, Space.Self);
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * m_LookSensitivity, -80f, 80f);
            m_Camera.transform.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
        }

        void Move()
        {
            transform.Rotate(0f, m_Turn.ReadValue<float>() * m_TurnSpeed * Time.deltaTime, 0f, Space.Self);

            var input = m_Move.ReadValue<Vector2>();
            var move = (transform.right * input.x + transform.forward * input.y) * m_MoveSpeed;

            m_VerticalVelocity = m_Controller.isGrounded ? -1f : m_VerticalVelocity + Physics.gravity.y * Time.deltaTime;
            move.y = m_VerticalVelocity;
            m_Controller.Move(move * Time.deltaTime);
        }

        // Ray starts at the eye. In play mode it follows the crosshair; in cursor mode it follows the mouse.
        void PoseHand()
        {
            var camT = m_Camera.transform;
            var dir = camT.forward;
            if (m_CursorMode && Mouse.current != null)
                dir = m_Camera.ScreenPointToRay(Mouse.current.position.ReadValue()).direction;

            var handT = m_Hand.transform;
            handT.position = camT.position;
            handT.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // One mouse button never triggers two tasks: over UI it presses UI, otherwise it activates the held tool.
        void DriveHandInputs()
        {
            bool overUI = m_Hand.TryGetCurrentUIRaycastResult(out _);
            bool primary = m_Activate.IsPressed();
            bool select = m_Select.IsPressed() && !m_TeleportAiming;

            m_Hand.uiPressInput.QueueManualState(primary && overUI, primary && overUI ? 1f : 0f);
            m_Hand.activateInput.QueueManualState(primary && !overUI && !m_TeleportAiming, primary && !overUI ? 1f : 0f);
            m_Hand.selectInput.QueueManualState(select, select ? 1f : 0f);
        }

        void RotateHeldObject(Vector2 delta)
        {
            var camT = m_Camera.transform;
            m_HandAttach.Rotate(camT.up, -delta.x * m_RotateSensitivity, Space.World);
            m_HandAttach.Rotate(camT.right, delta.y * m_RotateSensitivity, Space.World);
        }

        void UpdateHoldDistance()
        {
            if (!m_Hand.hasSelection) return;
            float scroll = m_Scroll.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f)) return;
            var p = m_HandAttach.localPosition;
            p.z = Mathf.Clamp(p.z + Mathf.Sign(scroll) * 0.1f, m_HoldDistanceRange.x, m_HoldDistanceRange.y);
            m_HandAttach.localPosition = p;
        }

        void OnHandSelectExited(SelectExitEventArgs _)
        {
            m_HandAttach.localRotation = Quaternion.identity;
            m_HandAttach.localPosition = m_HandAttachStartLocalPos;
        }

        // Hold T to aim at an anchor/area, release to confirm. Same destinations the XR teleport uses.
        void UpdateTeleport()
        {
            if (m_Teleport.WasPressedThisFrame() && !m_Hand.hasSelection)
                m_TeleportAiming = true;

            if (!m_TeleportAiming || !m_Teleport.WasReleasedThisFrame())
                return;

            m_TeleportAiming = false;
            var ray = new Ray(m_Hand.transform.position, m_Hand.transform.forward);
            if (!Physics.Raycast(ray, out var hit, m_TeleportRange, ~0, QueryTriggerInteraction.Collide))
                return;

            var target = hit.collider.GetComponentInParent<BaseTeleportationInteractable>();
            if (target == null) return;

            Vector3 destination = hit.point;
            Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (target is TeleportationAnchor anchor && anchor.teleportAnchorTransform != null)
            {
                destination = anchor.teleportAnchorTransform.position;
                yaw = Quaternion.Euler(0f, anchor.teleportAnchorTransform.eulerAngles.y, 0f);
            }

            m_Controller.enabled = false;
            transform.SetPositionAndRotation(destination, yaw);
            m_Controller.enabled = true;
        }

        void OnGUI()
        {
            if (!m_CursorMode)
            {
                var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                GUI.Box(new Rect(c.x - 3, c.y - 3, 6, 6), GUIContent.none);
            }

            GUI.Label(new Rect(10, Screen.height - 28, 1200, 24),
                "WASD move | Q/E turn | Mouse look | RMB hold grab | LMB use tool/UI | R+mouse rotate | Wheel push/pull | T teleport | Esc cursor");
        }
    }
}
