# Inspector Enhancements for VRChat

## Overview

This Unity editor extension enhances the Inspector window.
It provides convenient tools for manipulating blend shapes and transforming GameObjects, tailored for use in VRChat environments.

## Features

Depending what you have selected, it will display different options.

### Transform

- **Transform Operations:**
  - Quickly reset position, rotation, and scale of selected GameObjects.

### Skinned Meshes
- **Blend Shape Management:**
  - View and adjust blend shapes via a searchable list.
  - Copy blend shape names and values to the clipboard.
  - Paste blend shape values from the clipboard.
  - Set blend shape weights to 0 or 100.
  - Create a Modular avatar blendshape Sync 
    - Automatically creates ModularAvatarBlendshapeSync or adds to existing one the blendshape.
    - It searchs across the avatar a skinned mesh that have the same blendshape name.

- **Set Bounds:**
  - Adjust the center bounds of SkinnedMeshRenderer components to (0, 0, 0) and extent to (1, 1, 1).


### Physbones
- **Physbones:**
  - Create and remove physbones with predefined force settings.

## Usage

### Installation

1. **Manual Installation:**
   - Clone or download this repository.
   - Add the `InspectorEnhancements.cs` script to your Unity project's `Editor` folder.

2. **Unity Package:**
   - Alternatively, download the `InspectorEnhancements.unitypackage` file from the [releases](https://github.com/raspichu/VRC-Inspector-Enhancements/releases) section.
   - Import the package into your Unity project by double-clicking it or using `Assets -> Import Package -> Custom Package`.

### How to Use

- Open the Unity Editor and navigate to `Window -> Pichu -> Inspector Enhancements`.
- Select a GameObject with a SkinnedMeshRenderer to utilize blend shape and set bounds features.
- Use the transform operations section for resetting transforms easily.

**Note:** Transform operations are only visible when a SkinnedMeshRenderer is not selected.

### Future Additions

- ~~Support for selecting multiple SkinnedMeshRenderers to search across all of them.~~
- Option to translate Japanese blend shapes for display purposes only (not applied to the mesh itself).
- Add/modify different predefined physbones.

## License
This project is licensed under the MIT License

## Author
- [Pichu](https://github.com/raspichu)
