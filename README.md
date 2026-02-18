# minol ⏱️⌨

A lightweight, always-on-top Windows overlay that displays a timer and keyboard input with a fully customizable appearance that can be saved and set as the default theme. Includes a Value Analyzer tool that captures a grayscale screenshot of your display, and lets you adjust the black and white extremes, as well as control the number of grey tones between.

## 💾 Installation

1. **Download** the latest release: [minol26.7z](https://github.com/kentmcamp/minimal-streaming-overlay/releases/tag/1.2.6)
2. **Extract** the archive to desired location
3. **Run** `minol.exe`

*No installation required. The app works immediately out of the zip file.*

## 🆕 Recent Updates
- v1.2.6 Stable Release
    - Added countdown function
    - Added icons to context menu
    - Remapped numbers and misc. special characters- 

## 🚧 Planned Updates
### Fixes and Patches Planned
- **Overwright Themes** - Right now themes can be edited only by entering the same name of a previously saved theme to overwrite it. Future updates should include an overwrite button directly in the menu.
- **Choose Directory** - The user should be able to change the theme and saved configurations directory themselves
- **Optimize Settings**
    - Make confirmation box on "Load Selected Screen" have an ok an cancel button. Clicking ok should instantly load theme.

## ✨ Features

### Core Timer Features
- ⏱️ **Timer** - Click to pause/resume
- 🎹 **Keyboard input display** - Shows keys pressed including key combinations.
- 🔲 **Value Viewer** - Capture a grayscale screenshot of your display, adjust black and white extremes, and change the number of gray tones.
- 👁️ **Always-on-top overlay** - Stays visible above all windows.

### Appearance & Customization
- 🎨 **Full theme management** - Save, load, and delete custom themes.
- 🔧 **Configurable styling** - Font family, size, colors, background color and opacity.
- 📍 **Window positioning** – Anchor the overlay to any corner with adjustable margins.

### Tonal Value Viewer
- 🔍 **Analyze Tonal Values** - Displays a screenshot of the current display in grayscale, with options for the following live adjustments...
    - 🎚️ **Gray Levels** - Adjust the number of visible tones (2–255).
    - 🎛️ **Black/White levels** - Remap the tonal range by adjusting black and white extremes.
    - 🔄 **Image transforms** - Flip the image horizontally or vertically for alternative perspective checks.

## 🎮 Usage

### Main Overlay
- **Left-click** the timer to pause/resume
- **Right-click** to open context menu:
  - Edit settings
  - Start/Pause/Reset timer
  - Analyze Values (Value/Greytones Viewer)
  - Exit application
- **Click, Hold & Drag** the overlay to reposition

### Values Analyzer
When you right-click and select "Analyze Values":
1. A screenshot of your monitor is captured
2. The image opens in grayscale in a new window
3. Use sliders to inspect pixel values:
   - Adjust **Gray Levels** to adjust the number of tones between black and white.
   - Use **Black/White levels** to remap the tonal range
   - **Flip** horizontally or vertically to see different perspectives.
4. **Drag** the window to move the window.
5. **Drag the outer edges** of the window to resize it.
6. **Exit** button closes the analyzer

### Theme Manager
1. **Right-click** to open the context menu, and select **Edit**
2. Choose the **Themes** tab in the top menu
3. Save any settings currently applied under the **Save Current Configuration** section.
    - Enter the name of your theme.
    - Click **Save Theme**
4. Load previously saved themes under the **Available Themes** section.
    - Click the theme you wish to load.
    - Click **Load Selected Theme**
    - Click **Apply** in the bottom, right.
    - You can also click **Delete Selected** to remove a theme from the list.

## ⚙️ Local Storage
Settings and Theme Configurations are stored in: `%AppData%/minol/settings.json`

## 🛠️ System Requirements
- Windows 10 or later
- .NET 8 Runtime
- Display resolution: 1920x1080 or higher recommended

## 📝 License
MIT License - Feel free to use, modify, and distribute.

Created by [Kent](https://github.com/kentmcamp)

