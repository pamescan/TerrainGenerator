using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainMeshBuilder  {
	private List<int> m_Vertices = new List<int> ();
	public List<int> Vertices {get{ return m_Vertices;}}

	private List<int> m_Normals = new List<int> ();
	public List<int> Normals {get{ return m_Normals;}}

	private List<int> m_UVs = new List<int> ();
	public List<int> UVs {get{ return m_UVs;}}

//	private List<int> m_Indices = new List<int> ();

	/*public void AddTriangle(Vector3 index0, Vector3 index1, Vector3 index2){

		m_Indices.Add (index0);
		m_Indices.Add (index1);
		m_Indices.Add (index2);

	}*/

	public Mesh CreateMesh(int length){
		Mesh mesh = new Mesh ();

		Vector3[] vertices = new Vector3[] {
			new Vector3 (length/2, 100, length/2),
			new Vector3 (length/2, 100, -length/2),
			new Vector3 (-length/2, 100, length/2),
			new Vector3 (-length/2, 100, -length/2),
		};
		Vector2[] UV = new Vector2[] {
			new Vector2 (1, 1),
			new Vector2(1,0),
			new Vector2(0,1),
			new Vector2(0,0),
		};

		int[] triangles = new int[] {
			0, 1, 2,
			2, 1, 3,
		};
			mesh.vertices=vertices;
		mesh.triangles=triangles;
		mesh.uv=UV;
		mesh.RecalculateNormals ();
		return mesh;

		/*
		mesh.vertices = m_Vertices.ToArray ();
		mesh.triangles = m_Indices.ToArray ();

		if(m_Normals.Count==m_Vertices.Count()){
			mesh.normals = m_Normals.ToArray ();
		}

		if (m_UVs.Count==m_Vertices.Count()){
			mesh.uv = m_UVs.ToArray ();
		}
		mesh.RecalculateBounds ();*/

	
	}

}
