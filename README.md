# minol ⏱️⌨

A lightweight, always-on-top Windows overlay that displays a timer and keyboard input with configurable appearance. Perfect for streaming, or recording content such as software tutorials.

## 📥 Installation

1. **Download** the latest release: [minol225rc1.7z](https://github.com/kentmcamp/minimal-streaming-overlay/releases/tag/2.2.5rc1)
2. **Extract** the archive to desired location
3. **Run** `minol.exe`

No installation required. The app works immediately.

## ✨ Features

### Core Timer Features
- ⏱️ **Timer** - Click to pause/resume
- 🎹 **Keyboard input display** - Shows keys pressed including key combinations.
- 👁️ **Always-on-top overlay** - Stays visible above all windows.

### Appearance & Customization
- 🎨 **Full theme management** - Save, load, and delete custom themes.
- 🔧 **Configurable styling** - Font family, size, colors, background color and opacity.
- 📍 **Window positioning** - Anchor to corner with custom margins.

### Image Tonal Value Analysis
- 🔍 **Analyze Tonal Values** - Displays a screenshot of the current display converted to grayscale. With options for the following live adjustments...
  - **Gray Levels** - Adjustable posterization (2-20 levels)
  - **Black/White levels** - Dynamic range remapping
- 🔄 **Image transforms** - Flip horizontal/vertical
- 🖱️ **Draggable & resizable** - Move and resize the analyzer window freely

## 🎮 Usage

### Main Overlay
- **Left-click** the timer to pause/resume
- **Right-click** to open context menu:
  - Edit settings
  - Start/Pause/Reset timer
  - Analyze Values (Value Inspector)
  - Exit application
- **Drag** the overlay to reposition

### Analyze Values Window
When you right-click and select "Analyze Values":
1. A screenshot of your monitor is captured
2. The image opens in grayscale in a new window
3. Use sliders to inspect pixel values:
   - Adjust **Gray Levels** to see banding/quantization
   - Use **Black/White levels** to remap the tonal range
   - **Flip** horizontally or vertically to examine different areas
4. **Drag** the window to move it, **drag edges** to resize
5. **Exit** button closes the analyzer

## ⚙️ Settings & Themes
Settings are stored in: `%AppData%/minol/settings.json`

## 🛠️ System Requirements
- Windows 10 or later
- .NET 8 Runtime (bundle included in minol.exe)
- Display resolution: 1920x1080 or higher recommended

## 📝 License
MIT License - Feel free to use, modify, and distribute.

---

**Created by [Kent](https://github.com/kentmcamp)**


