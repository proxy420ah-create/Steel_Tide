# Steel Tide: Voxel Asset Studio
# material_library.py - Material definitions and colors
# MUST MATCH Unity VoxelBits.cs and VoxelRenderer.cs!

# Material IDs - Complete synchronized system
# Theme: Distant Planet Border Colony (Futuristic Sci-Fi)
MATERIALS = {
    # Core Building Materials (0-5)
    0: {"name": "Air", "color": (0.0, 0.0, 0.0, 0.0)},                      # Transparent
    1: {"name": "Prefab Composite", "color": (0.65, 0.6, 0.55, 1.0)},       # Tan - Modular colony buildings
    2: {"name": "Regolith Concrete", "color": (0.25, 0.2, 0.18, 1.0)},      # Dark brown - Local roads/foundations
    3: {"name": "Concrete", "color": (0.5, 0.5, 0.5, 1.0)},                 # Gray - Standard structures
    4: {"name": "Flesh", "color": (1.0, 0.6, 0.6, 1.0)},                    # Pink - Organic matter
    5: {"name": "Durasteel", "color": (0.28, 0.3, 0.32, 1.0)},              # Blue-gray - Reinforced metal alloy
    
    # Terrain Materials (6-10)
    6: {"name": "Regolith", "color": (0.4, 0.3, 0.2, 1.0)},                 # Brown - Alien planet soil
    7: {"name": "Xenoflora", "color": (0.15, 0.55, 0.5, 1.0)},              # Teal - Alien plant life
    8: {"name": "Basalt", "color": (0.25, 0.25, 0.27, 1.0)},                # Dark gray - Volcanic rock
    9: {"name": "Wood", "color": (0.6, 0.4, 0.2, 1.0)},                     # Tan - Imported lumber
    10: {"name": "Transparent Aluminum", "color": (0.85, 0.92, 1.0, 0.4)},  # Light blue - Sci-fi transparent metal
    
    # Utility/Clothing (11-12)
    11: {"name": "Uniform", "color": (0.2, 0.3, 0.2, 1.0)},                 # Dark green - Military uniform
    12: {"name": "Bone", "color": (0.9, 0.86, 0.78, 1.0)},                   # White/Beige - Skeleton bones (indestructible)
    
    # Damage States (13-15)
    13: {"name": "Damaged Concrete", "color": (0.85, 0.15, 0.15, 1.0)},     # Crimson - Battle damage
    14: {"name": "Damaged Steel", "color": (0.9, 0.4, 0.1, 1.0)},           # Orange - Heat damage
    15: {"name": "Damaged Armor", "color": (0.8, 0.2, 0.2, 1.0)},           # Dark red - Heavy damage
    
    # Advanced Materials (16-20)
    16: {"name": "Ablative Plating", "color": (0.15, 0.16, 0.18, 1.0)},     # Charcoal - Heat-resistant armor
    17: {"name": "Reactive Armor", "color": (0.35, 0.38, 0.36, 1.0)},       # Gunmetal - Explosive tiles
    18: {"name": "Foam-Crete", "color": (0.75, 0.75, 0.78, 1.0)},           # Light gray - Quick-deploy foam
    19: {"name": "Nanomesh Fabric", "color": (0.2, 0.25, 0.3, 1.0)},        # Dark blue - Smart fabric
    20: {"name": "Plasteel Panels", "color": (0.45, 0.48, 0.5, 1.0)},       # Medium gray - Plastic-metal hybrid
    
    # Skeleton Materials (21)
    21: {"name": "Joint", "color": (1.0, 0.25, 0.25, 1.0)},                  # Red - Skeleton joints (indestructible)
}

# Default paint materials (all available materials for painting)
DEFAULT_MATERIALS = [
    0,   # Air
    1,   # Prefab Composite
    2,   # Regolith Concrete
    3,   # Concrete
    4,   # Flesh
    5,   # Durasteel
    6,   # Regolith
    7,   # Xenoflora
    8,   # Basalt
    9,   # Wood
    10,  # Transparent Aluminum
    11,  # Uniform
    13,  # Damaged Concrete
    14,  # Damaged Steel
    15,  # Damaged Armor
    16,  # Ablative Plating
    17,  # Reactive Armor
    18,  # Foam-Crete
    19,  # Nanomesh Fabric
    20,  # Plasteel Panels
    12,  # Bone
    21,  # Joint
]

def get_material_name(material_id):
    """Get material name from ID"""
    return MATERIALS.get(material_id, {}).get("name", f"Unknown ({material_id})")

def get_material_color(material_id):
    """Get RGBA color for material (0-1 range)"""
    return MATERIALS.get(material_id, {}).get("color", (1.0, 1.0, 1.0, 1.0))

def get_material_color_255(material_id):
    """Get RGBA color for material (0-255 range for Qt)"""
    color = get_material_color(material_id)
    return tuple(int(c * 255) for c in color)
