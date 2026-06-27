# Steel Tide: Voxel Asset Studio
# viewport_moderngl.py - ModernGL-based 3D viewport with proper per-voxel colors

from PyQt6.QtOpenGLWidgets import QOpenGLWidget
from PyQt6.QtCore import pyqtSignal, QTimer, Qt, QPoint
from PyQt6.QtGui import QMouseEvent, QSurfaceFormat
import moderngl
import numpy as np
from material_library import get_material_color
import glm  # PyGLM for matrix math

class VoxelViewportModernGL(QOpenGLWidget):
    """3D viewport using ModernGL for proper voxel rendering with per-voxel colors"""
    
    voxel_clicked = pyqtSignal(int, int, int)
    
    def __init__(self, parent=None):
        # Set OpenGL format
        fmt = QSurfaceFormat()
        fmt.setVersion(3, 3)
        fmt.setProfile(QSurfaceFormat.OpenGLContextProfile.CoreProfile)
        fmt.setDepthBufferSize(24)
        QSurfaceFormat.setDefaultFormat(fmt)
        
        super().__init__(parent)
        
        # Voxel data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.voxel_size = 0.125
        
        # ModernGL context
        self.ctx = None
        self.prog = None
        self.vao = None
        
        # Camera (adjusted for 32×32×32 grid at 0.125 voxel size = 4×4×4 world units)
        self.camera_distance = 12.0
        self.camera_rotation = glm.vec2(45, 30)  # azimuth, elevation
        self.camera_target = glm.vec3(2.0, 2.0, 2.0)  # Center of grid (32*0.125/2 = 2)
        
        # Mouse
        self.last_mouse_pos = None
        self.mouse_config = {
            "orbit": "Right Button",
            "pan": "Middle Button",
            "paint": "Left Button"
        }
        
        # FPS
        self.fps = 60
        self.frame_count = 0
        self.fps_timer = QTimer()
        self.fps_timer.timeout.connect(self._update_fps)
        self.fps_timer.start(1000)
        
        # Continuous render timer (60 FPS)
        self.render_timer = QTimer()
        self.render_timer.timeout.connect(self.update)
        self.render_timer.start(16)  # ~60 FPS
        
        # Enable mouse tracking
        self.setMouseTracking(True)
        self.setFocusPolicy(Qt.FocusPolicy.StrongFocus)
        
    def initializeGL(self):
        """Initialize OpenGL context and shaders"""
        self.ctx = moderngl.create_context()
        
        # Enable depth testing
        self.ctx.enable(moderngl.DEPTH_TEST)
        
        # Create shader program
        self.prog = self.ctx.program(
            vertex_shader='''
                #version 330
                
                uniform mat4 mvp;
                
                in vec3 in_position;
                in vec3 in_color;
                in vec3 in_offset;  // Per-instance voxel position
                
                out vec3 v_color;
                
                void main() {
                    vec3 world_pos = in_position + in_offset;
                    gl_Position = mvp * vec4(world_pos, 1.0);
                    v_color = in_color;
                }
            ''',
            fragment_shader='''
                #version 330
                
                in vec3 v_color;
                out vec4 f_color;
                
                void main() {
                    f_color = vec4(v_color, 1.0);
                }
            '''
        )
        
        print("✅ ModernGL initialized")
        
    def resizeGL(self, w, h):
        """Handle window resize"""
        self.ctx.viewport = (0, 0, w, h)
        
    def paintGL(self):
        """Render the scene"""
        if self.ctx is None:
            return
            
        self.ctx.clear(0.05, 0.08, 0.15)  # Dark blue background
        
        if self.vao is None:
            # Debug: Show we're rendering but no voxels yet
            if self.frame_count % 60 == 0:  # Every 60 frames
                print("⚠️ paintGL called but no VAO (voxels not loaded yet)")
            self.frame_count += 1
            return
            
        # Calculate MVP matrix
        projection = glm.perspective(glm.radians(45.0), self.width() / max(self.height(), 1), 0.1, 100.0)
        
        # Calculate camera position from rotation
        azimuth = glm.radians(self.camera_rotation.x)
        elevation = glm.radians(self.camera_rotation.y)
        
        cam_x = self.camera_target.x + self.camera_distance * np.cos(elevation) * np.sin(azimuth)
        cam_y = self.camera_target.y + self.camera_distance * np.cos(elevation) * np.cos(azimuth)
        cam_z = self.camera_target.z + self.camera_distance * np.sin(elevation)
        
        camera_pos = glm.vec3(cam_x, cam_y, cam_z)
        view = glm.lookAt(camera_pos, self.camera_target, glm.vec3(0, 0, 1))
        
        mvp = projection * view
        
        # Convert GLM matrix to bytes for ModernGL
        mvp_bytes = np.array(mvp, dtype='f4').tobytes()
        
        # Set uniform
        self.prog['mvp'].write(mvp_bytes)
        
        # Render voxels
        self.vao.render(moderngl.TRIANGLES, instances=self.instance_count)
        
        # Debug: Print render info occasionally
        if self.frame_count % 120 == 0:  # Every 2 seconds at 60fps
            print(f"🎨 Rendering {self.instance_count} voxel instances")
        
        self.frame_count += 1
        
    def set_voxels(self, voxels):
        """Load and render voxel data"""
        self.voxels = voxels
        self.grid_size = voxels.shape
        
        # Ensure OpenGL is initialized before rendering
        if self.ctx is None:
            print("⚠️ OpenGL not initialized yet, deferring render")
            QTimer.singleShot(100, self.render_voxels)
        else:
            self.render_voxels()
        
    def render_voxels(self):
        """Create GPU buffers for voxel rendering"""
        if self.voxels is None or self.ctx is None:
            return
            
        # Find non-air voxels
        coords = np.argwhere(self.voxels > 0)
        
        if len(coords) == 0:
            print("⚠️ No voxels to render")
            return
            
        # Create cube vertices (cube from 0,0,0 to voxel_size,voxel_size,voxel_size)
        s = self.voxel_size
        cube_vertices = np.array([
            # Front face (Z+)
            0, 0, s,  s, 0, s,  s, s, s,
            0, 0, s,  s, s, s,  0, s, s,
            # Back face (Z-)
            0, 0, 0,  s, s, 0,  s, 0, 0,
            0, 0, 0,  0, s, 0,  s, s, 0,
            # Top face (Y+)
            0, s, 0,  0, s, s,  s, s, s,
            0, s, 0,  s, s, s,  s, s, 0,
            # Bottom face (Y-)
            0, 0, 0,  s, 0, s,  0, 0, s,
            0, 0, 0,  s, 0, 0,  s, 0, s,
            # Right face (X+)
            s, 0, 0,  s, s, s,  s, 0, s,
            s, 0, 0,  s, s, 0,  s, s, s,
            # Left face (X-)
            0, 0, 0,  0, 0, s,  0, s, s,
            0, 0, 0,  0, s, s,  0, s, 0,
        ], dtype='f4')
        
        # Create per-instance data (position + color for each voxel)
        instance_data = []
        for x, y, z in coords:
            material = self.voxels[x, y, z]
            color = get_material_color(material)
            
            # Position offset
            pos_x = x * self.voxel_size
            pos_y = y * self.voxel_size
            pos_z = z * self.voxel_size
            
            # Add instance: [offset_x, offset_y, offset_z, color_r, color_g, color_b]
            instance_data.append([pos_x, pos_y, pos_z, color[0], color[1], color[2]])
        
        instance_data = np.array(instance_data, dtype='f4')
        self.instance_count = len(instance_data)
        
        # Create buffers
        vbo_vertices = self.ctx.buffer(cube_vertices.tobytes())
        vbo_instances = self.ctx.buffer(instance_data.tobytes())
        
        # Create VAO
        self.vao = self.ctx.vertex_array(
            self.prog,
            [
                (vbo_vertices, '3f', 'in_position'),
                (vbo_instances, '3f 3f/i', 'in_offset', 'in_color'),  # /i = per-instance
            ]
        )
        
        print(f"✅ Rendered {self.instance_count:,} voxels with ModernGL (GPU instancing)")
        print(f"   Camera: distance={self.camera_distance}, target={self.camera_target}")
        
        # Force immediate repaint
        self.update()
        self.repaint()
        
    def update_voxel(self, x, y, z, material_id):
        """Update a single voxel"""
        if self.voxels is not None:
            self.voxels[x, y, z] = material_id
            # Re-render (optimize later to update only one instance)
            self.render_voxels()
            
    def set_mouse_config(self, config):
        """Update mouse configuration"""
        self.mouse_config = config.copy()
        
    def get_fps(self):
        """Get current FPS"""
        return self.fps
        
    def _update_fps(self):
        """Update FPS counter"""
        self.fps = self.frame_count
        self.frame_count = 0
        
    def mousePressEvent(self, ev):
        """Handle mouse press"""
        self.last_mouse_pos = ev.pos()
        
    def mouseMoveEvent(self, ev):
        """Handle mouse drag for camera control"""
        if self.last_mouse_pos is None:
            return
            
        delta = ev.pos() - self.last_mouse_pos
        self.last_mouse_pos = ev.pos()
        
        # Right-click: Orbit
        if ev.buttons() & Qt.MouseButton.RightButton:
            self.camera_rotation.x += delta.x() * 0.5
            self.camera_rotation.y = max(-89, min(89, self.camera_rotation.y + delta.y() * 0.5))
            self.update()
            
        # Middle-click: Pan
        elif ev.buttons() & Qt.MouseButton.MiddleButton:
            pan_speed = 0.01
            self.camera_target.x -= delta.x() * pan_speed
            self.camera_target.y += delta.y() * pan_speed
            self.update()
            
    def wheelEvent(self, ev):
        """Handle mouse wheel for zoom"""
        delta = ev.angleDelta().y()
        self.camera_distance *= 0.9 if delta > 0 else 1.1
        self.camera_distance = max(1.0, min(50.0, self.camera_distance))
        self.update()
        
    def keyPressEvent(self, ev):
        """Handle WASD camera panning"""
        pan_speed = 0.2
        
        if ev.key() == Qt.Key.Key_W:
            self.camera_target.y += pan_speed
        elif ev.key() == Qt.Key.Key_S:
            self.camera_target.y -= pan_speed
        elif ev.key() == Qt.Key.Key_A:
            self.camera_target.x -= pan_speed
        elif ev.key() == Qt.Key.Key_D:
            self.camera_target.x += pan_speed
        elif ev.key() == Qt.Key.Key_Q:
            self.camera_target.z -= pan_speed
        elif ev.key() == Qt.Key.Key_E:
            self.camera_target.z += pan_speed
        else:
            super().keyPressEvent(ev)
            return
            
        self.update()
