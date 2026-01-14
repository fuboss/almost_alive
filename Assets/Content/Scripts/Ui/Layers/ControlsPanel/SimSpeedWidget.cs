using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Content.Scripts.Ui.Layers.ControlsPanel {
  public class SimSpeedWidget : MonoBehaviour {
    [Title("Buttons")] [SerializeField] private SimSpeedButton _pauseButton;
    [SerializeField] private SimSpeedButton _speed1Button;
    [SerializeField] private SimSpeedButton _speed2Button;
    [SerializeField] private SimSpeedButton _speed3Button;

    private SimulationTimeController _simTime;

    private InputSystem_Actions _input;
    private InputSystem_Actions.PlayerActions _playerActions;

    public void Init(SimulationTimeController simTime) {
      _simTime = simTime;
      Debug.LogError("_simTime added", this);
      _simTime.OnSpeedChanged -= UpdateButtonStates;
      _simTime.OnSpeedChanged += UpdateButtonStates;
      UpdateButtonStates(_simTime.currentSpeed);
    }

    private void Awake() {
      _input = new InputSystem_Actions();
      _playerActions = _input.Player;
    }

    private void OnEnable() {
      _playerActions.Enable();

      // Input bindings
      _playerActions.Pause.performed += OnPauseInput;
      _playerActions.Speed1.performed += OnSpeed1Input;
      _playerActions.Speed2.performed += OnSpeed2Input;
      _playerActions.Speed3.performed += OnSpeed3Input;

      // Button clicks
      if (_pauseButton != null) _pauseButton.OnClicked += OnPauseClicked;
      if (_speed1Button != null) _speed1Button.OnClicked += OnSpeed1Clicked;
      if (_speed2Button != null) _speed2Button.OnClicked += OnSpeed2Clicked;
      if (_speed3Button != null) _speed3Button.OnClicked += OnSpeed3Clicked;
    }

    private void OnDisable() {
      _playerActions.Pause.performed -= OnPauseInput;
      _playerActions.Speed1.performed -= OnSpeed1Input;
      _playerActions.Speed2.performed -= OnSpeed2Input;
      _playerActions.Speed3.performed -= OnSpeed3Input;

      if (_pauseButton != null) _pauseButton.OnClicked -= OnPauseClicked;
      if (_speed1Button != null) _speed1Button.OnClicked -= OnSpeed1Clicked;
      if (_speed2Button != null) _speed2Button.OnClicked -= OnSpeed2Clicked;
      if (_speed3Button != null) _speed3Button.OnClicked -= OnSpeed3Clicked;

      if (_simTime != null) _simTime.OnSpeedChanged -= UpdateButtonStates;

      _playerActions.Disable();
    }

    private void OnDestroy() {
      _input?.Dispose();
    }

    // Input handlers
    private void OnPauseInput(InputAction.CallbackContext ctx) => _simTime?.TogglePause();
    private void OnSpeed1Input(InputAction.CallbackContext ctx) => _simTime?.SetSpeed(SimSpeed.NORMAL);
    private void OnSpeed2Input(InputAction.CallbackContext ctx) => _simTime?.SetSpeed(SimSpeed.FAST);
    private void OnSpeed3Input(InputAction.CallbackContext ctx) => _simTime?.SetSpeed(SimSpeed.FASTEST);

    // Button click handlers
    private void OnPauseClicked() => _simTime?.TogglePause();
    private void OnSpeed1Clicked() => _simTime?.SetSpeed(SimSpeed.NORMAL);
    private void OnSpeed2Clicked() => _simTime?.SetSpeed(SimSpeed.FAST);
    private void OnSpeed3Clicked() => _simTime?.SetSpeed(SimSpeed.FASTEST);

    private void UpdateButtonStates(SimSpeed speed) {
      _pauseButton?.SetActive(speed == SimSpeed.PAUSED);
      _speed1Button?.SetActive(speed == SimSpeed.NORMAL);
      _speed2Button?.SetActive(speed == SimSpeed.FAST);
      _speed3Button?.SetActive(speed == SimSpeed.FASTEST);
    }
  }
}