# RimWorld Systems Reference

Quick reference for core RimWorld mechanics to inform "Almost Alive" design.

## Colonist Needs System

**Basic Needs** (constantly draining):
- Food (hunger) → Eat meals
- Rest → Sleep in bed
- Recreation → Joy activities
- Comfort → Quality furniture

**Mood System**:
- Mood = sum of all mood modifiers (thoughts)
- Thoughts have duration and stack rules
- Mental break thresholds: minor (25%), major (15%), extreme (5%)
- Break types depend on traits and situation

**Key Insight**: Simple needs + complex thought interactions = emergent behavior

## Work Priority System

- 4-level priority (1=urgent, 4=low, unchecked=never)
- Jobs queued by priority, then distance
- Colonists pick nearest valid job in highest priority category

**Pareto Note**: This simple system drives 90% of colony automation

## Traits & Skills

**Traits**: 2-3 per colonist, define personality
- Some enable/disable work types
- Affect mood modifiers
- Create social dynamics (conflicts, bonds)

**Skills**: 12 skills, 0-20 levels
- Passion levels affect learning speed (2x, 4x)
- Decay over time if unused
- Quality of output depends on skill

## Social System

- Opinion scores (-100 to +100)
- Interactions generate opinion changes
- Relationships: rivals, friends, lovers, family
- Social fights, insults, romances emerge from opinion + traits

## Storyteller System

**Core Loop**:
1. Track colony "points" (wealth, population, tech)
2. Select event from pool based on points
3. Apply timing rules (minimum gaps, threat cycles)
4. Scale event difficulty to colony strength

**Event Categories**:
- Threats (raids, infestations, mechanoids)
- Opportunities (traders, wanderers, quests)
- Environmental (weather, toxic fallout)
- Social (visitors, caravans)

**Key Design**: Player never knows exact trigger — feels organic

## Room System

- Rooms defined by enclosed walls + doors
- Room stats: impressiveness, cleanliness, space, beauty
- Room types auto-detected by furniture (bed → bedroom, table → dining room)
- Affects mood modifiers and work speed

## Schedule System

- 24-hour day divided into blocks
- Blocks: sleep, work, recreation, anything
- Simple but crucial for colony rhythm

---

*Use this as quick reference when designing analogous systems for Almost Alive*
