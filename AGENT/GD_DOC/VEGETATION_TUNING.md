# Vegetation Tuning Guide

> GD Skill руководство по настройке растительности в биомах.
> Created: 2026-01-28

---

## 📐 System Architecture

```
BiomeSO._data.vegetation (BiomeVegetationConfig)
├── globalDensity (0-3)        # Master multiplier
├── maxDensityPerCell (8-255)  # Unity terrain detail density
└── categories[] (VegetationCategory)
    ├── name, size (Small/Medium/Large)
    ├── enabled, densityMultiplier
    ├── noise (VegetationNoiseSettings)
    │   ├── mode (Perlin)
    │   ├── scale (0.001-0.2)    # Patch size
    │   ├── octaves (1-6)        # Detail levels
    │   ├── persistence (0.1-0.9)
    │   ├── threshold (0-1)      # Coverage %
    │   └── blend (0-0.5)        # Edge softness
    ├── Terrain Filters (curves)
    │   ├── biomeEdgeFalloff
    │   ├── slopeFalloff
    │   └── heightFalloff
    └── layers[] (VegetationLayerConfig)
        ├── prototype (VegetationPrototypeSO)
        ├── density (0-1)
        ├── weight (0.1-5)
        └── Per-Layer Noise (optional)
```

---

## 🎯 Natural Distribution Goals

### Что делает траву "природной":
1. **Кластеризация** - трава растёт группами, не равномерно
2. **Проплешины** - открытые участки земли между группами
3. **Density variation** - разная плотность в разных местах
4. **Edge falloff** - меньше травы у границ биома
5. **Slope response** - меньше травы на склонах

---

## 🔧 Key Parameters Explained

### Category Noise Settings

| Parameter | Effect | Natural Feel |
|-----------|--------|--------------|
| **scale** | Размер пятен | 0.03-0.05 = средние пятна (20-30м) |
| **octaves** | Уровни детализации | 2-3 = достаточно для травы |
| **persistence** | Сила мелких деталей | 0.4-0.6 = умеренная |
| **threshold** | % покрытия | 0.25-0.35 = 65-75% покрытие |
| **blend** | Мягкость краёв | 0.15-0.25 = мягкие края пятен |

### Coverage Formula
```
Approx coverage = (1 - threshold) + blend/2
threshold=0.3, blend=0.2 → ~75% coverage
threshold=0.4, blend=0.15 → ~65% coverage
```

### Layer Settings

| Parameter | Effect | Recommended |
|-----------|--------|-------------|
| **density** | Базовое покрытие | 0.3-0.6 для травы |
| **weight** | Приоритет слоя | 1.0 = равный, 1.2+ = доминант |
| **useLayerNoise** | Доп. вариация | true для разнообразия |
| **layerNoiseScale** | Размер вариации | 0.03-0.05 |
| **layerNoiseStrength** | Сила вариации | 0.2-0.4 |

---

## 🌲 Forest Biome Recipe (v1)

### Ground Cover Category (Small)

**Философия:** Лесной покров - густой но неравномерный. Тень от деревьев → трава кластерами.

**Noise Settings:**
```yaml
scale: 0.04          # Средние пятна (~25м)
octaves: 3           # Достаточно деталей
persistence: 0.5     # Сбалансированный
threshold: 0.3       # ~70% покрытие
blend: 0.2           # Мягкие края
```

**Layers (3 вида для разнообразия):**

| Layer | Prototype | Density | Weight | Layer Noise |
|-------|-----------|---------|--------|-------------|
| Base | VP_GrassA | 0.5 | 1.2 | scale=0.03, str=0.25 |
| Variation | VP_GrassB | 0.4 | 1.0 | scale=0.035, str=0.3 |
| Accent | VP_GrassC | 0.3 | 0.8 | scale=0.04, str=0.35 |

**Terrain Filters:**
- slopeFalloff: 0°=1.0 → 45°=0.0 (линейный)
- biomeEdgeFalloff: 0=1.0 → 1.0=0.3 (ease-out)
- heightFalloff: constant 1.0

**Global:**
- globalDensity: 1.0
- maxDensityPerCell: 48 (умеренно, не давить GPU)

---

## 🏜️ Other Biome Recipes (TODO)

### Meadow
- Очень густая трава
- threshold: 0.2 (больше покрытие)
- Добавить цветы как Medium category

### Desert
- Редкая сухая трава
- threshold: 0.6 (мало покрытия)
- VP_DryGrass variants

### RockyHills
- Минимум травы
- threshold: 0.65
- Только в низинах

---

## ⚡ Performance Guidelines

| Factor | Budget | Notes |
|--------|--------|-------|
| maxDensityPerCell | 32-64 | Выше = больше полигонов |
| Total layers per biome | 3-5 | Больше = дороже рендер |
| useInstancing | true | Обязательно для GPU instancing |
| Detail Resolution | 512-1024 | На TerrainData |

**Правило:** Если FPS падает, уменьшай maxDensityPerCell или threshold.

---

## 🧪 Testing Checklist

- [ ] Визуально: есть проплешины?
- [ ] Визуально: трава кластерами?
- [ ] Границы биома: falloff работает?
- [ ] Склоны: меньше травы?
- [ ] FPS: приемлемый?
- [ ] Разнообразие: видны разные типы?

---

## 📝 Session Log

### Forest v1 (2026-01-28)
- 3 grass layers: GrassA (dominant), GrassB (variety), GrassC (accent)
- Noise: scale=0.04, threshold=0.3, blend=0.2
- Per-layer noise enabled for natural micro-variation
- maxDensityPerCell=48 (balanced for performance)
- Target: natural clusters with ~70% coverage
- **BUG FIX:** VegetationPhase.cs used wrong resolution for heightmap
  - detailResolution ≠ heightmapResolution
  - Fixed: convert detail coords → heightmap coords for height/slope
