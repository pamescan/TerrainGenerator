using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainGenerator
{
    public class TerrainChunkGenerator : MonoBehaviour
    {
        public Material TerrainMaterial;

        public Texture2D FlatTexture;
        public Texture2D SteepTexture;


        private TerrainChunkSettings Settings;

        private NoiseProvider NoiseProvider;

        private ChunkCache Cache;

		private MainParameters mainParameters;

        private void Awake()
        {
			mainParameters = GameObject.FindObjectOfType<GameController> ().mainParameters;
			Settings = new TerrainChunkSettings(
				mainParameters.heightmapResolution, 
				mainParameters.alphamapResolution, 
				mainParameters.length, 
				mainParameters.height,
				FlatTexture,
				SteepTexture,
				TerrainMaterial);
            NoiseProvider = new NoiseProvider(
				mainParameters.TerrainOctaves,
				mainParameters.TerrainFrequency,
				mainParameters.Seed);
			//Debug.Log (mainParameters.Seed);

            Cache = new ChunkCache();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
            }

            Cache.Update();
        }

        private void GenerateChunk(int x, int z)
        {
            if (Cache.ChunkCanBeAdded(x, z))
            {
                var chunk = new TerrainChunk(Settings, NoiseProvider, x, z);
                Cache.AddNewChunk(chunk);
            }
        }

        private void RemoveChunk(int x, int z)
        {
            if (Cache.ChunkCanBeRemoved(x, z))
                Cache.RemoveChunk(x, z);
        }

        private List<Vector2i> GetChunkPositionsInRadius(Vector2i chunkPosition, int radius)
        {
            var result = new List<Vector2i>();

            for (var zCircle = -radius; zCircle <= radius; zCircle++)
            {
                for (var xCircle = -radius; xCircle <= radius; xCircle++)
                {
                    if (xCircle * xCircle + zCircle * zCircle < radius * radius)
                        result.Add(new Vector2i(chunkPosition.X + xCircle, chunkPosition.Z + zCircle));
                }
            }

            return result;
        }

        public void UpdateTerrain(Vector3 worldPosition, int radius)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            var newPositions = GetChunkPositionsInRadius(chunkPosition, radius);

            var loadedChunks = Cache.GetGeneratedChunks();
            var chunksToRemove = loadedChunks.Except(newPositions).ToList();

            var positionsToGenerate = newPositions.Except(chunksToRemove).ToList();
            foreach (var position in positionsToGenerate)
                GenerateChunk(position.X, position.Z);

            foreach (var position in chunksToRemove)
                RemoveChunk(position.X, position.Z);
        }

        public Vector2i GetChunkPosition(Vector3 worldPosition)
        {
            var x = (int)Mathf.Floor(worldPosition.x / Settings.Length);
            var z = (int)Mathf.Floor(worldPosition.z / Settings.Length);

            return new Vector2i(x, z);
        }

        public bool IsTerrainAvailable(Vector3 worldPosition)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            return Cache.IsChunkGenerated(chunkPosition);
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            var chunk = Cache.GetGeneratedChunk(chunkPosition);
            if (chunkPosition != null)
                return chunk.GetTerrainHeight(worldPosition);

            return 0;
        }

		public void ReStart()
		{
			//Debug.Log ("Hola");
			var loadedChunks = Cache.GetGeneratedChunks();
			NoiseProvider = new NoiseProvider(mainParameters.TerrainOctaves,mainParameters.TerrainFrequency,mainParameters.Seed);
			foreach (var position in loadedChunks)
				RemoveChunk(position.X, position.Z);
			

		}
    }
}