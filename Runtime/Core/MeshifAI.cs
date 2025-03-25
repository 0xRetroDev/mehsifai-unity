using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MeshifAI.Core
{
    /// <summary>
    /// Main API for MeshifAI model generation
    /// </summary>
    public static class MeshifAI
    {
        /// <summary>
        /// Generate a 3D model from a text description with full callbacks
        /// </summary>
        /// <param name="prompt">Text description of the model to generate</param>
        /// <param name="onComplete">Callback when generation completes successfully</param>
        /// <param name="onError">Callback when an error occurs</param>
        /// <param name="onStatus">Callback for status updates</param>
        /// <param name="variance">Variance value (0-1) controlling generation diversity</param>
        /// <param name="applyDefaultMaterial">Whether to apply a default material to the model</param>
        /// <returns>Handle that can be used to cancel the generation</returns>
        public static object GenerateModel(
            string prompt,
            Action<GameObject> onComplete,
            Action<string> onError = null,
            Action<string, float> onStatus = null,
            float variance = 0.2f,
            bool applyDefaultMaterial = true)
        {
            // Start the coroutine using the runner (allow multiple generations)
            Coroutine handle = MeshifaiRunner.RunCoroutine(
                MeshifAIGenerator.GenerateModelCoroutine(
                    prompt,
                    onComplete,
                    onError,
                    onStatus,
                    variance,
                    applyDefaultMaterial
                )
            );

            return handle;
        }

        /// <summary>
        /// Cancel a specific model generation
        /// </summary>
        /// <param name="handle">The handle returned by GenerateModel</param>
        public static void CancelGeneration(object handle)
        {
            if (handle is Coroutine coroutine)
            {
                MeshifaiRunner.StopRunner(coroutine);
            }
        }

        /// <summary>
        /// Create a copy of an existing MeshifAI-generated model
        /// </summary>
        /// <param name="originalModel">The original model to copy</param>
        /// <returns>A new copy of the model with all generation data preserved</returns>
        public static GameObject CloneModel(GameObject originalModel)
        {
            if (originalModel == null)
                return null;

            // Check if this is a MeshifAI-generated model
            ModelGenerationData originalData = originalModel.GetComponent<ModelGenerationData>();
            if (originalData == null)
            {
                Debug.LogWarning("The provided GameObject is not a MeshifAI-generated model");
                return UnityEngine.Object.Instantiate(originalModel);
            }

            // Create a copy
            GameObject copy = UnityEngine.Object.Instantiate(originalModel);

            // Get the generation data component on the copy
            ModelGenerationData copyData = copy.GetComponent<ModelGenerationData>();

            // If for some reason it wasn't copied, add it
            if (copyData == null)
            {
                copyData = copy.AddComponent<ModelGenerationData>();
                copyData.Initialize(originalData.Prompt, originalData.Variance, originalData.GenerationTime);
            }

            return copy;
        }
    }
}