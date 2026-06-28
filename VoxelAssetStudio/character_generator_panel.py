# Steel Tide: Voxel Asset Studio
# character_generator_panel.py - Advanced character generation with part locking

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QPushButton, 
                             QLabel, QGroupBox, QCheckBox, QSpinBox, QComboBox)
from PyQt6.QtCore import pyqtSignal, Qt
import time

class CharacterGeneratorPanel(QWidget):
    """
    Advanced character generation panel with:
    - Seed-based variation
    - Part locking (head, torso, arms, legs)
    - Material selection per part
    - Re-generate with locked parts
    """
    
    generate_requested = pyqtSignal(dict)  # Emits generation parameters
    
    def __init__(self, parent=None):
        super().__init__(parent)
        
        # Generation state
        self.current_seed = None
        self.locked_parts = {
            'head': False,
            'torso': False,
            'arms': False,
            'legs': False,
            'entire': False
        }
        
        # Stored part data (for locking)
        self.part_data = {
            'head': None,
            'torso': None,
            'arms': None,
            'legs': None
        }
        
        self.init_ui()
        
    def init_ui(self):
        """Build the UI"""
        layout = QVBoxLayout()
        layout.setAlignment(Qt.AlignmentFlag.AlignTop)
        
        # Title
        title = QLabel("🤖 Character Generator")
        title.setStyleSheet("font-size: 16px; font-weight: bold; padding: 10px;")
        layout.addWidget(title)
        
        # === SIZE CONTROLS ===
        size_group = QGroupBox("Size")
        size_layout = QVBoxLayout()
        
        # Height
        height_row = QHBoxLayout()
        height_row.addWidget(QLabel("Height:"))
        self.height_spin = QSpinBox()
        self.height_spin.setRange(16, 64)
        self.height_spin.setValue(32)
        self.height_spin.setSuffix(" voxels")
        height_row.addWidget(self.height_spin)
        size_layout.addLayout(height_row)
        
        size_group.setLayout(size_layout)
        layout.addWidget(size_group)
        
        # === MATERIAL SELECTION ===
        material_group = QGroupBox("Materials")
        material_layout = QVBoxLayout()
        
        # Head material
        head_row = QHBoxLayout()
        head_row.addWidget(QLabel("Head:"))
        self.head_material_combo = QComboBox()
        self._populate_material_combo(self.head_material_combo, default=4)  # Flesh
        head_row.addWidget(self.head_material_combo)
        material_layout.addLayout(head_row)
        
        # Body material
        body_row = QHBoxLayout()
        body_row.addWidget(QLabel("Body:"))
        self.body_material_combo = QComboBox()
        self._populate_material_combo(self.body_material_combo, default=11)  # Uniform
        body_row.addWidget(self.body_material_combo)
        material_layout.addLayout(body_row)
        
        # Limb material
        limb_row = QHBoxLayout()
        limb_row.addWidget(QLabel("Limbs:"))
        self.limb_material_combo = QComboBox()
        self._populate_material_combo(self.limb_material_combo, default=20)  # Plasteel
        limb_row.addWidget(self.limb_material_combo)
        material_layout.addLayout(limb_row)
        
        # Armor material
        armor_row = QHBoxLayout()
        armor_row.addWidget(QLabel("Armor:"))
        self.armor_material_combo = QComboBox()
        self._populate_material_combo(self.armor_material_combo, default=20)  # Plasteel
        armor_row.addWidget(self.armor_material_combo)
        material_layout.addLayout(armor_row)
        
        material_group.setLayout(material_layout)
        layout.addWidget(material_group)
        
        # === PART LOCKING ===
        lock_group = QGroupBox("🔒 Lock Parts")
        lock_layout = QVBoxLayout()
        
        lock_info = QLabel("Lock parts to keep them when re-generating:")
        lock_info.setWordWrap(True)
        lock_info.setStyleSheet("font-size: 10px; color: #888;")
        lock_layout.addWidget(lock_info)
        
        self.lock_head_cb = QCheckBox("Lock Head")
        self.lock_head_cb.stateChanged.connect(lambda: self._update_lock('head', self.lock_head_cb.isChecked()))
        lock_layout.addWidget(self.lock_head_cb)
        
        self.lock_torso_cb = QCheckBox("Lock Torso")
        self.lock_torso_cb.stateChanged.connect(lambda: self._update_lock('torso', self.lock_torso_cb.isChecked()))
        lock_layout.addWidget(self.lock_torso_cb)
        
        self.lock_arms_cb = QCheckBox("Lock Arms")
        self.lock_arms_cb.stateChanged.connect(lambda: self._update_lock('arms', self.lock_arms_cb.isChecked()))
        lock_layout.addWidget(self.lock_arms_cb)
        
        self.lock_legs_cb = QCheckBox("Lock Legs")
        self.lock_legs_cb.stateChanged.connect(lambda: self._update_lock('legs', self.lock_legs_cb.isChecked()))
        lock_layout.addWidget(self.lock_legs_cb)
        
        self.lock_entire_cb = QCheckBox("🔐 Lock Entire Character")
        self.lock_entire_cb.setStyleSheet("font-weight: bold;")
        self.lock_entire_cb.stateChanged.connect(lambda: self._update_lock('entire', self.lock_entire_cb.isChecked()))
        lock_layout.addWidget(self.lock_entire_cb)
        
        lock_group.setLayout(lock_layout)
        layout.addWidget(lock_group)
        
        # === GENERATION BUTTONS ===
        button_layout = QVBoxLayout()
        
        self.generate_btn = QPushButton("✨ Generate Character")
        self.generate_btn.clicked.connect(self._on_generate)
        self.generate_btn.setStyleSheet("font-size: 14px; padding: 10px; background-color: #4CAF50; color: white;")
        button_layout.addWidget(self.generate_btn)
        
        self.regenerate_btn = QPushButton("🔄 Re-Generate (New Variation)")
        self.regenerate_btn.clicked.connect(self._on_regenerate)
        self.regenerate_btn.setEnabled(False)
        self.regenerate_btn.setStyleSheet("font-size: 12px; padding: 8px;")
        button_layout.addWidget(self.regenerate_btn)
        
        self.unlock_all_btn = QPushButton("🔓 Unlock All Parts")
        self.unlock_all_btn.clicked.connect(self._unlock_all)
        self.unlock_all_btn.setStyleSheet("font-size: 10px; padding: 6px;")
        button_layout.addWidget(self.unlock_all_btn)
        
        layout.addLayout(button_layout)
        
        # Seed display
        self.seed_label = QLabel("Seed: None")
        self.seed_label.setStyleSheet("font-size: 10px; color: #666; padding: 5px;")
        layout.addWidget(self.seed_label)
        
        self.setLayout(layout)
        self.setMaximumWidth(300)
        
    def _populate_material_combo(self, combo, default=3):
        """Populate material combo box with common materials"""
        from material_library import get_material_name
        
        materials = [
            (0, "Air"),
            (1, "Prefab Composite"),
            (2, "Regolith Concrete"),
            (3, "Concrete"),
            (4, "Flesh"),
            (5, "Durasteel"),
            (11, "Uniform"),
            (16, "Ablative Plating"),
            (17, "Reactive Armor"),
            (20, "Plasteel Panels")
        ]
        
        for mat_id, name in materials:
            combo.addItem(f"{name} ({mat_id})", mat_id)
            if mat_id == default:
                combo.setCurrentIndex(combo.count() - 1)
                
    def _update_lock(self, part, locked):
        """Update lock state for a part"""
        self.locked_parts[part] = locked
        
        # If entire character is locked, lock all parts
        if part == 'entire' and locked:
            self.lock_head_cb.setChecked(True)
            self.lock_torso_cb.setChecked(True)
            self.lock_arms_cb.setChecked(True)
            self.lock_legs_cb.setChecked(True)
            
        print(f"🔒 {part.capitalize()} {'locked' if locked else 'unlocked'}")
        
    def _unlock_all(self):
        """Unlock all parts"""
        self.lock_head_cb.setChecked(False)
        self.lock_torso_cb.setChecked(False)
        self.lock_arms_cb.setChecked(False)
        self.lock_legs_cb.setChecked(False)
        self.lock_entire_cb.setChecked(False)
        print("🔓 All parts unlocked")
        
    def _on_generate(self):
        """Generate new character"""
        self.current_seed = int(time.time() * 1000) % 10000
        self._generate_with_seed(self.current_seed)
        self.regenerate_btn.setEnabled(True)
        
    def _on_regenerate(self):
        """Re-generate with new seed but keep locked parts"""
        new_seed = int(time.time() * 1000) % 10000
        self.current_seed = new_seed
        self._generate_with_seed(new_seed)
        
    def _generate_with_seed(self, seed):
        """Generate character with specific seed"""
        params = {
            'height': self.height_spin.value(),
            'head_material': self.head_material_combo.currentData(),
            'body_material': self.body_material_combo.currentData(),
            'limb_material': self.limb_material_combo.currentData(),
            'armor_material': self.armor_material_combo.currentData(),
            'seed': seed,
            'locked_parts': self.locked_parts.copy(),
            'part_data': self.part_data.copy()
        }
        
        self.seed_label.setText(f"Seed: {seed}")
        self.generate_requested.emit(params)
        
        locked_count = sum(1 for locked in self.locked_parts.values() if locked)
        if locked_count > 0:
            print(f"🔄 Re-generating with {locked_count} locked part(s)")
        else:
            print(f"✨ Generating new character (seed: {seed})")
            
    def store_part_data(self, part_name, data):
        """Store part data for locking (called by main editor after generation)"""
        self.part_data[part_name] = data
        
    def get_generation_params(self):
        """Get current generation parameters"""
        return {
            'height': self.height_spin.value(),
            'head_material': self.head_material_combo.currentData(),
            'body_material': self.body_material_combo.currentData(),
            'limb_material': self.limb_material_combo.currentData(),
            'armor_material': self.armor_material_combo.currentData(),
            'seed': self.current_seed
        }
