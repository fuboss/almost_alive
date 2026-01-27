using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.TopBar {
  /// <summary>
  /// Single resource entry: icon + count.
  /// </summary>
  public class ResourceEntryWidget : MonoBehaviour {
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;

    private string _id;
    private Func<int> _valueGetter;

    public void Setup(string id, Sprite icon, Func<int> getter) {
      _id = id;
      _valueGetter = getter;
      if (_icon != null && icon != null) _icon.sprite = icon;
      Refresh();
    }

    public void Refresh() {
      if (_valueGetter == null || _countText == null) return;
      _countText.text = _valueGetter().ToString();
    }
  }
}
