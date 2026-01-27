# Roadmap

This document tracks the high-level vision and progress for Atomic.Net. 

---

# CRITICAL: PLEASE READ THIS ENTIRE FILE, NOT JUST PORTIONS OF IT

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

### Persist data between scenes and game runs
- [x] Expand the behavior of the "Global" partition to do more than just store global entities. We need to have other types of stuff persisted between scenes (user inventory, experience points, game state overall, that sort of stuff)
- [x] Save and persist data into a LiteDB database between games (Save data) 

---

### Query + Command System
- [x] Can query entity state from data (distances, properties, etc.)
- [ ] Can modify entity state from data

This is the big and hard one, this will likely require us to develop an entire sql-esque or css-esque language to select from one
selector, and then pass that to an operator

This should HEAVILY be cooked on to really hash out a very performance approach. This is THE engine, a LOT of our work has been leading to this feature, this is what we have been laying down all the groundwork for.

---

### Tag system
- [x] TagBehavior needs to be implemented with TagRegistry, and it has to be wired up to properly work with the tag EntitySelector and be tracked in the SelectorRegistry

### Time-Based Logic
- [ ] Can script sequences over time (animations, transitions, cutscenes)

---

### State-Based Logic
- [ ] Can model complex behavior as state graphs (AI, multi-phase encounters)

---

### Sprite loading
- [ ] We need to start looking into both static textures and animations of sprites for entities that are data driven off behaviors

---

### Physics
- [ ] With all the pieces in place, we will want to make at least a half decent physics engine to go with everything, we can start with fairly basic RigidBody stuff, we can add more stuff later as needed but we can just start with things like making groups of entities into a whole body, anchoring joints and etc from 1 entity to another, the sort of basic stuff that is important for physics to baseline work
- [ ] careful consideration on how to efficiently make this wire into the CollisionEnter entity selector and CollisionExit entity selectors.
- [ ] We should be able to now actually make our first game using all the above!

---


## Success Criteria

The roadmap is **complete** when:
- A complete game can be built without writing C# gameplay code
- All game logic lives in JSON files
- Designers can iterate on gameplay by editing JSON (no recompile)
- Performance meets baseline (60+ FPS with 8000+ entities, zero GC allocations during gameplay)
