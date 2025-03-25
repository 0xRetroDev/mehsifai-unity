# MeshifAI for Unity

![Untitledvideo-MadewithClipchamp12-ezgif com-optimize](https://github.com/user-attachments/assets/9db86905-4a6e-46fe-85f6-96f1e2708740)


MeshifAI is a powerful Unity plugin that generates 3D models from text descriptions using artificial intelligence. Simply describe what you want, and MeshifAI creates a fully-realized 3D model ready to use in your project.

## Features

- **Text-to-3D Generation**: Create 3D models from simple text descriptions
- **Editor Tool**: Generate models directly in the Unity Editor
- **Runtime API**: Generate models at runtime within your game or application
- **Material Support**: Automatic or custom material application
- **Multiple Generations**: Run several generations simultaneously
- **Generation Metadata**: Every model includes data about how it was created

## Quick Start

```csharp
using MeshifAI.Core;
using UnityEngine;

public class QuickStart : MonoBehaviour
{
    void Start()
    {
        // Generate a model with a simple description
        Meshifai.GenerateModel(
            "A futuristic spaceship",
            model => {
                // Use the generated model
                model.transform.SetParent(transform);
            }
        );
    }
}
```

## Installation

1. Import the MeshifAI package into your Unity project
2. Ensure the [GLTFast package](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.10/manual/installation.html) is installed (required dependency)
3. You're ready to start generating models!

## Documentation

For detailed documentation, check out:
- [Quick Start Guide](https://docs.meshifai.com/unity-engine/scripting-api/quick-start)
- [API Reference](https://docs.meshifai.com/unity-engine/scripting-api/api-reference)
- [Editor Tool Guide](https://docs.meshifai.com/unity-engine/editor-tool)

## Examples

### Generate with Error Handling

```csharp
Meshifai.GenerateModel(
    "A medieval castle",
    OnModelGenerated,
    OnGenerationError,
    OnGenerationStatus
);

void OnModelGenerated(GameObject model) {
    // Handle the completed model
}

void OnGenerationError(string errorMessage) {
    Debug.LogError($"Generation failed: {errorMessage}");
}

void OnGenerationStatus(string status, float progress) {
    // Update UI with generation progress
}
```

### Interactive Demo

Try the included Interactive Demo scene to experience the power of MeshifAI. Walk around in first-person view, point at surfaces, and generate models where you click!

## System Requirements

- Unity 2020.3 or higher
- .NET 4.x Scripting Runtime
- Internet connection for model generation

## License

[Include your license information here]

## Support

For support requests, please [contact us](mailto:support@meshifai.com) or [open an issue](https://github.com/yourusername/meshifai/issues) on GitHub.

---

Made with ❤️ by [Your Company/Name]
