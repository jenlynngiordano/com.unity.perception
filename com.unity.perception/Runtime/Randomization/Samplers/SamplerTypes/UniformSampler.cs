﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns uniformly distributed random values within a designated range.
    /// </summary>
    [Serializable]
    public class UniformSampler : ISampler
    {
        /// <summary>
        /// A range bounding the values generated by this sampler
        /// </summary>
        [field: SerializeField]
        public FloatRange range { get; set; }

        /// <summary>
        /// Constructs a new uniform distribution sampler
        /// </summary>
        /// <param name="min">The smallest value contained within the range</param>
        /// <param name="max">The largest value contained within the range</param>
        public UniformSampler(float min, float max)
        {
            range = new FloatRange(min, max);
        }

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public float Sample()
        {
            var rng = new Unity.Mathematics.Random(ScenarioBase.activeScenario.NextRandomState());
            return math.lerp(range.minimum, range.maximum, rng.NextFloat());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of generated samples</returns>
        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<float>(
                sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob
            {
                min = range.minimum,
                max = range.maximum,
                seed = ScenarioBase.activeScenario.NextRandomState(),
                samples = samples
            }.ScheduleBatch(sampleCount, SamplerUtility.samplingBatchSize);
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJobParallelForBatch
        {
            public float min;
            public float max;
            public uint seed;
            public NativeArray<float> samples;

            public void Execute(int startIndex, int count)
            {
                var endIndex = startIndex + count;
                var batchIndex = startIndex / SamplerUtility.samplingBatchSize;
                var rng = new Unity.Mathematics.Random(SamplerUtility.IterateSeed((uint)batchIndex, seed));
                for (var i = startIndex; i < endIndex; i++)
                    samples[i] = rng.NextFloat(min, max);
            }
        }
    }
}
