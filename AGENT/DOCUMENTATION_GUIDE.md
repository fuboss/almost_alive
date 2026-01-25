# Documentation Optimization Recommendations

## Current Setup ✅

```
AGENT/
├── PROJECT_INDEX.md           # Main index - architecture, services, recent features
├── DI_REGISTRATION.md         # (DELETE - merged into PROJECT_INDEX)
└── quick-reference/
    └── RESOURCES.md           # Prefabs, configs, addressables
```

## Recommended Structure

```
AGENT/
├── PROJECT_INDEX.md                    # High-level overview, directory tree, services
├── quick-reference/
│   ├── RESOURCES.md                    # Prefabs, SOs, addressables
│   ├── COMMON_TASKS.md                 # How-to snippets (add service, create SO, etc)
│   └── TROUBLESHOOTING.md              # Common issues & solutions
├── features/
│   ├── BUILDING_SYSTEM.md              # Building feature deep dive
│   ├── EXPANSION_SYSTEM.md             # Expansion feature docs
│   ├── VISUAL_MANAGEMENT.md            # Visuals feature docs
│   └── [feature-name].md               # One file per major feature
└── session-logs/
    └── YYYY-MM-DD_topic.md             # Conversation summaries (optional)
```

## Optimization Guidelines

### 1. Auto-Update Triggers

**When to update docs:**
- ✅ New service created → Update PROJECT_INDEX (services table) + DI example
- ✅ New prefab/SO created → Update RESOURCES.md
- ✅ New addressable label → Update RESOURCES.md
- ✅ New feature completed → Create features/FEATURE.md
- ✅ Architecture change → Update PROJECT_INDEX.md

**Template for new service:**
```markdown
### [ServiceName] (in PROJECT_INDEX.md services table)
| Service | Purpose |
|---------|---------|
| StructureVisualsModule | Decoration visibility management |
```

### 2. Quick Reference Philosophy

**RESOURCES.md should answer:**
- "Where is GameScope?"
- "Where are structure definitions stored?"
- "What addressable labels exist?"
- "What's the keyed dependency for ghost material?"

**COMMON_TASKS.md should answer:**
- "How do I add a new service?"
- "How do I create a new StructureDefinitionSO?"
- "How do I register an addressable?"

**Keep it SCANNABLE:**
- Use tables for lists
- Use code blocks for examples
- Use ✅ for confirmed info, 📁 for directories, ⚠️ for important notes

### 3. Feature Documentation

**When feature is complex (>3 files):**
Create `features/FEATURE_NAME.md`:
```markdown
# Feature Name

## Overview
What it does, why it exists

## Architecture
Components, services, data flow

## Usage Examples
Code snippets, common scenarios

## Integration Points
Where it hooks into other systems

## Files
List of all related files
```

**Example:** EXPANSION_SYSTEM.md, VISUAL_MANAGEMENT.md

### 4. Indexing Strategy

**PROJECT_INDEX.md - The "Table of Contents":**
- Directory structure (high-level)
- Services table
- Key components
- Recent features (last 5)
- Links to detailed feature docs

**Don't duplicate - LINK:**
```markdown
## Visual Management
See [features/VISUAL_MANAGEMENT.md](features/VISUAL_MANAGEMENT.md) for details.
```

### 5. Session Artifacts

**Option A: Embed in feature docs**
- Add "Implementation Log" section to feature docs
- Keep chronological notes

**Option B: Separate session logs**
- `session-logs/2026-01-26_visuals-system.md`
- Link from feature doc
- Useful for "why we made this decision"

### 6. Maintenance Rules

**Every Session End:**
- [ ] Update PROJECT_INDEX.md with new services/components
- [ ] Update RESOURCES.md with new prefabs/SOs
- [ ] Create/update feature doc if major addition
- [ ] Mark TODOs in docs for incomplete info

**Monthly Review:**
- [ ] Remove outdated TODOs
- [ ] Consolidate duplicate info
- [ ] Archive old session logs

## Recommended Actions NOW

1. **Delete DI_REGISTRATION.md** (info merged into PROJECT_INDEX)
2. **Create COMMON_TASKS.md** with snippets
3. **Create features/VISUAL_MANAGEMENT.md** from VISUALS_DESIGN.md
4. **Create features/EXPANSION_SYSTEM.md** from expansion notes

## Tools Integration (Future)

**AI Assistant hints:**
```markdown
<!-- AI: Always check RESOURCES.md before asking about prefabs -->
<!-- AI: Update PROJECT_INDEX services table when adding service -->
```

**Git hooks (optional):**
- Pre-commit: Check if new .cs service file → remind to update docs
- Post-merge: Check AGENT/ for conflicts

## Example Workflow

**User:** "Add chimney decoration system"

**AI Actions:**
1. Read PROJECT_INDEX.md (understand architecture)
2. Read RESOURCES.md (find relevant prefabs)
3. Read features/VISUAL_MANAGEMENT.md (understand visuals system)
4. Implement feature
5. Update PROJECT_INDEX.md services table
6. Update features/VISUAL_MANAGEMENT.md with new component
7. Create example in COMMON_TASKS.md ("How to add decoration")

## Anti-Patterns to Avoid

❌ **Don't:** Duplicate info in multiple files  
✅ **Do:** Link to single source of truth

❌ **Don't:** Write novel-length docs  
✅ **Do:** Keep it scannable (tables, bullets, code blocks)

❌ **Don't:** Document implementation details  
✅ **Do:** Document "why" and "how to use"

❌ **Don't:** Create docs that are never read  
✅ **Do:** Make docs essential for workflow (DI registration, prefab paths)

## Success Metrics

Documentation is working if:
- ✅ AI can find prefab paths without asking
- ✅ AI can add services without asking where to register
- ✅ AI can understand feature without reading all source files
- ✅ User can onboard new developer with AGENT/ docs alone
