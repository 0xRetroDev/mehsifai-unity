using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MeshifAI.Core
{
    /// <summary>
    /// Internal implementation of the model generation process
    /// </summary>
    internal static class MeshifAIGenerator
    {
        /// <summary>
        /// Coroutine implementation of model generation
        /// </summary>
        internal static IEnumerator GenerateModelCoroutine(
            string prompt,
            Action<GameObject> onComplete,
            Action<string> onError,
            Action<string, float> onStatus,
            float variance,
            bool applyDefaultMaterial)
        {
            // Validate input
            if (string.IsNullOrEmpty(prompt))
            {
                onError?.Invoke("Prompt cannot be empty");
                yield break;
            }

            // Notify status
            onStatus?.Invoke("Sending request to API...", 0.1f);

            // Start API request task
            var apiTask = MeshifaiApiClient.GenerateModelAsync(prompt, variance);

            // Wait until API request completes - outside of try/catch
            while (!apiTask.IsCompleted)
                yield return null;

            // Handle API result
            MeshifaiResponse response = null;
            try
            {
                // Check for exceptions
                if (apiTask.IsFaulted)
                {
                    string errorMsg = apiTask.Exception != null ? apiTask.Exception.Message : "API request failed";
                    onError?.Invoke(errorMsg);
                    yield break;
                }

                // Get the response
                response = apiTask.Result;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error processing API response: {ex.Message}");
                yield break;
            }

            if (response == null || !response.success)
            {
                onError?.Invoke("Model generation failed");
                yield break;
            }

            // Notify status
            onStatus?.Invoke("Downloading model...", 0.4f);

            // Start download task
            var downloadTask = MeshifaiApiClient.DownloadModelAsync(response.download_url);

            // Wait until download completes - outside of try/catch
            while (!downloadTask.IsCompleted)
                yield return null;

            // Handle download result
            byte[] modelData = null;
            try
            {
                // Check for exceptions
                if (downloadTask.IsFaulted)
                {
                    string errorMsg = downloadTask.Exception != null ? downloadTask.Exception.Message : "Download failed";
                    onError?.Invoke(errorMsg);
                    yield break;
                }

                // Get the downloaded data
                modelData = downloadTask.Result;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error downloading model: {ex.Message}");
                yield break;
            }

            if (modelData == null || modelData.Length == 0)
            {
                onError?.Invoke("Downloaded model data is empty");
                yield break;
            }

            // Notify status
            onStatus?.Invoke("Processing model...", 0.7f);

            // Create temp file path
            string tempFilePath = "";
            try
            {
                // Write to temporary file
                tempFilePath = Path.Combine(Application.temporaryCachePath, $"model_{DateTime.Now.Ticks}.glb");
                File.WriteAllBytes(tempFilePath, modelData);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error saving model data: {ex.Message}");
                yield break;
            }

            // Launch the load model coroutine
            yield return LoadGlbModelSafe(tempFilePath, applyDefaultMaterial, onComplete, onError);
        }

        /// <summary>
        /// Wrapper for LoadGlbModel that doesn't use try/catch blocks with yield returns
        /// </summary>
        private static IEnumerator LoadGlbModelSafe(
            string filePath,
            bool applyDefaultMaterial,
            Action<GameObject> onComplete,
            Action<string> onError)
        {
            // Create root GameObject
            GameObject modelObject = null;

            try
            {
                modelObject = new GameObject("MeshifAI_Model");
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Failed to create model object: {ex.Message}");
                yield break;
            }

            // Create glTFast importer
            GLTFast.GltfImport gltfImport = null;
            try
            {
                gltfImport = new GLTFast.GltfImport();
            }
            catch (Exception ex)
            {
                if (modelObject != null)
                    UnityEngine.Object.Destroy(modelObject);

                onError?.Invoke($"Failed to create GLTFast importer: {ex.Message}");
                CreateFallbackModel(onComplete, applyDefaultMaterial);
                yield break;
            }

            // Start loading
            var loadTask = gltfImport.Load(filePath);

            // Wait for load to complete
            while (!loadTask.IsCompleted)
                yield return null;

            // Check load results
            bool loadSuccess = false;
            try
            {
                if (loadTask.IsFaulted)
                {
                    Debug.LogWarning($"GLB load task faulted: {loadTask.Exception?.Message}");
                    loadSuccess = false;
                }
                else
                {
                    loadSuccess = loadTask.Result;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error checking GLB load result: {ex.Message}");
                loadSuccess = false;
            }

            // Handle load failure
            if (!loadSuccess)
            {
                Debug.LogWarning($"Failed to load GLB file. Creating fallback cube.");

                // Clean up
                if (modelObject != null)
                    UnityEngine.Object.Destroy(modelObject);

                // Create fallback
                CreateFallbackModel(onComplete, applyDefaultMaterial);
                yield break;
            }

            // Start instantiation
            var instantiateTask = gltfImport.InstantiateMainSceneAsync(modelObject.transform);

            // Wait for instantiation
            while (!instantiateTask.IsCompleted)
                yield return null;

            // Process the model
            try
            {
                // Center the model
                CenterModel(modelObject);

                // Apply default material if requested
                if (applyDefaultMaterial)
                {
                    ApplyDefaultMaterial(modelObject);
                }

                // Complete
                onComplete?.Invoke(modelObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing model after loading: {ex.Message}");

                // Clean up
                if (modelObject != null)
                    UnityEngine.Object.Destroy(modelObject);

                // Call error
                onError?.Invoke($"Error processing model: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a fallback cube model when GLB loading fails
        /// </summary>
        private static void CreateFallbackModel(Action<GameObject> onComplete, bool applyDefaultMaterial)
        {
            try
            {
                GameObject fallbackObject = new GameObject("MeshifAI_Fallback");
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(fallbackObject.transform);

                if (applyDefaultMaterial)
                {
                    Renderer renderer = cube.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.sharedMaterial = defaultMaterial;
                    }
                }

                Debug.LogWarning("Using fallback cube as model.");
                onComplete?.Invoke(fallbackObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create fallback model: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply default material to all renderers in the model
        /// </summary>
        private static void ApplyDefaultMaterial(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return;

            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = Color.white;

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = defaultMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Center the model around its origin
        /// </summary>
        private static void CenterModel(GameObject model)
        {
            Bounds bounds = new Bounds();
            bool boundsInitialized = false;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (!boundsInitialized)
                {
                    bounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (boundsInitialized)
            {
                Vector3 offset = model.transform.position - bounds.center;
                foreach (Transform child in model.transform)
                {
                    child.position += offset;
                }
            }
        }

        /// <summary>
        /// Clean a string to be used as an object name
        /// </summary>
        private static string SanitizeObjectName(string name)
        {
            string sanitized = "";

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != ' ' && c != '-')
                    continue;

                sanitized += c;
            }

            sanitized = sanitized.Replace(' ', '_');

            if (sanitized.Length > 30)
                sanitized = sanitized.Substring(0, 30);

            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Model";

            return sanitized;
        }
    }
}