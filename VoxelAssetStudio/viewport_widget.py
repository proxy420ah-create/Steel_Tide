# Steel Tide: Voxel Asset Studio
# viewport_widget.py - 3D OpenGL viewport for voxel rendering

from pyqtgraph.opengl import GLViewWidget, GLScatterPlotItem, GLGridItem
from PyQt6.QtCore import pyqtSignal, QTimer, Qt
from PyQt6.QtGui import QMouseEvent
import numpy as np
from material_library import get_material_color

class VoxelViewport(GLViewWidget):
    """3D viewport for rendering and interacting with voxels"""
    
    voxel_clicked = pyqtSignal(int, int, int)  # x, y, z coordinates
    
    def __init__(self, parent=None):
        super().__init__(parent)
        
        # Camera setup (similar to RGVS widget!)
        self.setCameraPosition(distance=60, elevation=30, azimuth=45)
        
        # Mouse configuration (default: L=Paint, M=Pan, R=Orbit)
        self.mouse_config = {
            "orbit": "Right Button",
            "pan": "Middle Button",
            "paint": "Left Button"
        }
        self.noRepeatKeys = []
        
        # Add grid helper
        grid = GLGridItem()
        grid.scale(2, 2, 1)
        self.addItem(grid)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.voxel_size = 0.125  # World units per voxel
        
        # Rendering
        self.voxel_plot = None
        
        # FPS counter
        self.fps = 0
        self.frame_count = 0
        self.fps_timer = QTimer()
        self.fps_timer.timeout.connect(self._update_fps)
        self.fps_timer.start(1000)  # Update every second
        
    def set_voxels(self, voxels):
        """Load and render voxel data"""
        self.voxels = voxels
        self.grid_size = voxels.shape
        self.render_voxels()
        
    def render_voxels(self):
        """Render all non-air voxels as scatter plot"""
        if self.voxels is None:
            return
            
        # Find non-air voxels
        coords = np.argwhere(self.voxels > 0)
        
        if len(coords) == 0:
            print("⚠️ No voxels to render (all air)")
            return
            
        # Get colors for each voxel
        colors = np.array([get_material_color(self.voxels[x, y, z]) 
                          for x, y, z in coords])
        
        # Scale coordinates to world space
        positions = coords.astype(float) * self.voxel_size
        
        # Remove old plot
        if self.voxel_plot is not None:
            self.removeItem(self.voxel_plot)
            
        # Create scatter plot (each voxel = cube-like point)
        self.voxel_plot = GLScatterPlotItem(
            pos=positions,
            color=colors,
            size=self.voxel_size * 1.2,  # Slightly larger for visibility
            pxMode=False  # Size in world units, not pixels
        )
        self.addItem(self.voxel_plot)
        
        print(f"✅ Rendered {len(coords):,} voxels")
        
    def update_voxel(self, x, y, z, material_id):
        """Update a single voxel (for painting)"""
        if self.voxels is not None:
            self.voxels[x, y, z] = material_id
            # For now, re-render everything (optimize later!)
            self.render_voxels()
            
    def paintGL(self):
        """Override to count frames for FPS"""
        super().paintGL()
        self.frame_count += 1
        
    def _update_fps(self):
        """Update FPS counter"""
        self.fps = self.frame_count
        self.frame_count = 0
        
    def get_fps(self):
        """Get current FPS"""
        return self.fps
        
    def set_mouse_config(self, config):
        """Update mouse button configuration"""
        self.mouse_config = config.copy()
        print(f"🖱️ Mouse config updated: {config}")
        
    def _get_button_for_action(self, action):
        """Get which Qt button is assigned to an action"""
        button_name = self.mouse_config.get(action, "Disabled")
        button_map = {
            "Left Button": Qt.MouseButton.LeftButton,
            "Middle Button": Qt.MouseButton.MiddleButton,
            "Right Button": Qt.MouseButton.RightButton,
            "Disabled": None
        }
        return button_map.get(button_name)
        
    def mousePressEvent(self, ev):
        """Handle mouse press with configurable buttons"""
        orbit_btn = self._get_button_for_action("orbit")
        pan_btn = self._get_button_for_action("pan")
        paint_btn = self._get_button_for_action("paint")
        
        if ev.button() == paint_btn and paint_btn is not None:
            # Paint/interact button - don't pass to camera controls
            # TODO: Implement voxel clicking
            return
        elif ev.button() == orbit_btn and orbit_btn is not None:
            # Orbit button → convert to left-click for GLViewWidget
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.LeftButton,
                Qt.MouseButton.LeftButton,
                ev.modifiers()
            )
            super().mousePressEvent(fake_ev)
        elif ev.button() == pan_btn and pan_btn is not None:
            # Pan button → convert to middle-click for GLViewWidget
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.MiddleButton,
                Qt.MouseButton.MiddleButton,
                ev.modifiers()
            )
            super().mousePressEvent(fake_ev)
        else:
            # Unassigned button - ignore
            pass
            
    def mouseMoveEvent(self, ev):
        """Handle mouse drag with configurable buttons"""
        orbit_btn = self._get_button_for_action("orbit")
        pan_btn = self._get_button_for_action("pan")
        
        if ev.buttons() & orbit_btn if orbit_btn else False:
            # Orbit drag → convert to left-drag
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.LeftButton,
                Qt.MouseButton.LeftButton,
                ev.modifiers()
            )
            super().mouseMoveEvent(fake_ev)
        elif ev.buttons() & pan_btn if pan_btn else False:
            # Pan drag → convert to middle-drag
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.MiddleButton,
                Qt.MouseButton.MiddleButton,
                ev.modifiers()
            )
            super().mouseMoveEvent(fake_ev)
        else:
            # Unassigned or paint drag - ignore
            pass
            
    def mouseReleaseEvent(self, ev):
        """Handle mouse release with configurable buttons"""
        orbit_btn = self._get_button_for_action("orbit")
        pan_btn = self._get_button_for_action("pan")
        paint_btn = self._get_button_for_action("paint")
        
        if ev.button() == paint_btn and paint_btn is not None:
            # Paint button release - ignore
            return
        elif ev.button() == orbit_btn and orbit_btn is not None:
            # Orbit release → convert to left-release
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.LeftButton,
                Qt.MouseButton.LeftButton,
                ev.modifiers()
            )
            super().mouseReleaseEvent(fake_ev)
        elif ev.button() == pan_btn and pan_btn is not None:
            # Pan release → convert to middle-release
            fake_ev = QMouseEvent(
                ev.type(),
                ev.position(),
                Qt.MouseButton.MiddleButton,
                Qt.MouseButton.MiddleButton,
                ev.modifiers()
            )
            super().mouseReleaseEvent(fake_ev)
        else:
            # Unassigned button - ignore
            pass
