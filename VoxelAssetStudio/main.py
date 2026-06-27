# Steel Tide: Voxel Asset Studio
# main.py - Application entry point

import sys
import os
from PyQt6.QtWidgets import QApplication
from PyQt6.QtGui import QPalette, QColor
from PyQt6.QtCore import Qt
from voxel_editor import VoxelEditor

def set_dark_theme(app):
    """Apply dark theme (like Kalshi Dashboard!)"""
    app.setStyle('Fusion')
    
    dark_palette = QPalette()
    dark_palette.setColor(QPalette.ColorRole.Window, QColor(53, 53, 53))
    dark_palette.setColor(QPalette.ColorRole.WindowText, Qt.GlobalColor.white)
    dark_palette.setColor(QPalette.ColorRole.Base, QColor(25, 25, 25))
    dark_palette.setColor(QPalette.ColorRole.AlternateBase, QColor(53, 53, 53))
    dark_palette.setColor(QPalette.ColorRole.ToolTipBase, Qt.GlobalColor.white)
    dark_palette.setColor(QPalette.ColorRole.ToolTipText, Qt.GlobalColor.white)
    dark_palette.setColor(QPalette.ColorRole.Text, Qt.GlobalColor.white)
    dark_palette.setColor(QPalette.ColorRole.Button, QColor(53, 53, 53))
    dark_palette.setColor(QPalette.ColorRole.ButtonText, Qt.GlobalColor.white)
    dark_palette.setColor(QPalette.ColorRole.BrightText, Qt.GlobalColor.red)
    dark_palette.setColor(QPalette.ColorRole.Link, QColor(42, 130, 218))
    dark_palette.setColor(QPalette.ColorRole.Highlight, QColor(42, 130, 218))
    dark_palette.setColor(QPalette.ColorRole.HighlightedText, Qt.GlobalColor.black)
    
    app.setPalette(dark_palette)

def main():
    """Application entry point"""
    print("=" * 50)
    print("  Voxel Asset Studio - Steel Tide")
    print("=" * 50)
    print()
    
    app = QApplication(sys.argv)
    
    # Apply dark theme
    set_dark_theme(app)
    
    # Create main window
    editor = VoxelEditor()
    editor.show()
    
    # Auto-load HighDensity32.stasset if it exists
    default_file = "../My project/Assets/StreamingAssets/HighDensity32.stasset"
    if os.path.exists(default_file):
        print(f"📂 Auto-loading: {default_file}")
        editor.load_asset(default_file)
    else:
        print(f"⚠️ Default file not found: {default_file}")
        print("   Use File → Open to load a voxel asset")
    
    print()
    print("✅ Application ready!")
    print()
    print("🖱️ Mouse Controls:")
    print("   - Left-click: Paint/interact with voxels")
    print("   - Middle-click: Pan camera")
    print("   - Right-click: Orbit camera")
    print("   - Wheel: Zoom in/out")
    print()
    print("⚠️ Note: Voxel clicking not yet implemented")
    print()
    
    sys.exit(app.exec())

if __name__ == "__main__":
    main()
