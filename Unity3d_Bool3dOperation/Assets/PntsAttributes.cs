using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PntsAttributes{
	public List<List<int>[]> inds; //i - num of aperture, //j - num of side (0 - not used (it is extern planes) ,1,2 or 3(>=3)), //z - num of index of vertex (it needed system)
	public List<int>[] externalPnts;
	public PntsAttributes(){
		inds = new List<List<int>[]> ();
		externalPnts = new List<int>[2];
		for (int i = 0; i < externalPnts.Length; i++) {
			externalPnts[i] = new List<int> ();
		}
	}
}