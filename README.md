# Color Ramp Baker

A Unity editor tool for baking color gradients into texture assets.

## Requirements

Unity 2021.3 or newer. No additional packages are required.

## Installation

In Unity, open **Window → Package Manager**, choose **Add package from git URL…**, and enter:

```text
https://github.com/FatLarva/unity-color-ramp-baker.git
```

The package ID is `com.fatlarva.colorrampbaker`. You can also clone the repository and use **Add package from disk…** to select its `package.json`, or add the repository as a Git submodule at `Packages/com.fatlarva.colorrampbaker`.

## Usage

### Create a color ramp

1. Open **Tools → Color Ramp Baker → Open**.
2. Enter a name, configure the gradient, and select a texture width from 64 to 1024 pixels.
3. Click **Generate & Save New Asset** and choose a location in your project's Assets folder.

The baker creates a single `GradientTextureAsset` containing an embedded `Texture2D` sub-asset. The texture is one pixel high, uses RGBA32 without mipmaps, and has bilinear filtering and clamp wrapping. No separate PNG is produced. Expand the asset in the Project window to access the texture and assign it to a material or another texture field.

### Edit an existing ramp

Drag a `GradientTextureAsset` into the **Load Asset** field, or right-click it in the Project window and choose **Edit Gradient Texture**.

Adjust the gradient, width, or name, then click **Resave Loaded Asset**. The existing texture is updated in place, including when its width changes, so material and scene references remain intact. Renaming the asset in the Project window also synchronizes the internal asset and texture names.

### Read the documentation

Choose **Tools → Color Ramp Baker → README** to read this documentation inside Unity. The window includes a contents menu, buttons to open the baker or edit the selected ramp, and copy buttons for code examples. The license also opens inside the documentation window. This works with both Git-installed and embedded packages.

## Runtime access

`GradientTextureAsset` remains available in player builds:

```csharp
using GradientTextureGenerator.Runtime;
using UnityEngine;

public class ColorRampExample : MonoBehaviour
{
    [SerializeField] private GradientTextureAsset _ramp;

    public Texture2D Texture => _ramp.Texture;
    public Gradient Gradient => _ramp.Gradient;
}
```

The baker window, context menu, and rename processor are editor-only.

## Compatibility with the original package

Color Ramp Baker replaces `com.gradienttexturegenerator.editor`. Remove the old package before installing this one. The original script GUIDs, C# namespaces, and assembly names (`GradientTextureGenerator.Runtime` and `GradientTextureGenerator.Editor`) are preserved so existing assets and assembly references remain valid.

## License

[MIT](LICENSE).
