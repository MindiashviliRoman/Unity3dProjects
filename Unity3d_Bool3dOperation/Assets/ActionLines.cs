using UnityEngine;
using System.Collections;

public class ActionLines {

	public static GameObject DrawLine(Vector3 pos1, Vector3 pos2, Color col, string s = "GameObject"){
		Vector3[] verts = new Vector3[]{pos1, pos2};
		int[] indicesForLineStrip = new int[]{0,1};
		Mesh meshline = new Mesh();
		meshline.vertices = verts;
		meshline.SetIndices(indicesForLineStrip, MeshTopology.LineStrip, 0);
		meshline.RecalculateBounds();
		GameObject line = new GameObject(s);
		line.AddComponent<MeshFilter>();
		line.AddComponent<MeshRenderer>();
		MeshFilter mf = line.GetComponent<MeshFilter>();
		Material pMat = new Material(Shader.Find ("LineGradient2"));
//		pMat.SetVector("_Rang", pos1);
		pMat.SetColor("_Color1",col);
		pMat.SetColor("_Color2", new Color(0,0,0));
		pMat.SetVector("_Pos1", pos1);
		pMat.SetVector("_Pos2", pos2);
		pMat.SetFloat("_Magnitude", (pos1-pos2).magnitude);
		line.GetComponent<MeshRenderer>().material=pMat;
//		mesh.RecalculateNormals();
		mf.mesh = meshline;
		return line;
	}
	public static GameObject Text3D(Vector3 pos1, Color col, string txt){
		GameObject go = new GameObject();
		go.transform.position = pos1;
		go.transform.Rotate (new Vector3(0,180,0));
		go.transform.localScale = new Vector3(0.05f,0.05f,0.05f);
		TextMesh txtmsh = go.AddComponent<TextMesh>();
		txtmsh.color = col;
		txtmsh.text = txt;
//		txtmsh.font = GUIFont;//(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
		MeshRenderer rend = go.GetComponentInChildren<MeshRenderer>();
		rend.material = txtmsh.font.material;  /* ADDED THIS */
		txtmsh.fontSize = 1000;
		go.gameObject.tag = "Player";
		return go;
	}
}
