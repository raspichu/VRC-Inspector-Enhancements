# Inspector Enhancements for VRChat

## Overview

This Unity editor extension enhances the Inspector window. It provides convenient tools for manipulating blend shapes and transforming GameObjects, tailored for use in VRChat environments.

## Features

Depending on what you have selected, it will display different options.

### Transform

- **Transform Operations:**
  - Quickly reset position, rotation, and scale of selected GameObjects.
  - Able to distrubite selected items along X axes

### Skinned Meshes

- **Blend Shape Management:**
  - View and adjust blend shapes via a searchable list.
  - Copy blend shape names and values to the clipboard.
  - Paste blend shape values from the clipboard.
  - Set blend shape weights to 0 or 100.
  - Create a Modular avatar blendshape Sync:
    - Automatically creates ModularAvatarBlendshapeSync or adds to an existing one the blendshape.
    - It searches across the avatar for a skinned mesh that has the same blendshape name.
  - Create a Modular avatar Shape Changer with that blendshape as Delete
  - Create a blendshape toggle with Vixen from the blendshape management.
  - Create PA Delete Polygons with that blendshape and fileterd skinned mesh:

- **Set Bounds:**
  - Adjust the center bounds of SkinnedMeshRenderer components to (0, 0, 0) and extent to (1, 1, 1).

### Vixen Toggles

- **GameObject Toggles:**
  - Create Vixen toggles for each or all selected GameObjects.

### MA Merge Armature

- **Copy Avatar Scale Adjuster to Armature**
  - Added a button on Merge Armature to copy MA Scale Adjuster to the clothes

## Usage
### How to Use

- Select a GameObject with a SkinnedMeshRenderer to utilize blend shape and set bounds features.
- Use the transform operations section for resetting transforms easily.
- Use the Vixen toggles section to create toggles for GameObjects or blend shapes.

**Note:** Transform operations are only visible when a SkinnedMeshRenderer is not selected.

### Future Additions

- ~~Support for selecting multiple SkinnedMeshRenderers to search across all of them.~~
- Option to translate Japanese blend shapes for display purposes only (not applied to the mesh itself).
- Add/modify different predefined physbones.

## Installation
1. **VCC Listing**
   - Go to [My VRChat Creator Companion listing](https://raspichu.github.io/vpm-listing/)
   - Press "Add to VCC"
   - Go to your project in `VCC -> Manage Project`
   - Search and add `Pichu Inspector Enhancements`
     
2. **Manual Installation:**
   - Clone or download this repository.
   - Add the `InspectorEnhancements.cs` script to your Unity project's `Editor` folder.

3. **Unity Package:**
   - Alternatively, download the `InspectorEnhancements.unitypackage` file from the [releases](https://github.com/raspichu/VRC-Inspector-Enhancements/releases) section.
   - Import the package into your Unity project by double-clicking it or using `Assets -> Import Package -> Custom Package`.

## License

This project is licensed under the MIT License.

## Author

- [Pichu](https://github.com/raspichu)
