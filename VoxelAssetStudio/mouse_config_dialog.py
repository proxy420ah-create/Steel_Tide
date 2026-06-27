# Steel Tide: Voxel Asset Studio
# mouse_config_dialog.py - Mouse controls configuration dialog

from PyQt6.QtWidgets import (QDialog, QVBoxLayout, QHBoxLayout, QLabel, 
                             QComboBox, QPushButton, QGroupBox, QMessageBox)
from PyQt6.QtCore import Qt, pyqtSignal

class MouseConfigDialog(QDialog):
    """Dialog for configuring mouse button assignments"""
    
    config_changed = pyqtSignal(dict)  # Emit new config when applied
    
    def __init__(self, current_config, parent=None):
        super().__init__(parent)
        
        self.setWindowTitle("Mouse Controls Configuration")
        self.setModal(True)
        self.resize(400, 300)
        
        self.current_config = current_config.copy()
        
        self.init_ui()
        
    def init_ui(self):
        """Build the dialog UI"""
        layout = QVBoxLayout()
        
        # Title
        title = QLabel("🖱️ Configure Mouse Controls")
        title.setStyleSheet("font-size: 16px; font-weight: bold; padding: 10px;")
        layout.addWidget(title)
        
        # Instructions
        instructions = QLabel("Assign mouse buttons to camera actions:")
        instructions.setStyleSheet("padding: 5px; color: #aaa;")
        layout.addWidget(instructions)
        
        # Configuration group
        config_group = QGroupBox("Button Assignments")
        config_layout = QVBoxLayout()
        
        # Orbit control
        orbit_layout = QHBoxLayout()
        orbit_layout.addWidget(QLabel("Orbit Camera:"))
        self.orbit_combo = QComboBox()
        self.orbit_combo.addItems(["Left Button", "Middle Button", "Right Button", "Disabled"])
        self.orbit_combo.setCurrentText(self.current_config.get("orbit", "Right Button"))
        orbit_layout.addWidget(self.orbit_combo)
        config_layout.addLayout(orbit_layout)
        
        # Pan control
        pan_layout = QHBoxLayout()
        pan_layout.addWidget(QLabel("Pan Camera:"))
        self.pan_combo = QComboBox()
        self.pan_combo.addItems(["Left Button", "Middle Button", "Right Button", "Disabled"])
        self.pan_combo.setCurrentText(self.current_config.get("pan", "Middle Button"))
        pan_layout.addWidget(self.pan_combo)
        config_layout.addLayout(pan_layout)
        
        # Paint/Interact control
        paint_layout = QHBoxLayout()
        paint_layout.addWidget(QLabel("Paint/Interact:"))
        self.paint_combo = QComboBox()
        self.paint_combo.addItems(["Left Button", "Middle Button", "Right Button", "Disabled"])
        self.paint_combo.setCurrentText(self.current_config.get("paint", "Left Button"))
        paint_layout.addWidget(self.paint_combo)
        config_layout.addLayout(paint_layout)
        
        config_group.setLayout(config_layout)
        layout.addWidget(config_group)
        
        # Presets
        preset_group = QGroupBox("Presets")
        preset_layout = QHBoxLayout()
        
        default_btn = QPushButton("Default (L:Paint, M:Pan, R:Orbit)")
        default_btn.clicked.connect(self.apply_default_preset)
        preset_layout.addWidget(default_btn)
        
        blender_btn = QPushButton("Blender-style (M:Orbit, Shift+M:Pan)")
        blender_btn.setEnabled(False)  # Not implemented yet
        blender_btn.setToolTip("Coming soon!")
        preset_layout.addWidget(blender_btn)
        
        preset_group.setLayout(preset_layout)
        layout.addWidget(preset_group)
        
        # Buttons
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        
        apply_btn = QPushButton("Apply")
        apply_btn.clicked.connect(self.apply_config)
        button_layout.addWidget(apply_btn)
        
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        button_layout.addWidget(cancel_btn)
        
        layout.addLayout(button_layout)
        
        self.setLayout(layout)
        
    def apply_default_preset(self):
        """Apply default mouse configuration"""
        self.orbit_combo.setCurrentText("Right Button")
        self.pan_combo.setCurrentText("Middle Button")
        self.paint_combo.setCurrentText("Left Button")
        
    def apply_config(self):
        """Validate and apply configuration"""
        new_config = {
            "orbit": self.orbit_combo.currentText(),
            "pan": self.pan_combo.currentText(),
            "paint": self.paint_combo.currentText()
        }
        
        # Check for conflicts (same button assigned to multiple actions)
        assignments = {}
        for action, button in new_config.items():
            if button != "Disabled":
                if button in assignments:
                    QMessageBox.warning(
                        self,
                        "Conflict Detected",
                        f"{button} is assigned to both '{assignments[button]}' and '{action}'.\n"
                        "Each button can only have one action."
                    )
                    return
                assignments[button] = action
        
        # Emit config and close
        self.config_changed.emit(new_config)
        self.accept()
