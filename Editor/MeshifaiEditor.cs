using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;

namespace MeshifAI.EditorTools
{
    public class MeshifaiEditor : EditorWindow
    {
        private const string ROOT_FOLDER = "Generated Models";
        private const float MIN_WINDOW_WIDTH = 450f;
        private const float MIN_WINDOW_HEIGHT = 650f;

        // UI variables
        private string prompt = "A Futuristic Spaceship";
        private float variance = 0.2f;
        private Vector2 scrollPosition;
        private bool isGenerating = false;
        private string statusMessage = "";

        // Style variables
        private GUIStyle headerStyle;
        private GUIStyle subheaderStyle;
        private GUIStyle buttonStyle;
        private Texture2D logoTexture;

        // Preview variables
        private GameObject previewModel;
        private UnityEditor.Editor previewModelEditor;

        // Model data
        private MeshifaiResponse lastResponse;
        private string modelName = "";

        [MenuItem("MeshifAI/Text-To-3D")]
        public static void ShowWindow()
        {
            MeshifaiEditor window = GetWindow<MeshifaiEditor>("MeshifAI 3D");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.position = new Rect(window.position.x, window.position.y, MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }

        private void OnEnable()
        {
            // Create root directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder($"Assets/{ROOT_FOLDER}"))
            {
                AssetDatabase.CreateFolder("Assets", ROOT_FOLDER);
                AssetDatabase.Refresh();
            }

            // Load the logo texture if it exists
            logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshifAI/Resources/MeshifAI_Logo.png");
        }

        private void OnDisable()
        {
            CleanupPreview();
        }

        private void CleanupPreview()
        {
            // Cleanup in a safer order to avoid NullReferenceException
            if (previewModel != null)
            {
                // Destroy all children first
                foreach (Transform child in previewModel.transform)
                {
                    DestroyImmediate(child.gameObject);
                }

                // Then destroy the model itself
                DestroyImmediate(previewModel);
                previewModel = null;
            }

            // Editor should be destroyed after the preview model
            if (previewModelEditor != null)
            {
                DestroyImmediate(previewModelEditor);
                previewModelEditor = null;
            }
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                // Header style
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 20;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                headerStyle.margin = new RectOffset(10, 10, 10, 10);

                // Subheader style
                subheaderStyle = new GUIStyle(EditorStyles.label);
                subheaderStyle.fontSize = 14;
                subheaderStyle.alignment = TextAnchor.MiddleCenter;
                subheaderStyle.wordWrap = true;

                // Button style
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 14;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.padding = new RectOffset(15, 15, 8, 8);
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // Draw header with logo
            DrawHeader();

            EditorGUILayout.Space(15);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw generation controls
            DrawGenerationControls();

            // Only show preview and save sections if we have a model
            if (previewModel != null)
            {
                EditorGUILayout.Space(20);

                // Draw preview
                DrawModelPreview();

                EditorGUILayout.Space(20);

                // Draw save controls
                DrawSaveControls();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            // Logo
            if (logoTexture != null)
            {
                float logoHeight = 80;
                float logoWidth = logoHeight * (logoTexture.width / (float)logoTexture.height);
                Rect logoRect = EditorGUILayout.GetControlRect(false, logoHeight);
                logoRect.width = logoWidth;
                logoRect.x = (position.width - logoWidth) / 2;
                GUI.DrawTexture(logoRect, logoTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback header text if logo not found
                EditorGUILayout.LabelField("MeshifAI 3D Model Generator", headerStyle);
            }

            EditorGUILayout.LabelField("Create 3D models from text descriptions", subheaderStyle);

            EditorGUILayout.Space(10);

            // Separator line
            Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void DrawGenerationControls()
        {
            EditorGUILayout.LabelField("Model Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(isGenerating);

            // Prompt field with better styling
            EditorGUILayout.LabelField("Describe your 3D model:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(60));

            EditorGUILayout.Space(10);

            // Variance slider with better layout
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Variance:", GUILayout.Width(70));
            variance = EditorGUILayout.Slider(variance, 0f, 1f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("(Lower values create more predictable results, higher values more varied)",
                                        EditorStyles.miniLabel);

            EditorGUILayout.Space(15);

            // Generate button with better styling
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Generate Model", buttonStyle, GUILayout.Width(200), GUILayout.Height(40)))
            {
                _ = GenerateModel();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            // Progress and status
            if (isGenerating)
            {
                EditorGUILayout.Space(15);

                Rect progressRect = EditorGUILayout.GetControlRect(false, 24);
                EditorGUI.ProgressBar(progressRect, 0.5f, "Generating model...");

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(100)))
                {
                    isGenerating = false;
                    statusMessage = "Generation cancelled";
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawModelPreview()
        {
            EditorGUILayout.LabelField("Model Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Create and display preview
            if (previewModelEditor == null && previewModel != null)
            {
                previewModelEditor = UnityEditor.Editor.CreateEditor(previewModel);
            }

            if (previewModelEditor != null)
            {
                previewModelEditor.OnInteractivePreviewGUI(
                    GUILayoutUtility.GetRect(0, 400), EditorStyles.helpBox);
            }
        }

        private void DrawSaveControls()
        {
            EditorGUILayout.LabelField("Save Model", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Model name field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model Name:", GUILayout.Width(100));
            modelName = EditorGUILayout.TextField(modelName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // Save button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !string.IsNullOrEmpty(modelName) && !isGenerating;
            if (GUILayout.Button("Save Model", buttonStyle, GUILayout.Width(200), GUILayout.Height(40)))
            {
                SaveModel();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private async Task GenerateModel()
        {
            try
            {
                isGenerating = true;
                statusMessage = "Sending request to MeshifAI...";

                // Force repaint to show progress
                Repaint();

                // Generate model
                lastResponse = await MeshifaiApiClient.GenerateModelAsync(prompt, variance);

                if (!lastResponse.success)
                {
                    statusMessage = "Generation failed";
                    isGenerating = false;
                    return;
                }

                // We don't display rate limits as requested

                statusMessage = "Downloading model...";
                Repaint();

                // Download the model
                byte[] modelData = await MeshifaiApiClient.DownloadModelAsync(lastResponse.download_url);

                statusMessage = "Processing model...";
                Repaint();

                // Create a temporary filename in the project's Assets folder
                string tempFolder = "Assets/MeshifAI/MeshifAITemp";

                if (!AssetDatabase.IsValidFolder(tempFolder))
                {
                    AssetDatabase.CreateFolder("Assets/MeshifAI", "MeshifAITemp");
                }

                // Generate a unique filename
                string tempFilename = $"temp_model_{DateTime.Now.Ticks}.glb";
                string tempFilePath = Path.Combine(tempFolder, tempFilename);

                // Write the file directly into the Assets folder
                File.WriteAllBytes(tempFilePath, modelData);

                // Refresh the AssetDatabase to make Unity import it
                AssetDatabase.Refresh();

                // Create preview
                await Task.Delay(1000); // Wait a bit for Unity to process the import

                // Clean up previous preview
                CleanupPreview();

                // Create a new preview object
                previewModel = new GameObject("MeshifAI_Preview");

                // Store the path to the GLB file
                previewModel.AddComponent<MeshifaiModelData>().Initialize(tempFilePath, prompt, variance);

                // Try to load the imported model
                GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(tempFilePath);

                if (importedModel != null)
                {
                    // If we successfully imported the model, use it for preview
                    GameObject previewInstance = Instantiate(importedModel, previewModel.transform);
                    previewInstance.transform.localPosition = Vector3.zero;

                    // Apply default material to fix pink shader
                    ApplyDefaultMaterial(previewInstance);

                    statusMessage = "Model successfully imported and previewed";
                }
                else
                {
                    // Fall back to a cube preview if import failed
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(previewModel.transform);

                    statusMessage = "Model generated successfully. Using placeholder preview.";
                }

                // Generate default name from prompt
                modelName = SanitizeAssetName(prompt);

                isGenerating = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating model: {ex.Message}");
                statusMessage = $"Error: {ex.Message}";
                isGenerating = false;
            }

            Repaint();
        }

        // Apply default material to all renderers in the hierarchy
        private void ApplyDefaultMaterial(GameObject obj)
        {
            // Get all mesh renderers in the object hierarchy
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            // Default material
            Material defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            if (defaultMaterial != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    // Create a new material array with the default material
                    Material[] materials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = defaultMaterial;
                    }

                    // Apply the materials
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private void SaveModel()
        {
            if (previewModel == null)
            {
                EditorUtility.DisplayDialog("Save Error", "No model to save", "OK");
                return;
            }

            try
            {
                // Get the model data component
                MeshifaiModelData modelData = previewModel.GetComponent<MeshifaiModelData>();
                if (modelData == null || string.IsNullOrEmpty(modelData.GlbFilePath))
                {
                    EditorUtility.DisplayDialog("Save Error", "Model data not found", "OK");
                    return;
                }

                // Check if the source GLB file exists
                if (!File.Exists(modelData.GlbFilePath))
                {
                    EditorUtility.DisplayDialog("Save Error", "Source GLB file not found", "OK");
                    return;
                }

                // Create subfolder
                string sanitizedName = SanitizeAssetName(modelName);
                string folderPath = $"Assets/{ROOT_FOLDER}/{sanitizedName}";

                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder($"Assets/{ROOT_FOLDER}", sanitizedName);
                }

                // Copy the GLB file to the destination folder
                string destGlbPath = $"{folderPath}/{sanitizedName}.glb";
                File.Copy(modelData.GlbFilePath, destGlbPath, true);

                // Refresh to trigger import
                AssetDatabase.Refresh();

                // Create metadata file for the prompt and settings
                string metadataPath = $"{folderPath}/{sanitizedName}_metadata.json";
                string metadataJson = JsonUtility.ToJson(new ModelMetadata
                {
                    prompt = modelData.Prompt,
                    variance = modelData.Variance,
                    generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, true);

                File.WriteAllText(metadataPath, metadataJson);

                // Wait to ensure import is complete
                EditorApplication.delayCall += () => {
                    // Check if the model file was imported successfully
                    GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(destGlbPath);

                    if (importedModel != null)
                    {
                        // Create a prefab
                        string prefabPath = $"{folderPath}/{sanitizedName}.prefab";

                        // Instantiate the model to apply materials
                        GameObject tempInstance = Instantiate(importedModel);

                        // Apply default material to fix pink shader
                        ApplyDefaultMaterial(tempInstance);

                        // Save as prefab
                        PrefabUtility.SaveAsPrefabAsset(tempInstance, prefabPath);
                        DestroyImmediate(tempInstance);

                        statusMessage = $"Model saved to {prefabPath}";
                        EditorUtility.DisplayDialog("Save Complete", "Model saved successfully", "OK");

                        // Ping the prefab in the Project view
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }
                    else
                    {
                        // If prefab creation failed, still notify about the GLB file
                        statusMessage = $"GLB file saved to {destGlbPath}, but prefab creation failed";
                        EditorUtility.DisplayDialog("Save Partial", "GLB file saved, but Unity couldn't create a prefab", "OK");

                        // Ping the GLB file in the Project view
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destGlbPath);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    // Clean up the temp file
                    AssetDatabase.DeleteAsset(modelData.GlbFilePath);

                    // Close the window after saving
                    Close();
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving model: {ex.Message}");
                EditorUtility.DisplayDialog("Save Error", $"Failed to save model: {ex.Message}", "OK");
            }
        }

        [Serializable]
        private class ModelMetadata
        {
            public string prompt;
            public float variance;
            public string generatedAt;
        }

        // Helper class to store model data
        public class MeshifaiModelData : MonoBehaviour
        {
            public string GlbFilePath;
            public string Prompt;
            public float Variance;

            public void Initialize(string path, string prompt, float variance)
            {
                GlbFilePath = path;
                Prompt = prompt;
                Variance = variance;
            }
        }

        private string SanitizeAssetName(string name)
        {
            // Remove invalid characters and limit length
            string sanitized = "";

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != ' ' && c != '-')
                    continue;

                sanitized += c;
            }

            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');

            // Limit length
            if (sanitized.Length > 30)
                sanitized = sanitized.Substring(0, 30);

            // Ensure it's not empty
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Model";

            // Add timestamp to ensure uniqueness
            sanitized += $"_{DateTime.Now:yyyyMMdd_HHmmss}";

            return sanitized;
        }
    }
}