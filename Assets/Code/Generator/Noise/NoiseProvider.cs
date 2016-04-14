//using LibNoise.Generator;
using AccidentalNoise;
namespace TerrainGenerator
{
    public class NoiseProvider : INoiseProvider
    {
		//protected int TerrainOctaves = 6;
		//protected double TerrainFrequency = 1.25;
		//protected int Seed =1;
        //private Perlin PerlinNoiseGenerator;
		private          ImplicitSelect PerlinNoiseGenerator;

		public NoiseProvider(int TerrainOctaves, double TerrainFrequency,int Seed)
        {
            //PerlinNoiseGenerator = new Perlin();
			var mountainTerrain=new ImplicitFractal (
				//FractalType.MULTI,
				//FractalType.MULTI,
				FractalType.RIDGEDMULTI,
				BasisType.SIMPLEX, 
				InterpolationType.QUINTIC, 
				TerrainOctaves, 
				TerrainFrequency, 
				Seed);

			var baseFlatTerrain = new ImplicitFractal (
				                      FractalType.BILLOW,
				                      BasisType.SIMPLEX,
				                      InterpolationType.QUINTIC,
										TerrainOctaves, 
										1.0, 
										Seed
			                      );

			var flatTerrain = new ImplicitScaleOffset (baseFlatTerrain,0.125,-0.75);
			var terrainType = new ImplicitFractal (
				FractalType.RIDGEDMULTI,
				BasisType.GRADIENT,
				InterpolationType.QUINTIC,
				1, 
				0.3, 
				Seed
			);
			var selectTerrainType = new ImplicitSelect (terrainType, 0, 1, 0.1, 0.5);

			var finalTerrain = new ImplicitSelect (selectTerrainType, flatTerrain, mountainTerrain,1.1, 0.8);


			PerlinNoiseGenerator = finalTerrain;
        }

        public float GetValue(float x, float z)
        {
           // return (float)(PerlinNoiseGenerator.GetValue(x, 0, z) / 2f) + 0.5f;
			//return (float)(PerlinNoiseGenerator.get(x, 0, z) / 2f) + 0.5f;
			return (float)(PerlinNoiseGenerator.Get(x, 0, z)/ 2f) + 0.5f;
		}
    }
}