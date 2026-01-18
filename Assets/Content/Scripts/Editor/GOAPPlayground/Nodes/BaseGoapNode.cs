using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground.Nodes {
  public abstract class BaseGoapNode : Node {
    public string guid;
    
    protected Port _execIn;
    protected Port _execOut;
    
    protected readonly Dictionary<string, Port> _beliefInputs = new();
    protected readonly Dictionary<string, Port> _beliefOutputs = new();

    public Port execIn => _execIn;
    public Port execOut => _execOut;
    public IReadOnlyDictionary<string, Port> beliefInputs => _beliefInputs;
    public IReadOnlyDictionary<string, Port> beliefOutputs => _beliefOutputs;

    protected BaseGoapNode(string title) {
      guid = System.Guid.NewGuid().ToString();
      this.title = title;
      AddToClassList("goap-node");
      
      // Compact style
      style.paddingTop = 2;
      style.paddingBottom = 2;
    }

    protected Port CreateExecInPort() {
      _execIn = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
      _execIn.portName = "";
      _execIn.portColor = Color.white;
      _execIn.style.width = 16;
      _execIn.style.height = 16;
      inputContainer.Insert(0, _execIn);
      return _execIn;
    }

    protected Port CreateExecOutPort() {
      _execOut = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
      _execOut.portName = "";
      _execOut.portColor = Color.white;
      _execOut.style.width = 16;
      _execOut.style.height = 16;
      outputContainer.Insert(0, _execOut);
      return _execOut;
    }

    protected Port CreateBeliefInput(string beliefName) {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
      port.portName = ShortenName(beliefName);
      port.tooltip = beliefName;
      port.portColor = new Color(0.9f, 0.6f, 0.2f);
      port.style.fontSize = 9;
      _beliefInputs[beliefName] = port;
      inputContainer.Add(port);
      return port;
    }

    protected Port CreateBeliefOutput(string beliefName) {
      var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
      port.portName = ShortenName(beliefName);
      port.tooltip = beliefName;
      port.portColor = new Color(0.2f, 0.8f, 0.4f);
      port.style.fontSize = 9;
      _beliefOutputs[beliefName] = port;
      outputContainer.Add(port);
      return port;
    }

    protected string ShortenName(string name) {
      name = name.Replace("Craft_Unfinished_", "")
                 .Replace("Craft_", "")
                 .Replace("Camp_", "")
                 .Replace("Inventory_", "")
                 .Replace("Remembers_Nearby/", "Near/")
                 .Replace("Transient_Is/", "")
                 .Replace("Transient_", "");
      
      return name.Length > 14 ? name.Substring(0, 12) + ".." : name;
    }

    public void SetHighlight(bool active, Color? color = null) {
      var c = color ?? new Color(0.3f, 0.8f, 0.3f);
      style.borderLeftColor = active ? c : Color.clear;
      style.borderRightColor = active ? c : Color.clear;
      style.borderTopColor = active ? c : Color.clear;
      style.borderBottomColor = active ? c : Color.clear;
      style.borderLeftWidth = active ? 3 : 0;
      style.borderRightWidth = active ? 3 : 0;
      style.borderTopWidth = active ? 3 : 0;
      style.borderBottomWidth = active ? 3 : 0;
    }

    public void SetExecutionHighlight(bool active) {
      if (_execIn != null) _execIn.portColor = active ? Color.green : Color.white;
      if (_execOut != null) _execOut.portColor = active ? Color.green : Color.white;
    }
  }
}
