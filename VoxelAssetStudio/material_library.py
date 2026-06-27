# Steel Tide: Voxel Asset Studio
# material_library.py - Material definitions and colors

# Material IDs (must match Unity VoxelBits.cs!)
MATERIALS = {
    0: {"name": "Air", "color": (0.0, 0.0, 0.0, 0.0)},              # Transparent
    2: {"name": "Chobham Armor", "color": (0.6, 0.4, 0.2, 1.0)},    # Brown
    3: {"name": "Concrete", "color": (0.5, 0.5, 0.5, 1.0)},         # Gray
    4: {"name": "Flesh", "color": (1.0, 0.6, 0.6, 1.0)},            # Pink
    5: {"name": "Steel", "color": (0.3, 0.3, 0.3, 1.0)},            # Dark gray
    13: {"name": "Damaged Concrete", "color": (1.0, 0.2, 0.2, 1.0)}, # Red
    14: {"name": "Damaged Steel", "color": (1.0, 0.5, 0.0, 1.0)},   # Orange
    15: {"name": "Damaged Armor", "color": (0.8, 0.3, 0.1, 1.0)},   # Dark orange
}

# Default paint materials (most commonly used)
DEFAULT_MATERIALS = [0, 3, 5, 2, 13, 14]

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
