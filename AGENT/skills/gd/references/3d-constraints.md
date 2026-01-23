# 3D Constraints & Opportunities

Differences from RimWorld's 2D tile-based approach that affect design decisions.

## Constraints

### Navigation
- **Grid System Available**: World is divided into a grid despite 3D terrain — can be used for building placement, spatial queries, room detection
- **Grid + Terrain Complexity**: Grid cells exist on uneven terrain — must account for height differences between adjacent cells
- **NavMesh for Movement**: Agents use NavMesh for pathfinding, not grid-based A*
- **Pathfinding Cost**: More expensive than A* on grid; minimize recalculations
- **Dynamic Obstacles**: NavMesh updates when terrain/buildings change — can be costly
- **Slopes**: Agents can traverse hills, but steep angles may block movement

### Performance
- **Draw Calls**: Many individual colonists/objects stress rendering
- **LOD Requirements**: Distant objects need simplified versions
- **Animation Overhead**: Skeletal animation per agent adds cost
- **Simulation Budget**: AI decisions must be throttled or staggered

### Camera
- **Visibility**: Top-down works, but walls/roofs may occlude content
- **Multiple Views**: Supporting cinematic camera adds UI complexity
- **Selection**: 3D picking more complex than tile coordinates

### Building System
- **Non-grid Placement**: Either enforce grid or handle free-form placement
- **Vertical Dimension**: If multi-story, adds massive complexity (pathfinding, rendering, UI)
- **Terrain Conformance**: Buildings must adapt to uneven ground

## Opportunities

### Visual Storytelling
- **Readable Animations**: Body language conveys mood, activity, relationships
- **Cinematic Moments**: Camera can zoom into emotional scenes
- **Environmental Detail**: Weather, lighting, day/night enhance immersion
- **Observation Mode**: Players can watch life unfold at ground level

### Spatial Design
- **Natural Layouts**: Colonies don't look like grid-locked boxes
- **Terrain Integration**: Settlements flow around hills, rivers
- **Height Advantage**: Combat and visibility can use verticality
- **Procgen Variety**: Terrain generation creates unique map identities

### Agent Believability
- **Smooth Movement**: Continuous motion feels more lifelike than tile-hopping
- **Procedural Animation**: IK, look-at, reactive poses enhance presence
- **Spatial Awareness**: Agents can respond to 3D environment (seek shade, shelter)

## Design Recommendations

1. **Use Grid for Spatial Logic**: Leverage existing grid for building placement, room detection, zone definitions
2. **Defer Multi-Story**: Start with single-level buildings on uneven terrain
3. **Stagger AI Updates**: Don't run all colonist decisions same frame
4. **Use LOD Aggressively**: Simplify distant colonists to capsules/icons
5. **Cache Paths**: Reuse navigation paths when possible
6. **Prioritize Readability**: If visual clarity conflicts with realism, choose clarity

---

*Reference when evaluating mechanic complexity vs 3D implementation cost*
