using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics {

	public static Texture2D noiseSource;


	public const float outerToInner = 0.866025404f;
	public const float innerToOuter = 1f / outerToInner;

	public const float outerRadius = 10f;
	public const float innerRadius = outerRadius * outerToInner;	
	public enum HexEdgeType
	{
		Flat,
		Slope,
		Cliff
	}

	public const float solidFactor = 0.8f;
	public const float blendfactor = 1f - solidFactor; 
	public const float elevationStep = 3f;
	public const int terracesPerSlope = 3;
	public const int terraceSteps = terracesPerSlope * 2 +1;
	public const float horizontalTerraceStepSize = 1f / terraceSteps; 
	public const float verticalTerraceStepSize = 1f/ (terracesPerSlope + 1);

	public const float streamBedElevationOffset = -1.75f;


	public const float cellPerturbStrength = 4f; //4f
	public const float elevationPerturbStrength = 1.5f;
	public const float noiseScale = 0.003f;

	#region TerrainFeaturesVars
	public const int hashGridSize = 256;
	public const float hashGridScale = 0.25f;
	static HexHash[] hashGrid;

	/// <summary>
	/// Adjust this to change the liklihood of terrain features being added (e.g. float a = hovel, float b = house,  float c = tower, and each array of floats corresponds to  terrain feature density)
	/// </summary>
	static float[][] featureThresholds = {
		new float[] { 0.0f, 0.0f, 0.4f },
		new float[] { 0.0f, 0.4f, 0.6f },
		new float[]{ 0.4f, 0.6f, 0.8f }
	};


	public const float wallHeight = 4f;
	public const float wallThickness = 0.75f;
	public const float wallElevationOffset = verticalTerraceStepSize;
	public const float wallTowerThreshold = 0.5f;
	public const float wallYOffset = -1f;

	public const float bridgeDesignLength = 7f;
	#endregion

	#region largerMaps
	public const int chunkSizex = 5, chunkSizeZ = 5;
	#endregion

	#region riverVars
	public const float waterElevationOffset = -0.5f; // how much lower the river water is than the top of the cell.
	#endregion

	#region waterVars
	public const float waterFactor = 0.6f;
	public const float waterBlendFactor = 1f - waterFactor;

	public static Vector3 GetFirstWaterCorner(HexDirection direction){
		return corners [(int)direction] * waterFactor;
	}

	public static Vector3 GetSecondWaterCorner(HexDirection direction){
		return corners [(int)direction + 1] * waterFactor;
	}



	public static Vector3 GetWaterBridge (HexDirection direction){
		return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
	}
	#endregion

	/// <summary>
	/// Defines the basic external structure of a hexagon, with seven points ( points 1 and 7 being the same spot. 
	/// </summary>
	public static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius), //NE(1)
		new Vector3(innerRadius, 0f, 0.5f * outerRadius), //E (2)
		new Vector3(innerRadius, 0f, -0.5f * outerRadius), //SE (3)
		new Vector3(0f, 0f, -outerRadius), //SW (4)
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius), //W (5)
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius), //NW (6)
		new Vector3(0f, 0f, outerRadius) //NE(7)
	};


	/// <summary>
	/// Gets the first external point of a hexagon, relative to the diretion you input. This is the furthest boundary of the cell
	/// </summary>
	/// <returns>The first corner.</returns>
	/// <param name="direction">Direction.</param>
	public static Vector3  GetFirstCorner(HexDirection direction){
		return corners[(int)direction];
	}

	/// <summary>
	/// Gets the second external point of a hexagon, relative to the diretion you input. This is the furthest boundary of the cell
	/// </summary>
	/// <returns>The second corner.</returns>
	/// <param name="direction">Direction.</param>
	public static Vector3 GetSecondCorner(HexDirection direction){
		return corners[(int)direction + 1];
	}


	/// <summary>
	///Gets the first internal point of a cell (i.e. the part of the cell which is "solid" in color and shape and which doesn't blend with other cells) relative to the direction you input. 
	/// </summary>
	/// <returns>The first solid corner.</returns>
	/// <param name="direction">Direction.</param>
	public static Vector3 GetFirstSolidCorner(HexDirection direction){
		return corners[(int) direction] * solidFactor;
	}


	/// <summary>
	///Gets the second internal point of a cell (i.e. the part of the cell which is "solid" in color and shape and which doesn't blend with other cells) relative to the direction you input. 
	/// </summary>
	/// <returns>The first solid corner.</returns>
	/// <param name="direction">Direction.</param>
	public static Vector3 GetSecondSolidCorner(HexDirection direction){
		return corners [(int)direction + 1] * solidFactor; 
	}



	public static Vector3 GetBridge(HexDirection direction){
		return (corners[(int)direction] + corners[(int)direction + 1]) * blendfactor;
	}

	public static	Vector3 TerraceLerp(Vector3 a, Vector3 b, int step){

		float horizontal = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * horizontal;
		a.z += (b.z - a.z) * horizontal;

		float vertical = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * vertical; 
		return a;
	}

	public static Color TerraceLerp(Color a, Color b, int step){
		float horizontal = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp (a, b, horizontal);
	}

	public static HexEdgeType GetHexEdgeType(int elevation1, int elevation2){

		if(elevation1 == elevation2){
			return HexEdgeType.Flat;
		}

		int delta = elevation2 - elevation1;

		if (delta == -1 || delta == 1) {
			return HexEdgeType.Slope;

		}
		return HexEdgeType.Cliff;

	}

	#region Making Hexagons Irregular
	public static Vector4 SampleNoise(Vector3 position){
		return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
	}



	public static Vector3 Perturb(Vector3 position){
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		//position.y += (sample.y * 2f - 1f) * HexMetrics.verticalPerturbStrength; //To keep cell centre flat don't adjust the y coord.
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}



	#endregion

	#region rivers
	public static Vector3 GetSolidEdgeMiddle(HexDirection direction){
		//averages two adjecent corner vectors  and applys the solid factor.
		return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
	}
	#endregion

	#region TerrainFeatures
	/// <summary>
	/// Initialises the hash grid. This approach is used so that you don't get completely random rotations every thime we change the terrain
	/// </summary>
	/// <param name="seed">Seed.</param>
	public static void InitialiseHashGrid(int seed){
		hashGrid = new HexHash[hashGridSize * hashGridSize];
		Random.State currentState = Random.state;
		Random.InitState (seed);
		for (int i = 0; i < hashGrid.Length; i++) {
			hashGrid [i] = HexHash.Create();
		}
		Random.state = currentState;
	}

	public static HexHash SampleHashGrid(Vector3 position){
		int x = (int)(position.x * hashGridScale) % hashGridSize;
		if (x < 0) {
			x += hashGridSize;
		}

		int z = (int)(position.z * hashGridScale) % hashGridSize;
		if (z < 0) {
			z += hashGridSize;
		}
		return hashGrid [x + z * hashGridSize]; 
	}

	/// <summary>
	/// Gets the percentage liklihood of each type of terrain feature being spawned (e.h. hovel, house or tower) for this density of a feature (1, 2, or 3) .
	/// </summary>
	/// <returns>The feature thresholds.</returns>
	/// <param name="level">Density of terrain feature in the cell.</param>
	public static float[] GetFeatureThresholds(int level){
		return featureThresholds[level -1]; 
	}

	public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far){
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (wallThickness * 0.5f);
	}

	public static Vector3 WallLerp(Vector3 near, Vector3 far){
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		float v = near.y < far.y ?	wallElevationOffset : (1f - wallElevationOffset);
		near.y += (far.y - near.y) * v + wallYOffset;
		return near;
	}
	#endregion



}
