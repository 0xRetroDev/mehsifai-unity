using System;
using UnityEngine;

namespace MeshifAI.Core
{
    /// <summary>
    /// Component attached to generated models that contains information about how they were created
    /// </summary>
    public class ModelGenerationData : MonoBehaviour
    {
        /// <summary>
        /// The prompt used to generate the model
        /// </summary>
        public string Prompt { get; private set; }

        /// <summary>
        /// The variance used during generation
        /// </summary>
        public float Variance { get; private set; }

        /// <summary>
        /// When the model was generated
        /// </summary>
        public DateTime GenerationTime { get; private set; }

        /// <summary>
        /// Initialize the generation data
        /// </summary>
        public void Initialize(string prompt, float variance)
        {
            Prompt = prompt;
            Variance = variance;
            GenerationTime = DateTime.Now;
        }

        /// <summary>
        /// Initialize the generation data with a specific time
        /// </summary>
        public void Initialize(string prompt, float variance, DateTime generationTime)
        {
            Prompt = prompt;
            Variance = variance;
            GenerationTime = generationTime;
        }
    }
}