using System.Threading;
using UnityEngine;

namespace TerrainGenerator
{
    public class TerrainChunk
    {
        public Vector2i Position { get; private set; }

        private Terrain Terrain { get; set; }

        private TerrainData Data { get; set; }

        private TerrainChunkSettings Settings { get; set; }

        private NoiseProvider NoiseProvider { get; set; }

        private TerrainChunkNeighborhood Neighborhood { get; set; }

        private float[,] Heightmap { get; set; }

		private Texture2D HeightmapTexture { get; set;}

		private GameObject quad { get; set;}

        private object HeightmapThreadLockObject { get; set; }

        public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noiseProvider, int x, int z)
        {
            HeightmapThreadLockObject = new object();

            Settings = settings;
            NoiseProvider = noiseProvider;
            Neighborhood = new TerrainChunkNeighborhood();

            Position = new Vector2i(x, z);





        }

        #region Heightmap stuff

        public void GenerateHeightmap()
        {
            var thread = new Thread(GenerateHeightmapThread);
            thread.Start();
        }

        private void GenerateHeightmapThread()
        {
            lock (HeightmapThreadLockObject)
            {
                var heightmap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];

                for (var zRes = 0; zRes < Settings.HeightmapResolution; zRes++)
                {
                    for (var xRes = 0; xRes < Settings.HeightmapResolution; xRes++)
                    {
                        var xCoordinate = Position.X + (float)xRes / (Settings.HeightmapResolution - 1);
                        var zCoordinate = Position.Z + (float)zRes / (Settings.HeightmapResolution - 1);

                        heightmap[zRes, xRes] = NoiseProvider.GetValue(xCoordinate, zCoordinate);
                    }
                }

                Heightmap = heightmap;
            }
        }

        public bool IsHeightmapReady()
        {
            return Terrain == null && Heightmap != null;
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            return Terrain.SampleHeight(worldPosition);
        }


		private Texture2D GenerateHeightmapTexture(){

			var texture = new Texture2D (Settings.HeightmapResolution, Settings.HeightmapResolution);
			var pixels = new Color [Settings.HeightmapResolution * Settings.HeightmapResolution];

			for (var z =0; z<Settings.HeightmapResolution;z++){
				for (var x =0; x<Settings.HeightmapResolution;x++){
					float value = Heightmap [z, x];
						pixels [x + z * 129] = Color.Lerp (Color.black, Color.white, value);
					//Debug.Log (x.ToString () + ":" + y.ToString ());
				}

			}
			texture.SetPixels (pixels);
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply ();
			return texture;
		}

        #endregion

        #region Main terrain generation

       public void CreateTerrain()
        {
            Data = new TerrainData();
            Data.heightmapResolution = Settings.HeightmapResolution;
            Data.alphamapResolution = Settings.AlphamapResolution;
            Data.SetHeights(0, 0, Heightmap);
            ApplyTextures(Data);


            Data.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);
            var newTerrainGameObject = Terrain.CreateTerrainGameObject(Data);
            newTerrainGameObject.transform.position = new Vector3(Position.X * Settings.Length, 0, Position.Z * Settings.Length);

			// pruebas de texturas
			var _m1 = new TerrainMeshBuilder ().CreateMesh (Settings.Length);
			quad = (GameObject)new GameObject (
				"mesh " + Position.X.ToString() + 	":" + Position.Z.ToString(),
				typeof(MeshRenderer),
				typeof(MeshFilter)
			);
			quad.GetComponent<MeshFilter>().mesh = _m1;
			quad.transform.position = new Vector3(Position.X * Settings.Length+ Settings.Length/2, 0, Position.Z * Settings.Length+ Settings.Length/2);
			quad.GetComponent<MeshRenderer> ().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			quad.GetComponent<MeshRenderer> ().receiveShadows = false;
			quad.GetComponent<Renderer> ().material.mainTexture = GenerateHeightmapTexture ();


            Terrain = newTerrainGameObject.GetComponent<Terrain>();
            Terrain.heightmapPixelError = 8;
            Terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
            Terrain.materialTemplate = Settings.TerrainMaterial;
            Terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Terrain.Flush();
        }

        private void ApplyTextures(TerrainData terrainData)
        {
            var flatSplat = new SplatPrototype();
            var steepSplat = new SplatPrototype();

            flatSplat.texture = Settings.FlatTexture;
            steepSplat.texture = Settings.SteepTexture;

            terrainData.splatPrototypes = new SplatPrototype[]
            {
                flatSplat,
                steepSplat
            };

            terrainData.RefreshPrototypes();

            var splatMap = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 2];

            for (var zRes = 0; zRes < terrainData.alphamapHeight; zRes++)
            {
                for (var xRes = 0; xRes < terrainData.alphamapWidth; xRes++)
                {
                    var normalizedX = (float)xRes / (terrainData.alphamapWidth - 1);
                    var normalizedZ = (float)zRes / (terrainData.alphamapHeight - 1);

                    var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                    var steepnessNormalized = Mathf.Clamp(steepness / 1.5f, 0, 1f);

                    splatMap[zRes, xRes, 0] = 1f - steepnessNormalized;
                    splatMap[zRes, xRes, 1] = steepnessNormalized;
                }
            }

            terrainData.SetAlphamaps(0, 0, splatMap);
        }

        #endregion

        #region Distinction

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TerrainChunk;
            if (other == null)
                return false;

            return this.Position.Equals(other.Position);
        }

        #endregion

        #region Chunk removal

        public void Remove()
        {
            Heightmap = null;
            Settings = null;

            if (Neighborhood.XDown != null)
            {
                Neighborhood.XDown.RemoveFromNeighborhood(this);
                Neighborhood.XDown = null;
            }
            if (Neighborhood.XUp != null)
            {
                Neighborhood.XUp.RemoveFromNeighborhood(this);
                Neighborhood.XUp = null;
            }
            if (Neighborhood.ZDown != null)
            {
                Neighborhood.ZDown.RemoveFromNeighborhood(this);
                Neighborhood.ZDown = null;
            }
            if (Neighborhood.ZUp != null)
            {
                Neighborhood.ZUp.RemoveFromNeighborhood(this);
                Neighborhood.ZUp = null;
            }

			if (Terrain != null) {
				GameObject.Destroy (Terrain.gameObject);
				GameObject.Destroy (quad.gameObject);
				}
			}

        public void RemoveFromNeighborhood(TerrainChunk chunk)
        {
            if (Neighborhood.XDown == chunk)
                Neighborhood.XDown = null;
            if (Neighborhood.XUp == chunk)
                Neighborhood.XUp = null;
            if (Neighborhood.ZDown == chunk)
                Neighborhood.ZDown = null;
            if (Neighborhood.ZUp == chunk)
                Neighborhood.ZUp = null;
        }

        #endregion

        #region Neighborhood

        public void SetNeighbors(TerrainChunk chunk, TerrainNeighbor direction)
        {
            if (chunk != null)
            {
                switch (direction)
                {
                    case TerrainNeighbor.XUp:
                        Neighborhood.XUp = chunk;
                        break;

                    case TerrainNeighbor.XDown:
                        Neighborhood.XDown = chunk;
                        break;

                    case TerrainNeighbor.ZUp:
                        Neighborhood.ZUp = chunk;
                        break;

                    case TerrainNeighbor.ZDown:
                        Neighborhood.ZDown = chunk;
                        break;
                }
            }
        }

        public void UpdateNeighbors()
        {
            if (Terrain != null)
            {
                var xDown = Neighborhood.XDown == null ? null : Neighborhood.XDown.Terrain;
                var xUp = Neighborhood.XUp == null ? null : Neighborhood.XUp.Terrain;
                var zDown = Neighborhood.ZDown == null ? null : Neighborhood.ZDown.Terrain;
                var zUp = Neighborhood.ZUp == null ? null : Neighborhood.ZUp.Terrain;
                Terrain.SetNeighbors(xDown, zUp, xUp, zDown);
                Terrain.Flush();
            }
        }

        #endregion
    }
}