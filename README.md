# minol ⏱️⌨

A lightweight, always-on-top Windows overlay that displays a timer and keyboard input with a fully customizable appearance that can be saved and set as the default theme. Includes a Value Analyzer tool that captures a grayscale screenshot of your display, and lets you adjust the black and white extremes, as well as control the number of grey tones between.

## 💾 Installation

1. **Download** the latest release: [minol.zip](https://github.com/kentmcamp/minimal-streaming-overlay/releases/tag/1.0.1)
2. **Extract** the archive to desired location
3. **Run** `minol.exe`

*No installation required. The app works immediately out of the zip file.*

## 🆕 Recent Updates

- v1.0.1 - 2026-02-16
    - Public release of **minol**
    - Patch: Fixed issue where the *Value Viewer* window moved erratically when dragging or resizing.

## 🚧 Planned Updates
### Patches and Minor Updates Planned:
- **Key Renaming** - Some non letter keys (like brackets, numbers, special charactrers) display unclear names or generic assigning like OEM6. These need to be mapped to clearer names.
- **Enable/Disable Timer/Keyinput** - Options in the edit menu to disable/enable the timer and the keyinputs, so the user can use one without the other.
- **Edit Themes** - Right now themes can be edited only by entering the same name of a previously saved theme to overwrite it. Future updates should include an overwrite button directly in the menu.
- **Choose Directory** - The user should be able to change the theme and saved configurations directory themselves
- **Double Click to Reset Timer** - Just like how single clicking on the app overlay pauses the timer, double-clicking can reset the timer to zero.
- **Shortcut Click Options** - The option to click to pause/unpause and later, double click to reset the timer, should have options in the edit menu to enable/disable these features.
- **Preview Font** - See each font name in the list using its own font family, so users can see what the font looks like before selecting.
- **Preview Configurations** - A way to see what the overlay (with timer, key input, and background) will look like before applying settings.
- **Change Edit to Settings** - The "Edit" option should probably just say "Settings".
- **Context Menu Icons** - Have icons for the options in the context menu.
- **Pause/Play Icons** - Temporarily show a pause or play icon when the user pauses/unpauses the timer via clicking on the overlay. 

### Full Features Planned:
- **Color Picker** - A streamlined color picker option in the context menu.

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

