# Steel Tide: Voxel Asset Studio
# skeleton_generator_tpose.py - Generate T-Pose skeleton with actual bone/joint voxels

import numpy as np
from typing import Dict, Tuple

# Material IDs
BONE_MATERIAL = 12  # White/Beige bone voxels
JOINT_MATERIAL = 21  # Red joint voxels

def generate_tpose_biped_skeleton(grid_size: Tuple[int, int, int] = (18, 32, 8)) -> Tuple[np.ndarray, Dict]:
    """
    Generate a T-Pose biped skeleton made of actual bone and joint voxels.
    
    Grid layout (18×32×8):
    - Arms extended horizontally (T-Pose)
    - Single voxel wide bones
    - Single red voxel joints
    - Feet, legs, knees, thighs, hips, spine, shoulders, arms, neck, head
    
    Returns:
        (voxels, skeleton_data): Voxel array + skeleton metadata
    """
    voxels = np.zeros(grid_size, dtype=np.uint16)  # uint16 for .stasset compatibility
    
    center_x = grid_size[0] // 2  # 9
    center_z = grid_size[2] // 2  # 4
    
    bones = []
    joints = []
    bone_id = 0
    joint_id = 0
    
    # ===== LEGS (Left and Right) =====
    for side, x_offset in [('left', -1), ('right', 1)]:
        leg_x = center_x + x_offset
        
        # Feet (y=0 to y=2)
        for y in range(0, 3):
            voxels[leg_x, y, center_z] = BONE_MATERIAL
        
        # Ankle joint (y=3)
        voxels[leg_x, 3, center_z] = JOINT_MATERIAL
        joints.append({
            'id': joint_id,
            'name': f'{side}_ankle',
            'type': 'HINGE',
            'position': [int(leg_x), 3, int(center_z)],
            'axis': 'X',
            'min_angle': -30.0,
            'max_angle': 30.0
        })
        joint_id += 1
        
        # Lower leg (y=4 to y=9)
        for y in range(4, 10):
            voxels[leg_x, y, center_z] = BONE_MATERIAL
        
        # Knee joint (y=10)
        voxels[leg_x, 10, center_z] = JOINT_MATERIAL
        joints.append({
            'id': joint_id,
            'name': f'{side}_knee',
            'type': 'HINGE',
            'position': [int(leg_x), 10, int(center_z)],
            'axis': 'X',
            'min_angle': -150.0,
            'max_angle': 0.0
        })
        joint_id += 1
        
        # Upper leg/thigh (y=11 to y=15)
        for y in range(11, 16):
            voxels[leg_x, y, center_z] = BONE_MATERIAL
    
    # ===== PELVIS/HIPS (y=16) =====
    # Wider pelvis (3 voxels)
    for x in range(center_x - 1, center_x + 2):
        voxels[x, 16, center_z] = BONE_MATERIAL
    
    # Hip joints (left and right)
    for side, x_offset in [('left', -1), ('right', 1)]:
        hip_x = center_x + x_offset
        voxels[hip_x, 16, center_z] = JOINT_MATERIAL  # Override bone with joint
        joints.append({
            'id': joint_id,
            'name': f'{side}_hip',
            'type': 'BALL',
            'position': [int(hip_x), 16, int(center_z)],
            'max_angle_x': 45.0,
            'max_angle_y': 30.0,
            'max_angle_z': 30.0
        })
        joint_id += 1
    
    # ===== SPINE (y=17 to y=23) =====
    for y in range(17, 24):
        voxels[center_x, y, center_z] = BONE_MATERIAL
    
    # Mid-spine joint (y=20)
    voxels[center_x, 20, center_z] = JOINT_MATERIAL
    joints.append({
        'id': joint_id,
        'name': 'mid_spine',
        'type': 'BALL',
        'position': [int(center_x), 20, int(center_z)],
        'max_angle_x': 30.0,
        'max_angle_y': 20.0,
        'max_angle_z': 20.0
    })
    joint_id += 1
    
    # ===== SHOULDERS (y=24) =====
    # Wide shoulders (7 voxels for T-Pose arms)
    for x in range(center_x - 3, center_x + 4):
        voxels[x, 24, center_z] = BONE_MATERIAL
    
    # Shoulder joints (left and right)
    for side, x_offset in [('left', -3), ('right', 3)]:
        shoulder_x = center_x + x_offset
        voxels[shoulder_x, 24, center_z] = JOINT_MATERIAL
        joints.append({
            'id': joint_id,
            'name': f'{side}_shoulder',
            'type': 'BALL',
            'position': [int(shoulder_x), 24, int(center_z)],
            'max_angle_x': 180.0,
            'max_angle_y': 90.0,
            'max_angle_z': 90.0
        })
        joint_id += 1
    
    # ===== ARMS (Extended horizontally for T-Pose) =====
    for side, x_range in [('left', range(center_x - 8, center_x - 3)), 
                          ('right', range(center_x + 4, center_x + 9))]:
        # Upper arm
        for x in x_range[:3]:
            voxels[x, 24, center_z] = BONE_MATERIAL
        
        # Elbow joint
        elbow_x = x_range[3]
        voxels[elbow_x, 24, center_z] = JOINT_MATERIAL
        joints.append({
            'id': joint_id,
            'name': f'{side}_elbow',
            'type': 'HINGE',
            'position': [int(elbow_x), 24, int(center_z)],
            'axis': 'Z',
            'min_angle': 0.0,
            'max_angle': 150.0
        })
        joint_id += 1
        
        # Lower arm (forearm)
        for x in x_range[4:]:
            voxels[x, 24, center_z] = BONE_MATERIAL
    
    # ===== NECK (y=25 to y=27) =====
    for y in range(25, 28):
        voxels[center_x, y, center_z] = BONE_MATERIAL
    
    # Neck joint (y=27)
    voxels[center_x, 27, center_z] = JOINT_MATERIAL
    joints.append({
        'id': joint_id,
        'name': 'neck',
        'type': 'BALL',
        'position': [int(center_x), 27, int(center_z)],
        'max_angle_x': 60.0,
        'max_angle_y': 45.0,
        'max_angle_z': 45.0
    })
    joint_id += 1
    
    # ===== HEAD (y=28 to y=31) =====
    for y in range(28, 32):
        voxels[center_x, y, center_z] = BONE_MATERIAL
    
    # Generate skeleton metadata
    skeleton_data = {
        'bones': bones,  # Will be populated by auto-detection
        'joints': joints,
        'influence_map': {},
        'attachments': []
    }
    
    return voxels, skeleton_data
