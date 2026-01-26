# WorldGrid Visualization System

**Project:** Almost Alive  
**Last Updated:** 2026-01-26

---

## 📋 Overview

Система визуализации grid-based placement. Рендерит тайлы под курсором для hover (1 ячейка) и footprint (N×M структуры).

---

## 📁 Structure

```
Assets/Content/
├── Scripts/World/Grid/
│   ├── GroundCoord.cs                    # Grid coordinate struct
│   ├── WorldGrid.cs                      # Static grid info (cellSize, queries)
│   └── Presentation/
│       ├── WorldGridPresentationModule.cs     # Main controller
│       ├── WorldGridPresentationConfigSO.cs   # Configuration SO
│       ├── TileMeshRenderer.cs                # Core tile renderer
│       ├── HoverVisualizer.cs                 # Single cell highlight
│       ├── FootprintVisualizer.cs             # Structure footprint
│       ├── IHoverVisualizer.cs                # Interface
│       ├── IFootprintVisualizer.cs            # Interface
│       └── GridVisualizationMode.cs           # Enum
│
└── Materials/Grid/
    ├── GridTile.shader                   # Tile shader (border + fill)
    └── GridTile.mat                      # Tile material
```

---

## 🎯 Setup

1. Open `GridPresentationConfig` asset
2. Assign `Tile Material` = **GridTile** (Content/Materials/Grid/)
3. Done!

---

## 🔧 TileMeshRenderer API

```csharp
// Single tile
renderer.ShowTile(coord, color, borderOnly: true);

// Footprint  
renderer.ShowFootprint(origin, size, color);

// Hide
renderer.HideAll();
```

---

## 🎮 Workflow

1. **F12** → DebugPanel
2. **Select action** → PlacementPreview mode activates
3. **Move mouse** → tiles follow cursor, adapt to terrain
4. **Click** → place

---

**End**
