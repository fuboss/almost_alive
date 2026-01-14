using System;
using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.ControlsPanel {
  public class SimSpeedButton : MonoBehaviour {
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private Color _normalColor = new(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _activeColor = new(0.4f, 0.6f, 0.2f, 0.9f);
    
    [ShowInInspector, ReadOnly] private bool _isActive;

    public event Action OnClicked;

    private void Awake() {
      if (_button == null) _button = GetComponent<Button>();
      if (_background == null) _background = GetComponent<Image>();
      
      _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy() {
      _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick() {
      OnClicked?.Invoke();
    }

    public void SetActive(bool active) {
      _isActive = active;
      _background.color = active ? _activeColor : _normalColor;
    }

    private void OnValidate() {
      if (_button == null) _button = GetComponent<Button>();
      if (_background == null) _background = GetComponent<Image>();
    }
  }
}
