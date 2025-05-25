using System;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using AhBearStudios.Pooling.Core.Pooling.Unity;
using AhBearStudios.Pooling.Core.Pooling.Core;
using AhBearStudios.Pooling.Core.Pooling.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// Partial class for object spawning in ExampleSceneManager
/// </summary>
public partial class ExampleSceneManager
{
    /// <summary>
    /// Different types of pools that can be used for spawning
    /// </summary>
    public enum PoolType
    {
        Standard,
        ThreadSafe,
        Native,
        BurstCompatible,
        JobCompatible
    }

    /// <summary>
    /// Request for spawning an object with position and rotation
    /// </summary>
    public struct SpawnRequest
    {
        /// <summary>
        /// Type of object to spawn
        /// </summary>
        public ObjectType Type;
        
        /// <summary>
        /// Position where the object should be spawned
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// Rotation of the spawned object
        /// </summary>
        public Quaternion Rotation;
        
        /// <summary>
        /// Creates a new spawn request
        /// </summary>
        /// <param name="type">Type of object to spawn</param>
        /// <param name="position">Position where the object should be spawned</param>
        /// <param name="rotation">Rotation of the spawned object</param>
        public SpawnRequest(ObjectType type, Vector3 position, Quaternion rotation)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Types of objects that can be spawned
    /// </summary>
    public enum ObjectType
    {
        Cube,
        Sphere,
        Particle
    }

    /// <summary>
    /// Spawns a cube from the pool at a random position
    /// </summary>
    public void SpawnCube()
    {
        if (cubePool == null) return;

        profiler?.BeginSample("SpawnCube", "CubePool");
        
        Vector3 position = GetRandomSpawnPosition();
        Quaternion rotation = Quaternion.Euler(
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f));

        GameObject cube = cubePool.AcquireAtPosition(position, rotation);
        
        // Schedule automatic return to pool
        StartCoroutine(ReturnToPoolAfterDelay(cube, cubePool, objectReturnDelay));
        
        profiler?.EndSample("SpawnCube", "CubePool", cubePool.ActiveCount, cubePool.InactiveCount);
    }

    /// <summary>
    /// Spawns a sphere from the pool at a random position
    /// </summary>
    public void SpawnSphere()
    {
        if (spherePool == null) return;

        profiler?.BeginSample("SpawnSphere", "SpherePool");
        
        Vector3 position = GetRandomSpawnPosition();
        Quaternion rotation = Quaternion.identity;

        GameObject sphere = spherePool.AcquireAtPosition(position, rotation);
        
        // Add force if it has a rigidbody
        if (sphere.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(
                UnityEngine.Random.Range(-5f, 5f),
                UnityEngine.Random.Range(1f, 8f),
                UnityEngine.Random.Range(-5f, 5f),
                ForceMode.Impulse);
        }
        
        // Schedule automatic return to pool
        StartCoroutine(ReturnToPoolAfterDelay(sphere, spherePool, objectReturnDelay));
        
        profiler?.EndSample("SpawnSphere", "SpherePool", spherePool.ActiveCount, spherePool.InactiveCount);
    }

    /// <summary>
    /// Spawns a particle effect from the pool at a random position
    /// </summary>
    public void SpawnParticleEffect()
    {
        if (particlePool == null) return;

        profiler?.BeginSample("SpawnParticle", "ParticlePool");
        
        Vector3 position = GetRandomSpawnPosition();
        Quaternion rotation = Quaternion.Euler(
            -90f, // Point particles upward
            UnityEngine.Random.Range(0f, 360f),
            0f);

        // Since we're using auto-release with the particle pool,
        // we don't need to manually return it
        ParticleSystem ps = particlePool.AcquireAndPlay(position, rotation, true);
        
        profiler?.EndSample("SpawnParticle", "ParticlePool", particlePool.ActiveCount, particlePool.InactiveCount);
    }

    /// <summary>
    /// Spawns multiple objects of random types
    /// </summary>
    public void SpawnMultipleObjects()
    {
        profiler?.BeginSample("SpawnMultiple", "BatchSpawner");
        
        int count = 1;
        if (spawnCountInput != null && !string.IsNullOrEmpty(spawnCountInput.text))
        {
            if (int.TryParse(spawnCountInput.text, out int parsedCount))
            {
                count = Mathf.Clamp(parsedCount, 1, maxPoolSize);
            }
        }
        
        // Since we're using UnsafeList now, ensure it has enough capacity
        if (spawnRequests.Capacity < count)
        {
            spawnRequests.Capacity = count * 2;
        }

        // Clear the list before adding new requests
        spawnRequests.Clear();

        // Create spawn requests
        for (int i = 0; i < count; i++)
        {
            // Randomly select object type
            ObjectType type = (ObjectType)UnityEngine.Random.Range(0, 3); // 0-2 for Cube, Sphere, Particle
            
            Vector3 position = GetRandomSpawnPosition();
            Quaternion rotation = Quaternion.Euler(
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0f, 360f));
                
            // Add to batch spawning queue
            SpawnRequest request = new SpawnRequest(type, position, rotation);
            spawnRequests.Add(request);
        }
        
        // Log the batch creation through diagnostics using an appropriate pool object
        if (count > 0)
        {
            // Use one of the actual pools for the diagnostics, depending on availability
            IPool poolToRecord = null;
            
            if (cubePool != null)
            {
                poolToRecord = cubePool;
            }
            else if (spherePool != null)
            {
                poolToRecord = spherePool;
            }
            else if (particlePool != null)
            {
                poolToRecord = particlePool;
            }
            
            if (poolToRecord != null)
            {
                // Record the creation in the diagnostics
                diagnostics?.RecordCreate(poolToRecord);
                
                // Log the batch creation
                logger?.LogInfoInstance($"Created batch of {count} spawn requests");
            }
        }
        
        profiler?.EndSample("SpawnMultiple", "BatchSpawner", count, 0);
    }
    
    /// <summary>
    /// Processes batched spawn requests
    /// </summary>
    private void ProcessBatchSpawn()
    {
        if (!spawnRequests.IsCreated || spawnRequests.Length == 0)
            return;
            
        profiler?.BeginSample("ProcessBatchSpawn", "BatchSpawner");
            
        // Process a limited number of requests per frame for better performance
        int requestsToProcess = Mathf.Min(spawnRequests.Length, (int)(spawnRate * Time.deltaTime));
        
        if (requestsToProcess <= 0)
            requestsToProcess = 1; // Process at least one request
            
        // Process the requests
        for (int i = 0; i < requestsToProcess && spawnRequests.Length > 0; i++)
        {
            // Get the last request in the list for efficient removal
            SpawnRequest request = spawnRequests[spawnRequests.Length - 1];
            spawnRequests.RemoveAt(spawnRequests.Length - 1);
            
            // Spawn the appropriate object
            switch (request.Type)
            {
                case ObjectType.Cube:
                    if (cubePool != null)
                    {
                        GameObject cube = cubePool.AcquireAtPosition(request.Position, request.Rotation);
                        StartCoroutine(ReturnToPoolAfterDelay(cube, cubePool, objectReturnDelay));
                    }
                    break;
                    
                case ObjectType.Sphere:
                    if (spherePool != null)
                    {
                        GameObject sphere = spherePool.AcquireAtPosition(request.Position, request.Rotation);
                        
                        // Add force if it has a rigidbody
                        if (sphere.TryGetComponent<Rigidbody>(out var rb))
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.AddForce(
                                UnityEngine.Random.Range(-5f, 5f),
                                UnityEngine.Random.Range(1f, 8f),
                                UnityEngine.Random.Range(-5f, 5f),
                                ForceMode.Impulse);
                        }
                        
                        StartCoroutine(ReturnToPoolAfterDelay(sphere, spherePool, objectReturnDelay));
                    }
                    break;
                    
                case ObjectType.Particle:
                    if (particlePool != null)
                    {
                        // Auto-release is handled by the particle pool
                        particlePool.AcquireAndPlay(request.Position, request.Rotation, true);
                    }
                    break;
            }
        }
        
        profiler?.EndSample("ProcessBatchSpawn", "BatchSpawner", requestsToProcess, spawnRequests.Length);
    }
    
    /// <summary>
    /// Clears all active objects and returns them to their pools
    /// </summary>
    public void ClearAllObjects()
    {
        profiler?.BeginSample("ClearAllObjects", "ExampleSceneManager");
        
        // Clear any pending spawn requests
        if (spawnRequests.IsCreated && spawnRequests.Length > 0)
        {
            spawnRequests.Clear();
        }
        
        // Stop any ongoing stress test
        if (stressTestActive && stressTestCoroutine != null)
        {
            StopCoroutine(stressTestCoroutine);
            stressTestActive = false;
            stressTestText.text = "Start Stress Test";
        }
        
        // Wait for any jobs to complete
        if (jobInProgress)
        {
            positionJobHandle.Complete();
            jobInProgress = false;
        }
        
        // Release any particle systems
        if (particlePool != null)
        {
            diagnostics?.RecordClear(particlePool);
            particlePool.Clear();
        }
        
        // Release any cubes
        if (cubePool != null)
        {
            diagnostics?.RecordClear(cubePool);
            cubePool.Clear();
        }
        
        // Release any spheres
        if (spherePool != null)
        {
            diagnostics?.RecordClear(spherePool);
            spherePool.Clear();
        }
        
        logger?.LogInfoInstance("All pools cleared");
        
        profiler?.EndSample("ClearAllObjects", "ExampleSceneManager", 0, 0);
    }
    
    /// <summary>
    /// Coroutine to return an object to its pool after a delay
    /// </summary>
    /// <param name="obj">Object to return</param>
    /// <param name="pool">Pool to return it to</param>
    /// <param name="delay">Delay in seconds</param>
    /// <returns>IEnumerator for coroutine</returns>
    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, GameObjectPool pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null && pool != null && !pool.IsDisposed)
        {
            diagnostics?.RecordRelease(pool, obj);
            pool.Release(obj);
        }
    }
    
    /// <summary>
    /// Processes any completed job results
    /// </summary>
    private void ProcessJobResults()
    {
        if (!jobInProgress)
            return;
            
        // Check if the job has completed
        if (positionJobHandle.IsCompleted)
        {
            profiler?.BeginSample("ProcessJobResults", "JobSystem");
            
            // Complete the job to get results
            positionJobHandle.Complete();
            jobInProgress = false;
            
            // Now we can use generatedPositions safely
            // Store positions for stress test to use
            
            profiler?.EndSample("ProcessJobResults", "JobSystem", generatedPositions.Length, 0);
        }
    }
}