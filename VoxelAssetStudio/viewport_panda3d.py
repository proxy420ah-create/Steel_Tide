# Steel Tide: Voxel Asset Studio
# viewport_panda3d.py - Panda3D-based 3D viewport with proper per-voxel colors

from direct.showbase.ShowBase import ShowBase
from panda3d.core import *
from PyQt6.QtWidgets import QWidget, QVBoxLayout
from PyQt6.QtCore import pyqtSignal, QTimer, Qt
import numpy as np
from material_library import get_material_color
import sys

class VoxelViewportPanda3D(QWidget):
    """3D viewport using Panda3D for proper voxel rendering with colors"""
    
    voxel_clicked = pyqtSignal(int, int, int)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.voxel_size = 0.125
        
        # Mouse config
        self.mouse_config = {
            "orbit": "Right Button",
            "pan": "Middle Button",
            "paint": "Left Button"
        }
        
        # Panda3D app
        self.panda_app = None
        self.voxel_nodes = []
        
        # FPS counter
        self.fps = 0
        
        # Setup UI
        self.init_ui()
        
    def init_ui(self):
        """Initialize the widget UI"""
        layout = QVBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        
        # Start Panda3D in embedded mode
        self.start_panda()
        
        self.setLayout(layout)
        
    def start_panda(self):
        """Start Panda3D embedded in Qt widget"""
        # Configure Panda3D for embedded mode
        loadPrcFileData("", "window-type offscreen")
        loadPrcFileData("", "audio-library-name null")
        
        # Create Panda3D app
        self.panda_app = PandaApp(self)
        
        # Setup camera
        self.panda_app.camera.setPos(10, -20, 10)
        self.panda_app.camera.lookAt(2, 2, 2)  # Look at center of 32x32x32 grid
        
    def set_voxels(self, voxels):
        """Load and render voxel data"""
        self.voxels = voxels
        self.grid_size = voxels.shape
        self.render_voxels()
        
    def render_voxels(self):
        """Render all voxels with proper colors using Panda3D"""
        if self.voxels is None or self.panda_app is None:
            return
            
        # Clear old voxels
        for node in self.voxel_nodes:
            node.removeNode()
        self.voxel_nodes.clear()
        
        # Find non-air voxels
        coords = np.argwhere(self.voxels > 0)
        
        if len(coords) == 0:
            print("⚠️ No voxels to render")
            return
        
        # Create cube geometry (reuse for all voxels)
        cube_geom = self.panda_app.create_cube_geometry(self.voxel_size)
        
        # Create voxel instances
        for x, y, z in coords:
            material_id = self.voxels[x, y, z]
            color = get_material_color(material_id)
            
            # Create node for this voxel
            voxel_node = self.panda_app.render.attachNewNode(f"voxel_{x}_{y}_{z}")
            voxel_node.setPos(x * self.voxel_size, y * self.voxel_size, z * self.voxel_size)
            
            # Attach geometry
            cube_geom.instanceTo(voxel_node)
            
            # Set color
            voxel_node.setColor(color[0], color[1], color[2], color[3])
            
            self.voxel_nodes.append(voxel_node)
        
        print(f"✅ Rendered {len(coords):,} voxels with Panda3D")
        
    def update_voxel(self, x, y, z, material_id):
        """Update a single voxel"""
        if self.voxels is not None:
            self.voxels[x, y, z] = material_id
            # For now, re-render everything (optimize later)
            self.render_voxels()
            
    def set_mouse_config(self, config):
        """Update mouse configuration"""
        self.mouse_config = config.copy()
        
    def get_fps(self):
        """Get current FPS"""
        if self.panda_app:
            return self.panda_app.get_fps()
        return 0


class PandaApp(ShowBase):
    """Panda3D application for voxel rendering"""
    
    def __init__(self, parent_widget):
        # Initialize Panda3D
        ShowBase.__init__(self)
        
        self.parent_widget = parent_widget
        
        # Setup lighting
        self.setup_lights()
        
        # Setup camera controls
        self.disable_mouse()  # Disable default controls
        
        # Cube geometry cache
        self.cube_geom_node = None
        
    def setup_lights(self):
        """Setup scene lighting"""
        # Ambient light
        ambient = AmbientLight("ambient")
        ambient.setColor((0.3, 0.3, 0.3, 1))
        ambient_np = self.render.attachNewNode(ambient)
        self.render.setLight(ambient_np)
        
        # Directional light
        dlight = DirectionalLight("dlight")
        dlight.setColor((0.8, 0.8, 0.8, 1))
        dlnp = self.render.attachNewNode(dlight)
        dlnp.setHpr(45, -45, 0)
        self.render.setLight(dlnp)
        
    def create_cube_geometry(self, size):
        """Create a cube geometry that can be instanced"""
        if self.cube_geom_node is not None:
            return self.cube_geom_node
            
        # Create cube using CardMaker (simple approach)
        # For better performance, use GeomVertexData
        from panda3d.core import CardMaker
        
        # Create a simple box using 6 cards
        # This is a simplified version - for production, use proper GeomVertexData
        cube = self.render.attachNewNode("cube_template")
        
        # For now, use loader.loadModel for a simple cube
        # In production, generate geometry programmatically
        cube_model = self.loader.loadModel("models/box")
        cube_model.reparentTo(cube)
        cube_model.setScale(size, size, size)
        
        self.cube_geom_node = cube
        return cube
        
    def get_fps(self):
        """Get current FPS"""
        return globalClock.getAverageFrameRate()
