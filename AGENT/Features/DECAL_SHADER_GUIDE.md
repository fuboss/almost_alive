# Decal Shader Graph Setup Guide

## Shader Creation (Unity Editor)

### Step 1: Create Shader Graph
1. Right-click in Project window → Create → Shader Graph → URP → Decal Shader Graph
2. Name: `GridDecalShader`

### Step 2: Shader Properties
Add these properties in Blackboard:

| Property Name | Type | Default Value | Mode |
|--------------|------|---------------|------|
| `_CellSize` | Float | 1.0 | Exposed |
| `_GridColor` | Color | (1, 1, 1, 0.08) | Exposed |
| `_Alpha` | Float | 1.0 | Exposed |
| `_LineThickness` | Float | 0.05 | Exposed |

### Step 3: Grid Pattern Logic

**Node Setup:**
```
1. UV Node → Split (R, G channels = X, Z in world space proxy)

2. For each axis (X and Z):
   a. Divide UV by _CellSize
   b. Fraction node (get decimal part)
   c. Multiply by _CellSize
   d. Compare: if < _LineThickness then 1 else 0
   
3. Maximum node (combine X and Z line tests)
4. Multiply result by _GridColor
5. Multiply alpha by _Alpha property
6. Output to Base Color and Alpha
```

**Pseudo-code:**
```hlsl
// Get UV coordinates (proxy for world XZ)
float2 uv = IN.UV;

// Grid pattern
float2 gridUV = frac(uv / _CellSize) * _CellSize;
float gridX = step(gridUV.x, _LineThickness);
float gridZ = step(gridUV.y, _LineThickness);
float grid = max(gridX, gridZ);

// Apply color
float4 color = _GridColor * grid;
color.a *= _Alpha;

return color;
```

### Step 4: Decal Settings
In Shader Graph settings:
- Material Type: **Decal**
- Surface Type: **Transparent**
- Blend Mode: **Alpha**
- Render Face: **Front**
- Enable **Affect Albedo** and **Affect Normal** (optional)

### Step 5: Create Material
1. Create Material from shader → Name: `GridDecalMaterial`
2. Assign to `WorldGridPresentationConfigSO.gridDecalMaterial`

## Fallback: Simple Line Material (LineRenderer mode)

If Decal shader doesn't work, use LineRenderer mode:

1. Create Material: Right-click → Create → Material
2. Shader: `Universal Render Pipeline/Unlit`
3. Color: White with low alpha (e.g., R:1 G:1 B:1 A:0.08)
4. Assign to `WorldGridPresentationConfigSO.gridLineMaterial`
5. Set `renderingType = GridRenderingType.LineRenderer` in config

## Testing

1. Create `WorldGridPresentationConfigSO` in Project
2. Assign materials (Decal or Line)
3. Drag config to `GameScope.gridPresentationConfig`
4. Play scene
5. Press F12 to open DebugPanel
6. Toggle grid visibility

## Troubleshooting

**Decal not visible:**
- Check DecalProjector is enabled
- Verify terrain has proper material (must accept decals in URP)
- Check camera angle (decals fade at steep angles)
- Verify DecalProjector.size covers visible area

**LineRenderer not visible:**
- Check material is assigned
- Verify LineRenderer.enabled = true
- Check camera distance (lines might be too thin)
- Increase `gridLineThickness` in config

**Performance issues:**
- Reduce `maxRenderDistance` in config
- Increase `hoverUpdateInterval` in config
- Use Decal mode (single draw call vs many LineRenderers)

## Advanced: Procedural Grid in Code

Alternative approach without Shader Graph:
1. Generate grid texture (e.g., 64x64) with code
2. Use Texture2D with grid lines drawn programmatically
3. Apply as Decal material base texture
4. Pro: No Shader Graph needed
5. Con: Less flexible, fixed resolution

Code snippet for texture generation:
```csharp
Texture2D CreateGridTexture(int resolution, float lineWidth) {
    var tex = new Texture2D(resolution, resolution);
    var linePixels = Mathf.CeilToInt(lineWidth * resolution);
    
    for (int y = 0; y < resolution; y++) {
        for (int x = 0; x < resolution; x++) {
            bool isLine = x < linePixels || y < linePixels;
            tex.SetPixel(x, y, isLine ? Color.white : Color.clear);
        }
    }
    
    tex.Apply();
    tex.wrapMode = TextureWrapMode.Repeat;
    return tex;
}
```
