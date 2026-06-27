# Voxel Asset Studio - Design Document

**Project:** Steel Tide: First Device  
**System:** Standalone Voxel Editor & Asset Generator  
**Version:** 1.0 (Simplified)  
**Date:** June 27, 2026  
**Status:** Ready to Build

---

## 🎯 Win Condition

**Double-click `VoxelStudio.bat` → App opens → HighDensity32.stasset loads → Start painting!**

Just like your Kalshi Dashboard launcher!

---

## 🖥️ Application Overview

### Standalone Python App (Not Unity Editor!)

```
Voxel Asset Studio
├─ Built with PyQt6 (familiar UI framework)
├─ OpenGL 3D viewport (reuse RGVS knowledge)
├─ Real-time voxel rendering
├─ Paint/modify tools
└─ Export to .stasset → Unity imports it!
```

**Why Standalone?**
- ✅ Fast iteration (no Unity recompilation)
- ✅ Familiar tech stack (PyQt6 + OpenGL)
- ✅ Reuse Kalshi Dashboard code patterns
- ✅ Independent development workflow
- ✅ Can run while Unity is open!

---

## 🎨 UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Voxel Asset Studio - HighDensity32.stasset      [_][□][X]  │
├─────────────────────────────────────────────────────────────┤
│ File  Edit  View  Generate  Export                          │
├──────────────┬──────────────────────────────────────────────┤
│              │                                               │
│  Tool Panel  │         3D Viewport (OpenGL)                 │
│              │                                               │
│ 🖌️ Paint     │      ┌─────────────────────┐                │
│ 🧊 Add       │      │                     │                │
│ 🗑️ Delete    │      │  [Rotating voxel    │                │
│ 📦 Fill      │      │   model with        │                │
│              │      │   orbit camera]     │                │
│ ─────────    │      │                     │                │
│              │      └─────────────────────┘                │
│ Materials:   │                                               │
│ ⬜ Air       │      Controls: Left-drag = Orbit             │
│ 🟫 Concrete  │                Right-drag = Pan              │
│ ⬛ Steel     │                Wheel = Zoom                  │
│ 🟦 Armor     │                Click voxel = Paint           │
│ 🟥 Damaged   │                                               │
│              │                                               │
│ ─────────    │                                               │
│              │                                               │
│ Brush Size:  │                                               │
│ ○ 1 voxel    │                                               │
│ ◉ 3×3×3      │                                               │
│ ○ 5×5×5      │                                               │
│              │                                               │
├──────────────┴──────────────────────────────────────────────┤
│ Grid: 32×32×32 | Voxels: 8,432 | Memory: 16.4 KB | FPS: 60 │
└─────────────────────────────────────────────────────────────┘
```

---

## 📐 Technical Specifications

### Voxel Data Structure

```python
# Core data: 3D numpy array
voxels = np.zeros((32, 32, 32), dtype=np.uint16)

# Each voxel = 16-bit packed data (same as Unity!)
# Bits 0-8:   Material ID (512 materials)
# Bits 9-11:  Rotation (8 rotations)
# Bits 12-15: Shape (16 shapes)
```

### Material IDs (from Unity VoxelBits.cs)

```python
MATERIALS = {
    0: "Air",              # Empty space
    3: "Concrete",         # Gray, medium durability
    5: "Steel",            # Dark gray, high durability
    2: "ChobhamArmor",     # Brown, highest durability
    4: "Flesh",            # Pink (future: bio-mechs)
    13: "DamagedConcrete", # Red, first hit state
    14: "DamagedSteel",    # Orange, first hit state
    15: "DamagedArmor",    # First hit state
}
```

### File Format (.stasset)

```
Header (16 bytes):
├─ Bytes 0-3:   Magic "STAS"
├─ Byte 4:      Version (1)
├─ Byte 5:      Flags (reserved)
├─ Bytes 6-7:   Width (uint16, little-endian)
├─ Bytes 8-9:   Height (uint16, little-endian)
├─ Bytes 10-11: Depth (uint16, little-endian)
└─ Bytes 12-15: Reserved (uint32)

Voxel Data:
└─ width × height × depth × 2 bytes (uint16, little-endian)
```

---

## 🛠️ Project Structure

```
SteelTide/
├─ VoxelAssetStudio/              # New folder for standalone app
│   ├─ main.py                    # Entry point
│   ├─ voxel_editor.py            # Main window (QMainWindow)
│   ├─ viewport_widget.py         # OpenGL 3D view (GLViewWidget)
│   ├─ tool_panel.py              # Left sidebar tools
│   ├─ material_library.py        # Material definitions & colors
│   ├─ voxel_data.py              # Grid data structure
│   ├─ stasset_io.py              # Import/Export .stasset files
│   ├─ requirements.txt           # PyQt6, numpy, pyqtgraph, PyOpenGL
│   └─ README.md                  # Setup instructions
├─ VoxelStudio.bat                # Launcher (double-click to run!)
└─ My project/Assets/StreamingAssets/
    └─ HighDensity32.stasset      # Test file (loads on startup)
```

---

## 🚀 Phase 1: Minimal Viable Editor (Week 1)

### Core Features

```
✅ 3D viewport with orbit camera (like RGVS!)
✅ Load .stasset file on startup (HighDensity32.stasset)
✅ Render voxels as instanced cubes (simple, fast)
✅ Paint tool (click voxel to change material)
✅ Erase tool (click voxel to set to Air)
✅ Material selector (5 materials to start)
✅ Export to .stasset (overwrite original file)
✅ Status bar (grid size, voxel count, FPS)
```

**That's it!** No procedural generation yet, just a working editor.

---

## 💻 Code Architecture

### Main Application

```python
# main.py

import sys
from PyQt6.QtWidgets import QApplication
from voxel_editor import VoxelEditor

def main():
    app = QApplication(sys.argv)
    
    # Set dark theme (like Kalshi Dashboard!)
    app.setStyle('Fusion')
    
    # Create main window
    editor = VoxelEditor()
    editor.show()
    
    # Load default asset
    editor.load_asset("My project/Assets/StreamingAssets/HighDensity32.stasset")
    
    sys.exit(app.exec())

if __name__ == "__main__":
    main()
```

### Main Window

```python
# voxel_editor.py

from PyQt6.QtWidgets import QMainWindow, QHBoxLayout, QWidget
from viewport_widget import VoxelViewport
from tool_panel import ToolPanel
import numpy as np

class VoxelEditor(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Voxel Asset Studio")
        self.resize(1200, 800)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.current_material = 3  # Concrete
        
        # UI Layout
        central = QWidget()
        layout = QHBoxLayout()
        
        # Left: Tool panel
        self.tool_panel = ToolPanel()
        self.tool_panel.material_changed.connect(self.set_material)
        layout.addWidget(self.tool_panel, stretch=1)
        
        # Right: 3D viewport
        self.viewport = VoxelViewport()
        self.viewport.voxel_clicked.connect(self.paint_voxel)
        layout.addWidget(self.viewport, stretch=4)
        
        central.setLayout(layout)
        self.setCentralWidget(central)
        
        # Status bar
        self.statusBar().showMessage("Ready")
        
    def load_asset(self, filepath):
        """Load .stasset file"""
        from stasset_io import load_stasset
        self.voxels, self.grid_size = load_stasset(filepath)
        self.viewport.set_voxels(self.voxels)
        self.statusBar().showMessage(f"Loaded: {filepath}")
        
    def paint_voxel(self, x, y, z):
        """Paint voxel at coordinates"""
        if self.voxels is not None:
            self.voxels[x, y, z] = self.current_material
            self.viewport.update_voxel(x, y, z, self.current_material)
            
    def set_material(self, material_id):
        """Change current paint material"""
        self.current_material = material_id
```

### 3D Viewport (Reuse RGVS Knowledge!)

```python
# viewport_widget.py

from pyqtgraph.opengl import GLViewWidget, GLScatterPlotItem
from PyQt6.QtCore import pyqtSignal
import numpy as np

class VoxelViewport(GLViewWidget):
    voxel_clicked = pyqtSignal(int, int, int)  # x, y, z
    
    def __init__(self):
        super().__init__()
        
        # Camera setup (like RGVS!)
        self.setCameraPosition(distance=50, elevation=30, azimuth=45)
        
        # Grid helper
        self.add_grid()
        
        # Voxel scatter plot (simple rendering)
        self.voxel_plot = None
        
    def set_voxels(self, voxels):
        """Load voxel data and render"""
        # Find non-air voxels
        coords = np.argwhere(voxels > 0)
        
        if len(coords) == 0:
            return
            
        # Get colors for each voxel
        colors = np.array([self.get_material_color(voxels[x,y,z]) 
                          for x, y, z in coords])
        
        # Create scatter plot (each voxel = point with size)
        if self.voxel_plot is not None:
            self.removeItem(self.voxel_plot)
            
        self.voxel_plot = GLScatterPlotItem(
            pos=coords * 0.125,  # Scale by voxel size
            color=colors,
            size=0.125,          # Voxel size
            pxMode=False
        )
        self.addItem(self.voxel_plot)
        
    def get_material_color(self, material_id):
        """Map material ID to RGB color"""
        COLORS = {
            0: (0, 0, 0, 0),           # Air (transparent)
            3: (0.5, 0.5, 0.5, 1),     # Concrete (gray)
            5: (0.3, 0.3, 0.3, 1),     # Steel (dark gray)
            2: (0.6, 0.4, 0.2, 1),     # Armor (brown)
            13: (1.0, 0.2, 0.2, 1),    # Damaged Concrete (red)
            14: (1.0, 0.5, 0.0, 1),    # Damaged Steel (orange)
        }
        return COLORS.get(material_id, (1, 1, 1, 1))
```

### File I/O

```python
# stasset_io.py

import struct
import numpy as np

def load_stasset(filepath):
    """Load .stasset file, return (voxels, dims)"""
    with open(filepath, 'rb') as f:
        # Read header
        magic = f.read(4)
        if magic != b'STAS':
            raise ValueError("Not a valid .stasset file!")
            
        version = struct.unpack('B', f.read(1))[0]
        f.read(1)  # flags
        
        width = struct.unpack('<H', f.read(2))[0]
        height = struct.unpack('<H', f.read(2))[0]
        depth = struct.unpack('<H', f.read(2))[0]
        f.read(4)  # reserved
        
        # Read voxel data
        count = width * height * depth
        voxel_bytes = f.read(count * 2)
        voxels = np.frombuffer(voxel_bytes, dtype='<u2')  # Little-endian uint16
        voxels = voxels.reshape((width, height, depth))
        
    return voxels, (width, height, depth)

def save_stasset(filepath, voxels):
    """Save voxels to .stasset file"""
    dims = voxels.shape
    
    with open(filepath, 'wb') as f:
        # Header
        f.write(b'STAS')
        f.write(struct.pack('B', 1))        # Version
        f.write(struct.pack('B', 0))        # Flags
        f.write(struct.pack('<H', dims[0])) # Width
        f.write(struct.pack('<H', dims[1])) # Height
        f.write(struct.pack('<H', dims[2])) # Depth
        f.write(struct.pack('<I', 0))       # Reserved
        
        # Voxel data
        voxel_data = voxels.flatten().astype('<u2')
        f.write(voxel_data.tobytes())
```

---

## 🎯 Launcher Script

### VoxelStudio.bat (Just like Kalshi!)

```batch
@echo off
echo Starting Voxel Asset Studio...
cd /d "%~dp0VoxelAssetStudio"
python main.py
pause
```

**Usage:** Double-click `VoxelStudio.bat` in the SteelTide folder!

---

## 📦 Dependencies

### requirements.txt

```
PyQt6>=6.6.0
numpy>=1.24.0
pyqtgraph>=0.13.0
PyOpenGL>=3.1.7
```

### Installation

```bash
cd VoxelAssetStudio
pip install -r requirements.txt
```

---

## 🎮 Workflow

```
1. Double-click VoxelStudio.bat
   ↓
2. App opens, loads HighDensity32.stasset
   ↓
3. Paint/modify voxels in 3D viewport
   ↓
4. File → Save (overwrites .stasset)
   ↓
5. Switch to Unity (already open!)
   ↓
6. Unity auto-reloads the asset
   ↓
7. Press Play → See your changes!
   ↓
8. Iterate: Back to step 3
```

**Fast iteration loop!** No Unity recompilation needed!

---

## 🚀 Future Phases (Add Later)

### Phase 2: Enhanced Tools (Week 2-3)
- [ ] Brush sizes (1×1×1, 3×3×3, 5×5×5)
- [ ] Fill tool (flood fill)
- [ ] Copy/paste regions
- [ ] Undo/redo stack
- [ ] Multiple viewports (top, side, front)

### Phase 3: Procedural Generation (Week 4-5)
- [ ] Generate → Building menu
- [ ] Parameter sliders (width, height, style)
- [ ] Preview before applying
- [ ] Save generation presets

### Phase 4: Advanced Features (Week 6+)
- [ ] LOD generation (auto-create 8×8×8 from 32×32×32)
- [ ] Material layers (paint inner/outer separately)
- [ ] Symmetry tools (mirror X/Y/Z)
- [ ] Import from MagicaVoxel (.vox files)

---

## ✅ Success Metrics

### Phase 1 Complete When:
- ✅ Double-click .bat → app opens in <2 seconds
- ✅ HighDensity32.stasset loads and renders
- ✅ Can paint voxels by clicking
- ✅ Can change materials
- ✅ Can save changes
- ✅ Unity auto-reloads the asset
- ✅ 60 FPS with 32×32×32 grid

---

## 📝 Development Notes

### Rendering Strategy

**Start Simple (Phase 1):**
```python
# Use GLScatterPlotItem (each voxel = point)
# Fast, easy, works immediately
# Good for 32×32×32 grids
```

**Optimize Later (Phase 2+):**
```python
# Use instanced cube meshes
# Or greedy meshing (combine faces)
# For larger grids (64×64×64+)
```

### Reuse Kalshi Dashboard Patterns

```python
# Dark theme
app.setStyle('Fusion')

# Status bar updates
self.statusBar().showMessage(f"Voxels: {count}")

# Real-time rendering
self.update_timer.start(16)  # 60 FPS

# OpenGL viewport
# (Same as RGVS widget!)
```

---

## 🎯 Next Steps

1. **Create VoxelAssetStudio folder**
2. **Write main.py** (entry point)
3. **Write voxel_editor.py** (main window)
4. **Write viewport_widget.py** (3D view)
5. **Write stasset_io.py** (file I/O)
6. **Create VoxelStudio.bat** (launcher)
7. **Test:** Load HighDensity32.stasset
8. **Implement:** Paint tool
9. **Test:** Save and reload in Unity

---

**Document Version:** 1.0 (Simplified)  
**Last Updated:** June 27, 2026  
**Status:** Ready to Build - Phase 1 Focused

**This design doc will grow as we add features!** 🚀
