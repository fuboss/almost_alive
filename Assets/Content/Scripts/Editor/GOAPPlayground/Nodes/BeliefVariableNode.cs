using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground.Nodes {
  public enum BeliefNodeType { Get, Set }
  
  public class BeliefVariableNode : Node {
    public string beliefName { get; }
    public BeliefNodeType nodeType { get; }
    public Port port { get; private set; }

    public BeliefVariableNode(string beliefName, BeliefNodeType type) {
      this.beliefName = beliefName;
      this.nodeType = type;
      
      // Remove default title container for compact look
      titleContainer.RemoveFromHierarchy();
      
      AddToClassList("belief-variable-node");
      
      // Compact style
      style.minWidth = 80;
      style.maxWidth = 140;
      style.paddingTop = 4;
      style.paddingBottom = 4;
      style.paddingLeft = 6;
      style.paddingRight = 6;
      
      var color = type == BeliefNodeType.Get 
        ? new Color(0.9f, 0.5f, 0.2f)
        : new Color(0.2f, 0.7f, 0.4f);
      
      style.backgroundColor = color;
      style.borderTopLeftRadius = 4;
      style.borderTopRightRadius = 4;
      style.borderBottomLeftRadius = 4;
      style.borderBottomRightRadius = 4;

      // Content container
      var content = new VisualElement {
        style = {
          flexDirection = FlexDirection.Row,
          alignItems = Align.Center
        }
      };
      mainContainer.Add(content);

      // Label
      var label = new Label(ShortenName(beliefName)) {
        style = {
          fontSize = 10,
          color = Color.white,
          unityFontStyleAndWeight = FontStyle.Bold,
          flexGrow = 1,
          overflow = Overflow.Hidden,
          textOverflow = TextOverflow.Ellipsis
        },
        tooltip = beliefName
      };
      content.Add(label);

      CreatePort();
      RefreshExpandedState();
      RefreshPorts();
    }

    private void CreatePort() {
      if (nodeType == BeliefNodeType.Get) {
        port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
        port.portName = "";
        port.portColor = new Color(1f, 0.7f, 0.4f);
        port.style.marginRight = -8;
        outputContainer.Add(port);
      } else {
        port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        port.portName = "";
        port.portColor = new Color(0.4f, 0.9f, 0.5f);
        port.style.marginLeft = -8;
        inputContainer.Add(port);
      }
    }

    private string ShortenName(string name) {
      // Remove common prefixes
      name = name.Replace("Craft_Unfinished_", "")
                 .Replace("Craft_", "")
                 .Replace("Camp_", "")
                 .Replace("Inventory_", "Inv_")
                 .Replace("Remembers_Nearby/", "")
                 .Replace("Transient_Is/", "")
                 .Replace("Transient_", "");
      
      return name.Length > 16 ? name.Substring(0, 14) + ".." : name;
    }

    public void SetHighlight(bool active) {
      var c = new Color(1f, 1f, 0.3f);
      style.borderLeftColor = active ? c : Color.clear;
      style.borderRightColor = active ? c : Color.clear;
      style.borderTopColor = active ? c : Color.clear;
      style.borderBottomColor = active ? c : Color.clear;
      style.borderLeftWidth = active ? 2 : 0;
      style.borderRightWidth = active ? 2 : 0;
      style.borderTopWidth = active ? 2 : 0;
      style.borderBottomWidth = active ? 2 : 0;
    }
  }
}
