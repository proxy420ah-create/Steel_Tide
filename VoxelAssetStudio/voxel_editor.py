# Steel Tide: Voxel Asset Studio
# voxel_editor.py - Main application window

from PyQt6.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout,
                             QMenuBar, QStatusBar, QFileDialog, QMessageBox)
from PyQt6.QtCore import QTimer, Qt
from PyQt6.QtGui import QAction
import numpy as np
import os

# For now, use pyqtgraph - it's simpler and actually works
from viewport_widget import VoxelViewport
print("✅ Using pyqtgraph viewport (fast and reliable)")
    
from tool_panel import ToolPanel
from brush_panel import BrushPanel
from settings_dialog import SettingsDialog
from stasset_io import load_stasset, save_stasset
from shape_generator import (generate_cube, generate_sphere, generate_hollow_shell, generate_test_cube,
                             generate_armored_cube, generate_armored_sphere, generate_armored_cylinder,
                             generate_truly_hollow_sphere, generate_truly_hollow_cube)

class VoxelEditor(QMainWindow):
    """Main application window for Voxel Asset Studio"""
    
    def __init__(self):
        super().__init__()
        
        self.setWindowTitle("Voxel Asset Studio - Steel Tide")
        self.resize(1400, 900)
        
        # Data
        self.voxels = None
        self.grid_size = (32, 32, 32)
        self.current_file = None
        self.modified = False
        
        # Settings
        self.settings = {
            'highlight_hover': True,
            'brush_size': 1
        }
        
        # Build UI
        self.init_ui()
        self.init_menu()
        
        # Status bar update timer
        self.status_timer = QTimer()
        self.status_timer.timeout.connect(self.update_status)
        self.status_timer.start(100)  # Update 10x per second
        
    def init_ui(self):
        """Build the main UI"""
        central = QWidget()
        main_layout = QHBoxLayout()
        
        # Left sidebar: Tool panel + Controls panel
        left_sidebar = QWidget()
        left_layout = QVBoxLayout()
        left_layout.setContentsMargins(0, 0, 0, 0)
        
        # Tool panel
        self.tool_panel = ToolPanel()
        self.tool_panel.material_changed.connect(self.on_material_changed)
        self.tool_panel.tool_changed.connect(self.on_tool_changed)
        left_layout.addWidget(self.tool_panel)
        
        # Brush panel
        self.brush_panel = BrushPanel()
        self.brush_panel.brush_size_changed.connect(self.on_brush_size_changed)
        left_layout.addWidget(self.brush_panel)
        
        left_sidebar.setLayout(left_layout)
        main_layout.addWidget(left_sidebar)
        
        # Right: 3D viewport
        self.viewport = VoxelViewport()
        self.viewport.voxel_clicked.connect(self.on_voxel_clicked)
        main_layout.addWidget(self.viewport, stretch=1)
        
        central.setLayout(main_layout)
        self.setCentralWidget(central)
        
        # Status bar
        self.statusBar().showMessage("Ready")
        
    def init_menu(self):
        """Create menu bar"""
        menubar = self.menuBar()
        
        # File menu
        file_menu = menubar.addMenu("File")
        
        open_action = QAction("Open...", self)
        open_action.setShortcut("Ctrl+O")
        open_action.triggered.connect(self.open_file)
        file_menu.addAction(open_action)
        
        save_action = QAction("Save", self)
        save_action.setShortcut("Ctrl+S")
        save_action.triggered.connect(self.save_file)
        file_menu.addAction(save_action)
        
        save_as_action = QAction("Save As...", self)
        save_as_action.setShortcut("Ctrl+Shift+S")
        save_as_action.triggered.connect(self.save_file_as)
        file_menu.addAction(save_as_action)
        
        file_menu.addSeparator()
        
        exit_action = QAction("Exit", self)
        exit_action.setShortcut("Ctrl+Q")
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)
        
        # View menu
        view_menu = menubar.addMenu("View")
        
        reset_camera_action = QAction("Reset Camera", self)
        reset_camera_action.setShortcut("Home")
        reset_camera_action.triggered.connect(self.reset_camera)
        view_menu.addAction(reset_camera_action)
        
        # Generate menu
        generate_menu = menubar.addMenu("Generate")
        
        test_cube_action = QAction("Test Cube (8×8×8)", self)
        test_cube_action.triggered.connect(self.generate_test_cube)
        generate_menu.addAction(test_cube_action)
        
        cube_action = QAction("Cube (32×32×32)", self)
        cube_action.triggered.connect(self.generate_full_cube)
        generate_menu.addAction(cube_action)
        
        sphere_action = QAction("Sphere (radius 15)", self)
        sphere_action.triggered.connect(self.generate_sphere)
        generate_menu.addAction(sphere_action)
        
        generate_menu.addSeparator()
        
        # Armored shapes (Steel shell + Concrete core - DENSE)
        armored_cube_action = QAction("🛡️ Armored Cube (Steel + Concrete)", self)
        armored_cube_action.triggered.connect(self.generate_armored_cube)
        generate_menu.addAction(armored_cube_action)
        
        armored_sphere_action = QAction("🛡️ Armored Sphere (Steel + Concrete)", self)
        armored_sphere_action.triggered.connect(self.generate_armored_sphere)
        generate_menu.addAction(armored_sphere_action)
        
        armored_cylinder_action = QAction("🛡️ Armored Cylinder (Steel + Concrete)", self)
        armored_cylinder_action.triggered.connect(self.generate_armored_cylinder)
        generate_menu.addAction(armored_cylinder_action)
        
        generate_menu.addSeparator()
        
        # Truly hollow shapes (Empty interior - LIGHTWEIGHT)
        hollow_sphere_action = QAction("⭕ Truly Hollow Sphere (Empty Interior)", self)
        hollow_sphere_action.triggered.connect(self.generate_truly_hollow_sphere)
        generate_menu.addAction(hollow_sphere_action)
        
        hollow_cube_action = QAction("⭕ Truly Hollow Cube (Empty Interior)", self)
        hollow_cube_action.triggered.connect(self.generate_truly_hollow_cube)
        generate_menu.addAction(hollow_cube_action)
        
        # Options menu
        options_menu = menubar.addMenu("Options")
        
        settings_action = QAction("Settings...", self)
        settings_action.triggered.connect(self.open_settings)
        options_menu.addAction(settings_action)
        
    def load_asset(self, filepath):
        """Load .stasset file"""
        try:
            self.voxels, self.grid_size = load_stasset(filepath)
            self.viewport.set_voxels(self.voxels)
            self.current_file = filepath
            self.modified = False
            self.update_title()
            self.statusBar().showMessage(f"✅ Loaded: {os.path.basename(filepath)}")
        except Exception as e:
            QMessageBox.critical(self, "Error", f"Failed to load file:\n{e}")
            print(f"❌ Error loading {filepath}: {e}")
            
    def open_file(self):
        """Open file dialog"""
        filepath, _ = QFileDialog.getOpenFileName(
            self, "Open Voxel Asset", 
            "../My project/Assets/StreamingAssets",
            "Voxel Assets (*.stasset);;All Files (*.*)"
        )
        if filepath:
            self.load_asset(filepath)
            
    def save_file(self):
        """Save to current file"""
        if self.current_file is None:
            self.save_file_as()
        else:
            self.save_to_file(self.current_file)
            
    def save_file_as(self):
        """Save as dialog"""
        filepath, _ = QFileDialog.getSaveFileName(
            self, "Save Voxel Asset",
            "../My project/Assets/StreamingAssets",
            "Voxel Assets (*.stasset);;All Files (*.*)"
        )
        if filepath:
            if not filepath.endswith('.stasset'):
                filepath += '.stasset'
            self.save_to_file(filepath)
            
    def save_to_file(self, filepath):
        """Save voxels to file"""
        try:
            save_stasset(filepath, self.voxels)
            self.current_file = filepath
            self.modified = False
            self.update_title()
            self.statusBar().showMessage(f"✅ Saved: {os.path.basename(filepath)}")
        except Exception as e:
            QMessageBox.critical(self, "Error", f"Failed to save file:\n{e}")
            print(f"❌ Error saving {filepath}: {e}")
            
    def on_voxel_clicked(self, x, y, z):
        """Handle voxel click (paint or erase)"""
        if self.voxels is None:
            return
            
        tool = self.tool_panel.get_current_tool()
        
        if tool == "paint":
            material = self.tool_panel.get_current_material()
            self.voxels[x, y, z] = material
            self.viewport.update_voxel(x, y, z, material)
            print(f"🖌️ Painted voxel ({x}, {y}, {z}) with material {material}")
        elif tool == "erase":
            self.voxels[x, y, z] = 0  # Air
            self.viewport.update_voxel(x, y, z, 0)
            print(f"🗑️ Erased voxel ({x}, {y}, {z})")
            
        self.modified = True
        self.update_title()
        
    def on_material_changed(self, material_id):
        """Material selection changed"""
        pass  # Already handled by tool panel
        
    def on_tool_changed(self, tool_name):
        """Tool selection changed"""
        pass  # Already handled by tool panel
        
    def reset_camera(self):
        """Reset camera to default position"""
        self.viewport.setCameraPosition(distance=60, elevation=30, azimuth=45)
        
    def open_settings(self):
        """Open settings dialog"""
        dialog = SettingsDialog(self, self.settings)
        if dialog.exec():
            new_settings = dialog.get_settings()
            self.settings.update(new_settings)
            self.apply_settings()
            self.statusBar().showMessage("✅ Settings updated")
    
    def apply_settings(self):
        """Apply current settings to viewport"""
        # Apply highlight hover setting
        if hasattr(self.viewport, 'set_highlight_hover'):
            self.viewport.set_highlight_hover(self.settings['highlight_hover'])
    
    def on_brush_size_changed(self, size):
        """Handle brush size change"""
        self.settings['brush_size'] = size
        self.viewport.set_brush_size(size)
        self.statusBar().showMessage(f"✅ Brush size: {size} voxel{'s' if size > 1 else ''}")
        
    def generate_test_cube(self):
        """Generate a simple 8×8×8 test cube"""
        self.voxels = generate_test_cube()
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated 8×8×8 test cube")
        
    def generate_full_cube(self):
        """Generate a full 32×32×32 cube"""
        self.voxels = generate_cube((32, 32, 32), material_id=3)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated 32×32×32 cube")
        
    def generate_sphere(self):
        """Generate a sphere"""
        self.voxels = generate_sphere((32, 32, 32), radius=15, center=(16, 16, 16), material_id=3)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated sphere")
        
    def generate_shell(self):
        """Generate a hollow shell (DEPRECATED - actually two-layer, not hollow)"""
        self.voxels = generate_hollow_shell((32, 32, 32), outer_radius=15, inner_radius=12, center=(16, 16, 16), outer_material=5, inner_material=3)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("⚠️ Generated two-layer shell (NOT hollow - has concrete core)")
    
    def generate_armored_cube(self):
        """Generate armored cube (Steel shell + Concrete core)"""
        self.voxels = generate_armored_cube(size=(32, 32, 32), shell_thickness=2)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated armored cube (Steel + Concrete)")
    
    def generate_armored_sphere(self):
        """Generate armored sphere (Steel shell + Concrete core)"""
        self.voxels = generate_armored_sphere(grid_size=(32, 32, 32), radius=15, center=(16, 16, 16), shell_thickness=2)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated armored sphere (Steel + Concrete)")
    
    def generate_armored_cylinder(self):
        """Generate armored cylinder (Steel shell + Concrete core)"""
        self.voxels = generate_armored_cylinder(grid_size=(32, 32, 32), radius=12, height=28, center=(16, 16, 2), shell_thickness=2)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated armored cylinder (Steel + Concrete)")
    
    def generate_truly_hollow_sphere(self):
        """Generate truly hollow sphere (empty interior)"""
        self.voxels = generate_truly_hollow_sphere(grid_size=(32, 32, 32), outer_radius=15, shell_thickness=2, center=(16, 16, 16), material=5)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated truly hollow sphere (empty interior)")
    
    def generate_truly_hollow_cube(self):
        """Generate truly hollow cube (empty interior)"""
        self.voxels = generate_truly_hollow_cube(size=(32, 32, 32), shell_thickness=2, material=5)
        self.grid_size = self.voxels.shape
        self.viewport.set_voxels(self.voxels)
        self.modified = True
        self.update_title()
        self.statusBar().showMessage("✅ Generated truly hollow cube (empty interior)")
        
    def update_status(self):
        """Update status bar with live info"""
        if self.voxels is None:
            return
            
        voxel_count = np.count_nonzero(self.voxels)
        total = self.voxels.size
        memory_kb = (total * 2) / 1024  # 2 bytes per voxel
        fps = self.viewport.get_fps()
        
        status = (f"Grid: {self.grid_size[0]}×{self.grid_size[1]}×{self.grid_size[2]} | "
                 f"Voxels: {voxel_count:,}/{total:,} | "
                 f"Memory: {memory_kb:.1f} KB | "
                 f"FPS: {fps}")
        
        self.statusBar().showMessage(status)
        
    def update_title(self):
        """Update window title"""
        filename = os.path.basename(self.current_file) if self.current_file else "Untitled"
        modified = "*" if self.modified else ""
        self.setWindowTitle(f"Voxel Asset Studio - {filename}{modified}")
        
    def closeEvent(self, event):
        """Handle window close"""
        if self.modified:
            reply = QMessageBox.question(
                self, "Unsaved Changes",
                "You have unsaved changes. Save before closing?",
                QMessageBox.StandardButton.Save | 
                QMessageBox.StandardButton.Discard | 
                QMessageBox.StandardButton.Cancel
            )
            
            if reply == QMessageBox.StandardButton.Save:
                self.save_file()
                event.accept()
            elif reply == QMessageBox.StandardButton.Discard:
                event.accept()
            else:
                event.ignore()
        else:
            event.accept()
