# User Interface Design

> Making complexity accessible and observation enjoyable.

## Status: 🟡 Design Phase

---

## Design Goals

- **Information-rich without overwhelming** — focus on what matters NOW
- **AI-debug first** — this is an AI engine, inspector is our best friend
- **Support both management and observation** — Free camera + Follow mode
- **Clean aesthetic** — RimWorld-inspired but slightly modernized
- **Scalable to different screen sizes** — responsive panels

---

## Layout Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  Day 3, 14:32  │  ⏸ ▶ ▶▶ ▶▶▶  │  Camera: [Free][Follow]  │  🪵24 🪨12 │
├─────────────────────────────────────────────────────────────────────┤
│                                                           ┌─────────┤
│  🔔 Notifications                                         │ INSPECT │
│  stack (top-left)                                         │─────────│
│                                                           │ [Plan]  │
│                       3D WORLD VIEW                       │ [Needs] │
│                                                           │[Beliefs]│
│                                                           │         │
│                                                           │ Agent   │
│                                                           │ Header  │
│                                                           │ + Tabs  │
│                                                           │ Content │
├───────────────────────────────────────────────────────────┴─────────┤
│  [🏠 Build] [⚙️ Orders] [📋 Work] [🗺️ Zones] [⚡ Debug]      [≡ Menu] │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Color Palette

| Role | Hex | Usage |
|------|-----|-------|
| Background | `#1a1a1a` | Main BG |
| Panel BG | `#252525` | All panels |
| Border | `#3a3a3a` | Panel borders |
| Text | `#d4d4d4` | Primary text |
| Text Muted | `#888888` | Secondary text |
| Accent | `#5c8a5c` | Active states, positive values |
| Warning | `#b8a052` | Alerts, medium stats |
| Danger | `#a05252` | Errors, low stats |
| Info | `#5c7a8a` | Neutral info |

---

## Camera Modes

### Free Camera (Default)
- WASD/Arrow movement
- Mouse drag to pan
- Scroll to zoom
- Q/E to rotate

### Follow Mode
- Activated via button or double-click on agent
- Camera tracks selected agent
- Player can still zoom/rotate around target
- API: `CameraModule.EnableFollowMode()` / `AddToTargetGroup(transform)`

---

## HUD Elements

### Top Bar (Always Visible)
- **Left**: Date/Time display, Simulation speed controls (⏸ ▶ ▶▶ ▶▶▶)
- **Center**: Camera mode indicator [Free] [Follow]
- **Right**: Resource counters (future)

### Bottom Bar (Always Visible)
- **Left**: Main command buttons (Build, Orders, Work, Zones, Debug)
- **Right**: Menu button

---

## Selection & Inspector

### Selection System
- **Current**: Single selection via click
- **Architecture**: Prepared for multi-select (future)
- Interface: `ISelectable`, `SelectionService`

### Inspector Panel (Right Side)

**Position**: Fixed right panel, 304px width
**Always shows**: Agent header (portrait, name, class, mood)

#### Tab: Plan (Primary - AI Debug Focus)
Most important tab for AI engine development.

Content:
1. **Current Goal** — name, priority, visual indicator
2. **Action Plan** — ordered list with current action highlighted
3. **Related Stats** — stats that affect current plan
4. **Related Beliefs** — beliefs checked by current goal/actions

```
┌─────────────────────────────┐
│ 👤 Marcus                   │
│    Builder • Level 3        │
│    😊 Content               │
├─────────────────────────────┤
│ [Plan] [Needs] [Beliefs]    │
├─────────────────────────────┤
│ CURRENT GOAL                │
│ ┌─────────────────────────┐ │
│ │ 🏗️ BuildStructure       │ │
│ │ Priority: 0.85          │ │
│ └─────────────────────────┘ │
│                             │
│ ACTION PLAN                 │
│ 1. GetResources ▶ (active)  │
│ 2. MoveTo(Blueprint)        │
│ 3. Build                    │
│                             │
│ RELATED STATS               │
│ Energy ████████░░ 65/100    │
│ Hunger ████░░░░░░ 45/100    │
│                             │
│ RELATED BELIEFS             │
│ ✓ HasResources: true        │
│ ✓ CanReachTarget: true      │
│ ✗ TargetComplete: false     │
└─────────────────────────────┘
```

#### Tab: Needs & Goals
All agent stats + available goals with priorities.

Content:
1. **Vital Stats** — Health, Energy, Hunger, Rest, Social (progress bars)
2. **Available Goals** — sorted by priority, current highlighted

#### Tab: Beliefs
Full beliefs dump for AI debugging.

Content:
- All beliefs with current evaluation (true/false)
- Grouped by category (future)

---

## Notifications & Alerts

### Position
Top-left corner, stacked vertically

### Structure (Future)
```csharp
public interface INotification {
    string Title { get; }
    string Description { get; }
    NotificationPriority Priority { get; }
    float Duration { get; }
    Action OnClick { get; }
}
```

### Priority Levels
- **Critical** (red border) — immediate threat
- **Warning** (yellow border) — attention needed
- **Info** (gray border) — informational

---

## Implementation Notes

### Existing Classes to Extend
- `MainInfoPanel` → add new tabs (PlanPanel, NeedsPanel)
- `ControlsPanelLayer` → add camera mode toggle
- `GameUIModule` → wire up selection → inspector

### New Classes Needed
- `PlanPanel : BaseInfoPanel` — Plan tab content
- `NeedsGoalsPanel : BaseInfoPanel` — Needs & Goals tab
- `SelectionService` — manages current selection
- `CameraModeWidget` — Free/Follow toggle

### Data Flow
```
User Click → SelectionService → MainInfoPanel.SetAgent()
                             → CameraModule (optional follow)
```

---

## Menus & Panels

*To be designed: Build menu, Work priorities, Zone management*

---

## Open Questions

- [ ] Build menu structure (categories, search?)
- [ ] Zone visualization (overlays, colors?)
- [ ] Multi-select UI (selection box, group commands)
- [ ] Keyboard shortcuts reference

---

*Last updated: 2025-01-27*
