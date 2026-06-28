# Steel Tide: Voxel Asset Studio
# viewport_widget.py - 3D OpenGL viewport for voxel rendering

from pyqtgraph.opengl import GLViewWidget, GLScatterPlotItem, GLGridItem, GLBoxItem, GLLinePlotItem
from pyqtgraph.opengl.MeshData import MeshData
from PyQt6.QtGui import QVector3D as Vec3
from PyQt6.QtCore import pyqtSignal, QTimer, Qt
from PyQt6.QtGui import QMouseEvent, QVector3D
from PyQt6.QtWidgets import QLabel
import numpy as np
from material_library import get_material_color, get_material_name
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
        self.pan_speed = 0.5  # Units per frame when moving
        self.keys_pressed = set()  # Track which keys are currently pressed
        
        # Camera mode: 'orbit' (locked around center) or 'free' (move camera position)
        self.camera_mode = 'orbit'  # Default to orbit mode
        
        # Continuous movement timer (60 FPS for smooth movement)
        self.move_timer = QTimer()
        self.move_timer.timeout.connect(self._update_continuous_movement)
        self.move_timer.start(16)  # ~60 FPS
        
        # Enable keyboard focus
        self.setFocusPolicy(Qt.FocusPolicy.StrongFocus)
        
        # Enable mouse tracking (so we get mouseMoveEvent even without buttons pressed!)
        self.setMouseTracking(True)
        
        # Add grid helper (aligned to voxel boundaries)
        grid = GLGridItem()
        grid.scale(2, 2, 1)
        grid.translate(1, 1, 0)  # Shift so grid lines align with 16-voxel boundaries
        self.addItem(grid)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.voxel_size = 0.125  # World units per voxel
        
        # Rendering
        self.voxel_plot = None  # Scatter plot for all voxels
        self.hover_highlight = None  # Highlight for voxel under cursor
        
        # Voxel coordinate mapping (for mouse picking)
        self.voxel_coords = []  # List of (x,y,z) tuples matching scatter plot order
        
        # Settings
        self.highlight_hover_enabled = True  # Now works with proper ray casting!
        self.brush_size = 1
        self.hover_voxel = None  # Current voxel under cursor
        
        # Debug: Add axis compass (RGB = XYZ)
        self._add_axis_compass()
        
        # FPS counter
        self.fps = 0
        self.frame_count = 0
        self.fps_timer = QTimer()
        self.fps_timer.timeout.connect(self._update_fps)
        self.fps_timer.start(1000)  # Update every second
        
        # Material label overlay (shows material name on hover)
        self.material_label = QLabel(self)
        self.material_label.setStyleSheet("""
            QLabel {
                background-color: rgba(0, 0, 0, 200);
                color: white;
                padding: 8px 12px;
                border-radius: 4px;
                font-size: 14px;
                font-weight: bold;
            }
        """)
        self.material_label.hide()  # Hidden by default

    # ========== COORDINATE SYSTEM TRANSFORMS ==========
    # Voxel data uses Y as up/height axis: [X, Y, Z] where Y=up, Z=depth
    # Viewport uses Z as up/height axis: [X, Y, Z] where Y=depth, Z=up
    # These methods centralize the coordinate transforms to avoid scattered Y/Z swaps

    def voxel_to_viewport_coords(self, x, y, z):
        """
        Convert voxel data coordinates (Y-up) to viewport coordinates (Z-up).
        Input: (x, y, z) where y=up, z=depth
        Output: (x, y, z) where y=depth, z=up
        """
        return (x, z, y)  # Swap Y and Z

    def viewport_to_voxel_coords(self, x, y, z):
        """
        Convert viewport coordinates (Z-up) to voxel data coordinates (Y-up).
        Input: (x, y, z) where y=depth, z=up
        Output: (x, y, z) where y=up, z=depth
        """
        return (x, z, y)  # Swap Y and Z

    def voxel_to_world_coords(self, x, y, z):
        """
        Convert voxel grid coordinates to world space coordinates.
        Applies Y/Z swap to match viewport's Z-up convention.
        Input: (x, y, z) voxel grid coordinates (y=up, z=depth)
        Output: (world_x, world_y, world_z) in viewport space (y=depth, z=up)
        """
        world_x = x * self.voxel_size
        world_y = z * self.voxel_size  # viewport Y = voxel Z (depth)
        world_z = y * self.voxel_size  # viewport Z = voxel Y (up)
        return (world_x, world_y, world_z)
        
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
        
        # Store voxel coordinates for mouse picking (CRITICAL!)
        self.voxel_coords = [tuple(c) for c in coords]
        
        # Scale coordinates to world space and apply Y/Z swap
        # Use centralized transform for consistency
        positions = np.array([self.voxel_to_world_coords(x, y, z) for x, y, z in coords])
        
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
            # Paint/interact button - pick voxel using ray casting
            mouse_pos = ev.position()
            hit_voxel = self._pick_voxel_at_mouse(mouse_pos.x(), mouse_pos.y())
            
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
        
        # ALWAYS update hover highlight on mouse move
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
            
    def _pick_voxel_at_mouse(self, mouse_x, mouse_y):
        """
        Pick voxel at mouse position using proper ray casting.
        Based on PyQtGraph GitHub issue #2647 solution.
        Returns (x, y, z) voxel coordinates or None if no hit.
        """
        if self.voxels is None:
            return None
        
        # Get ray from mouse position (proper method!)
        ray_origin, ray_direction = self._get_ray_from_mouse(mouse_x, mouse_y)
        
        # Use DDA raymarch to find voxel intersection
        hit_voxel = self._dda_raymarch(ray_origin, ray_direction)
        
        return hit_voxel
    
    def _get_ray_from_mouse(self, x_coord, y_coord):
        """
        Get ray origin and direction from mouse click.
        Uses PyQtGraph's projection/view matrices for accurate ray casting.
        
        Returns: (ray_origin, ray_direction) as numpy arrays
        
        Based on: https://github.com/pyqtgraph/pyqtgraph/issues/2647
        """
        # Get viewport dimensions
        viewport = self.getViewport()
        x0, y0, width, height = viewport
        
        # Mouse coordinates from ev.position() are already widget-relative
        # Do NOT subtract viewport offset (that would be double-offsetting)
        mouse_x = x_coord
        mouse_y = y_coord
        
        # Ray origin is camera position
        ray_origin = np.array(self.cameraPosition())
        
        # Get projection and view matrices from PyQtGraph
        # projectionMatrix(region, viewport) - region is the area to render (use full viewport)
        region = viewport  # Use full viewport as region
        projection_matrix = np.array(self.projectionMatrix(region, viewport).data()).reshape(4, 4)
        view_matrix = np.array(self.viewMatrix().data()).reshape(4, 4)
        
        # CRITICAL: PyQtGraph's viewMatrix needs to be transposed!
        view_matrix = np.transpose(view_matrix)
        
        # Convert mouse coordinates to NDC (Normalized Device Coordinates)
        # CRITICAL: Qt Y=0 is at TOP, OpenGL NDC Y=-1 is at BOTTOM - must invert Y
        ndc_x = (2.0 * mouse_x) / width - 1.0
        ndc_y = 1.0 - (2.0 * mouse_y) / height
        
        # Create ray in clip space (near plane)
        ray_clip = np.array([ndc_x, ndc_y, -1.0, 1.0])
        
        # Unproject to eye space
        ray_eye = np.linalg.inv(projection_matrix) @ ray_clip
        # Convert to direction (w=0 for direction vector)
        ray_eye = np.array([ray_eye[0], ray_eye[1], -1.0, 0.0])
        
        # Transform to world space
        ray_world = np.linalg.inv(view_matrix) @ ray_eye
        ray_direction = ray_world[:3]
        ray_direction = ray_direction / np.linalg.norm(ray_direction)
        
        return ray_origin, ray_direction
        
    def _dda_raymarch(self, ray_origin, ray_dir, max_steps=256):
        """
        DDA (Digital Differential Analyzer) raymarching through voxel grid.
        Returns (x, y, z) of first solid voxel hit, or None.
        
        IMPORTANT: Returns voxel grid coordinates (integers), NOT world coordinates!
        """
        # Convert ray to voxel space (ray_dir stays normalized, just scale origin)
        voxel_origin = ray_origin / self.voxel_size
        
        # Current voxel position (integer grid coordinates)
        voxel_pos = np.floor(voxel_origin).astype(int)
        
        # Step direction (+1 or -1 for each axis)
        step = np.sign(ray_dir).astype(int)
        
        # Distance to next voxel boundary along each axis
        # (ray_dir is already normalized in world space, works for voxel stepping)
        t_delta = np.abs(1.0 / (ray_dir + 1e-10))  # Avoid division by zero
        
        # Distance to first voxel boundary
        if ray_dir[0] > 0:
            t_max_x = (np.floor(voxel_origin[0]) + 1 - voxel_origin[0]) * t_delta[0]
        else:
            t_max_x = (voxel_origin[0] - np.floor(voxel_origin[0])) * t_delta[0]
            
        if ray_dir[1] > 0:
            t_max_y = (np.floor(voxel_origin[1]) + 1 - voxel_origin[1]) * t_delta[1]
        else:
            t_max_y = (voxel_origin[1] - np.floor(voxel_origin[1])) * t_delta[1]
            
        if ray_dir[2] > 0:
            t_max_z = (np.floor(voxel_origin[2]) + 1 - voxel_origin[2]) * t_delta[2]
        else:
            t_max_z = (voxel_origin[2] - np.floor(voxel_origin[2])) * t_delta[2]
        
        t_max = np.array([t_max_x, t_max_y, t_max_z])
        
        # March through grid
        for _ in range(max_steps):
            # Check if current voxel is in bounds (viewport space: X, Y=depth, Z=up)
            if (0 <= voxel_pos[0] < self.grid_size[0] and
                0 <= voxel_pos[1] < self.grid_size[2] and  # viewport Y = voxel Z (depth)
                0 <= voxel_pos[2] < self.grid_size[1]):  # viewport Z = voxel Y (up)
                
                # Check if voxel is solid (non-air)
                # Use centralized transform: viewport space → voxel data space
                voxel_x, voxel_y, voxel_z = self.viewport_to_voxel_coords(*voxel_pos)
                
                material = self.voxels[voxel_x, voxel_y, voxel_z]
                if material > 0:  # Hit!
                    return (voxel_x, voxel_y, voxel_z)
            
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
        
        # Pick voxel at mouse position using proper ray casting
        hit_voxel = self._pick_voxel_at_mouse(mouse_x, mouse_y)
        
        # Update hover voxel (silent - use Spacebar to debug)
        if hit_voxel != self.hover_voxel:
            self.hover_voxel = hit_voxel
            self._render_hover_highlight()
    
    def _render_hover_highlight(self):
        """Render highlight box around hover voxel (snapped to voxel grid)"""
        # Remove old highlight
        if self.hover_highlight is not None:
            self.removeItem(self.hover_highlight)
            self.hover_highlight = None
        
        if self.hover_voxel is None:
            self.material_label.hide()
            return
        
        # Get voxel grid coordinates (integers)
        voxel_x, voxel_y, voxel_z = self.hover_voxel
        
        # Calculate brush area (centered on clicked voxel)
        radius = (self.brush_size - 1) // 2
        
        # CRITICAL: Swap Y and Z to match viewport coordinate system!
        # In set_voxels (lines 100-103), the swap is:
        #   positions[:, 1] = voxel_z  (viewport Y = voxel Z)
        #   positions[:, 2] = voxel_y  (viewport Z = voxel Y)
        # We must apply the SAME transformation here
        
        # Box bounds in WORLD SPACE (converted from voxel grid coordinates)
        # Use centralized transform for consistency
        min_x = (voxel_x - radius) * self.voxel_size
        max_x = (voxel_x - radius + self.brush_size) * self.voxel_size
        min_y = (voxel_z - radius) * self.voxel_size  # viewport Y = voxel Z (depth)
        max_y = (voxel_z - radius + self.brush_size) * self.voxel_size
        min_z = (voxel_y - radius) * self.voxel_size  # viewport Z = voxel Y (up)
        max_z = (voxel_y - radius + self.brush_size) * self.voxel_size
        
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
        
        # Update material label
        if self.voxels is not None and 0 <= voxel_x < self.grid_size[0] and 0 <= voxel_y < self.grid_size[1] and 0 <= voxel_z < self.grid_size[2]:
            material_id = self.voxels[voxel_x, voxel_y, voxel_z]
            material_name = get_material_name(material_id)
            
            # Format label text
            label_text = f"Material {material_id}: {material_name}"
            if material_id == 0:
                label_text = "Air (Empty)"
            
            self.material_label.setText(label_text)
            self.material_label.show()
            
            # Position label near cursor (offset slightly)
            cursor_pos = self.mapFromGlobal(self.cursor().pos())
            self.material_label.move(cursor_pos.x() + 15, cursor_pos.y() + 15)
    
    def _clear_hover_highlight(self):
        """Clear hover highlight"""
        if self.hover_highlight is not None:
            self.removeItem(self.hover_highlight)
            self.hover_highlight = None
            self.hover_voxel = None
            self.material_label.hide()
            self.update()
    
    def keyPressEvent(self, ev):
        """Handle WASD camera panning with continuous movement"""
        key = ev.key()
        
        # Track key state for continuous movement
        if key in [Qt.Key.Key_W, Qt.Key.Key_S, Qt.Key.Key_A, Qt.Key.Key_D, 
                   Qt.Key.Key_Q, Qt.Key.Key_E]:
            self.keys_pressed.add(key)
        elif key == Qt.Key.Key_Space:
            # Debug: Print current hover info
            self._print_debug_info()
        elif key == Qt.Key.Key_F:
            # Toggle camera mode (orbit/free)
            self.camera_mode = 'free' if self.camera_mode == 'orbit' else 'orbit'
            print(f"🎥 Camera mode: {self.camera_mode.upper()}")
        else:
            # Pass other keys to parent
            super().keyPressEvent(ev)
    
    def keyReleaseEvent(self, ev):
        """Handle key release for continuous movement"""
        key = ev.key()
        if key in self.keys_pressed:
            self.keys_pressed.remove(key)
        else:
            super().keyReleaseEvent(ev)
    
    def _update_continuous_movement(self):
        """Update camera position continuously based on pressed keys"""
        if not self.keys_pressed:
            return
        
        # Get camera center
        center_vec = self.opts['center']
        center = np.array([center_vec.x(), center_vec.y(), center_vec.z()])
        
        # Get view matrix and extract camera direction vectors
        view_matrix = np.array(self.viewMatrix().data()).reshape(4, 4)
        view_matrix = np.transpose(view_matrix)  # PyQtGraph matrices are transposed
        
        # Extract direction vectors from view matrix (column-major in OpenGL)
        # View matrix columns: [right, up, -forward, translation]
        right = -view_matrix[0, :3]  # First column (negated for correct direction)
        up = view_matrix[1, :3]      # Second column
        forward = -view_matrix[2, :3] # Third column (negated because view looks down -Z)
        
        # Normalize vectors
        right_norm = np.linalg.norm(right)
        if right_norm > 0:
            right = right / right_norm
        
        up_norm = np.linalg.norm(up)
        if up_norm > 0:
            up = up / up_norm
        
        forward_norm = np.linalg.norm(forward)
        if forward_norm > 0:
            forward = forward / forward_norm
        
        # Calculate movement vector based on pressed keys
        movement = np.array([0.0, 0.0, 0.0])
        
        if Qt.Key.Key_W in self.keys_pressed:
            movement += forward  # Move forward
        if Qt.Key.Key_S in self.keys_pressed:
            movement -= forward  # Move backward
        if Qt.Key.Key_A in self.keys_pressed:
            movement += right  # Strafe left (fixed direction)
        if Qt.Key.Key_D in self.keys_pressed:
            movement -= right  # Strafe right (fixed direction)
        if Qt.Key.Key_Q in self.keys_pressed:
            movement -= up  # Move down (camera-relative)
        if Qt.Key.Key_E in self.keys_pressed:
            movement += up  # Move up (camera-relative)
        
        # Normalize diagonal movement
        if np.linalg.norm(movement) > 0:
            movement = movement / np.linalg.norm(movement) * self.pan_speed
        
        # Apply movement based on camera mode
        # For now, both modes move the center point
        # Future: free look mode could have different mouse orbit behavior
        new_center = center + movement
        self.opts['center'] = QVector3D(*new_center)
        
        self.update()
    
    def _print_debug_info(self):
        """Print detailed debug info when Spacebar is pressed"""
        if self.hover_voxel is None:
            print("🔍 DEBUG: No voxel under cursor")
            return
        
        # Get current mouse position (from last hover event)
        cursor_pos = self.mapFromGlobal(self.cursor().pos())
        mouse_x, mouse_y = cursor_pos.x(), cursor_pos.y()
        
        # Get viewport info
        viewport = self.getViewport()
        x0, y0, width, height = viewport
        
        # Adjusted mouse coords
        adj_x = mouse_x - x0
        adj_y = mouse_y - y0
        
        # Camera info
        cam_pos = self.cameraPosition()
        
        print("\n" + "="*60)
        print("🔍 VOXEL PICKING DEBUG INFO")
        print("="*60)
        print(f"📍 Voxel Grid Coords: {self.hover_voxel}")
        print(f"🖱️  Mouse Screen Pos: ({mouse_x:.0f}, {mouse_y:.0f})")
        print(f"📐 Viewport: offset=({x0}, {y0}), size=({width}×{height})")
        print(f"🎯 Adjusted Mouse: ({adj_x:.0f}, {adj_y:.0f})")
        print(f"📷 Camera: pos={cam_pos}, dist={self.opts['distance']:.1f}")
        print(f"🌍 World Coords: voxel_size={self.voxel_size}")
        
        # Calculate world position of voxel using centralized transform
        world_x, world_y, world_z = self.voxel_to_world_coords(*self.hover_voxel)
        print(f"📦 Voxel World Pos: ({world_x:.3f}, {world_y:.3f}, {world_z:.3f})")
        print("="*60 + "\n")
    
    def _add_axis_compass(self):
        """Add RGB axis lines at origin for debugging coordinate system"""
        axis_length = 10.0
        
        # X axis - RED
        x_axis = np.array([[0, 0, 0], [axis_length, 0, 0]])
        x_line = GLLinePlotItem(pos=x_axis, color=(1, 0, 0, 1), width=3, antialias=True)
        self.addItem(x_line)
        
        # Y axis - GREEN
        y_axis = np.array([[0, 0, 0], [0, axis_length, 0]])
        y_line = GLLinePlotItem(pos=y_axis, color=(0, 1, 0, 1), width=3, antialias=True)
        self.addItem(y_line)
        
        # Z axis - BLUE
        z_axis = np.array([[0, 0, 0], [0, 0, axis_length]])
        z_line = GLLinePlotItem(pos=z_axis, color=(0, 0, 1, 1), width=3, antialias=True)
        self.addItem(z_line)
