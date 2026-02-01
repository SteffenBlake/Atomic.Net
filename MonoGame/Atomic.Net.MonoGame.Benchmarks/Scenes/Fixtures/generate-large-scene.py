import json
import random

# Generate a large scene with 1000 entities
entities = []

# Create a root entity
entities.append({
    "id": "root",
    "transform": {
        "position": {"x": 0, "y": 0, "z": 0},
        "rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
        "scale": {"x": 1, "y": 1, "z": 1}
    },
    "properties": {
        "type": "root-container"
    },
    "tags": ["container", "root"]
})

# Create 999 child entities with varied properties
random.seed(42)
for i in range(1, 1000):
    entity = {
        "id": f"entity-{i}",
        "transform": {
            "position": {
                "x": random.uniform(-1000, 1000),
                "y": random.uniform(-1000, 1000),
                "z": random.uniform(0, 100)
            },
            "rotation": {
                "x": random.uniform(-1, 1),
                "y": random.uniform(-1, 1),
                "z": random.uniform(-1, 1),
                "w": random.uniform(-1, 1)
            },
            "scale": {
                "x": random.uniform(0.5, 2.0),
                "y": random.uniform(0.5, 2.0),
                "z": random.uniform(0.5, 2.0)
            }
        },
        "properties": {
            "health": random.randint(1, 100),
            "mana": random.randint(0, 100),
            "level": random.randint(1, 50),
            "type": random.choice(["enemy", "ally", "neutral", "item", "decoration"]),
            "faction": random.choice(["red", "blue", "green", "yellow"])
        },
        "tags": []
    }
    
    # Add 1-3 random tags
    tag_count = random.randint(1, 3)
    possible_tags = ["enemy", "ally", "neutral", "interactive", "static", "animated", "collidable", "persistent"]
    entity["tags"] = random.sample(possible_tags, tag_count)
    
    # 80% of entities have the root as parent, 20% have another entity as parent
    if random.random() < 0.8:
        entity["parent"] = "@root"
    else:
        # Pick a random previous entity as parent (to ensure parent exists)
        parent_id = random.randint(1, max(1, i-1))
        entity["parent"] = f"@entity-{parent_id}"
    
    entities.append(entity)

# Create the scene
scene = {
    "entities": entities
}

# Write to file
with open('large-scene.json', 'w') as f:
    json.dump(scene, f, indent=2)

print(f"Generated scene with {len(entities)} entities")
