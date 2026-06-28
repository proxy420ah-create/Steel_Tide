"""
Procedural Character Generation for Steel Tide
Generates humanoid voxel characters with proper proportions.
"""

import numpy as np

# Material IDs - Futuristic Border Colony Materials
# (must match Unity VoxelBits.cs and material_library.py)
class MaterialId:
    # Core Building Materials
    AIR = 0
    PREFAB_COMPOSITE = 1      # Tan modular building panels
    REGOLITH_CONCRETE = 2     # Dark brown local roads/foundations
    CONCRETE = 3              # Gray standard structures
    FLESH = 4                 # Pink organic matter
    DURASTEEL = 5             # Blue-gray reinforced metal
    
    # Terrain Materials
    REGOLITH = 6              # Brown alien soil
    XENOFLORA = 7             # Teal alien plants
    BASALT = 8                # Dark gray volcanic rock
    WOOD = 9                  # Tan imported lumber
    TRANSPARENT_ALUMINUM = 10 # Light blue sci-fi glass
    
    # Utility/Clothing
    UNIFORM = 11              # Dark green military uniform
    
    # Damage States
    DAMAGED_CONCRETE = 13
    DAMAGED_STEEL = 14
    DAMAGED_ARMOR = 15
    
    # Advanced Materials
    ABLATIVE_PLATING = 16     # Charcoal heat-resistant armor
    REACTIVE_ARMOR = 17       # Gunmetal explosive tiles
    FOAM_CRETE = 18           # Light gray quick-deploy foam
    NANOMESH_FABRIC = 19      # Dark blue smart fabric
    PLASTEEL_PANELS = 20      # Medium gray plastic-metal hybrid

def generate_humanoid(
    height=32,
    body_material=MaterialId.UNIFORM,
    head_material=MaterialId.FLESH,
    limb_material=MaterialId.PLASTEEL_PANELS,
    armor_material=MaterialId.PLASTEEL_PANELS,
    seed=None,
    locked_parts=None
):
    """
    Generate a humanoid character voxel model with seed-based variations.
    
    Proportions based on realistic human anatomy (scaled up for visibility):
    - Height: 32 voxels (4m tall in voxel world)
    - Head: ~4 voxels (0.5m)
    - Torso: ~12 voxels (1.5m)
    - Legs: ~16 voxels (2m)
    - Width: ~10 voxels (1.25m)
    
    Seed-based variations:
    - Body width/depth (±10%)
    - Limb thickness (±15%)
    - Head size (±10%)
    - Shoulder width (±20%)
    
    Args:
        height: Total height in voxels (default 32 = 4m)
        body_material: Material ID for torso
        head_material: Material ID for head
        limb_material: Material ID for arms/legs
        armor_material: Material ID for armor plating
        seed: Random seed for variations (None = random)
        locked_parts: Dict of parts to preserve from previous generation
                     {'head': array, 'torso': array, 'legs': array, 'arms': array}
    
    Returns:
        3D numpy array (uint16) with voxel data (Y is up)
    """
    if seed is not None:
        np.random.seed(seed)
    
    # Base proportions (based on height)
    head_height = max(4, height // 8)
    torso_height = max(12, height // 3)
    leg_height = height - head_height - torso_height
    
    # Base width/depth proportions (larger for visibility)
    base_width = max(10, height // 3)
    base_depth = max(6, height // 5)
    
    # Seed-based variations (±10% for body dimensions)
    if seed is not None:
        width_variation = np.random.uniform(0.9, 1.1)
        depth_variation = np.random.uniform(0.9, 1.1)
        limb_variation = np.random.uniform(0.85, 1.15)
        head_variation = np.random.uniform(0.9, 1.1)
        shoulder_variation = np.random.uniform(0.8, 1.2)
    else:
        width_variation = 1.0
        depth_variation = 1.0
        limb_variation = 1.0
        head_variation = 1.0
        shoulder_variation = 1.0
    
    width = max(8, int(base_width * width_variation))
    depth = max(5, int(base_depth * depth_variation))
    
    # Create grid (X, Y, Z) where Y is up
    grid = np.zeros((width, height, depth), dtype=np.uint16)
    
    # Build from bottom up
    y_pos = 0
    
    # === LEGS ===
    leg_width = max(2, int((width // 3) * limb_variation))
    leg_depth = max(2, int((depth // 2) * limb_variation))
    
    # Left leg
    left_leg_x = width // 4
    grid[left_leg_x:left_leg_x+leg_width, y_pos:y_pos+leg_height, :leg_depth] = limb_material
    
    # Right leg
    right_leg_x = width - width // 4 - leg_width
    grid[right_leg_x:right_leg_x+leg_width, y_pos:y_pos+leg_height, :leg_depth] = limb_material
    
    y_pos += leg_height
    
    # === TORSO ===
    torso_width = width - 2
    torso_depth = depth - 1
    torso_x = 1
    torso_z = 0
    
    grid[torso_x:torso_x+torso_width, y_pos:y_pos+torso_height, torso_z:torso_z+torso_depth] = body_material
    
    # Add shoulders (wider than torso, with variation)
    shoulder_y = y_pos + torso_height - 2
    shoulder_width = max(width, int(width * shoulder_variation))
    shoulder_offset = max(0, (shoulder_width - width) // 2)
    if shoulder_offset > 0:
        # Extend shoulders beyond body width
        grid[:, shoulder_y:shoulder_y+2, :] = body_material
    else:
        grid[:, shoulder_y:shoulder_y+2, :] = body_material
    
    y_pos += torso_height
    
    # === HEAD ===
    head_width = max(3, int((width // 2) * head_variation))
    head_depth = max(3, int((depth // 2) * head_variation))
    head_x = (width - head_width) // 2
    head_z = (depth - head_depth) // 2
    
    grid[head_x:head_x+head_width, y_pos:y_pos+head_height, head_z:head_z+head_depth] = head_material
    
    # === ARMS ===
    arm_width = max(2, int((width // 5) * limb_variation))  # Thicker arms
    arm_length = torso_height - 2
    arm_depth = max(2, int((depth // 3) * limb_variation))  # Thicker arms
    
    # Left arm
    left_arm_x = 0
    arm_y = y_pos - torso_height + 1
    grid[left_arm_x:left_arm_x+arm_width, arm_y:arm_y+arm_length, :arm_depth] = limb_material
    
    # Right arm
    right_arm_x = width - arm_width
    grid[right_arm_x:right_arm_x+arm_width, arm_y:arm_y+arm_length, :arm_depth] = limb_material
    
    # === PART LOCKING SYSTEM ===
    # If locked_parts is provided, composite locked parts from previous generation
    if locked_parts is not None:
        part_data = locked_parts.get('part_data', {})
        locks = locked_parts.get('locks', {})
        
        # If entire character is locked, return the stored character
        if locks.get('entire', False) and 'entire' in part_data:
            return part_data['entire'].copy()
        
        # Composite individual locked parts
        # Calculate Y positions for each part
        legs_y_start = 0
        legs_y_end = leg_height
        torso_y_start = leg_height
        torso_y_end = leg_height + torso_height
        head_y_start = leg_height + torso_height
        head_y_end = leg_height + torso_height + head_height
        arms_y_start = torso_y_start + 1
        arms_y_end = torso_y_end - 1
        
        # Lock legs
        if locks.get('legs', False) and 'legs' in part_data:
            locked_legs = part_data['legs']
            # Copy locked legs into new grid (resize if needed)
            min_width = min(grid.shape[0], locked_legs.shape[0])
            min_depth = min(grid.shape[2], locked_legs.shape[2])
            grid[:min_width, legs_y_start:legs_y_end, :min_depth] = locked_legs[:min_width, :, :min_depth]
        
        # Lock torso
        if locks.get('torso', False) and 'torso' in part_data:
            locked_torso = part_data['torso']
            min_width = min(grid.shape[0], locked_torso.shape[0])
            min_depth = min(grid.shape[2], locked_torso.shape[2])
            grid[:min_width, torso_y_start:torso_y_end, :min_depth] = locked_torso[:min_width, :, :min_depth]
        
        # Lock head
        if locks.get('head', False) and 'head' in part_data:
            locked_head = part_data['head']
            min_width = min(grid.shape[0], locked_head.shape[0])
            min_depth = min(grid.shape[2], locked_head.shape[2])
            # Center the locked head
            head_offset_x = (grid.shape[0] - locked_head.shape[0]) // 2
            head_offset_z = (grid.shape[2] - locked_head.shape[2]) // 2
            grid[head_offset_x:head_offset_x+locked_head.shape[0], 
                 head_y_start:head_y_end, 
                 head_offset_z:head_offset_z+locked_head.shape[2]] = locked_head
        
        # Lock arms
        if locks.get('arms', False) and 'arms' in part_data:
            locked_arms = part_data['arms']
            min_width = min(grid.shape[0], locked_arms.shape[0])
            min_depth = min(grid.shape[2], locked_arms.shape[2])
            grid[:min_width, arms_y_start:arms_y_end, :min_depth] = locked_arms[:min_width, :, :min_depth]
    
    return grid


def extract_character_parts(grid, height):
    """
    Extract individual parts from a generated character for locking.
    
    Args:
        grid: Full character voxel grid
        height: Character height (to calculate part positions)
    
    Returns:
        Dictionary with extracted part data:
        {
            'head': numpy array,
            'torso': numpy array,
            'legs': numpy array,
            'arms': numpy array,
            'entire': numpy array (full character)
        }
    """
    head_height = max(4, height // 8)
    torso_height = max(12, height // 3)
    leg_height = height - head_height - torso_height
    
    # Calculate Y positions
    legs_y_start = 0
    legs_y_end = leg_height
    torso_y_start = leg_height
    torso_y_end = leg_height + torso_height
    head_y_start = leg_height + torso_height
    head_y_end = leg_height + torso_height + head_height
    arms_y_start = torso_y_start + 1
    arms_y_end = torso_y_end - 1
    
    return {
        'legs': grid[:, legs_y_start:legs_y_end, :].copy(),
        'torso': grid[:, torso_y_start:torso_y_end, :].copy(),
        'head': grid[:, head_y_start:head_y_end, :].copy(),
        'arms': grid[:, arms_y_start:arms_y_end, :].copy(),
        'entire': grid.copy()
    }


def generate_mech_walker(
    height=24,
    body_material=MaterialId.DURASTEEL,
    armor_material=MaterialId.REACTIVE_ARMOR,
    weapon_material=MaterialId.ABLATIVE_PLATING,
    seed=None
):
    """
    Generate a bipedal mech walker.
    
    Larger and more armored than humanoid.
    - Height: 24 voxels (3m tall)
    - Bulkier proportions
    - Weapon mounts on arms
    
    Args:
        height: Total height in voxels (default 24 = 3m)
        body_material: Material ID for main body
        armor_material: Material ID for armor plating
        weapon_material: Material ID for weapons
        seed: Random seed for variations
    
    Returns:
        3D numpy array (uint16) with voxel data (Y is up)
    """
    if seed is not None:
        np.random.seed(seed)
    
    # Mech proportions (stockier than human)
    head_height = max(3, height // 8)
    torso_height = max(10, height // 2)
    leg_height = height - head_height - torso_height
    
    width = max(12, height // 2)
    depth = max(8, height // 3)
    
    grid = np.zeros((width, height, depth), dtype=np.uint16)
    
    y_pos = 0
    
    # === LEGS (thick and armored) ===
    leg_width = width // 3
    leg_depth = depth // 2
    
    # Left leg
    left_leg_x = width // 6
    grid[left_leg_x:left_leg_x+leg_width, y_pos:y_pos+leg_height, :leg_depth] = armor_material
    
    # Right leg
    right_leg_x = width - width // 6 - leg_width
    grid[right_leg_x:right_leg_x+leg_width, y_pos:y_pos+leg_height, :leg_depth] = armor_material
    
    # Add knee joints
    knee_y = y_pos + leg_height // 2
    grid[left_leg_x:left_leg_x+leg_width, knee_y:knee_y+2, :leg_depth] = body_material
    grid[right_leg_x:right_leg_x+leg_width, knee_y:knee_y+2, :leg_depth] = body_material
    
    y_pos += leg_height
    
    # === TORSO (bulky, armored) ===
    torso_width = width - 2
    torso_depth = depth - 1
    
    grid[1:1+torso_width, y_pos:y_pos+torso_height, :torso_depth] = body_material
    
    # Add armor plating (outer layer)
    grid[0:2, y_pos:y_pos+torso_height, :] = armor_material
    grid[width-2:width, y_pos:y_pos+torso_height, :] = armor_material
    grid[:, y_pos:y_pos+2, :] = armor_material
    
    y_pos += torso_height
    
    # === HEAD (cockpit) ===
    head_width = width // 2
    head_depth = depth // 2
    head_x = (width - head_width) // 2
    
    grid[head_x:head_x+head_width, y_pos:y_pos+head_height, :head_depth] = armor_material
    
    # === WEAPON ARMS ===
    arm_width = width // 4
    arm_length = torso_height
    arm_depth = depth // 3
    
    # Left weapon arm
    arm_y = y_pos - torso_height
    grid[0:arm_width, arm_y:arm_y+arm_length, :arm_depth] = weapon_material
    
    # Right weapon arm
    grid[width-arm_width:width, arm_y:arm_y+arm_length, :arm_depth] = weapon_material
    
    return grid


def generate_simple_humanoid(seed=None):
    """
    Quick humanoid with default settings (2m tall).
    
    Materials (realistic organic character):
    - Body: UNIFORM (dark green) - Clothing/armor
    - Head: FLESH (pink) - Skin
    - Limbs: FLESH (pink) - Exposed skin on arms/legs
    """
    return generate_humanoid(
        height=16,
        body_material=MaterialId.UNIFORM,  # Dark green uniform
        head_material=MaterialId.FLESH,    # Pink flesh (face)
        limb_material=MaterialId.FLESH,    # Pink flesh (arms/legs)
        seed=seed
    )


def generate_tall_humanoid(seed=None):
    """Taller humanoid (2.5m tall)"""
    return generate_humanoid(height=20, seed=seed)


def generate_combat_mech(seed=None):
    """Standard combat mech (3m tall)"""
    return generate_mech_walker(height=24, seed=seed)


def generate_heavy_mech(seed=None):
    """Heavy mech (4m tall)"""
    return generate_mech_walker(height=32, seed=seed)
