# Camera Module - RTS Camera System

## Архитектура

```
Camera/
├── CameraModule.cs                    # Главный оркестратор
├── CameraState.cs                     # Состояние камеры (shared)
├── Settings/
│   └── CameraSettingsSO.cs           # ScriptableObject с настройками
├── Strategies/
│   ├── ICameraMovementStrategy.cs    # Интерфейс стратегии
│   ├── FreeCameraMovement.cs         # WASD + Edge panning
│   ├── FollowTargetMovement.cs       # Следование за группой
│   └── FocusOnPointMovement.cs       # Анимированный переход к точке
├── Input/
│   └── CameraInputHandler.cs         # Обработка ввода (Input System)
├── Components/
│   ├── CameraZoomController.cs       # Zoom + Tilt по кривой
│   ├── CameraRotationController.cs   # Вращение Q/E + MMB
│   ├── CameraDebugVisualizer.cs      # Debug отображение
│   └── CameraRuntimeCalibrator.cs    # Runtime калибровка
└── Editor/
    └── CameraCalibrationWindow.cs    # Editor окно калибровки
```

## Основные фичи

### 1. Zoom с динамическим углом (Tilt-Zoom)
- При zoom in (близко) — камера смотрит более горизонтально (30-45°)
- При zoom out (далеко) — камера смотрит почти вертикально (70-85°)
- Настраивается через `AnimationCurve` в `CameraSettingsSO`

### 2. Движение
- **WASD** — перемещение по горизонтальной плоскости
- **Edge Panning** — перемещение при наведении на край экрана
- Скорость зависит от уровня zoom (дальше = быстрее)

### 3. Вращение
- **Q/E** — дискретные повороты (настраиваемый шаг)
- **Middle Mouse Drag** — свободное вращение

### 4. Режимы (Strategy Pattern)
- **Free** — свободное управление (приоритет 0)
- **FocusOnPoint** — анимированный переход к точке (приоритет 5)
- **Follow** — следование за TargetGroup (приоритет 10)

## Использование

### 1. Создание настроек
1. `Right Click → Create → Game → Camera → Settings`
2. Настройте кривые и параметры в инспекторе

### 2. Настройка GameScope
```csharp
public class GameScope : LifetimeScope {
    public CameraSettingsSO cameraSettings; // Назначить в инспекторе
    // ...
}
```

### 3. API CameraModule
```csharp
// Инъекция через VContainer
[Inject] private CameraModule _camera;

// Следование за юнитами
_camera.EnableFollowMode();
_camera.DisableFollowMode();

// Фокус на точке (анимированный)
_camera.FocusOn(worldPosition);

// Фокус на точке (мгновенный)
_camera.FocusOnImmediate(worldPosition);

// Управление zoom
_camera.SetZoom(0.5f); // 0 = близко, 1 = далеко
_camera.SetZoom(0.5f, immediate: true);

// Управление поворотом
_camera.SetYaw(45f);

// Target Group
_camera.AddToTargetGroup(unitTransform);
_camera.RemoveFromTargetGroup(unitTransform);

// Получение состояния
var state = _camera.GetState();
Debug.Log($"Zoom: {state.NormalizedZoom}, Height: {state.CurrentHeight}");
```

## Калибровка

### Editor Window
`Tools → Game → Camera Calibrator`
- Live preview значений
- Quick presets (RTS Classic, Tactical, Cinematic)
- Редактирование кривых

### Runtime Calibrator
1. Добавьте `CameraRuntimeCalibrator` на любой GameObject
2. Назначьте `CameraSettingsSO`
3. Изменяйте параметры в Play Mode
4. Нажмите "Save to Settings" для сохранения

## Рекомендуемые настройки

### RTS Classic
- Height: 8-60m
- Pitch: 40° → 70°
- Pan Speed: 25

### Tactical (ближе к юнитам)
- Height: 5-40m
- Pitch: 30° → 80°
- Pan Speed: 20

### Cinematic (низкий угол)
- Height: 3-25m
- Pitch: 20° → 55°
- Pan Speed: 15

