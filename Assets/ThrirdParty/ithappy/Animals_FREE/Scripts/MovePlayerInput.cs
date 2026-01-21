using UnityEngine;
using UnityEngine.InputSystem;

namespace ithappy.Animals_FREE {
  [RequireComponent(typeof(CreatureMover))]
  public class MovePlayerInput : MonoBehaviour {
    [Header("Character")] [SerializeField]
    private string m_HorizontalAxis = "Horizontal"; // оставлены для совместимости/документации

    [SerializeField] private string m_VerticalAxis = "Vertical";
    [SerializeField] private string m_JumpButton = "Jump";
    [SerializeField] private KeyCode m_RunKey = KeyCode.LeftShift;

    [Header("Input Actions (new Input System)")] [Tooltip("Vector2: X = left/right, Y = forward/back")] [SerializeField]
    private InputActionReference m_MoveAction;

    [Tooltip("Vector2: Mouse delta or right stick")] [SerializeField]
    private InputActionReference m_LookAction;

    [Tooltip("Float: scroll wheel")] [SerializeField]
    private InputActionReference m_ScrollAction;

    [Tooltip("Button: jump")] [SerializeField]
    private InputActionReference m_JumpAction;

    [Tooltip("Button/Value: run")] [SerializeField]
    private InputActionReference m_RunAction;

    [Header("Camera")] [SerializeField] private PlayerCamera m_Camera;
    [SerializeField] private string m_MouseX = "Mouse X";
    [SerializeField] private string m_MouseY = "Mouse Y";
    [SerializeField] private string m_MouseScroll = "Mouse ScrollWheel";

    private CreatureMover m_Mover;

    private Vector2 m_Axis;
    private bool m_IsRun;
    private bool m_IsJump;

    private Vector3 m_Target;
    private Vector2 m_MouseDelta;
    private float m_Scroll;

    private void Awake() {
      m_Mover = GetComponent<CreatureMover>();
    }

    private void OnEnable() {
      EnableAction(m_MoveAction);
      EnableAction(m_LookAction);
      EnableAction(m_ScrollAction);
      EnableAction(m_JumpAction);
      EnableAction(m_RunAction);
    }

    private void OnDisable() {
      DisableAction(m_MoveAction);
      DisableAction(m_LookAction);
      DisableAction(m_ScrollAction);
      DisableAction(m_JumpAction);
      DisableAction(m_RunAction);
    }

    private void EnableAction(InputActionReference reference) {
      if (reference != null && reference.action != null && !reference.action.enabled)
        reference.action.Enable();
    }

    private void DisableAction(InputActionReference reference) {
      if (reference != null && reference.action != null && reference.action.enabled)
        reference.action.Disable();
    }

    private void Update() {
      GatherInput();
      SetInput();
    }

    public void GatherInput() {
      // Movement
      if (m_MoveAction != null && m_MoveAction.action != null)
        m_Axis = m_MoveAction.action.ReadValue<Vector2>();
      else
        m_Axis = Vector2.zero;

      // Run - prefer new input system, fall back to old KeyCode check if needed
      if (m_RunAction != null && m_RunAction.action != null) {
        // Try reading as float (button) or Vector2 (axis)
        var runFloat = m_RunAction.action.ReadValue<float>();
        if (runFloat != 0f)
          m_IsRun = runFloat > 0.5f;
        else {
          // fallback to button triggered/pressed
          m_IsRun = m_RunAction.action.triggered || m_RunAction.action.phase == InputActionPhase.Performed;
        }
      }
      else {
        m_IsRun = Keyboard.current != null ? Keyboard.current[Key.LeftShift].isPressed : Input.GetKey(m_RunKey);
      }

      // Jump - use triggered property when available
      if (m_JumpAction != null && m_JumpAction.action != null) {
        m_IsJump = m_JumpAction.action.triggered;
      }
      else {
        m_IsJump = Input.GetButton(m_JumpButton);
      }

      // Camera target from camera component (unchanged)
      m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;

      // Look / Mouse delta
      if (m_LookAction != null && m_LookAction.action != null)
        m_MouseDelta = m_LookAction.action.ReadValue<Vector2>();
      else
        m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));

      // Scroll
      if (m_ScrollAction != null && m_ScrollAction.action != null)
        m_Scroll = m_ScrollAction.action.ReadValue<float>();
      else
        m_Scroll = Input.GetAxis(m_MouseScroll);
    }

    public void BindMover(CreatureMover mover) {
      m_Mover = mover;
    }

    public void SetInput() {
      if (m_Mover != null) {
        m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);
      }

      if (m_Camera != null) {
        m_Camera.SetInput(in m_MouseDelta, m_Scroll);
      }
    }
  }
}