# Unity Conventions & Gotchas

> Project-specific notes on Unity limitations and conventions.

---

## Folder Restrictions

### `Editor/` Folder

**CRITICAL:** Scripts in `Editor/` folders are **editor-only** and stripped from builds.

- ✅ Custom Editors (`[CustomEditor]`)
- ✅ Editor Windows (`EditorWindow`)
- ✅ Property Drawers (`[CustomPropertyDrawer]`)
- ✅ Menu items, editor-only utilities
- ❌ **MonoBehaviours that need to be added to GameObjects**
- ❌ ScriptableObjects used at runtime
- ❌ Any runtime code

**Solution for editor-helper MonoBehaviours:**
```
Scripts/
├── Editor/              — CustomEditors, EditorWindows ONLY
├── EditorUtilities/     — MonoBehaviours for editor workflows
│                          (wrap editor-only code in #if UNITY_EDITOR)
└── Runtime/             — All runtime code
```

### `Resources/` Folder

- Loaded via `Resources.Load<T>()`
- Included in build even if not referenced
- Use sparingly — prefer Addressables for large assets

### `Plugins/` Folder

- Compiled before other scripts
- Use for third-party native plugins

---

## Assembly Definitions

If using .asmdef files:
- Editor asmdefs must reference only editor assemblies
- Runtime asmdefs cannot reference editor asmdefs
- `Editor/` folder auto-excludes from runtime even without asmdef

---

*Add more gotchas as discovered.*
