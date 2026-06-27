# Steel Tide: Voxel Asset Studio
# stasset_io.py - Import/Export .stasset files

import struct
import numpy as np

def load_stasset(filepath):
    """
    Load .stasset file and return voxel data.
    
    Args:
        filepath: Path to .stasset file
        
    Returns:
        tuple: (voxels, dims) where voxels is 3D numpy array (uint16)
               and dims is (width, height, depth)
    """
    with open(filepath, 'rb') as f:
        # Read header (16 bytes)
        magic = f.read(4)
        if magic != b'STAS':
            raise ValueError(f"Not a valid .stasset file! Magic: {magic}")
            
        version = struct.unpack('B', f.read(1))[0]
        if version != 1:
            raise ValueError(f"Unsupported version: {version}")
            
        f.read(1)  # flags (reserved)
        
        width = struct.unpack('<H', f.read(2))[0]   # Little-endian uint16
        height = struct.unpack('<H', f.read(2))[0]
        depth = struct.unpack('<H', f.read(2))[0]
        f.read(4)  # reserved
        
        # Read voxel data
        count = width * height * depth
        voxel_bytes = f.read(count * 2)
        
        if len(voxel_bytes) != count * 2:
            raise ValueError(f"Truncated file! Expected {count * 2} bytes, got {len(voxel_bytes)}")
        
        # Convert to numpy array (little-endian uint16)
        voxels = np.frombuffer(voxel_bytes, dtype='<u2')
        voxels = voxels.reshape((width, height, depth))
        
    print(f"✅ Loaded {filepath}")
    print(f"   Dimensions: {width}×{height}×{depth}")
    print(f"   Total voxels: {count:,}")
    print(f"   Non-air voxels: {np.count_nonzero(voxels):,}")
    
    return voxels, (width, height, depth)


def save_stasset(filepath, voxels):
    """
    Save voxel data to .stasset file.
    
    Args:
        filepath: Output path
        voxels: 3D numpy array (uint16)
    """
    dims = voxels.shape
    
    with open(filepath, 'wb') as f:
        # Header (16 bytes)
        f.write(b'STAS')                    # Magic
        f.write(struct.pack('B', 1))        # Version
        f.write(struct.pack('B', 0))        # Flags (reserved)
        f.write(struct.pack('<H', dims[0])) # Width
        f.write(struct.pack('<H', dims[1])) # Height
        f.write(struct.pack('<H', dims[2])) # Depth
        f.write(struct.pack('<I', 0))       # Reserved (4 bytes)
        
        # Voxel data (little-endian uint16)
        voxel_data = voxels.flatten().astype('<u2')
        f.write(voxel_data.tobytes())
    
    count = dims[0] * dims[1] * dims[2]
    print(f"✅ Saved {filepath}")
    print(f"   Dimensions: {dims[0]}×{dims[1]}×{dims[2]}")
    print(f"   Total voxels: {count:,}")
    print(f"   File size: {len(voxel_data.tobytes()) + 16:,} bytes")
