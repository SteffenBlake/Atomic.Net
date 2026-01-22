# Roadmap

This document tracks the high-level vision and progress for Atomic.Net. 

---

## Vision

Build a **data-driven, zero runtime allocation game engine** where entire games can be defined in JSON files, designers can edit gameplay without touching C# code, and performance is baseline excellent (60+ FPS with 8000+ entities).

---

## Core Philosophy

**Data-Driven:** Game logic lives in JSON, not C#.  The engine provides primitives, but game-specific logic is authored in data files.

**Zero-Allocation:** All allocations happen at load time.  Gameplay code runs allocation-free to prevent GC pauses.

**Test-Driven:** Integration tests validate the full data pipeline.  Tests define correctness. 

**Entity-Component-System:** Entities are lightweight indices.  Components are structs in cache-friendly arrays.  Systems process via events.

---

## Milestones

### M0: Core ECS Foundation
- [x] Basic entity and component system working

---

### M1: Data Pipeline
- [x] Scenes can load from JSON files, the entire game can be defined as a directory of json files

---

### Entity References
- [x] Entities can reference each other in JSON very efficiently and fast

---

### Entity Property Bag
- [x] Entities have a generic Property behavior that lets users add arbitrary extra properties to any entity, that dont do anything on their own, but is effectively a dictionary bag

---

### Persist data between scenes
- [ ] Expand the behavior of the "Persistent" partition to do more than just store persistent entities. We need to have other types of stuff persisted between scenes (user inventory, experience points, game state overall, that sort of stuff)

---

### Query System
- [ ] Can query entity state from data (distances, properties, etc.)

---

### Command System
- [ ] Can modify entity state from data (change scenes, fire events, etc.)

---

### Reactive Logic
- [ ] Can connect queries to commands in data ("when X, do Y")

---

### Time-Based Logic
- [ ] Can script sequences over time (animations, transitions, cutscenes)

---

### State-Based Logic
- [ ] Can model complex behavior as state graphs (AI, multi-phase encounters)

---

### Sprite loading
- [ ] We need to start looking into both static textures and animations of sprites for entities that are data driven off behaviors

---

## Success Criteria

The roadmap is **complete** when:
- A complete game can be built without writing C# gameplay code
- All game logic lives in JSON files
- Designers can iterate on gameplay by editing JSON (no recompile)
- Performance meets baseline (60+ FPS with 8000+ entities, zero GC allocations during gameplay)
