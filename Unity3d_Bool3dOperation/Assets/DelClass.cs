using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelClass : MonoBehaviour {
	public static void DeleteObj(GameObject go){
		DelByChild (go);
	}

/*
	public static void DeleteObj(GameObject go){
		if (go != null) {
			int cnt = go.transform.childCount;
			if (cnt > 0) {
				for (int i = 0; i < cnt; i++) {
					GameObject goCld = go.transform.GetChild (0).gameObject; 
					if (goCld.GetComponent<Renderer> () != null) {
						DestroyImmediate (goCld.GetComponent<Renderer> ().sharedMaterial);
					}

					if (goCld.GetComponent<MeshCollider> () != null) {
						DestroyImmediate (goCld.GetComponent<MeshCollider> ());
					}
					if (goCld.GetComponent<MeshFilter> () != null) {
						DestroyImmediate (goCld.GetComponent<MeshFilter> ().sharedMesh);
						DestroyImmediate (goCld.GetComponent<MeshFilter> ().mesh);
					}
					GameObject.DestroyImmediate (goCld);
				}
			}
			if (go.GetComponent<Renderer> () != null) {
				DestroyImmediate (go.GetComponent<Renderer> ().sharedMaterial);
			}

			if (go.GetComponent<MeshCollider> () != null) {
				DestroyImmediate (go.GetComponent<MeshCollider> ());
			}
			if (go.GetComponent<MeshFilter> () != null) {
				DestroyImmediate (go.GetComponent<MeshFilter> ().sharedMesh);
				DestroyImmediate (go.GetComponent<MeshFilter> ().mesh);
			}
			GameObject.DestroyImmediate (go);
		}
	}
*/

	//Needed to add to deleting of collider (with it mesh)!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	public static void DelByChild(GameObject go){
		if (go != null) {
			int cnt = go.transform.childCount;
			if (cnt > 0) {
				for (int i = 0; i < cnt; i++) {
					DelByChild (go.transform.GetChild (0).gameObject);
				}
			}
			//Не ясно как работать с объектами-спрайтами	
			if (go.GetComponent<SpriteRenderer> () == null) {
				if (go.GetComponent<Renderer> () != null) {
					int cntm = go.GetComponent<Renderer> ().sharedMaterials.Length;

					Material[] pMats = go.GetComponent<Renderer> ().sharedMaterials;
					for (int i = 0; i < cntm; i++) {
//Не ясно как избавиться от этого условия!!!!!!!!!!!!!!!!!					
//					if (pMats [i].shader.name != "Sprites-Default") {
						DestroyImmediate (pMats [i]); 
//					}
					}
// repeating for materials
//					pMats = go.GetComponent<Renderer> ().materials;
//					for (int i = 0; i < cntm; i++) {
//						DestroyImmediate (pMats [i]); 
//					}

//				DestroyImmediate (go.GetComponent<Renderer> ().sharedMaterial);
				}

				if (go.GetComponent<MeshCollider> () != null) {
					Destroy (go.GetComponent<MeshCollider> ());
				}
				if (go.GetComponent<MeshFilter> () != null) {
					Destroy(go.GetComponent<MeshFilter> ().sharedMesh);
					Destroy (go.GetComponent<MeshFilter> ().mesh);
				}
			}
			GameObject.Destroy(go);
		}
	}

}
