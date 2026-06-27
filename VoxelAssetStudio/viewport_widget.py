# Steel Tide: Voxel Asset Studio
# viewport_widget.py - 3D OpenGL viewport for voxel rendering

from pyqtgraph.opengl import GLViewWidget, GLScatterPlotItem, GLGridItem, GLBoxItem, GLLinePlotItem
from pyqtgraph.opengl.MeshData import MeshData
from PyQt6.QtGui import QVector3D as Vec3
from PyQt6.QtCore import pyqtSignal, QTimer, Qt
from PyQt6.QtGui import QMouseEvent, QVector3D
import numpy as np
from material_library import get_material_color
import pyqtgraph.opengl as gl

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
        
        # Keyboard pan settings
        self.pan_speed = 0.5  # Units per key press
        
        # Enable keyboard focus
        self.setFocusPolicy(Qt.FocusPolicy.StrongFocus)
        
        # Enable mouse tracking (so we get mouseMoveEvent even without buttons pressed!)
        self.setMouseTracking(True)
        
        # Add grid helper
        grid = GLGridItem()
        grid.scale(2, 2, 1)
        self.addItem(grid)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.voxel_size = 0.125  # World units per voxel
        
        # Rendering
        self.voxel_plot = None  # Scatter plot for all voxels
        self.hover_highlight = None  # Highlight for voxel under cursor
        
        # Settings
        self.highlight_hover_enabled = True
        self.brush_size = 1
        self.hover_voxel = None  # Current voxel under cursor
        
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
        """Render all non-air voxels as fast scatter plot"""
        if self.voxels is None:
            return
            
        # Remove old plot
        if self.voxel_plot is not None:
            self.removeItem(self.voxel_plot)
            self.voxel_plot = None
        
        # Find non-air voxels
        coords = np.argwhere(self.voxels > 0)
        
        if len(coords) == 0:
            print("⚠️ No voxels to render (all air)")
            return
        
        # Scale coordinates to world space
        positions = coords.astype(float) * self.voxel_size
        
        # For now: Use material ID to determine color intensity
        # This is a workaround - scatter plot has limited color support
        materials = np.array([self.voxels[x, y, z] for x, y, z in coords])
        
        # Map materials to colors (simplified - just use grayscale based on material ID)
        # Material 0=Air (skip), 3=Concrete, 5=Steel, 13=DamagedConcrete, etc.
        colors = np.zeros((len(materials), 4))
        for i, mat in enumerate(materials):
            color = get_material_color(mat)
            colors[i] = color
            colors[i][3] = 1.0  # Force full opacity (no transparency!)
        
        # Create scatter plot with SOLID rendering
        self.voxel_plot = GLScatterPlotItem(
            pos=positions,
            color=colors,
            size=self.voxel_size,
            pxMode=False,
            glOptions='opaque'  # Force opaque rendering!
        )
        
        # Replace the default sphere mesh with a cube mesh
        # 8 vertices of a unit cube centered at origin
        verts = np.array([
            [-0.5, -0.5, -0.5], [0.5, -0.5, -0.5], [0.5, 0.5, -0.5], [-0.5, 0.5, -0.5],  # Bottom
            [-0.5, -0.5,  0.5], [0.5, -0.5,  0.5], [0.5, 0.5,  0.5], [-0.5, 0.5,  0.5],  # Top
        ], dtype=float)
        
        # 12 triangles (2 per face, 6 faces)
        faces = np.array([
            [0,1,2], [0,2,3],  # Bottom
            [4,6,5], [4,7,6],  # Top
            [0,4,5], [0,5,1],  # Front
            [2,6,7], [2,7,3],  # Back
            [0,3,7], [0,7,4],  # Left
            [1,5,6], [1,6,2],  # Right
        ], dtype=np.uint32)
        
        # Create cube mesh and FORCE replace the scatter plot's mesh
        cube_mesh = MeshData(vertexes=verts, faces=faces)
        self.voxel_plot.setData(pos=positions, color=colors, size=self.voxel_size, pxMode=False)
        self.voxel_plot.mesh = cube_mesh
        self.voxel_plot.meshdata = cube_mesh  # Try both attributes
        
        self.addItem(self.voxel_plot)
        
        print(f"✅ Rendered {len(coords):,} voxels (fast mode)")
        
    def update_voxel(self, x, y, z, material_id):
        """Update voxel(s) based on brush size"""
        if self.voxels is None:
            return
        
        # Calculate brush radius
        radius = (self.brush_size - 1) // 2
        
        # Paint all voxels in brush area
        for dx in range(-radius, radius + 1):
            for dy in range(-radius, radius + 1):
                for dz in range(-radius, radius + 1):
                    vx = x + dx
                    vy = y + dy
                    vz = z + dz
                    
                    # Check bounds
                    if (0 <= vx < self.grid_size[0] and
                        0 <= vy < self.grid_size[1] and
                        0 <= vz < self.grid_size[2]):
                        self.voxels[vx, vy, vz] = material_id
        
        # Re-render entire grid to show changes
        self.render_voxels()
        # Force viewport update
        self.update()
            
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
            # Paint/interact button - perform raycasting
            mouse_pos = ev.position()
            hit_voxel = self._raycast_voxel(mouse_pos.x(), mouse_pos.y())
            
            if hit_voxel is not None:
                # Emit signal with voxel coordinates
                self.voxel_clicked.emit(hit_voxel[0], hit_voxel[1], hit_voxel[2])
                print(f"🎯 Clicked voxel: {hit_voxel}")
            else:
                print("❌ No voxel hit")
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
        
        # ALWAYS update hover highlight on mouse move (not just during drag)
        if self.highlight_hover_enabled:
            mouse_pos = ev.position()
            self._update_hover_highlight(mouse_pos.x(), mouse_pos.y())
        
        # Handle button-specific drag behavior
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
            # No buttons pressed - just hovering (this is normal!)
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
            
    def _raycast_voxel(self, mouse_x, mouse_y):
        """
        Raycast from mouse position to find clicked voxel.
        Uses camera orientation to calculate accurate ray direction.
        Returns (x, y, z) voxel coordinates or None if no hit.
        """
        if self.voxels is None:
            return None
            
        # Get camera parameters
        camera_pos = self.cameraPosition()
        center = self.opts['center']
        
        # Camera vectors
        cam_pos = np.array([camera_pos.x(), camera_pos.y(), camera_pos.z()])
        cam_center = np.array([center.x(), center.y(), center.z()])
        
        # Forward vector (camera to center)
        forward = cam_center - cam_pos
        forward = forward / np.linalg.norm(forward)
        
        # Up vector (assume Z is up)
        up = np.array([0, 0, 1])
        
        # Right vector (cross product)
        right = np.cross(forward, up)
        right = right / np.linalg.norm(right)
        
        # Recalculate up (to be perpendicular)
        up = np.cross(right, forward)
        up = up / np.linalg.norm(up)
        
        # Convert mouse to normalized coordinates (-1 to 1)
        width = self.width()
        height = self.height()
        aspect = width / max(height, 1)
        
        ndc_x = (2.0 * mouse_x / width) - 1.0
        ndc_y = 1.0 - (2.0 * mouse_y / height)
        
        # Field of view (approximate from GLViewWidget default)
        fov = 60.0  # degrees
        fov_rad = np.radians(fov)
        tan_fov = np.tan(fov_rad / 2.0)
        
        # Calculate ray direction
        ray_dir = forward.copy()
        ray_dir += right * ndc_x * tan_fov * aspect
        ray_dir += up * ndc_y * tan_fov
        ray_dir = ray_dir / np.linalg.norm(ray_dir)
        
        # DDA raymarching through voxel grid
        hit_voxel = self._dda_raymarch(cam_pos, ray_dir)
        
        return hit_voxel
        
    def _dda_raymarch(self, ray_origin, ray_dir, max_steps=256):
        """
        DDA (Digital Differential Analyzer) raymarching through voxel grid.
        Returns (x, y, z) of first solid voxel hit, or None.
        """
        # Convert ray to voxel space
        voxel_origin = ray_origin / self.voxel_size
        voxel_dir = ray_dir / self.voxel_size
        
        # Current voxel position
        voxel_pos = np.floor(voxel_origin).astype(int)
        
        # Step direction (+1 or -1 for each axis)
        step = np.sign(voxel_dir).astype(int)
        
        # Distance to next voxel boundary along each axis
        t_delta = np.abs(1.0 / (voxel_dir + 1e-10))  # Avoid division by zero
        
        # Distance to first voxel boundary
        if voxel_dir[0] > 0:
            t_max_x = (np.floor(voxel_origin[0]) + 1 - voxel_origin[0]) * t_delta[0]
        else:
            t_max_x = (voxel_origin[0] - np.floor(voxel_origin[0])) * t_delta[0]
            
        if voxel_dir[1] > 0:
            t_max_y = (np.floor(voxel_origin[1]) + 1 - voxel_origin[1]) * t_delta[1]
        else:
            t_max_y = (voxel_origin[1] - np.floor(voxel_origin[1])) * t_delta[1]
            
        if voxel_dir[2] > 0:
            t_max_z = (np.floor(voxel_origin[2]) + 1 - voxel_origin[2]) * t_delta[2]
        else:
            t_max_z = (voxel_origin[2] - np.floor(voxel_origin[2])) * t_delta[2]
        
        t_max = np.array([t_max_x, t_max_y, t_max_z])
        
        # March through grid
        for _ in range(max_steps):
            # Check if current voxel is in bounds
            if (0 <= voxel_pos[0] < self.grid_size[0] and
                0 <= voxel_pos[1] < self.grid_size[1] and
                0 <= voxel_pos[2] < self.grid_size[2]):
                
                # Check if voxel is solid (non-air)
                material = self.voxels[voxel_pos[0], voxel_pos[1], voxel_pos[2]]
                if material > 0:  # Hit!
                    return tuple(voxel_pos)
            
            # Step to next voxel
            if t_max[0] < t_max[1]:
                if t_max[0] < t_max[2]:
                    voxel_pos[0] += step[0]
                    t_max[0] += t_delta[0]
                else:
                    voxel_pos[2] += step[2]
                    t_max[2] += t_delta[2]
            else:
                if t_max[1] < t_max[2]:
                    voxel_pos[1] += step[1]
                    t_max[1] += t_delta[1]
                else:
                    voxel_pos[2] += step[2]
                    t_max[2] += t_delta[2]
        
        return None  # No hit
        
    def set_highlight_hover(self, enabled):
        """Enable/disable hover highlighting"""
        self.highlight_hover_enabled = enabled
        if not enabled:
            self._clear_hover_highlight()
        print(f"🎨 Hover highlight: {'ON' if enabled else 'OFF'}")
    
    def set_brush_size(self, size):
        """Set brush size for painting"""
        self.brush_size = max(1, min(10, size))
        print(f"🖌️ Brush size: {self.brush_size}")
    
    def _update_hover_highlight(self, mouse_x, mouse_y):
        """Update hover highlight to show voxel under cursor"""
        if self.voxels is None:
            return
        
        # Raycast to find voxel under cursor
        hit_voxel = self._raycast_voxel(mouse_x, mouse_y)
        
        # Debug: Print when hover changes
        if hit_voxel != self.hover_voxel:
            if hit_voxel is not None:
                print(f"🎯 Hovering: {hit_voxel}")
            self.hover_voxel = hit_voxel
            self._render_hover_highlight()
    
    def _render_hover_highlight(self):
        """Render highlight box around hover voxel"""
        # Remove old highlight
        if self.hover_highlight is not None:
            self.removeItem(self.hover_highlight)
            self.hover_highlight = None
        
        if self.hover_voxel is None:
            return
        
        # Create highlight box
        x, y, z = self.hover_voxel
        
        # Calculate brush area (centered on clicked voxel)
        radius = (self.brush_size - 1) // 2
        
        # Box bounds in voxel space
        min_x = (x - radius) * self.voxel_size
        max_x = (x - radius + self.brush_size) * self.voxel_size
        min_y = (y - radius) * self.voxel_size
        max_y = (y - radius + self.brush_size) * self.voxel_size
        min_z = (z - radius) * self.voxel_size
        max_z = (z - radius + self.brush_size) * self.voxel_size
        
        # Create wireframe box using line segments (12 edges of a cube)
        edges = np.array([
            # Bottom face (4 edges)
            [min_x, min_y, min_z], [max_x, min_y, min_z],
            [max_x, min_y, min_z], [max_x, max_y, min_z],
            [max_x, max_y, min_z], [min_x, max_y, min_z],
            [min_x, max_y, min_z], [min_x, min_y, min_z],
            # Top face (4 edges)
            [min_x, min_y, max_z], [max_x, min_y, max_z],
            [max_x, min_y, max_z], [max_x, max_y, max_z],
            [max_x, max_y, max_z], [min_x, max_y, max_z],
            [min_x, max_y, max_z], [min_x, min_y, max_z],
            # Vertical edges (4 edges)
            [min_x, min_y, min_z], [min_x, min_y, max_z],
            [max_x, min_y, min_z], [max_x, min_y, max_z],
            [max_x, max_y, min_z], [max_x, max_y, max_z],
            [min_x, max_y, min_z], [min_x, max_y, max_z],
        ])
        
        # Create line plot (yellow wireframe)
        self.hover_highlight = GLLinePlotItem(
            pos=edges,
            color=(1.0, 1.0, 0.0, 1.0),  # Bright yellow
            width=2.0,
            antialias=True,
            mode='lines'
        )
        
        self.addItem(self.hover_highlight)
        self.update()
        print(f"📦 Drew hover box at ({x}, {y}, {z}) size={self.brush_size}")
    
    def _clear_hover_highlight(self):
        """Clear hover highlight"""
        if self.hover_highlight is not None:
            self.removeItem(self.hover_highlight)
            self.hover_highlight = None
            self.hover_voxel = None
            self.update()
    
    def keyPressEvent(self, ev):
        """Handle WASD camera panning"""
        key = ev.key()
        
        # Get current camera center
        center = self.opts['center']
        
        # Pan based on key
        if key == Qt.Key.Key_W:
            # Pan forward (positive Y)
            self.opts['center'] = center + QVector3D(0, self.pan_speed, 0)
            self.update()
        elif key == Qt.Key.Key_S:
            # Pan backward (negative Y)
            self.opts['center'] = center + QVector3D(0, -self.pan_speed, 0)
            self.update()
        elif key == Qt.Key.Key_A:
            # Pan left (negative X)
            self.opts['center'] = center + QVector3D(-self.pan_speed, 0, 0)
            self.update()
        elif key == Qt.Key.Key_D:
            # Pan right (positive X)
            self.opts['center'] = center + QVector3D(self.pan_speed, 0, 0)
            self.update()
        elif key == Qt.Key.Key_Q:
            # Pan down (negative Z)
            self.opts['center'] = center + QVector3D(0, 0, -self.pan_speed)
            self.update()
        elif key == Qt.Key.Key_E:
            # Pan up (positive Z)
            self.opts['center'] = center + QVector3D(0, 0, self.pan_speed)
            self.update()
        else:
            # Pass other keys to parent
            super().keyPressEvent(ev)
