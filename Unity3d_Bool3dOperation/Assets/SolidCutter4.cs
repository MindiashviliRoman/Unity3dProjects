using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Diagnostics;

namespace Solid4{
	public class SolidCutter: MonoBehaviour{
        private static bool flgMode1;
        private static bool flgMode2;
        private static int intFlgMode1;
        private static int intFlgMode2;

        public static bool OptimazeMode = true;
        public static bool OldTrisLinksGetMode = false;

        private static Body[] bodyes = new Body[2]; //0 - slised body, 1 - sliser body
        private static Dictionary<Point, List<Poligon>>[] PoligonsByPoint = new Dictionary<Point, List<Poligon>>[] { new Dictionary<Point, List<Poligon>>(), new Dictionary<Point, List<Poligon>>() }; //0 - slised body, 1 - sliser body
        private static List<Point>[] LVects = new List<Point>[] { new List<Point>(), new List<Point>() };
        private static int[] indxs = new int[] { 0, 1, 2, 0 };
        private static Point[] LastPnt = new Point[2];//Предыдущая точка
        private static Point[] CurPnt = new Point[2];//Текущая точка
        //        private static int[][][][] crsBorders = new int[2][][][];//for control of adding crossPnt of two triangles //[bodyNum][elementOfTrianglesArray][numBorderThisTriang][numBorderOtherTriang]

        public static int kCount=0;
		public static int triCount1=0;
		public static int triCount2=0;
		public static int CountCross;
		public static int[] offset;
        public static int kk = 0;
        public static int kn = 0;

        private static List<GameObject> ListGO = new List<GameObject>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m3_new"></param>
        /// <param name="Basic"></param>
        /// <param name="go"></param>
        /// <param name="mode: 0 - Basic-go, 1 - go-Basic, 2 - Basic+go, 3 - -Basic-go"></param>
        /// <param name="mats"></param>
        /// <param name="connectVerts"></param>
        /// <returns></returns>
        public static Mesh DeductValue(Mesh m3_new, GameObject Basic, GameObject go, ref Material[] mats, ref PntsAttributes connectVerts, int mode = 0) {

            switch (mode){
                case 0:
                    flgMode1 = true;
                    flgMode2 = false;
                    intFlgMode1 = 1;
                    intFlgMode2 = -1;
                    break;
                case 1:
//                    GameObject swaper = Basic;
//                    Basic = go;
//                    go = swaper;
                    flgMode1 = false;
                    flgMode2 = true;
                    intFlgMode1 = -1;
                    intFlgMode2 = 1;
                    break;
                case 2:
                    flgMode1 = true;
                    flgMode2 = true;
                    intFlgMode1 = 1;
                    intFlgMode2 = 1;
                    break;
                case 3:
                    flgMode1 = false;
                    flgMode2 = false;
                    intFlgMode1 = 1;
                    intFlgMode2 = 1;
                    break;
                default:
                    break;
            }

            foreach(GameObject g in ListGO){
                Destroy(g);
            }
            ListGO.Clear();
            kk = 0;
			CountCross=0;
			int oldCntAperture = connectVerts.inds.Count;

			kCount=0;
			Mesh m1 = Basic.GetComponent<MeshFilter>().sharedMesh;
//			Mesh m3_new = m1;
            Mesh m2 = go.GetComponent<MeshFilter>().sharedMesh;
            int triCnt1 = m1.triangles.Length;
            int triCnt2 = m2.triangles.Length;
            Vector3[] v1 = m1.vertices;
			Vector3[] v2 = m2.vertices;
			Vector3[] n1 = m1.normals;
			Vector3[] n2 = m2.normals;
			Vector4[] tan1 = m1.tangents;
			Vector4[] tan2 = m2.tangents;
			Vector2[] uv1 = m1.uv;
			Vector2[] uv2 = m2.uv;

            Matrix4x4 MT1toWorld = Basic.transform.localToWorldMatrix;
            Matrix4x4 MT1toLocal = Basic.transform.worldToLocalMatrix;
            Matrix4x4 MT2toWorld = go.transform.localToWorldMatrix;
//            Matrix4x4 MT2toLocal = go.transform.worldToLocalMatrix;

            v1 = TrmMshPntToWrdPnt (v1, Basic.transform);
            for (int i = 0; i < n1.Length; i++) {
                n1[i] = MT1toWorld * n1[i];
            }
            v2 = TrmMshPntToWrdPnt(v2, go.transform);
            for (int i = 0; i < n2.Length; i++) {
                n2[i] = MT2toWorld * n2[i];
            }

            PoligonsByPoint[0].Clear();//Для отсечения лишних треугольников
            //Задел для обработки многоматериальных тел
            int vertPolCnt = 3;
            int subMshCnt1 = m1.subMeshCount;

			Poligon[] bodyTris1 = new Poligon[triCnt1 / vertPolCnt];
			Point[] bodyPnts1 = new Point[v1.Length];
            List<Border> bodyBorders1 = new List<Border>();
            int[] indxxs = null;
            Point curPnt = null;
            /*
             * Any subMsh can to have different pnts with coincides coordinates!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
             * in This reason may be troubles with borders by index of tris elements!!!!
             */
            int triOffst = 0;
            Dictionary<Border, int> newBorderMaker = new Dictionary<Border, int>();//It not should to correspond to subMshNum
            for (int itri =0; itri<subMshCnt1; itri++){
                Dictionary<int, Point> newPntsMaker = new Dictionary<int, Point>();
                int[] tri_ = m1.GetTriangles(itri);
				for(int i=0; i<tri_.Length; i+= vertPolCnt) {
                    int[] curBorders = new int[vertPolCnt];
                    int[] curNeighborsInd = new int[vertPolCnt];
                    List<Point> pnts = new List<Point>();
                    bool[] flgBoost = new bool[] { false, false, false }; //for acceleration of Adding AddToBodyTriangles
                    for (int z = 0; z < vertPolCnt; z++) {
                        int j = tri_[i+z];
                        if (newPntsMaker.ContainsKey(j)){
                            curPnt = newPntsMaker[j];
                            flgBoost[z] = true;
                        } else {
                            curPnt = new Point(true, v1[j], intFlgMode1 * n1[j], j, itri, uv1[j], tan1[j]);
                            bodyPnts1[j] = curPnt;
                            newPntsMaker.Add(j, curPnt);
                        }
                        pnts.Add(curPnt);
                        if (z == vertPolCnt-1) {
                            indxxs = AddBorderMakerDict(ref newBorderMaker, ref bodyBorders1, ref v1, triOffst, j, tri_[i], z);
                        } else {
                            indxxs = AddBorderMakerDict(ref newBorderMaker, ref bodyBorders1, ref v1, triOffst, j, tri_[i + z + 1], z);
                        }
                        curBorders[z] = indxxs[0];
                        curNeighborsInd[z] = indxxs[1];
                    }
					Poligon newPoligon  = new Poligon(pnts, curBorders, curNeighborsInd, itri, intFlgMode1);
					newPoligon.AddToBodyTriangles(ref PoligonsByPoint[0], flgBoost);
                    bodyTris1[triOffst] = newPoligon;
                    triOffst++;
                }
            }
            bodyes[0] = new Body(bodyPnts1, bodyBorders1, bodyTris1);
            LinksCreatorPar newLinks = null;
            if (OldTrisLinksGetMode) {
                newLinks = new LinksCreatorPar(connectVerts);
				for (int i = 0; i < connectVerts.externalPnts.Length; i++) {
					for (int j = 0; j < connectVerts.externalPnts [i].Count; j++) {
						int z = connectVerts.externalPnts [i] [j];
						bodyPnts1 [z].IsAroundSidePnt = true;
						bodyPnts1 [z].OldIndxs = new int[]{ i, j }; //for save order in lists
					}
				}
                for (int i = 0; i < connectVerts.inds.Count; i++) {
                    for (int j = 0; j < connectVerts.inds[i].Length; j++) {
                        for (int z = 0; z < connectVerts.inds[i][j].Count; z++) {
                            int tmpInd = connectVerts.inds[i][j][z]; //kill link
                            bodyPnts1[tmpInd].IsUsedByTranslate = true;
                            bodyPnts1[tmpInd].OldApertNum = i;
                            bodyPnts1[tmpInd].OldIndxs = new int[] { j, z }; //for save order in lists
                        }
                    }
                }
            }

            PoligonsByPoint[1].Clear();//Для отсечения лишних треугольников
            int subMshCnt2 = m2.subMeshCount;
            Poligon[] bodyTris2 = new Poligon[triCnt2/3];
            Point[] bodyPnts2 = new Point[v2.Length];
            List<Border> bodyBorders2 = new List<Border>();
            triOffst = 0;
            newBorderMaker = new Dictionary<Border, int>();//It not should to correspond to subMshNum
            for (int itri =0; itri<subMshCnt2; itri++){
                Dictionary<int, Point> newPntsMaker = new Dictionary<int, Point>();
                int[] tri_ = m2.GetTriangles(itri);
				for(int i=0; i < tri_.Length ; i+= vertPolCnt) {
                    int[] curBorders = new int[vertPolCnt];
                    int[] curNeighborsInd = new int[vertPolCnt];
                    List<Point> pnts = new List<Point>();
                    bool[] flgBoost = new bool[] { false, false, false }; //for acceleration of Adding AddToBodyTriangles
                    for (int z = 0; z < vertPolCnt; z++) {
                        int j = tri_[i+z];
                        if (newPntsMaker.ContainsKey(j)) {
                            curPnt = newPntsMaker[j];
                            flgBoost[z] = true;
                        } else {
                            curPnt = new Point(true, v2[j], intFlgMode2 * n2[j], itri, j, uv2[j], tan2[j]);
                            bodyPnts2[j] = curPnt;
                            newPntsMaker.Add(j, curPnt);
                        }
                        pnts.Add(curPnt);
                        if (z == vertPolCnt - 1) {
                            indxxs = AddBorderMakerDict(ref newBorderMaker, ref bodyBorders2, ref v2, triOffst, j, tri_[i], z);
                        } else {
                            indxxs = AddBorderMakerDict(ref newBorderMaker, ref bodyBorders2, ref v2, triOffst, j, tri_[i + z + 1], z);
                        }
                        curBorders[z] = indxxs[0];
                        curNeighborsInd[z] = indxxs[1];
                    }
					Poligon newPoligon  = new Poligon(pnts, curBorders, curNeighborsInd, itri, intFlgMode2);
					newPoligon.AddToBodyTriangles(ref PoligonsByPoint[1], flgBoost);
                    bodyTris2[triOffst] = newPoligon;
                    triOffst++;
                }
            }
            bodyes[1] = new Body(bodyPnts2, bodyBorders2, bodyTris2);

            bool flgIsCrossed = false;
			ResultInfo Ri = GetMeshInterSectGO(bodyes, subMshCnt1, subMshCnt2, newLinks, ref flgIsCrossed);
			if (flgIsCrossed) {

                LinksCreator listLinks = new LinksCreator ();
                listLinks.OldInds = connectVerts.inds;//= oldInds;
                listLinks.ExternPnts = connectVerts.externalPnts;

                m3_new = Ri.bodyMsh.MeshRefresh(m3_new);
                Vector3[] newV1 = m3_new.vertices;
                Vector3[] newN = m3_new.normals;
                for (int i = 0; i < newN.Length; i++) {
                    newN[i] = MT1toLocal * newN[i];
                }
                newV1 = TrmWrdPntToMshPnt (newV1, Basic.transform);//, roundScale);
				m3_new.vertices = newV1;
                m3_new.normals = newN;
                m3_new.RecalculateBounds ();
                if (OptimazeMode) {
					//for optimization recreate offseted aperture
					if (listLinks.IsRefreshedOld) {
						connectVerts.inds = listLinks.GetOldIndxs ();
					}
//					connectVerts.inds = listLinks.GetList ();
//					connectVerts.externalPnts = listLinks.GetExternIndxs ();
				}
            }else {
                m3_new = CopyMshAttribute(m1, m3_new);
            }
			//Возвращаем все получившиеся материалы
			List<Material> mat = new List<Material> ();
			mats = Basic.GetComponent<Renderer> ().sharedMaterials;
			//Используем subMshCnt1, а не mats.Length,
			//чтобы правильно забрать материалы, которые на реально существующих сабмешах
			//ошибка может получиться, когда вручную назначишь большее число материал, чем реально сабмешей
			for (int i = 0; i < subMshCnt1; i++) {
				mat.Add (mats [i]);
			}
			if (flgIsCrossed) {
				mats = go.GetComponent<Renderer> ().sharedMaterials;
				for (int i = 0; i < subMshCnt2; i++) {
					mat.Add (mats [i]);
				}
			}
			mats = mat.ToArray ();

			return m3_new;
		}

        public static int[] AddBorderMakerDict(ref Dictionary<Border, int> newBorderMaker, ref List<Border> bodyBorders, ref Vector3[] pnts, int polyIndex, int i, int j, int z) {
            //in this nBorder i and j may be swapped. and it allow to comparation Equals by ref
            Border nBorder = new Border(i, j, pnts[i], pnts[j]);
            int[] posBorder_IndxPoly = new int[2];//new int[3];//[posBord][indxSideInPoly][indxPolyInCurBord]
            if (newBorderMaker.ContainsKey(nBorder)) {
                posBorder_IndxPoly[0] = newBorderMaker[nBorder];
                nBorder = bodyBorders[posBorder_IndxPoly[0]];//Link to real exist border
                posBorder_IndxPoly[1] = 1;
                nBorder.IndxSideInPoly[1] = z;//i;//polyIndex;
                nBorder.IndxNeighbors[1] = polyIndex;//index in poligon massive in body

            } else {
                posBorder_IndxPoly[0] = bodyBorders.Count;
                bodyBorders.Add(nBorder);
                posBorder_IndxPoly[1] = 0;
                nBorder.IndxSideInPoly[0] = z; //i;
                nBorder.IndxNeighbors[0] = polyIndex;//index in poligon massive in body
                newBorderMaker.Add(nBorder, posBorder_IndxPoly[0]);
            }
            return posBorder_IndxPoly;
        }

		public static ResultInfo GetMeshInterSectGO(Body[] bds, int subMshCnt1, int subMshCnt2, LinksCreatorPar links, ref bool flgIsCrossedBody) {
            /*
             * Можно сразу отслеживать отсечённые треугольники попробовать....
             * И если они уже отсечены, с ними расчётов не проделывать!!!!!!!!!!
             */
            LVects[0].Clear();
            LVects[1].Clear();
            int allSubMshCnt = subMshCnt1 + subMshCnt2;
            BodyCreator newBody = new BodyCreator(allSubMshCnt, OptimazeMode, links);
            bool flgSide = flgMode1;//body 1
            Poligon[] t1 = bds[0].Poligons;
            Poligon[] t2 = bds[1].Poligons;
            for (int i = 0; i < t1.Length; i++) {
                kk++;
                for (int j = 0; j < t2.Length; j++) {
                    t1[i].CrossToPoligon(t2[j], bds, i, j);
                    flgIsCrossedBody = flgIsCrossedBody || t1[i].Nets.Count > 0;
                }

                if (flgIsCrossedBody) {
                    GetCrossedFaces(flgSide, 0, 0, t1[i], ref newBody, intFlgMode1);
                }
            }
			if (flgIsCrossedBody) {
                GetNonCrossedFaces(flgSide, 0, 0, ref newBody);

                //Body 2
                flgSide = flgMode2;//false;
                for (int i = 0; i < t2.Length; i++) {
                    GetCrossedFaces(flgSide, 1, subMshCnt1, t2[i], ref newBody, intFlgMode2);
                }
                GetNonCrossedFaces(flgSide, 1, subMshCnt1, ref newBody);

            }
            ResultInfo RI = new ResultInfo(newBody.BodyGenerate());

            return RI;
		}
        private static void GetCrossedFaces(bool flgSide, int numBody, int offsetSubMshNum, Poligon pol, ref BodyCreator nBody, int flgMode) {
            if (pol.NetKeys.Count > 0) {
                               
                List<Point> SegsOfIntersect = pol.NetKeys;
                Holes SegsOfIntsct = SortSegs(SegsOfIntersect, pol);
                /*
                for (int i = 0; i < SegsOfIntsct.BorderHoles.Lst.Count; i++) {
                    for (int j = 0; j < SegsOfIntsct.BorderHoles.Lst[i].Count; j++) {
                        ListGO.Add(VectorExtension.CreatePntGO("Border_" + i.ToString() + "_" + j.ToString() + "_" + SegsOfIntsct.BorderHoles.Lst[i][j].IsTriVertex + "_"+ SegsOfIntsct.BorderHoles.Lst[i][j].IsCrossBorderedPnt + "_" + SegsOfIntsct.BorderHoles.Lst[i][j].IsBorderedPnt + "_" + SegsOfIntsct.BorderHoles.Lst[i][j].IsTriVertex  + "_" + SegsOfIntsct.BorderHoles.Lst[i][j].V, SegsOfIntsct.BorderHoles.Lst[i][j].V));
                    }
                }
                for (int i = 0; i < pol.Pnts.Count; i++) {
                    ListGO.Add(VectorExtension.CreatePntGO("curTri_" + i.ToString() + "_" + pol.Pnts[i].V, pol.Pnts[i].V));
                }
                for (int i = 0; i < SegsOfIntsct.InnerHoles.Lst.Count; i++) {
                    for(int j=0; j< SegsOfIntsct.InnerHoles.Lst[i].Count; j++) {
                        ListGO.Add(VectorExtension.CreatePntGO("Inner_" + i.ToString() + "_" + j.ToString() + "_" + SegsOfIntsct.InnerHoles.Lst[i][j].IsTriVertex + "_" + SegsOfIntsct.InnerHoles.Lst[i][j].IsCrossBorderedPnt + "_" + SegsOfIntsct.InnerHoles.Lst[i][j].IsBorderedPnt + "_" + SegsOfIntsct.InnerHoles.Lst[i][j].IsTriVertex + "_" + SegsOfIntsct.InnerHoles.Lst[i][j].V, SegsOfIntsct.InnerHoles.Lst[i][j].V));
                    }
                }
                */
                List<List<Point>> PerTriPnts = null;
                bool flgExistsBorderHoles = SegsOfIntsct.BorderHoles.Lst.Count > 0;
                if (flgExistsBorderHoles) {

                    PerTriPnts = GetPerimetrPnts(pol, SegsOfIntsct.BorderHoles, flgSide, numBody);
                  /*
                    for (int j = 0; j < PerTriPnts.Count; j++) {
                        for (int z = 0; z < PerTriPnts[j].Count; z++) {
                            ListGO.Add(VectorExtension.CreatePntGO("Perimeter_"+ numBody.ToString()+"_" + j.ToString() + "_" + z.ToString() + "_" + PerTriPnts[j][z].V, PerTriPnts[j][z].V));
                        }
                    }
                    */
                }

                List<List<Point>> PerTriPntsHoles = null;
                bool flgExistsInnerHoles = SegsOfIntsct.InnerHoles.Lst.Count > 0;
                if (flgExistsInnerHoles) {
                    if (flgExistsBorderHoles) {
                        for (int i = 0; i < PerTriPnts.Count; i++) { //необходимо не добавлять в конце точку GetPerimetrPnts
                            if (PerTriPnts[i].Count > 0) { 
                                PerTriPnts[i].RemoveAt(0);
                            }
                        }
                        pol.nTrPnts = PerTriPnts;
                    } else {
                        pol.nTrPnts = new List<List<Point>>();
                        if (flgSide) {
                            //Adding only for cutting
                            pol.nTrPnts.Add(pol.Pnts);
                        }
                    }

                    PerTriPntsHoles = GetArrWithHole(SegsOfIntsct.InnerHoles, pol, flgSide, flgMode, numBody);
                    /*
                    for (int j = 0; j < PerTriPntsHoles.Count; j++) {
                        for (int z = 0; z < PerTriPntsHoles[j].Count; z++) {
                            ListGO.Add(VectorExtension.CreatePntGO("Perimeter_Inner_" + numBody.ToString() + "_" + j.ToString() + "_" + z.ToString() + "_" + PerTriPntsHoles[j][z].V.ToString(), PerTriPntsHoles[j][z].V));
                        }
                    }
                    */
                }
                if (flgExistsBorderHoles && !flgExistsInnerHoles) {
                    for (int ii = 0; ii < PerTriPnts.Count; ii++) {
                        if (PerTriPnts[ii].Count > 0) {
                            PerTriPnts[ii].RemoveAt(0);
                            List<int> tris = Triangulation.GetTriangles(PerTriPnts[ii], 0, pol.Norm);
                            pol.IsCrossed = true;
                            int indx = pol.IndSubMsh + offsetSubMshNum;
                            nBody.AddNewPoligonAttributes(indx, tris, PerTriPnts[ii]);
                        }
                    }
                }
                if (flgExistsInnerHoles) {
                    for (int ii = 0; ii < PerTriPntsHoles.Count; ii++) {
                        if (PerTriPntsHoles[ii].Count > 0) {
                            PerTriPntsHoles[ii].RemoveAt(0);
                            if (!flgSide) {
                                PerTriPntsHoles[ii].Reverse();
                            }
                            List<Vector2> tmpVectors2 = Point.GetListProjVector2(PerTriPntsHoles[ii]);
                            List<int> tris = Triangulation.GeneralTriangulation(tmpVectors2, 0, pol.PrjNorm, true, flgSide);//, (flgMode == 1) == flgSide);//flgSide);//Triangulation.GetTriangles(PerTriPntsHoles[ii], 0, pol.Norm);
                            pol.IsCrossed = true;
                            int indx = pol.IndSubMsh + offsetSubMshNum;
                            nBody.AddNewPoligonAttributes(indx, tris, PerTriPntsHoles[ii]);
                        }
                    }
                }
                if (flgSide) {
                    pol.IsChangedFace = flgExistsBorderHoles || flgExistsInnerHoles;
                }else {
                    pol.IsChangedFace = flgExistsBorderHoles || flgExistsInnerHoles;
                }
            }
        }

        private static void GetNonCrossedFaces(bool flgSide, int numBody, int offsetSubMshNum, ref BodyCreator nBody) {
            List<Point> vBad = LVects[numBody];
            while (vBad.Count > 0) {
                vBad = NxtvBads(vBad, numBody);
            }
            List<List<Poligon>> tmpTrs = PoligonsByPoint[numBody].Values.ToList();
            List<Poligon> tmpPols = new List<Poligon>();
            for (int j = 0; j < tmpTrs.Count; j++) {
                for (int z = 0; z < tmpTrs[j].Count; z++) {
                    if (!tmpTrs[j][z].IsFiltred) {
                        tmpTrs[j][z].IsFiltred = true;
                        tmpPols.Add(tmpTrs[j][z]);
                    }
                }
            }
            Poligon[] pols = new Poligon[tmpPols.Count];
            for (int j = 0; j < pols.Length; j++) {
                pols[j] = tmpPols[j];
            }

            for (int i = 0; i < pols.Length; i++) {
                if (pols[i].Pnts.Count > 2) {
                    List<int> tris = null;
                    if (!pols[i].IsSliser) {
                        tris = new List<int>(new int[] { 0, 1, 2 });
                    } else {
                        tris = new List<int>(new int[] { 0, 2, 1 });
                    }
                    int indx = pols[i].IndSubMsh + offsetSubMshNum;
                    nBody.AddNewPoligonAttributes(indx, tris, pols[i].Pnts);
                }
            }
        }
        
        public static List<Point> NxtvBads(List<Point> v, int indBody){
			List<Point> tmpLV = new List<Point>();
            Dictionary<Point, List<Poligon>> curDict = PoligonsByPoint[indBody];
            for (int i = 0; i<v.Count;i++){
				if(curDict.ContainsKey(v[i])){
                    List<Poligon> tmpTris = curDict[v[i]];
                    for (int x = 0; x < tmpTris.Count; x++) {
                        if (tmpTris[x].LinkPnts.Count > 0) {
                            tmpTris[x].LinkPnts.Remove(v[i]);// можно записать индекс?? в нескольких полигонах мб 1 и та же общая точка
                            tmpLV.AddRange(tmpTris[x].LinkPnts);
                        }
                    }
                    //Если из треуголника удалена хотя бы 1 точка удалим треугольник
                    curDict.Remove (v[i]);
				}
			}
			return tmpLV;
		}



        public static Holes SortSegs(List<Point> Keys, Poligon pol) {
            //При таком подходе не обязательно иметь список пар точек... поскольку ссылки уже в словаре !!!!!!!
            Holes Hol = new Holes();

            List<int> indxsGroup = null;
            int curPosition = -1;
            int offsetCntr = -1;
            bool flgDelitedLinksToCurPnt = false;

            do {
                List<Point> Seq = new List<Point>();
                List<Vector3[]> SeqNrmCutter = new List<Vector3[]>();

                indxsGroup = new List<int>();
                curPosition = 0;
                offsetCntr = 0;
                flgDelitedLinksToCurPnt = false;
                Point nxtPnt = Keys[0];
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                Point lastPnt = null;
                if (nxtPnt.IsBorderedPnt) {
                    lastPnt = pol.Pnts[nxtPnt.Side[0]];
                    lastPnt.NormCutterContainer[1] = Vector3.Cross(nxtPnt.V - lastPnt.V, pol.Norm);
                } else {
                    lastPnt = nxtPnt;//null;//nxtPnt;
                }
                List<Point> bonds = GetnxtPnts(nxtPnt, lastPnt, pol, true); //GetnxtPnts(nxtPnt, nxtPnt, pol, true);
                AddToSeq(1, nxtPnt, ref Seq, ref SeqNrmCutter, ref indxsGroup, ref curPosition, ref offsetCntr);
                Keys.RemoveAt(0); //LastS.Remove(nxtPnt); //191208
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                if (bonds != null) {
                    bool flgBordered = nxtPnt.IsBorderedPnt;//Возможно, не хватает проверки на точках bounds[i]
//                    bool flgBordered = nxtPnt.IsBorderedPnt && !nxtPnt.IsCrossBorderedPnt;
                    int bndCnt = bonds.Count;
                    if (bndCnt > 0) {
                        //nxtbonds = список списков на случай, когда в 1 точке стыкуются больше 2-х линий (пока не продумано)
                        List<List<Point>> nxtbonds = new List<List<Point>>();
                        for (int i = 0; i < bndCnt; i++) {
                            if (!flgBordered) {
                                flgBordered = bonds[i].IsBorderedPnt;
//                                flgBordered = bonds[i].IsBorderedPnt && !bonds[i].IsCrossBorderedPnt;
                            }
                            List<Point> tmpLbonds = new List<Point>();
                            tmpLbonds.Add(bonds[i]);
                            nxtbonds.Add(tmpLbonds);
                            flgDelitedLinksToCurPnt = DelonDictNxtPnt(nxtPnt, bonds[i], pol);//Чистим словарь
                        }
                        CurPnt[0] = nxtPnt;//Предыдущая точка
                        CurPnt[1] = nxtPnt;
                        bool flgGoNxt = false;
                        do {
                            for (int i = 0; i < nxtbonds.Count; i++) {
                                if (nxtbonds[i] != null) {
                                    //Пока не нашли граничной точки, делаем проверку.
                                    if (!flgBordered) {
                                        flgBordered = nxtbonds[i][0].IsBorderedPnt;
//                                        flgBordered = nxtbonds[i][0].IsBorderedPnt && !nxtbonds[i][0].IsCrossBorderedPnt;
                                    }
                                    flgDelitedLinksToCurPnt = DelonDictNxtPnt(nxtbonds[i][0], CurPnt[i], pol);//Чистим словарь от обратки
                                    LastPnt[i] = CurPnt[i];//Предыдущая точка
                                    CurPnt[i] = nxtbonds[i][0];//текущая точка
                                    nxtbonds[i] = GetnxtPnts(CurPnt[i], LastPnt[i], pol, false, i);

                                    AddToSeq(i, CurPnt[i], ref Seq, ref SeqNrmCutter, ref indxsGroup, ref curPosition, ref offsetCntr);
                                    Keys.Remove(CurPnt[i]);
                                    if (nxtbonds[i] != null) {
                                        flgDelitedLinksToCurPnt = DelonDictNxtPnt(CurPnt[i], nxtbonds[i][0], pol);//Чистим словарь от пройденного пути
                                    }
                                }
                                //Отделяем дырки, не пересекающие границу/ или замыкающиеся в точке на границе или вершине полигона
                                if(CurPnt[0] == CurPnt[1] && flgDelitedLinksToCurPnt) {
                                    int lastEl = SeqNrmCutter.Count - 1;
                                    if (Seq[0].bordCloseIndx == 1) {
                                        SeqNrmCutter[0][0] = SeqNrmCutter[lastEl][0];
                                        SeqNrmCutter[0][1] = SeqNrmCutter[lastEl][1];
                                    } else { 
                                        SeqNrmCutter[lastEl][0] = SeqNrmCutter[0][0];
                                        SeqNrmCutter[0][1] = SeqNrmCutter[lastEl][1];
                                    }
                                    flgGoNxt = true;
                                    break;
                                }

                                //Отделяем дырки, пересекающие границу
                                if (nxtbonds[i] == null && nxtbonds[nxtbonds.Count - 1 - i] == null) {
                                    int lastEl = SeqNrmCutter.Count - 1;
                                    SeqNrmCutter[0][0] = SeqNrmCutter[0][1];
                                    SeqNrmCutter[lastEl][1] = SeqNrmCutter[lastEl][0];
                                    flgGoNxt = true;
                                    break;
                                }

                            }
                        } while (!flgGoNxt);
                        for (int i = 0; i < nxtbonds.Count; i++) {
                            if (nxtbonds[i] != null) {
                                DelonDictNxtPnt(nxtbonds[i][0], CurPnt[i], pol);//Чистим словарь
                                if (!flgBordered) {
                                    flgBordered = nxtbonds[i][0].IsBorderedPnt;
//                                    flgBordered = nxtbonds[i][0].IsBorderedPnt && !nxtbonds[i][0].IsCrossBorderedPnt;
                                }
                            }
                        }
                    }
                    //Добавляем в список "дырок" список точек контура очередной "дырки"
                    if (indxsGroup.Count == 0) {
                        indxsGroup = null;
                    } else {
                        for (int i = 0; i < indxsGroup.Count; i++) {
                            indxsGroup[i] += offsetCntr;
                        }
                        pol.bHoles.IndexCrossBorderPntsInner.Add(indxsGroup);
                    }

                    if (flgBordered) {
                        pol.bHoles.BorderHoles.Lst.Add(Seq);
                        pol.bHoles.BorderHoles.NormCutts.Add(SeqNrmCutter);
                        pol.bHoles.BorderHoles.CreateLinkToNormCutt();
                    } else {
                        pol.bHoles.InnerHoles.Lst.Add(Seq);
                        pol.bHoles.InnerHoles.NormCutts.Add(SeqNrmCutter);
                        pol.bHoles.InnerHoles.CreateLinkToNormCutt();
                    }
                }
            } while (Keys.Count > 0);
            /*
            for (int i = 0; i < pol.bHoles.BorderHoles.Lst.Count; i++) {
                int last = pol.bHoles.BorderHoles.Lst[i].Count - 1;
                Point PP;
                Vector3[] PV;
                for (int j = 0; j< pol.bHoles.BorderHoles.Lst[i].Count; j++) {
                    PP = pol.bHoles.BorderHoles.Lst[i][j];
                    PV = pol.bHoles.BorderHoles.NormCutts[i][j];
                    ListGO.Add(VectorExtension.CreatePntGO("Border_with_NormCutt_" + i.ToString() + "_" + PP.V + "_" + PV[0] + "_" + PV[1], PP.V));
                    ListGO.Add(ActionLines.DrawLine(PP.V, PP.V+PV[0], Color.red, i.ToString() + "_" + PV[0]));
                    ListGO.Add(ActionLines.DrawLine(PP.V, PP.V+PV[1], Color.red, i.ToString() + "_" + PV[1]));
                }
            }
            for (int i = 0; i < pol.bHoles.InnerHoles.Lst.Count; i++) {
                int last = pol.bHoles.InnerHoles.Lst[i].Count - 1;
                Point PP;
                Vector3[] PV;
                for (int j = 0; j < pol.bHoles.InnerHoles.Lst[i].Count; j++) {
                    PP = pol.bHoles.InnerHoles.Lst[i][j];
                    PV = pol.bHoles.InnerHoles.NormCutts[i][j];
                    ListGO.Add(VectorExtension.CreatePntGO("Inner_with_NormCutt_" + i.ToString() + "_" + PP.V + "_" + PV[0] + "_" + PV[1], PP.V));
                    ListGO.Add(ActionLines.DrawLine(PP.V, PP.V + PV[0], Color.red, i.ToString() + "_" + PV[0]));
                    ListGO.Add(ActionLines.DrawLine(PP.V, PP.V + PV[1], Color.red, i.ToString() + "_" + PV[1]));
                }
            }
            */

            return pol.bHoles;
		}

        private static void AddToSeq(int i, Point pnt, ref List<Point> Seq, ref List<Vector3[]> SeqNrmCutts, ref List<int> indxsGroup, ref int curPosition, ref int offsets) {
            int len = pnt.NormCutterContainer.Length;
            Vector3[] nwArr = new Vector3[len];
            System.Array.Copy(pnt.NormCutterContainer, nwArr, len);
            if (i == 0) {
                Seq.Add(pnt);
                SeqNrmCutts.Add(nwArr);
            } else {
                Seq.Insert(0, pnt);
                SeqNrmCutts.Insert(0, nwArr);
                offsets++;
            }
            if (pnt.IsCrossBorderedPnt) {//write start position
                indxsGroup.Add(curPosition);
                curPosition++;
            }
        }

        //FirstPnt возможно они и не нужен!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public static List<Point> GetnxtPnts(Point pnt, Point LastPnt, Poligon pol, bool FirstPnt, int nindx = 0){
			List<Point> nxtPnts = null;
            List<Vector3> nxtPntsSecat = null;
			int indxList= -1;
			if(pol.Nets.ContainsKey(pnt)){
                indxList = pol.Nets[pnt];
                List<Point> netLinks = pol.NetLinks[indxList];
                nxtPntsSecat = pol.NetSecatorByPnt[indxList];

                int otherIndx = 1 - nindx;

                //For fix border pnts as first or last
                if((!FirstPnt) && pnt.IsBorderedPnt) {
                    return null;
                }

                if (FirstPnt && pnt.IsCrossBorderedPnt && pnt.IsBorderedPnt) {
                    if (netLinks.Count == 2) {
                        netLinks.RemoveAt(1);
                        nxtPntsSecat.RemoveAt(1);
                        pnt.bordCloseIndx = 1;
                    }
                } /*else {
                    if (pnt.IsCrossBorderedPnt) {
                        pnt.NormCutterContainer[otherIndx] = nxtPntsSecat[0];
                        netLinks.RemoveAt(0);
                        nxtPntsSecat.RemoveAt(0);
                        return null;
                    }
                }
                */
                nxtPnts = new List<Point>(netLinks);
                nxtPnts.Capacity = netLinks.Capacity;

                bool flgMultyPnt = false;
                if (FirstPnt) {
                    flgMultyPnt = nxtPnts.Count > 2;
                }else {
                    flgMultyPnt = nxtPnts.Count > 1;
                }

                if (flgMultyPnt) {
                    Vector3[] subNxtPntsSecat = new Vector3[2];
                    nxtPnts = GetSolidBond(nindx, pnt, LastPnt, pol.Norm, FirstPnt, ref netLinks, ref nxtPntsSecat);//, ref subNxtPntsSecat);
                    pnt.NormCutterContainer[nindx] = nxtPnts[0].NormCutterContainer[0];
                } else {
                    pnt.NormCutterContainer[otherIndx] = nxtPntsSecat[0];
                    if (FirstPnt) {
                        if (netLinks.Count == 2) {
                            pnt.NormCutterContainer[nindx] = nxtPntsSecat[1];
                        }else {
                            pnt.NormCutterContainer[nindx] = nxtPntsSecat[0];
                        }
                        for (int i = 0; i < netLinks.Count; i++) {
                            int otherIndx2 = 1 - i;
                            nxtPnts[i] = netLinks[i];
                            nxtPnts[i].NormCutterContainer[i] = pnt.NormCutterContainer[otherIndx2];
                        }
                    } else {
                        for (int i = 0; i < netLinks.Count; i++) {
                            nxtPnts[i] = netLinks[i];
                            nxtPnts[i].NormCutterContainer[nindx] = pnt.NormCutterContainer[otherIndx];
                        }
                    }
                }
            }
			return nxtPnts;
		}
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Необходимо будет править

        public static List<Point> GetSolidBond(int pos, Point curPnt, Point lastPnt, Vector3 NormTri, bool FirstPnt, ref List<Point> nxtPnts, ref List<Vector3> nxtPSecats) {//, ref Vector3[] subNxtPSecats) {
            List<Point> result = new List<Point>();
            Vector3 lastDir = lastPnt.V - curPnt.V;
            Point nxtPnt1 = null;
            Point nxtPnt2 = null;
            Vector3 nxtNormSec0 = Vector3.zero;
            Vector3 nxtNormSec1 = Vector3.zero;
            int indxPnt1 = -1;
            int indxPnt2 = -1;
            float curAngle = 0f;
            float MinAngl = 400f;
            float MaxAngl = 0f;
            kn++;
//            float sn = Mathf.Sign(Vector3.Dot(Vector3.Cross(lastDir, NormTri), lastPnt.CutterNorm));
//if it will pnt is not crossBorder it not will influanceted
            float sn = Mathf.Sign(Vector3.Dot(Vector3.Cross(lastDir, NormTri), lastPnt.NormCutterContainer[0]));
            for (int i = 0; i < nxtPnts.Count; i++) {//191208
                Vector3 CurDirect = nxtPnts[i].V - curPnt.V;
                curAngle = VectorExtension.GetFullAngle(lastDir, CurDirect, NormTri);
                if (MaxAngl < curAngle) {
                    MaxAngl = curAngle;
                    indxPnt1 = i;
                }
                if (MinAngl > curAngle) {
                    MinAngl = curAngle;
                    indxPnt2 = i;
                }
            }
            if (sn > 0) {
                nxtPnt1 = nxtPnts[indxPnt1];
                nxtPnt2 = nxtPnts[indxPnt2];
                nxtNormSec0 = nxtPSecats[indxPnt1];
                nxtNormSec1 = nxtPSecats[indxPnt2];
                nxtPnt1.NormCutterContainer[pos] = nxtNormSec0;
                nxtPnt2.NormCutterContainer[pos] = nxtNormSec1;
            } else {
                nxtPnt1 = nxtPnts[indxPnt2];
                nxtPnt2 = nxtPnts[indxPnt1];
                nxtNormSec0 = nxtPSecats[indxPnt2];
                nxtNormSec1 = nxtPSecats[indxPnt1];
                nxtPnt1.NormCutterContainer[pos] = nxtNormSec0;
                nxtPnt2.NormCutterContainer[pos] = nxtNormSec1;
            }
            result.Add(nxtPnt1);
            curPnt.NormCutterContainer[0] = nxtNormSec0;
            ///////////////////////////////////////////!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! necessary to Check!!!!
            if (nxtPnt1.IsCrossBorderedPnt) {
                int indx = nxtPnts.IndexOf(nxtPnt2);
                nxtPnts.RemoveAt(indx);
                nxtPSecats.RemoveAt(indx);
            }else {
                if (FirstPnt) {
                    result.Add(nxtPnt2);
                    curPnt.NormCutterContainer[1 - pos] = nxtNormSec1;
                }
            }
            return result;
        }

        public static bool DelonDictNxtPnt(Point cPnt, Point lastcPnts, Poligon pol){
			//Скорее всего эту проверку на содержание ключа можно убрать!!!!!!!!!!!!!!!!
			if(pol.Nets.ContainsKey(cPnt)){
				int indxList = pol.Nets[cPnt];
				List<Point> netLinks = pol.NetLinks[indxList];
                if (netLinks.Count > 2) {
                    string s = "";                }
                List<Vector3> netSecNorms = pol.NetSecatorByPnt[indxList];
                int indxLastPnt = netLinks.IndexOf(lastcPnts);
                if (indxLastPnt > -1) {
                    netLinks.RemoveAt(indxLastPnt);
                    netSecNorms.RemoveAt(indxLastPnt);
                }
                //HashSets deleting is not necessary
                //Attantion. Not delete element of pol.NetLinks
                //It will be to impact to index of List<point> in it
                if (netLinks.Count==0){
                    pol.Nets.Remove(cPnt);
				}
                if(netLinks.Count < 1) {
                    return true;
                }
            } else {
                return true;
            }
            return false;
		}

		public static float AreaOfTriangleByV(Vector3 v1, Vector3 v2, Vector3 v3){
            return Vector3.Cross(v2 - v1, v3 - v1).magnitude;
		}

        public static List<List<Point>> GetPerimetrPnts(Poligon pol, HoleAttribute holeAttr, bool flg, int indBody) {
            List<Point> StaydTri = new List<Point>(pol.Pnts);
            int polPntsCnt = pol.Pnts.Count;
            int pntsCnt = polPntsCnt + 1;
            Point[] tri = new Point[pntsCnt];//данный массив в этой функции изменяться не должен
            pol.Pnts.CopyTo(tri);
            tri[pntsCnt - 1] = tri[0];
            Point[] triCleaner = new Point[pntsCnt];//Ввожу дополнительный массив, которым буду удалять всё.
            tri.CopyTo(triCleaner, 0);
            triCleaner = new Point[] { pol.Pnts[0], pol.Pnts[1], pol.Pnts[2], pol.Pnts[0] };
            int cnt = -1;
            Vector3[] triVec = new Vector3[pntsCnt];
            for (int i = 0; i < triVec.Length; i++) {
                triVec[i] = tri[i].V;
            }


            List<List<Point>> LPerPnts = new List<List<Point>>();//объект (треугольник) может быть разбит на несколько подобъектов (многоугольников)

            List<WrapSlice>[] LSlices = new List<WrapSlice>[polPntsCnt];
            int LSlicesAllCnt = 0;

            int tmpflgSign = flg ? 1 : -1;

            for (int i = 0; i < polPntsCnt; i++) {
                LSlices[i] = new List<WrapSlice>();
            }

            if (!flg) {
                string sss = "";
            }
            for (int i = 0; i < polPntsCnt; i++) {  //Цикл по всем точкам данного треугольника
                for (int k = holeAttr.Lst.Count - 1; k >= 0; k--) {  //Идем циклом по всем граничным "срезам" (отдельным линиям пересечения, пересекающим границу дан. треугольника)
                    int cntPnts = (holeAttr.Lst[k].Count - 1);//Индекс последней точки в текущем срезе
                    int ck = 0;
                    List<Point> curTriSidePnts = new List<Point>();//Borders Points on cur side of trinangles
                    //Check for first pnt and last pnt of curSlice
                    for (int j = 0; j < 2; j++) { //Берем начальную и конечную точки граничного "среза"
                        Point curBrdPnt = holeAttr.Lst[k][j * cntPnts];
                        for (int z = 0; z < curBrdPnt.Side.Count; z++) {
                            if (holeAttr.Lst[k][j * cntPnts].Side[z] < 0) {
                                string sss = "123";
                            }
                            if (i == curBrdPnt.Side[z]) {//Если точка "среза" на текущей стороне треугольника
                                ck++;
                                curTriSidePnts.Add(curBrdPnt);
                            }
                        }
                    }
                    //If exist pnt on cur side of triangle
                    if (ck > 0) {//Если точек  на границе > 0
                        if(cnt == -1) {
                            cnt = i;
                        }
                        Slice SL = NearPoint(triVec, curTriSidePnts, holeAttr, k, i, pol.NormSecator, tmpflgSign);
                        holeAttr.Lst.RemoveAt(k);//Можно записать в массив по индексу флаг использованности и вместо удаления, проверять по нему
                        holeAttr.NormCutts.RemoveAt(k);
                        //Можно убрать в NearPoint и сразу создавать SL с инверсией
                        LSlices[SL.i[0]].Add(new WrapSlice(0, SL, triVec[SL.i[0] + 1] - triVec[SL.i[0]], SL.Lst[1].V - SL.p[0].V));
                        SL.IndxInLst[0] = LSlices[SL.i[0]].Count - 1;
                        LSlicesAllCnt++;
//                        if (!(SL.p[0] == SL.p[1] && SL.p[0].IsCrossBorderedPnt)) {
                            int lastVIndx = SL.Lst.Count - 1;
                            LSlices[SL.i[1]].Add(new WrapSlice(1, SL, triVec[SL.i[1] + 1] - triVec[SL.i[1]], SL.Lst[lastVIndx - 1].V - SL.p[1].V));
                            SL.IndxInLst[1] = LSlices[SL.i[1]].Count - 1;
                            LSlicesAllCnt++;
//                        }
                    }
                }

            }
            for(int i = 0; i< LSlices.Length; i++) {
//                LSlices[i].Sort(   delegate (Slice a, Slice b) { return a.d1.CompareTo(b.d1); } );
                WrapSlice.Sort(LSlices[i]);
            }

            List<Point> contour = new List<Point>();// current contour of slised pilogon
            FstPnt FirstPnt;
            FirstPnt.v = new Point(flg, new Vector3(), new Vector3());
            FirstPnt.n = new Vector3();
            FirstPnt.triNumb = -1;
            int kc = 0;
            int indxFstSide = -1;
            //            while (LSlices.Count > 0 && cnt > -1) { //&& cnt<LSlices.Count){
            while (LSlicesAllCnt > 0 && cnt > -1 && kc < 1000) {
                kc++;
                WrapSlice curWrapSlice = LSlices[cnt][0];
                Slice curSlice = curWrapSlice.SL;
                int curBordPnt = curWrapSlice.BordPntIndx;

                int fp = 0;//first pnt
                int sp = 0;//secont pnt
                Vector3 v1 = tri[curSlice.i[0]].V;
                Vector3 v2 = tri[curSlice.i[0] + 1].V;
                
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //!!!!Тут сложный момент. В случае, когда точка пересечения принадлижит сразу двум полигонам одного тела, в точку может попасть нормаль
                //соседней секущей плоскости. Но поскольку для Solid объекта, всегда обе нормали соседних полигонов секущей поверхности спроецируются 
                //с одинаковым знаком на рассматриваемую сторону полигона, проблем не должно быть!!!!
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //                if ((tmpflgSign * VectorExtension.SignProjectV3toV3(v2-v1, LSlices[cnt][0].p1.CutterNorm))>0) { //Проблема в CutterNorm
                //                if ((tmpflgSign * VectorExtension.SignProjectV3toV3(v2 - v1, LSlices[cnt][0].p1.NormCutter[0])) > 0) { //Проблема в CutterNorm                                                                                               //Идём по прямой дальше
                if ((tmpflgSign * VectorExtension.SignProjectV3toV3(v2 - v1, curSlice.GetStartPositionNrmCutt())) > 0) { //Проблема в CutterNorm    
                    fp = curBordPnt;
                    if (indxFstSide == -1) {
                        //Если точка первая, добавим начальную вершину треугольника
                        contour.Add(tri[curSlice.i[fp]]);//may be tri[cnt];????

                        //Удаляем из списка удалённых вершин треугольника
                        StaydTri.Remove(tri[curSlice.i[fp]]);
                        indxFstSide = curSlice.i[fp];
                    }
                    contour.AddRange(curSlice.Lst);
                    curSlice.IsUsed = true;
                } else {
                    fp = 1 - curBordPnt;
                    //Вставляем точки сзади (до точки лежащей на другой стороне)
                    curSlice.ReverseBordsPnt();
//                    curSlice.Lst.Reverse();
                    contour.InsertRange(0, curSlice.Lst);

                    if (indxFstSide == -1) {
                        //Если точка первая, добавим начальную вершину треугольника
                        indxFstSide = curSlice.i[fp];
                    }
                    curSlice.IsUsed = true;
                    curSlice.IsClamper = true;
                }
                cnt = curSlice.i[fp];
                LSlices[cnt].RemoveAt(curSlice.IndxInLst[0]);
                OffsetBrds(LSlices[cnt], curSlice.IndxInLst[0]);

                LSlicesAllCnt -= 1;

                sp = 1 - fp;
                int secontPntIndx = sp;
                cnt = curSlice.i[1];
                int prevSide = -1;
//                bool flgPrevIsTriVert = false;
                do {
                    kc++;
                    int LSlicesCnt = LSlices[cnt].Count;// LSlices[curSlice.i[1]].Count;
                    int nxtIndx = curSlice.IndxInLst[secontPntIndx]; // offsts[curSlice.i[1]];
//                    if (!flgPrevIsTriVert) {
//                        nxtIndx++;
//                    }

                    if (LSlicesCnt > 0 && nxtIndx < LSlicesCnt) {
//                        flgPrevIsTriVert = false;
                        curWrapSlice = LSlices[cnt][nxtIndx];
                        curSlice = curWrapSlice.SL;
                        curBordPnt = curWrapSlice.BordPntIndx;
                        prevSide = cnt;
                        int lstContour = contour.Count - 1;

                        if (!curSlice.IsUsed) {
                            if (contour[lstContour].IsEqualLinks(curSlice.Lst[0]) && curSlice.Lst[0].IsCrossBorderedPnt) {
                                //if crossBorderPnt may be dublicated
                                contour.RemoveAt(lstContour);
                            }
                            contour.AddRange(curSlice.Lst);
                            curSlice.IsUsed = true;
                            secontPntIndx = 1 - curBordPnt;
                            cnt = curSlice.i[secontPntIndx];
                        } else if (curSlice.IsClamper) {//(contour[0].IsEqualLinks(curSlice.p[0])) {//curSlice.p[0] == contour [0]) {
                                                                            //Clump if go to back, pnt of some slice.
                            if (!curSlice.p[0].IsEqualLinks(curSlice.p[1])) {
                                //if not clumped slice with crossBorderPnt
                                contour.Add(curSlice.p[0]);
                            }
                            
                        }else if(nxtIndx + 1 == LSlicesCnt){
                            cnt = curSlice.i[curBordPnt] + 1;
                            if (tri.Length <= cnt + 1) {
                                cnt = 0;
                            }
                            contour.Add(tri[cnt]);
                            StaydTri.Remove(tri[cnt]);
                        }
                    
                        LSlices[prevSide].RemoveAt(nxtIndx);
                        OffsetBrds(LSlices[prevSide], nxtIndx);
                        LSlicesAllCnt -= 1;
                    } else {
//                        flgPrevIsTriVert = true;
                        cnt++;
                        if (tri.Length <= cnt + 1) {
                            cnt = 0;
                        }
                        contour.Add(tri[cnt]);
                        StaydTri.Remove(tri[cnt]);
                    }

                } while (!contour[0].IsEqualLinks(contour.Last()) && kc < 1000);

                bool flgCntCatched = false;
                if (LSlicesAllCnt > 0) {
                    for (int i = 0; i < LSlices.Length; i++) {
                        List<WrapSlice> curWSLs = LSlices[i];
                        int cntSLByCurSide = curWSLs.Count;
                        if (cntSLByCurSide > 0) {
                            for (int j = cntSLByCurSide - 1; j >-1 ; j--) {
                                //if on side stay on only used SL
                                if (curWSLs[j].SL.IsUsed) {
                                    LSlices[i].RemoveAt(j);
                                    OffsetBrds(curWSLs, j);
                                    LSlicesAllCnt--;
                                }
                            }
                            if (!flgCntCatched) {
                                if (curWSLs.Count > 0) {
                                    cnt = i;
                                    flgCntCatched = true;
                                }
                            }
                        }
                    }
                }
                contour[0] = contour.Last();
                NxtList(LPerPnts, ref contour);
                indxFstSide = -1;
                continue;
            }
            for (int i = 0; i < StaydTri.Count; i++) {
                FillUpVTries(StaydTri[i], triCleaner, indBody);
            }
            DelCurTriangle(pol, indBody);
            return LPerPnts;
        }


        public static void OffsetBrds(List<WrapSlice> slices, int from) {
            for(int i = from; i < slices.Count; i++) {
                Slice curSlice = slices[i].SL;
                int curBordPntIndx = slices[i].BordPntIndx;
                curSlice.IndxInLst[curBordPntIndx]--;
            }
        }

        private static bool IsNoGoodWise(List<Point> LstSlice, Vector3 triDir) {
            int last = LstSlice.Count - 1;
            float angl1 = VectorExtension.UltAnglByVect(triDir, LstSlice[1].V - LstSlice[0].V);
            float angl2 = VectorExtension.UltAnglByVect(triDir, LstSlice[last-1].V - LstSlice[last].V);
            return angl1 > angl2;
        }
/*
        private static bool GetGoodWise(List<Point> LstSlice, Vector3 triDir, Vector3 norm, int flgInt) {
            Vector3 slDir = LstSlice[1].V - LstSlice[0].V;
            if ((flgInt * Vector3.Dot(Vector3.Cross(slDir, triDir), norm)) < 0) {
                return false;
            }
            return true;
        }
*/
		public static void DelCurTriangle(Poligon pol, int indBody){
			for(int i = 0; i<pol.Pnts.Count;i++){
				Point tmpMyV = new Point (pol.Pnts[i].V);
                PoligonsByPoint[indBody][tmpMyV].Remove(pol);
			}
		}
		//Во всех оставшихся вершинах удаляю ссылку на удалённую вершину треугольника
		public static void FillUpVTries(Point pnt, Point[] triVss, int indBody){
			List<Poligon> trisesByPnt = PoligonsByPoint[indBody][pnt];
			for(int x = 0; x<trisesByPnt.Count;x++){
				for(int y=0; y<triVss.Length;y++){
					trisesByPnt[x].LinkPnts.Remove(triVss[y]);
				}
			}
            LVects[indBody].Add(pnt);
        }

        public static void NxtList(List<List<Point>> listlstPnts, ref List<Point> curContour) {
            if (curContour.Count > 0) {
                listlstPnts.Add(curContour);
                curContour = new List<Point>();
            }
        }

        public static Slice NearPoint(Vector3[] vTris, List<Point> PntsOnCurSide, HoleAttribute BordHole, int k, int curSideTri, Vector3 nrmSec, int flgSign) {

            bool flgRevesed = false;
            List<float> LDistance = new List<float>();
            int j1 = curSideTri;
            int j2 = curSideTri;
            for (int i = 0; i < PntsOnCurSide.Count; i++) {
                float fDict = (PntsOnCurSide[i].V - vTris[curSideTri]).sqrMagnitude;
                LDistance.Add(fDict);
            }
            //if slise on one side of tri, sort
            if (LDistance.Count == 2) {
                bool flgRev = false;
                if (PntsOnCurSide[0].IsCrossBorderedPnt) {
//                    if (GetGoodWise(BordHole.Lst[k], vTris[curSideTri + 1] - vTris[curSideTri], nrmSec, flgSign)) {
                    if(IsNoGoodWise(BordHole.Lst[k], vTris[curSideTri + 1] - vTris[curSideTri])) { 
                        flgRev = true;
                        if (PntsOnCurSide[0].IsEqualLinks(PntsOnCurSide[1])){
                            flgRevesed = true;
                        }
                    }
                } else {
                    if (LDistance[0] > LDistance[1]) {
                        flgRev = true;
                    }
                }
                if (flgRev) {
                    float swiper = LDistance[1];
                    Point swiperP = PntsOnCurSide[1];
                    LDistance[1] = LDistance[0];
                    LDistance[0] = swiper;
                    PntsOnCurSide[1] = PntsOnCurSide[0];
                    PntsOnCurSide[0] = swiperP;
                }
            }
            //Создаём направление
            List<Point> curBordHolePnts = BordHole.Lst[k];
            int secondPnt = curBordHolePnts.Count - 1;
            if (PntsOnCurSide[0] != curBordHolePnts[0]) {
                flgRevesed = true;
                secondPnt = 0;
            }
            if (PntsOnCurSide.Count == 1) {//Если на текущей стороне треугольника только одна точка
                Point tmpSecPnt = curBordHolePnts[secondPnt];//another end pnt (border pnt)
                for (int i = 0; i < vTris.Length - 1; i++) {    //Цикл по всем точкам данного треугольника
                    for (int z = 0; z < tmpSecPnt.Side.Count; z++) {
                        if (i == tmpSecPnt.Side[z]) {//Если точка "среза" на текущей стороне треугольника
                            LDistance.Add((vTris[i] - tmpSecPnt.V).sqrMagnitude);//Добавляем в Slice расстояние 2й точки среза до соотвествующей точки соотвествующей стороны треугольника
                            j2 = i;
                        } else if (tmpSecPnt.Side[z] < 0 || tmpSecPnt.Side[z] > vTris.Length - 1) {
                            Debug.Log("Do not catch nearPnt");
                        }
                    }
                }
            }
            Slice tmpSl = null;
            try {
                //                tmpSl = new Slice(dDistances[LDistance[0]], dDistances[LDistance[1]], LDistance[0], LDistance[1], BordHole, k, j1, j2);
                tmpSl = new Slice(flgRevesed, LDistance[0], LDistance[1], BordHole, k, j1, j2);
            } catch (System.ArgumentOutOfRangeException e) {

                Debug.Log(e.ToString());
            }
            return tmpSl;   
        }


		public static Vector3[] TrmWrdPntToMshPnt (Vector3[] v, Transform goTr){//, float f){
			//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
				v[i] = goTr.InverseTransformPoint(v[i]);
			}
			return v;
		}

		public static Vector3[] TrmMshPntToWrdPnt (Vector3[] v, Transform goTr) {//, float f){
			//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
				v[i] = goTr.TransformPoint(v[i]);
			}
			return v;
		}

        public static Vector3[] TrmWrdPntToMshPnt2(Vector3[] v, GameObject go) {
            float f = 10f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
            for (int i = 0; i < v.Length; i++) {
                if (f > -1) {
                    v[i] = new Vector3(v[i].x / f, v[i].y / f, v[i].z / f);
                }
                v[i] = go.transform.InverseTransformPoint(v[i]);
            }
            return v;
        }

        public static Vector3[] TrmMshPntToWrdPnt2(Vector3[] v, GameObject go) {
            float f = 10f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
            for (int i = 0; i < v.Length; i++) {
                v[i] = go.transform.TransformPoint(v[i]);
                if (f > -1) {
                    v[i] = v[i] * f;
                }
            }
            return v;
        }

        public static List<List<Point>> GetArrWithHole(HoleAttribute holeAttr, Poligon pol, bool flg, int flgMode, int indBody) {
            if (flg) {
                DelCurTriangle(pol, indBody); //If Slised body
            } else {
                LVects[indBody].Add(pol.Pnts[0]); // if Sliser body
            }
            List<List<Point>> holes = holeAttr.Lst;
            List<List<Point>> Result = null;
            int preLeft = 0;
            int afterLeft = 0;
            int lastInd = 0;
            int kCntHole = 0;
            int holesCnt = holes.Count;

            ///
            Result = new List<List<Point>>();
            List<List<Point>> SubPerimeters = pol.nTrPnts;
            List<List<Vector3[]>> normCutts = holeAttr.NormCutts;
            List<List<Point>> holesChecker = new List<List<Point>>(holes);
            List<List<int>> indexBelongsHole = new List<List<int>>();

            List<bool> isDirect = new List<bool>();
            List<bool> isSubHole = new List<bool>();
            isDirect.Capacity = holes.Count;
            isSubHole.Capacity = holes.Count;

            int flgInt = flg ? 1 : -1;
            int left = 0;
            Matrix4x4 MT = GetM4MRotate(ref pol.PrjNorm);
            for (int i=0; i< holesCnt; i++) {
                List<Point> curHole = holes[i];
                List<Vector3[]> curCutts = normCutts[i];
                int lastBegin = curHole.Count - 1;
                curHole.RemoveAt(lastBegin);//Нужно будет убрать замыкания в формировании Holes
                left = 0;
                GetOrthoProjection(MT, ref curHole, ref left);
                lastInd = curHole.Count;
                if (left == 0) {
                    preLeft = lastInd - 1;
                    afterLeft = left + 1;

                } else {
                    preLeft = left - 1;
                    afterLeft = left + 1;
                }

                curHole.Add(holes[i][0]);
                //Catch sign of hole. it will be say about wise direction
                Vector2 v1 = curHole[preLeft].ProjV - curHole[left].ProjV;
                Vector2 v2 = curHole[afterLeft].ProjV - curHole[left].ProjV;

                Vector3 v_1 = curHole[afterLeft].V - curHole[left].V;
                Vector3 v_2 = Vector3.Cross(v_1, pol.Norm);

                if ((double)Vector3.Dot(Vector3.Cross(v1, v2), pol.PrjNorm) > 0) {
                    Vector3 vSecat = curCutts[left][1];
                    isDirect.Add(true);
                    isSubHole.Add(flgInt * (Vector3.Dot(v_2, vSecat)) < 0);
                } else {
                    Vector3 vSecat = curCutts[left][1];
                    isDirect.Add(false);
                    isSubHole.Add(flgInt * (Vector3.Dot(v_2, vSecat)) > 0);
                }
            }

            //necessary to delete this bad code. To create hole container with property "isSubHole"
            List<bool> isSubHoleChecker = new List<bool>(isSubHole);
            if (flg) {
                //only for flg = true
                int cntCurRayCross = 0;
                for (int i = 0; i < SubPerimeters.Count; i++) {
                    List<int> curHolesInds = new List<int>();
                    List<Point> curShape = SubPerimeters[i];
                    if (curShape.Count > 0) {
                        left = 0;
                        GetOrthoProjection(MT, ref curShape, ref left);
                        lastInd = curShape.Count;
                        if (left == 0) {
                            preLeft = lastInd - 1;
                            afterLeft = left + 1;
                        } else {
                            preLeft = left - 1;
                            afterLeft = left + 1;
                        }
                        curShape.Add(curShape[0]);
                        Vector2 v1 = curShape[preLeft].ProjV - curShape[left].ProjV;
                        Vector2 v2 = curShape[afterLeft].ProjV - curShape[left].ProjV;
                        if (Vector3.Dot(Vector3.Cross(v1, v2), pol.PrjNorm) > 0) {
                            curShape.Reverse();
                        }
                        //Set attachment TrisShape - HoleShape
                        bool curFlg = false;
                        //enouth to chech one of pnt of hole
                        for (int ii = holesChecker.Count - 1; ii > -1; ii--) {
                            cntCurRayCross = 0;
                            for (int j = 0; j < curShape.Count - 1; j++) {
                                curFlg = IsInnerPntCurShape(holesChecker[ii][0].ProjV, curShape[j].ProjV, curShape[j + 1].ProjV);
                                if (curFlg) {
                                    cntCurRayCross++;
                                }
                            }
                            if (cntCurRayCross % 2 > 0) {
                                curHolesInds.Add(ii);
                                holesChecker.RemoveAt(ii);
                                isSubHoleChecker.RemoveAt(ii);
                            }
                        }
                        indexBelongsHole.Add(curHolesInds);
                    }
                }
                //Cicle by contours of curPoligon
                do {
                    kCntHole++;
                    SubPerimeters = GetSubPerimetersByHole(SubPerimeters, holes, pol, isDirect, isSubHole, kCntHole, ref indexBelongsHole, ref Result);
                } while (indexBelongsHole.Count > 0 && kCntHole < 1000000);//temporary
            } else {
                //for flgSide = false necessury control direct also as flgSide = true
                int cntCurRayCross = 0;
                for (int i = 0; i < SubPerimeters.Count; i++) {
                    List<int> curHolesInds = new List<int>();
                    List<Point> curShape = SubPerimeters[i];
                    if (curShape.Count > 0) {
                        left = 0;
                        GetOrthoProjection(MT, ref curShape, ref left);
                        lastInd = curShape.Count;
                        if (left == 0) {
                            preLeft = lastInd - 1;
                            afterLeft = left + 1;
                        } else {
                            preLeft = left - 1;
                            afterLeft = left + 1;
                        }
                        curShape.Add(curShape[0]);
                        Vector2 v1 = curShape[preLeft].ProjV - curShape[left].ProjV;
                        Vector2 v2 = curShape[afterLeft].ProjV - curShape[left].ProjV;
                        if (Vector3.Dot(Vector3.Cross(v1, v2), pol.PrjNorm) * flgInt > 0) {
                            curShape.Reverse();
                        }

                        //Можно проверить, если есть isSubHole, то его можно добавить в SubPerimeters, и дальше по новой сделать, как отдельный сабполигон!!
/*
                        //Set attachment TrisShape - HoleShape
                        bool curFlg = false;
                        //enouth to chech one of pnt of hole
                        for (int ii = holesChecker.Count - 1; ii > -1; ii--) {
                            cntCurRayCross = 0;
                            for (int j = 0; j < curShape.Count - 1; j++) {
                                curFlg = IsInnerPntCurShape(holesChecker[ii][0].ProjV, curShape[j].ProjV, curShape[j + 1].ProjV);
                                if (curFlg) {
                                    cntCurRayCross++;
                                }
                            }
                            if (cntCurRayCross % 2 > 0) {
                                curHolesInds.Add(ii);
                                holesChecker.RemoveAt(ii);
                                isSubHoleChecker.RemoveAt(ii);
                            }
                        }
                        indexBelongsHole.Add(curHolesInds);

                    */
                    }
                }
/*
                //Cicle by contours of curPoligon
                do {
                    kCntHole++;
                    SubPerimeters = GetSubPerimetersByHole(SubPerimeters, holes, pol, isDirect, isSubHole, kCntHole, ref indexBelongsHole, ref Result);
                } while (indexBelongsHole.Count > 0 && kCntHole < 1000000);//temporary
                */
            }

            //Adding all isSubHole holes , that not exist no one shape
            //Work for flg = true and flg = false
            for (int i = 0; i < holesChecker.Count; i++) {
                if (!flg) {
                    if (!isDirect[i]) {
                        //for Hole by secutor
                        holesChecker[i].Reverse();
                    }
                }
                if (isSubHoleChecker[i]) {
//                if ((flg && isSubHoleChecker[i])||(!flg && !isSubHoleChecker[i])) {
                    Result.Add(holesChecker[i]);
                }
            }
            if (!flg) {
                for (int i = 0; i < SubPerimeters.Count; i++) {
                    Result.Add(SubPerimeters[i]);
                }
            }

            return Result;
        }

        public static Vector3 ProjectPntToLine(Vector3 A, Vector3 B, Vector3 P) {
            Vector3 AB = (B - A);
            return A + (AB).normalized * Vector3.Dot(P - A, AB) / AB.magnitude;
        }

        public static List<List<Point>> GetSubPerimetersByHole(List<List<Point>> Shapes, List<List<Point>> HolesPnts,
            Poligon pol,
            List<bool> isDirect, List<bool> isSubHole, int cntCutHole, //List<int> shapeLeftIndx,
            ref List<List<int>> indsHoles, ref List<List<Point>> outResult) {

            Vector3 norm = pol.PrjNorm;
            float minDist = 0f;
            float curDist = 0f;
            int indxShapePnt = 0;
            int indxHolePnt = 0;
            bool flgCatchOneHole = false;
//            for(int i = Shapes.Count-1; i > -1; i--) {
            for (int i = indsHoles.Count - 1; i > -1; i--) {
                int cnt = indsHoles[i].Count;
                if (cnt > 0) {
                    List<Point> curShape = Shapes[i];

                    //cur Hole
                    for (int j = cnt-1; j >-1; j--) {
                        flgCatchOneHole = false;
                        int curIndsHole = indsHoles[i][j];
                        List<Point> curHole = HolesPnts[curIndsHole];
                        if (!isSubHole[curIndsHole]) {
                            //Pnts of curHole
                            for (int z = 0; z < curHole.Count - 1; z++) {
                                //cicle by cur shape
                                for (int ii = 0; ii < curShape.Count - 1; ii++) {

                                    //Sign of position
                                    Vector2 vCur = curShape[ii+1].ProjV - curShape[ii].ProjV;
                                    Vector2 toHol = curHole[z].ProjV - curShape[ii].ProjV;
                                    Vector3 tmp = Vector3.Cross(vCur, toHol);
                                    float fs = tmp.z;
                                    fs = Mathf.Sign(fs);//System.Math.Sign(fs);
                                    if ((fs * Mathf.Sign(norm.z)) > 0f) {

                                        curDist = (toHol).sqrMagnitude;
                                        if (curDist > 0f) {
                                            if (minDist == 0f) {
                                                minDist = curDist;
                                                indxShapePnt = ii;
                                            } else if (minDist >= curDist) {
                                                minDist = curDist;
                                                indxShapePnt = ii;
                                            }
                                            flgCatchOneHole = true;
                                        }
                                    }
                                }
                                if (flgCatchOneHole) {
                                    //nearest vertex of cur hole
                                    for (int zz = z + 1; zz < curHole.Count - 1; zz++) {
                                        Vector2 toHol = curHole[zz].ProjV - curShape[indxShapePnt].ProjV;
                                        curDist = (toHol).sqrMagnitude;
                                        if (minDist >= curDist) {
                                            minDist = curDist;
                                            indxHolePnt = zz;
                                        }
                                    }

                                    //Check to crossing cutted triangle with another holes:
                                    for (int zz = j-1; zz > -1; zz--) {
                                        int otherIndsHole = indsHoles[i][zz];
                                        List<Point> otherHole = HolesPnts[otherIndsHole];
                                        for (int w = 0; w < otherHole.Count - 1; w++) {
                                            if (VectorExtension.IsCrossInsidedVect(otherHole[w].ProjV, otherHole[w+1].ProjV, curShape[indxShapePnt].ProjV, curHole[indxHolePnt].ProjV)) {
                                                goto nxtHole;
                                            }
                                        }
                                    }
                                    //Periodical change direct for holes
                                    bool flgDirect = isDirect[curIndsHole];
                                    Shapes[i] = GetSlise(curHole, i, j, indxHolePnt, indxShapePnt, flgDirect, ref curShape, ref indsHoles);
                                    if (indsHoles.Count == i) {
                                        outResult.Add(Shapes[i]);
                                    }
                                    return Shapes;
                                }
                                break;
                            }
                        }else {
                            outResult.Add(Shapes[i]);
                            outResult.Add(curHole);
                            indsHoles.RemoveAt(i);
                        }

                    //Go to nxt Hole
                    nxtHole:;

                    }
                } else {
                    outResult.Add(Shapes[i]);
                    indsHoles.RemoveAt(i);
                }
            }
            return Shapes;
        }

        private static bool IsInsideTri(Vector2 curV, Vector2[] tri) {
            //For tris enaugh check is crossed with one even if
            bool curFlg = false;
//            int cntCurRayCross = 0;
            for(int i= 0; i< tri.Length-1; i++) {
                curFlg = IsInnerPntCurShape(curV, tri[i], tri[i+1]);
                if (curFlg) {
                    return true;
                }
            }
            return false;
        }
        
        private static List<Point> GetSlise(List<Point> curHole, int indShape, int curIndHoleInShape, int indxHolePnt, int indxSidePer, bool isDirect,
            ref List<Point> curShape, ref List<List<int>> indsHoles) {
            //Режем вдоль линии
            List<Point> newShape = new List<Point>();
            List<Point> HoleDirectRange = new List<Point>();
            HoleDirectRange.Capacity = curHole.Count + 1;
            newShape.Capacity = curShape.Count + curHole.Capacity;
            if (!isDirect) {
                for (int i = indxHolePnt; i > -1; i--) {
                    HoleDirectRange.Add(curHole[i]);
                }
                for (int i = curHole.Count - 2; i > indxHolePnt - 1; i--) {
                    HoleDirectRange.Add(curHole[i]);
                }
            } else {
                for (int i = indxHolePnt; i < curHole.Count-1; i++) {
                    HoleDirectRange.Add(curHole[i]);
                }
                for (int i = 0; i < indxHolePnt + 1; i++) {
                    HoleDirectRange.Add(curHole[i]);
                }
            }
            newShape.Add(HoleDirectRange[0]);
            for (int i = indxSidePer; i < curShape.Count - 1; i++) {
                newShape.Add(curShape[i]);
            }
            for (int i = 0; i < indxSidePer+1; i++) {
                newShape.Add(curShape[i]);
            }
            newShape.AddRange(HoleDirectRange);
            indsHoles[indShape].RemoveAt(curIndHoleInShape);
            if (indsHoles[indShape].Count == 0) {
                indsHoles.RemoveAt(indShape);
            }
            curShape = newShape;
            return newShape;
        }


        //Necessary to offset (it may be via matrix MT 4 dimentions (it will be more preffered) or subseting of all coordinates) of any pnt of poligon. Then will be projection in start coordinates axises!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private static Matrix4x4 GetM4MRotate(ref Vector3 nrm) {

            Matrix4x4 MT;//reverse order (first rotate by oY, then oX)
            Vector3 nrmN = nrm.normalized;

            float CosY = 0f;
            float SinY = 0f;
            float SinX = 0f;
            float CosX = 0f;

            float r = Mathf.Pow(nrmN.z * nrmN.z + nrmN.x * nrmN.x, 0.5f);
            if (r != 0f) {
                CosY = nrmN.z / r;
                SinY = -nrmN.x / r;

                CosX = r;
                SinX = nrmN.y;
            }else {
                //Rotate on axis X by 90 degrees
                SinX = 1f;
                CosX = 0f;

                SinY = 0f;
                CosY = 1f;
            }

            Matrix4x4 MTx = new Matrix4x4();
            MTx[0, 0] = 1;
            MTx[0, 1] = 0;
            MTx[0, 2] = 0;
            MTx[1, 0] = 0;
            MTx[1, 1] = CosX;
            MTx[1, 2] = -SinX;
            MTx[2, 0] = 0;
            MTx[2, 1] = SinX;
            MTx[2, 2] = CosX;
            MTx[3, 3] = 1;

            Matrix4x4 MTy = new Matrix4x4();
            MTy[0, 0] = CosY;
            MTy[0, 1] = 0;
            MTy[0, 2] = SinY;
            MTy[1, 0] = 0;
            MTy[1, 1] = 1;
            MTy[1, 2] = 0;
            MTy[2, 0] = -SinY;
            MTy[2, 1] = 0;
            MTy[2, 2] = CosY;
            MTy[3, 3] = 0;

            /*
            Matrix4x4 MTz = new Matrix4x4();
            MTz[0, 0] = CosZ;
            MTz[0, 1] = -SinZ;
            MTz[0, 2] = 0;
            MTz[1, 0] = -MTz[0, 1];
            MTz[1, 1] = MTz[0, 0];
            MTz[1, 2] = 0;
            MTz[2, 0] = 0;
            MTz[2, 1] = 0;
            MTz[2, 2] = 1;
            MTz[3, 3] = 1;
            */
            MT = MTx * MTy;//reverse order (first rotate by oY, then oX)
            nrm = MT * nrm;
            return MT;
        }

        private static void GetOrthoProjection(Matrix4x4 MT, ref List<Point> v1, ref int LeftIndx) {

            for (int i = 0; i < v1.Count; i++) {
                v1[i].ProjV = MT * v1[i].V;
                LeftIndx = GetLeftedPoint(LeftIndx, i, v1);
            }
        }
        private static List<Vector2> GetOrthoProjectionV2(Matrix4x4 MT, ref List<Point> v1, ref int LeftIndx) {
            List<Vector2> result = new List<Vector2>();
            result.Capacity = v1.Count;
            for (int i = 0; i < v1.Count; i++) {
                v1[i].ProjV = MT * v1[i].V;
                LeftIndx = GetLeftedPoint(LeftIndx, i, v1);
                result.Add(v1[i].ProjV);
            }
            return result;
        }

        private static int GetLeftedPoint(int prevInd, int curInd, List<Point> v) {
            if (curInd == 0) {
                prevInd = 0;
            } else {
                float delta = v[prevInd].ProjV.y - v[curInd].ProjV.y;
                if (delta > VectorExtension.rFactor) {
                    prevInd = curInd;
                } else if (delta <= VectorExtension.rFactor && delta >= -VectorExtension.rFactor) {
                    if (v[prevInd].ProjV.x > v[curInd].ProjV.x) {
                        prevInd = curInd;
                    }
                }
            }
            return prevInd;
        }


        /*
         * Поскольку внутренние отверстия уже точно не пересекают Фигуру, достаточно проверить одну любую точку отверстия на вхождение в Фигуру
         *
         */
        private static bool IsInnerPntCurShape(Vector2 holePnt, Vector2 curShapePnt, Vector2 nxtShapePnt) {
            //horisontal ray
            //if HolePnt.y and prevPnt.y and curPnt.y are identical, this occasion is not considered
            float delta1 = Mathf.Abs(holePnt.y - curShapePnt.y);
            float delta2 = Mathf.Abs(curShapePnt.y - nxtShapePnt.y);
            bool flg = false;
            //if cur border ofshape is horizontal this is not considered
            if(delta2 > VectorExtension.rFactor) {
                //if pnt shape is not extremum
                if (delta1 > VectorExtension.rFactor) {
                    flg = VectorExtension.IsCrossHorizontalRay(curShapePnt - holePnt, nxtShapePnt - holePnt);
                }
            }
            return flg;
        }

        public static bool SignSide(Vector3 v1, Vector3 v2, Vector3 norm) {
            return (Mathf.Sign(Vector3.Dot(Vector3.Cross(v1, v2), norm))>0);
        }

        public static Mesh PolCreate(Vector3[] V0, Vector3[] N0, Vector2[] UV0, int[] TRI, Vector4[] TNGs){
			Mesh mesh = new Mesh();
			mesh.vertices = V0;
			mesh.triangles = TRI;
			mesh.normals = N0;
			mesh.uv = UV0;
			mesh.tangents = TNGs;
//			mesh.RecalculateTangents (); //Иначе некорректно свет ложится. Видимо неправильный алгоритм расчёта тангенсов
			return mesh;
		}

        private static Mesh CopyMshAttribute(Mesh msh1, Mesh msh2) {
            msh2.Clear();
//            msh2.MarkDynamic();//for using dynimic buffers
            msh2.vertices = msh1.vertices;
            msh2.subMeshCount = msh1.subMeshCount;
            for (int i = 0; i < msh1.subMeshCount; i++) {
                msh2.SetIndices(msh1.GetIndices(i), msh1.GetTopology(i), i);
            }
            msh2.normals = msh1.normals;
            msh2.uv = msh1.uv;
            msh2.uv2 = msh1.uv2;
            msh2.uv3 = msh1.uv3;
            msh2.uv4 = msh1.uv4;
            msh2.tangents = msh1.tangents;
            msh2.colors = msh1.colors;
            msh2.colors32 = msh1.colors32;
            msh2.bounds = msh1.bounds;
            msh2.boneWeights = msh1.boneWeights;
            msh2.bindposes = msh1.bindposes;
            return msh2;
        }


        //!!!!!!!!______________________________другие классы_________________________!!!!!!!!!!!!!!!!!!!!!

        #region BodyDefinitionClass
        public class BodyRawAttributes {
            public List<int[]> tris = null;
            public Vector3[] v = null;
            public Vector3[] n = null;
            public Vector2[] uv = null;
            public Vector4[] tng = null;
            public int subMshCnt;

            public BodyRawAttributes(int pntsCnt, int SetSubMshCnt) {
                v = new Vector3[pntsCnt];
                n = new Vector3[pntsCnt];
                uv = new Vector2[pntsCnt];
                tng = new Vector4[pntsCnt];
                tris = new List<int[]>();
            }
            public Mesh MeshRefresh(Mesh msh) {
                msh.Clear();
                subMshCnt = this.tris.Count;
                msh.subMeshCount = subMshCnt;
                msh.vertices = this.v;
                for (int i = 0; i < subMshCnt; i++) {
                    msh.SetIndices(this.tris[i], MeshTopology.Triangles, i);
                }
                msh.normals = this.n;
                msh.uv = this.uv;
                msh.tangents = this.tng;
                return msh;
            }
        }

        public class Body {
            Point[] points = null;
            List<Border> borders = null;
            Poligon[] poligons = null;

            public Point[] Points {
                get { return points; }
            }
            public List<Border> Borders {
                get { return borders; }
            }
            public Poligon[] Poligons{
                get { return poligons; }
            }


            public Body(Point[] pnts, List<Border> brds, Poligon[] pols) {
                points = pnts;
                borders = brds;
                poligons = pols;

            }
        }

        public class BodyCreator {
            //I do not connecting to PoligonAttribute. Cause in future I can optimazed mesh correspond by position and Normals of pnts in the one subMsh!!!!
            private int subMshCnt = -1;
            private List<int>[] tris = null;
            private List<Point>[] pnts = null;
            private bool OptimazedCrossPnt = false;
            private Dictionary<DirectedPoint, int>[] OptimalContol = null;

            //links to some crossed pnts
            private LinksCreatorPar links = null;

            private int[] index = null;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="sbMshCnt"></param>
            /// <param name="flg - for crossPnt count optimazing (for each submesh will be optimazed crosspnts by position and normal to vertex)"></param>
            public BodyCreator(int sbMshCnt, bool flg=false, LinksCreatorPar oldlinks = null) {
                subMshCnt = sbMshCnt;
                OptimazedCrossPnt = flg;
                tris = new List<int>[subMshCnt];
                pnts = new List<Point>[subMshCnt];
                index = new int[subMshCnt];
                if (OptimazedCrossPnt) {
                    OptimalContol = new Dictionary<DirectedPoint, int>[subMshCnt];
                }
                for (int i = 0; i < subMshCnt; i++) {
                    tris[i] = new List<int>();
                    pnts[i] = new List<Point>();
                    if (OptimazedCrossPnt) {
                        OptimalContol[i] = new Dictionary<DirectedPoint, int>();
                    }
                }
                if (OldTrisLinksGetMode) {
                    links = oldlinks;
                }
            }

            public void AddNewPoligonAttributes(int subMsh, List<int> tri, List<Point> nPnts) {
                pnts[subMsh].Capacity += nPnts.Count;
                tris[subMsh].Capacity += tri.Count;
                Point curPnt = null;
                int curIndx = -1;
                for (int i = 0; i < tri.Count; i++) {
                    curPnt = nPnts[tri[i]];
                    if (!curPnt.IsAddedToMshBody) {
                        if (OptimazedCrossPnt) {
                            Dictionary<DirectedPoint, int> curOptControl = OptimalContol[subMsh];
                            DirectedPoint dirPnt = new DirectedPoint(curPnt);
                            if (curOptControl.ContainsKey(dirPnt)) {
                                curIndx = curOptControl[dirPnt];
                            } else {
                                curPnt.CurIndx = index[subMsh];
                                curPnt.IsAddedToMshBody = true;
                                pnts[subMsh].Add(curPnt);
                                curIndx = index[subMsh];
                                curOptControl.Add(dirPnt, curIndx);
                                index[subMsh]++;
                            }
                        } else {
                            curPnt.CurIndx = index[subMsh];
                            curPnt.IsAddedToMshBody = true;
                            pnts[subMsh].Add(curPnt);
                            curIndx = index[subMsh];
                            index[subMsh]++;
                        }
                    } else {
                        curIndx = curPnt.CurIndx;
                    }
                    tris[subMsh].Add(curIndx);
                }
            }

            public BodyRawAttributes BodyGenerate() {
                List<int>[] nwAperture = new List<int>[subMshCnt];//for update links

                OptimalContol = null;
                int vertCnt = 0;
                int vertOffst = 0;
                int trisOffst = 0;
                for (int i = 0; i < subMshCnt; i++) {
                    vertCnt += pnts[i].Count;
                }

                BodyRawAttributes ba = new BodyRawAttributes(vertCnt, subMshCnt);
                for (int i = 0; i < subMshCnt; i++) {
                    nwAperture[i] = new List<int>();
                    MergeMsh(i, ref trisOffst, ref vertOffst, ref ba, ref links, ref nwAperture[i]);
                }
                if (OldTrisLinksGetMode) {
                    links.OldRefreshedPnts.Add(nwAperture);
                }
                return ba;
            }

            private void MergeMsh(int subMsh, ref int trisOffst, ref int vertOffst, ref BodyRawAttributes ba, ref LinksCreatorPar links, ref List<int> newLink) {
                int[] ttr = new int[3];//for one triangle
                bool[] flgCrssd = new bool[3];//for one triangle
                List<Point> mshPnts = pnts[subMsh];
                for (int i = 0; i < mshPnts.Count; i++) {
                    Point curPnt = mshPnts[i];
                    int idx = vertOffst + i;
                    ba.v[idx] = curPnt.V;
                    ba.n[idx] = curPnt.N;
                    ba.uv[idx] = curPnt.UV;
                    ba.tng[idx] = curPnt.Tng;
                    if (OldTrisLinksGetMode) {
                        if (curPnt.IsAroundSidePnt) {
                            int[] ips = curPnt.OldIndxs;
                            links.externPnts[ips[0]][ips[1]] = idx;
                        } else if (curPnt.IsUsedByTranslate) {
                            int[] ips = curPnt.OldIndxs;
                            links.OldRefreshedPnts[ips[0]][subMsh][ips[1]] = idx;
                        } else if (curPnt.IsCrossing) {
                            newLink.Add(idx);
                        }
                    }
                }

                vertOffst += mshPnts.Count;

                int[] trises = new int[tris[subMsh].Count];
                for (int i = 0; i < tris[subMsh].Count; i++) {
                    trises[i] = tris[subMsh][i] + trisOffst;
                }
                ba.tris.Add(trises);
                trisOffst += mshPnts.Count;

            }
            private void SetBorderNets(int iii, int[] ttr, bool[] flgCrssd, List<Point> sPnts) {
                if (flgCrssd[iii]) {
                    if (flgCrssd[1]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[1]]);
                    }
                    if (flgCrssd[2]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[2]]);
                    }
                }
                iii = 1;
                if (flgCrssd[iii]) {
                    if (flgCrssd[0]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[0]]);
                    }
                    if (flgCrssd[2]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[2]]);
                    }
                }
                iii = 2;
                if (flgCrssd[iii]) {
                    if (flgCrssd[0]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[0]]);
                    }
                    if (flgCrssd[1]) {
                        sPnts[ttr[iii]].AddNewLinkToEdge(sPnts[ttr[1]]);
                    }
                }
            }
        }


        #endregion 

        #region PoligonDefinitionClass
        public class Poligon: System.IEquatable<Poligon>{
            private static List<Point>[] crPnts = new List<Point>[] { new List<Point>(), new List<Point>() };//it more faster then new allocation
            private static List<Vector3>[] crNormSecats = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };//it more faster then new allocation
            //            private int[] crsPntsControl;//For crossing one triangle with another max 2 cross pnts
            public bool ContainsBorderPnt = false;
            public Dictionary<Point, int> Nets;
            //NetKeys, NetLinks, controlNets are have equal elements quantity = Nets.count
            public List<Point> NetKeys;
            public List<List<Point>> NetLinks;
            public List<List<Vector3>> NetSecatorByPnt; //this is direction normal to secator plane in cur pnt
			public List<HashSet<Point>> ControlNets;

            private bool isChangedFace;

            private int indSubMsh = -1; //index of submesh
			private int hashCode=-1;
			private List<Point> pnts;
            private int[] borders; //indexes in general list of body
            public int[] neighborsIndx;
            public Dictionary<int, VectBox> crssPntByBorder = null; //crossPnts with borders of other body poligons
			public List<Point> LinkPnts;//for deleting links to another
			private Vector3 norm;//this normal direction will be dipend to type plane
            private Vector3 normSecator;//normal of plane. It is indipendent to of type plane (Slising or Sliser)
            private bool isCrossed = false;
            private bool isSliser = false;

            private Dictionary<Point, Point> crossPnts = null;

            public Holes bHoles;
            public List<List<Point>> nTrPnts = null;//Link to ordered pnts - mapping slised contour area of triang

            //used in NxtvBads (IsFiltred)
            private bool isFiltredInPntToTrisDict = false;//this for optimization in triangle cutting algoritm (cutting of redundant triangles)

            public Vector3 PrjNorm;
//            public int[] InnerLefts;// "Lefter" pnt position for each innerHole
//            public List<int> BorderLefts = new List<int>();// "Lefter" pnt position for each BorderHole

            public bool IsChangedFace {
                set { isChangedFace = value; }
                get { return isChangedFace; }
            }

            //used in NxtvBads
            public bool IsFiltred {
                set { isFiltredInPntToTrisDict = value; }
                get { return isFiltredInPntToTrisDict; }
            }

            public bool IsCrossed {
                set { isCrossed = value; }
                get { return isCrossed; }
            }
            public bool IsSliser
            {
                set { isSliser = value; }
                get { return isSliser; }
            }
            public int[] Borders {
                get { return borders; }
            }
            public List<Point> Pnts {
                get { return pnts; }
            }

            public Vector3 Norm{
				set{norm = value;}
				get{return norm;}
			}
            public Vector3 NormSecator {
                set { normSecator = value; }
                get { return normSecator; }
            }
            public int IndSubMsh {
				set{indSubMsh = value;}
				get{return indSubMsh; }
			}

			public Poligon(List<Point> points, int[] setBorders, int[] nbrs, int iSubMsh, int direct = 1) {//, bool forvardDirect = true){
				pnts = points;
                borders = setBorders;
                neighborsIndx = nbrs;
				LinkPnts = new List<Point>(pnts);
				indSubMsh = iSubMsh;
                normSecator = Vector3.Cross(points[2].V - points[0].V, points[1].V - points[0].V);
                isSliser = direct < 0;
                if (isSliser) {
                    norm = -normSecator;
                }else {
                    norm = normSecator;
                }

                crossPnts = new Dictionary<Point, Point>();
                Nets = new Dictionary<Point, int>();
                NetKeys = new List<Point>();
                NetLinks = new List<List<Point>>();
                NetSecatorByPnt = new List<List<Vector3>>();
				ControlNets = new List<HashSet<Point>>();
                crssPntByBorder = new Dictionary<int, VectBox>();

                List<List<Vector2>> PrjPnts = new List<List<Vector2>>();

                bHoles = new Holes();
                PrjNorm = norm;
            }

            public void AddToBodyTriangles(ref Dictionary<Point, List<Poligon>> dict, bool[] flgBoost){
				for(int i = 0; i< LinkPnts.Count; i++){
                    if (flgBoost[i]) {
                        dict[LinkPnts[i]].Add(this);
                    } else {
                        if (dict.ContainsKey(LinkPnts[i])) {
                            dict[LinkPnts[i]].Add(this);
                        } else {
                            List<Poligon> poligons = new List<Poligon>();
                            poligons.Add(this);
                            dict.Add(LinkPnts[i], poligons);
                        }
                    }
				}
			}

            private void ChangeHash(){
				for (int i = 0; i < pnts.Count; i++) {
					hashCode += VectorExtension.VecRound (pnts[i].V, 0f).GetHashCode();
				}
				hashCode = hashCode >> pnts.Count;
			}
				
			public bool Equals(Poligon other){
				//Примем что в идентичных треугольниках и порядок следования вершин должен совпадать, тогда не нужно перебирать варианты
				float f = VectorExtension.rFactor; //28.07.2018 была 0.001f. Для уменьшения вероятности ошибки пришлось снизить точность
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				bool flg = true;
				for (int i = 0; i < pnts.Count; i++) {
					bool flg2 = this.pnts[i].V.Equals(other.pnts[i].V);
					flg = flg && flg2;
				}
				return flg;
			}

			public override bool Equals(object obj){
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((Poligon) obj);
			}
				
			public override int GetHashCode(){
				return hashCode;
			}

            //Functional
            public bool CrossToPoligon(Poligon other, Body[] bds, int indexThis, int indexOther) {
                int cnt = this.pnts.Count;
                Vector3[] tt1 = new Vector3[cnt + 1];
                Point[] pts1 = new Point[cnt + 1];
                for (int i=0; i< cnt; i++) {
                    tt1[i] = this.pnts[i].V;//////////////////////нужно избавиться от этих tt1. Создать массив!!!
                    pts1[i] = this.pnts[i];
                }
                tt1[cnt] = tt1[0];
                pts1[cnt] = pts1[0];

                cnt = other.pnts.Count;
                Vector3[] tt2 = new Vector3[cnt + 1];
                Point[] pts2 = new Point[cnt + 1];
                for (int i = 0; i < cnt; i++) {
                    tt2[i] = other.pnts[i].V;
                    pts2[i] = other.pnts[i];
                }

                tt2[cnt] = tt2[0];
                pts2[cnt] = pts2[0];

                /*
                  Необходимо искать точки граничные и внутренние в разных циклах, поскольку иначе может получиться, что будет 1 и та же точка 2 раза 
                  (если она лежит на границе) она будет в обоих циклах. Может попасться оба раза за 1 цикл и тогда точку исключим.
                  но нужно помнить, что у 2х треугольников общие точки лежат на максимум 1 отрезке (а минимум 1 точка).
                */

                bool flgCrossBordering = false;
                int side = -1;
                bool flgIsTriVertex = false;
                int NumVert = -1;

                float S = 0.0f;
                float Sa = 0.0f;
                float Sb = 0.0f;
                float Sc = 0.0f;
                Vector3 cP = new Vector3();
                Vector3 t1 = new Vector3();
                Vector3 t2 = new Vector3();

                bool flgSlised = true;
                Border curBorder = null;
                Point cPcL = null;
                int indxBordInList = 0;
                for (int i = 0; i < tt1.Length - 1; i++) {
                    flgCrossBordering = false;
                    flgIsTriVertex = false;
                    indxBordInList = this.Borders[i];
                    curBorder = bds[0].Borders[indxBordInList];
                    if (curBorder.CrossPntWithPlane == null) {
                        curBorder.DetermCrossPntWithPlane(bds[1].Borders.Count);
                    }
                    VectBox cVect = null;
                    if (curBorder.CrossPntWithPlane[indexOther] == null) {
                        WrapVectBox curWrapVectBox = new WrapVectBox(cVect);
                        if (CrossTrianglBySegment(ref cP, tt2, curBorder.Pnts, ref flgCrossBordering, ref side, ref flgIsTriVertex, ref NumVert)) {
                            cVect = new VectBox(flgCrossBordering, flgIsTriVertex, side, NumVert, ref cP);
                            curWrapVectBox.VB = cVect;
                            if (flgCrossBordering) {
                                int anotherPos = other.Borders[side];
                                Border anotherBord = bds[1].Borders[anotherPos];
                                //Save into neighbor poligon
                                int otherAnothPolyInt = anotherBord.IndxNeighbors[1 - other.neighborsIndx[side]];
                                curBorder.CrossPntWithPlane[otherAnothPolyInt] = curWrapVectBox;//new WrapVectBox(cVect);
                                //

                                if (anotherBord.CrossPntWithPlane == null) {
                                    anotherBord.DetermCrossPntWithPlane(bds[0].Borders.Count);
                                }
                                if (anotherBord.CrossPntWithPlane[indexThis] == null) {
//                                    cVect.side; do not this, it will be figured on
                                    anotherBord.CrossPntWithPlane[indexThis] = curWrapVectBox;//new WrapVectBox(cVect);
                                } else {
                                    VectBox savedVB = anotherBord.CrossPntWithPlane[indexThis].VB;
                                    if (savedVB != null) {
                                        Point anotherBordCrssPnt = null;
                                        bool flgChng = false;
                                        for (int z = 0; z < savedVB.LinkToPnt.Count; z++) {
                                            anotherBordCrssPnt = savedVB.LinkToPnt[z];
                                            if (!anotherBordCrssPnt.IsCrossBorderedPnt) {
                                                flgChng = true;
                                                if (anotherBordCrssPnt.IsBorderedPnt) {
                                                    anotherBordCrssPnt.V = cVect.V;//Not requare to AddSide -> becouse isBorderedPnt
                                                    anotherBordCrssPnt.IsCrossBorderedPnt = cVect.flgCrossBordering;
                                                } else {
                                                    anotherBordCrssPnt.V = cVect.V;
                                                    anotherBordCrssPnt.SetSide(side);
                                                    anotherBordCrssPnt.IsCrossBorderedPnt = cVect.flgCrossBordering;
                                                }
                                            }
                                        }
                                        if (!flgChng) {
                                            cVect.V = anotherBordCrssPnt.V;//Save general coordinate of pnt
                                        }
                                    }
                                    //May be to added to curBorder another Poly (anotherBord.IndxNeighbors)!!!!!!
                                }
                                int thisAnothPolyInt = curBorder.IndxNeighbors[1 - this.neighborsIndx[i]];
                                if (anotherBord.CrossPntWithPlane[thisAnothPolyInt] == null) {
                                    anotherBord.CrossPntWithPlane[thisAnothPolyInt] = curWrapVectBox;//new WrapVectBox(cVect);
                                }
                            }
                        }
                        curBorder.CrossPntWithPlane[indexOther] = curWrapVectBox;//new WrapVectBox(cVect);
                    } else {
                        cVect = curBorder.CrossPntWithPlane[indexOther].VB;
                        if (cVect != null) {
                            cVect.side = curBorder.IndxSideInPoly[1 - this.neighborsIndx[i]];//side index in another poligon
                        }
                    }
                    if (cVect != null) {

                        t1 = (pts1[i + 1].V - pts1[i].V);
                        t2 = (cVect.V - pts1[i].V);
                        //This will be search crossed point on border for t1 and not sure for t2
                        //Интерполируем нормаль, создавая объект PntCL
                        cPcL = new Point(flgSlised, cVect.V, VectorExtension.VLerP(pts1[i].N, pts1[i + 1].N, t1, t2));
                        cPcL.IsCrossing = true;
                        if (!this.crossPnts.ContainsKey(cPcL)) {
                            //this
                            cPcL.IsCrossBorderedPnt = cVect.flgCrossBordering;
                            cPcL.IsTriVertex = cVect.flgIsTriVertex;
                            cPcL.SetSide(i);
                            cPcL.IsBorderedPnt = true;
                            cPcL.UV = VectorExtension.VLerP2d(pts1[i].UV, pts1[i + 1].UV, t1, t2);
                            cPcL.Tng = VectorExtension.VLerP4d(pts1[i].Tng, pts1[i + 1].Tng, t1, t2);
                            cPcL.IndxSubMsh = this.indSubMsh;
//                            cPcL.NormCutterContainer[0] = other.normSecator;
                            this.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[0], cPcL, crNormSecats[0], other.normSecator);
                        } else {
//                            cPcL = this.crossPnts[cPcL];
                            Point p = this.crossPnts[cPcL];
//                            p.SetEqualAverage(cPcL);
                            cPcL = p;

                            if (cVect.flgCrossBordering && !cPcL.IsCrossBorderedPnt) {//If was be added from t2 cicle
                                cPcL.IsCrossBorderedPnt = true;
                                this.ContainsBorderPnt = true;
                                cPcL.IsBorderedPnt = true;// yet true
                                cPcL.SetSide(i);
                                cPcL.Neighbors[0] = indxs[i];
                                cPcL.Neighbors[1] = indxs[i + 1];
                            }
                            if(cVect.flgIsTriVertex && !cPcL.IsTriVertex) {
                                cPcL.IsTriVertex = cVect.flgIsTriVertex;
                            }
                            cPcL.IsBorderedPnt = true;
                            AddCrossPnt(crPnts[0], cPcL, crNormSecats[0], other.normSecator);
                        }
                        cVect.LinkToPnt.Add(cPcL);
                        if (!cVect.flgCrossBordering) {//if bordering then it will be added in cicle by t2
                            if (!other.crossPnts.ContainsKey(cPcL)) {
                                //other
                                cPcL = cPcL.CloneEmpty();
                                cPcL.IsCrossing = true;
                                cPcL.IsBorderedPnt = false;
                                cPcL.IsSlised = false;
                                S = AreaOfTriangleByV(tt2[0], tt2[1], tt2[2]);
                                Sa = AreaOfTriangleByV(tt2[1], tt2[2], cP) / S;
                                Sb = AreaOfTriangleByV(tt2[2], tt2[0], cP) / S;
                                Sc = AreaOfTriangleByV(tt2[0], tt2[1], cP) / S;
                                cPcL.N = Sa * pts2[0].N + Sb * pts2[1].N + Sc * pts2[2].N;
                                cPcL.UV = Sa * pts2[0].UV + Sb * pts2[1].UV + Sc * pts2[2].UV;
                                cPcL.Tng = Sa * pts2[0].Tng + Sb * pts2[1].Tng + Sc * pts2[2].Tng;
                                cPcL.IndxSubMsh = other.indSubMsh;
//                                cPcL.NormCutterContainer[0] = this.normSecator;
                                if (cVect.flgCrossBordering) {
                                    cPcL.IsBorderedPnt = true;
                                    cPcL.SetSide(cVect.side);
                                }
                                other.crossPnts.Add(cPcL, cPcL);
                                AddCrossPnt(crPnts[1], cPcL, crNormSecats[1], this.normSecator);
                            } else {
//                                cPcL = other.crossPnts[cPcL];
                                Point p = other.crossPnts[cPcL];
//                                p.SetEqualAverage(cPcL);
                                cPcL = p;

                                AddCrossPnt(crPnts[1], cPcL, crNormSecats[1], this.normSecator);
                            }
                        }
                        cVect.LinkToPnt.Add(cPcL);
                        //в словарь other.crossPnts можно добавлять без проверки, поскольку у них точки пересечения общие и всё должно в словарях совпадать с небольшой разницей по принадлежности
                    }
                }
                //По-хорошему, нужно убирать только те треуголники, которые реальное пересечение имеют между собой
                for (int i = 0; i < tt2.Length - 1; i++) {
                    flgCrossBordering = false;

                    indxBordInList = other.Borders[i];
                    curBorder = bds[1].Borders[indxBordInList];
                    if (curBorder.CrossPntWithPlane == null) {
                        curBorder.DetermCrossPntWithPlane(bds[0].Borders.Count);
                    }
                    VectBox cVect = null;
                    if (curBorder.CrossPntWithPlane[indexThis] == null) {
                        WrapVectBox curWrapVectBox = new WrapVectBox(cVect);
                        if (CrossTrianglBySegment(ref cP, tt1, curBorder.Pnts, ref flgCrossBordering, ref side, ref flgIsTriVertex, ref NumVert)) {
                            cVect = new VectBox(flgCrossBordering, flgIsTriVertex, side, NumVert, ref cP);
                            curWrapVectBox.VB = cVect;
                            if (flgCrossBordering) {
                                int anotherPos = this.Borders[side];
                                Border anotherBord = bds[0].Borders[anotherPos];
                                //Save into neighbor poligon
                                int otherAnothPolyInt = anotherBord.IndxNeighbors[1 - this.neighborsIndx[side]];
                                curBorder.CrossPntWithPlane[otherAnothPolyInt] = curWrapVectBox;//new WrapVectBox(cVect);
                                //

                                if (anotherBord.CrossPntWithPlane == null) {
                                    anotherBord.DetermCrossPntWithPlane(bds[1].Borders.Count);
                                }
                                if (anotherBord.CrossPntWithPlane[indexOther] == null) {
//                                    cVect.side = anotherBord.IndxSideInPoly[1 - this.neighborsIndx[i]];
                                    anotherBord.CrossPntWithPlane[indexOther] = curWrapVectBox;//new WrapVectBox(cVect);
                                } else {
                                    VectBox savedVB = anotherBord.CrossPntWithPlane[indexOther].VB;
                                    if (savedVB != null) {
                                        Point anotherBordCrssPnt = null;
                                        bool flgChng = false;
                                        for (int z = 0; z < savedVB.LinkToPnt.Count; z++) {
                                            anotherBordCrssPnt = savedVB.LinkToPnt[z];
                                            if (!anotherBordCrssPnt.IsCrossBorderedPnt) {
                                                flgChng = true;
                                                if (anotherBordCrssPnt.IsBorderedPnt) {
                                                    anotherBordCrssPnt.V = cVect.V;
                                                    anotherBordCrssPnt.IsCrossBorderedPnt = cVect.flgCrossBordering;
                                                } else {
                                                    anotherBordCrssPnt.V = cVect.V;
                                                    anotherBordCrssPnt.SetSide(side);
                                                    anotherBordCrssPnt.IsCrossBorderedPnt = cVect.flgCrossBordering;
                                                }
                                            }
                                        }
                                        if (!flgChng) {
                                            cVect.V = anotherBordCrssPnt.V;//Save general coordinate of pnt
                                        }
                                    }
                                    //May be to added to curBorder another Poly (anotherBord.IndxNeighbors)!!!!!!
                                }
                                int thisAnothPolyInt = curBorder.IndxNeighbors[1 - other.neighborsIndx[i]];
                                if (anotherBord.CrossPntWithPlane[thisAnothPolyInt] == null) {
                                    anotherBord.CrossPntWithPlane[thisAnothPolyInt] = curWrapVectBox;//new WrapVectBox(cVect);
                                }
                            }
                        }
                        curBorder.CrossPntWithPlane[indexThis] = curWrapVectBox;//new WrapVectBox(cVect);
                    } else {
                        cVect = curBorder.CrossPntWithPlane[indexThis].VB;
                        if (cVect != null) {
                            cVect.side = curBorder.IndxSideInPoly[1 - other.neighborsIndx[i]];//side index in another poligon
                        }
                    }
                    if (cVect!=null) {
                        t1 = (pts2[i + 1].V - pts2[i].V);
                        t2 = (cVect.V - pts2[i].V);
                        //This will be search crossed point on border for t2 and not sure for t1
                        cPcL = new Point(cVect.V, flgSlised);
                        cPcL.IsCrossing = true;
                        if (!this.crossPnts.ContainsKey(cPcL)) {
                            //this
                            //найдём барицентрические координаты в треугольнике через площади. по ним осуществим интерполяцию
                            S = AreaOfTriangleByV(tt1[0], tt1[1], tt1[2]);
                            Sa = AreaOfTriangleByV(tt1[1], tt1[2], cP) / S;
                            Sb = AreaOfTriangleByV(tt1[2], tt1[0], cP) / S;
                            Sc = AreaOfTriangleByV(tt1[0], tt1[1], cP) / S;

                            cPcL.IsBorderedPnt = false;
                            cPcL.N = Sa * pts1[0].N + Sb * pts1[1].N + Sc * pts1[2].N;
                            cPcL.UV = Sa * pts1[0].UV + Sb * pts1[1].UV + Sc * pts1[2].UV;
                            cPcL.Tng = Sa * pts1[0].Tng + Sb * pts1[1].Tng + Sc * pts1[2].Tng;
                            cPcL.IndxSubMsh = this.indSubMsh;
//                            cPcL.NormCutterContainer[0] = other.normSecator;
                            if (cVect.flgCrossBordering) {
                                cPcL.IsBorderedPnt = true;
                                cPcL.SetSide(cVect.side);
                            }
                            this.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[0], cPcL, crNormSecats[0], other.normSecator);
                        } else {
//                            cPcL = this.crossPnts[cPcL];
                            Point p = this.crossPnts[cPcL];
//                            p.SetEqualAverage(cPcL);
                            cPcL = p;

                            AddCrossPnt(crPnts[0], cPcL, crNormSecats[0], other.normSecator);
                        }
                        cVect.LinkToPnt.Add(cPcL);
                        if (!other.crossPnts.ContainsKey(cPcL)) {
                            //other
                            cPcL = cPcL.CloneEmpty();
                            cPcL.IsCrossBorderedPnt = cVect.flgCrossBordering;
                            cPcL.IsTriVertex = cVect.flgIsTriVertex;
                            cPcL.SetSide(i);
                            cPcL.IsCrossing = true;
                            cPcL.IsBorderedPnt = true;
                            cPcL.IsSlised = false;
                            cPcL.N = VectorExtension.VLerP(pts2[i].N, pts2[i + 1].N, t1, t2);
                            cPcL.UV = VectorExtension.VLerP2d(pts2[i].UV, pts2[i + 1].UV, t1, t2);
                            cPcL.Tng = VectorExtension.VLerP4d(pts2[i].Tng, pts2[i + 1].Tng, t1, t2);
                            cPcL.IndxSubMsh = other.indSubMsh;
//                            cPcL.NormCutterContainer[0] = this.normSecator;
                            other.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[1], cPcL, crNormSecats[1], this.normSecator);
                        } else {
//                            cPcL = other.crossPnts[cPcL];
                            Point p = other.crossPnts[cPcL];
//                            p.SetEqualAverage(cPcL);
                            cPcL = p;

                            if (cVect.flgCrossBordering && !cPcL.IsCrossBorderedPnt) {//If was be added from t1 cicle
                                cPcL.IsCrossBorderedPnt = true;
                                other.ContainsBorderPnt = true;
                                cPcL.IsBorderedPnt = true;  //yet true
                                cPcL.SetSide(i);
                                cPcL.Neighbors[0] = indxs[i];
                                cPcL.Neighbors[1] = indxs[i + 1];
                            }
                            if(cVect.flgIsTriVertex && !cPcL.IsTriVertex) {
                                cPcL.IsTriVertex = cVect.flgIsTriVertex;
                            }
                            cPcL.IsBorderedPnt = true;
                            AddCrossPnt(crPnts[1], cPcL, crNormSecats[1], this.normSecator);
                        }
                        cVect.LinkToPnt.Add(cPcL);
                    }
                }
                //Не будем учитывать прикосновения поверхностей 1й точкой
                //Сразу полагаем, что всего точек две. И для каждой линии разреза, когда не произошло Connect, может быть только одна из этих двух точек
                for (int ii = 0; ii < crPnts.Length; ii++) {
//                    if (crPnts[ii].Count == 2) {
                    if (crPnts[ii].Count == 2) {//consider only 2 crss pnts between two trianles
						if(ii==0){
							AddDict (this, crPnts[ii], crNormSecats[ii]);
						} else{
							AddDict (other, crPnts[ii], crNormSecats[ii]);
						}
                    }
                    crPnts[ii].Clear();//it more faster then new allocation
                    crNormSecats[ii].Clear();
                }

                return crPnts[0].Count > 0;
            }

            private void AddCrossPnt(List<Point> crPnts, Point cPcL, List<Vector3> secators, Vector3 secNorm) {
                if (crPnts.Count > 0) {
                    bool flg=false;
                    for(int i=0; i < crPnts.Count; i++) {
                        if (crPnts[i].Equals(cPcL)) {
                            flg = true;
                            break;
                        }
                    }
                    if (!flg) {
                        crPnts.Add(cPcL);
                        secators.Add(secNorm);
                    }
                }else {
                    crPnts.Add(cPcL);
                    secators.Add(secNorm);
                }
            }

/*
            private void AddCrossPnt(Poligon pol, List<Point> crPnts, Point cPcL, int indexAnother) {
                if (pol.crsPntsControl[indexAnother] < 2) {
                    if (pol.crsPntsControl[indexAnother] > 0) {
                        if (!crPnts[0].Equals(cPcL)){
                            crPnts.Add(cPcL);
                            pol.crsPntsControl[indexAnother]++;
                        }
                    } else {
                        crPnts.Add(cPcL);
                        pol.crsPntsControl[indexAnother]++;
                    }
                }
            }
*/

            private void AddDict(Poligon pol, List<Point> pnts, List<Vector3> normSecator) {
                List<Point> lstPnts = null;
                Point ctrlPnt = null;
                HashSet<Point> control = null;
                List<Vector3>  secNorms = null;
                int indxList = -1;
                for (int i = 0; i < pnts.Count; i++) {
                    if (pol.Nets.ContainsKey(pnts[i])) {
                        indxList = pol.Nets[pnts[i]];
                        control = pol.ControlNets[indxList];
                        if (pnts[i].IsBorderedPnt) {
                            pol.NetKeys[indxList].IsBorderedPnt = true;//May be it is redundant
                        }
                        for (int j = 0; j < pnts.Count; j++) {
                            if (j != i) {
                                ctrlPnt = pnts[j];//otherPnt
                                if (!control.Contains(ctrlPnt)) {
                                    pol.NetLinks[indxList].Add(ctrlPnt);
                                    pol.NetSecatorByPnt[indxList].Add(normSecator[j]);
                                    control.Add(ctrlPnt);

//                                    pnts[i].AddNetSecatorBySide(pnts[j], normSecator[j]);
                                }
                            }
                        }
                    } else {
                        lstPnts = new List<Point>();
                        secNorms = new List<Vector3>();
                        control = new HashSet<Point>();
                        for (int j = 0; j < pnts.Count; j++) {
                            if (j != i) {
                                ctrlPnt = pnts[j];//otherPnt
                                lstPnts.Add(ctrlPnt);
                                secNorms.Add(normSecator[j]);
                                control.Add(ctrlPnt);

//                                pnts[i].AddNetSecatorBySide(pnts[j], normSecator[j]);
                            }
                        }
                        pol.Nets.Add(pnts[i], pol.NetLinks.Count); //Point - index in NetLinks
                        pol.NetKeys.Add(pnts[i]);
                        pol.NetLinks.Add(lstPnts);//pol.NetLinks.Count == pol.controlNets.Count!
                        pol.NetSecatorByPnt.Add(secNorms);
                        pol.ControlNets.Add(control);
                    }
                }
            }
           
            public bool CrossTrianglBySegment(ref Vector3 CrPoint, Vector3[] tri, Vector3[] seg,
			        ref bool flgBordering, ref int side, ref bool flgTriVertex, ref int vrtx) {

			    Vector3 A = tri[0];
			    Vector3 B = tri[1];
			    Vector3 C = tri[2];
			    Vector3 D = seg[0];
			    Vector3 E = seg[1];

                Vector3 p1 = VectorExtension.Substitude(D, A);//D-A;
                Vector3 p2 = VectorExtension.Substitude(D, E);//D-E;
                Vector3 p3 = VectorExtension.Substitude(B, A);//B-A;
                Vector3 p4 = VectorExtension.Substitude(C, A);//C-A;

                Vector3 crssP3P4 = Vector3.Cross (p3, p4);//normal for plane of triangle

//			    float Delta = VectorExtension.Round(Vector3.Dot(p2, crssP3P4), 4f);
			    float Delta = Vector3.Dot(p2, crssP3P4);

			    float t = 0f;
			    float u = 0f;
			    float v = 0f;
			    float l = -0.00001f;
			    float h = 1.00001f;
			    float almostZero = 0.00001f;
                float sumMax = 0.99999f;
			    if (Delta>=l && Delta<=almostZero){//(Delta==0f){ //
				    //Отрезок параллелен плоскости треугольника (или лежит на ней, в этом случае он тоже пересекает)
				    //В случае, если при проходе секущего треугольника мы пересечём таки пересекаемый треугольник, у которого эта сторона (ED)
				    //параллельна плоскости секущего треугольника, то точки пересечения, найденные по секущему треугольнику (второй цикл) будут тоже граничными
				    //поэтому запоминаем сторону
//					    float tmpPar = VectorExtension.Round(Vector3.Dot (p1, crssP3P4), 4f);
                    float tmpPar = Vector3.Dot(p1, crssP3P4);
                    if (tmpPar >= l && tmpPar <= almostZero) {
						//Отрезок точно лежит в плоскости треугольника
						//поэтому будем искать на расчёте по секущему треугольнику граничные точки
					} 
			    }
			    else{
//				    t = VectorExtension.Round(Vector3.Dot (p1, crssP3P4)/Delta, 4f);
                    t = Vector3.Dot(p1, crssP3P4) / Delta;

                    if (t>=l&&t<=h){//if(t>=0&&t<=1){
//					    u = VectorExtension.Round(Vector3.Dot (p2,Vector3.Cross(p1,p4))/Delta, 4f);
                        u = Vector3.Dot(p2, Vector3.Cross(p1, p4)) / Delta;

                        if (u>=l&&u<=h){//if(u>=0&&u<=1){
//						    v = VectorExtension.Round(Vector3.Dot (p2,Vector3.Cross(p3,p1))/Delta, 4f);
                            v = Vector3.Dot(p2, Vector3.Cross(p3, p1)) / Delta;
                            if (v>=l&&v<=h){//if(v>=0&&v<=1){
							    float USumV = u+v;
							    if(USumV<=h){//if((u+v)<=1){//<=1){
                                    CrPoint = A + p3 * u + p4 * v;//VectorExtension.VecRound(A+p3*u+p4*v, 2f);
                                    if (CrPoint.x == 40.57599f && CrPoint.y == 84.59999f && CrPoint.z == 75.00121f) {
                                        string ss = "";
                                    }
									if (u <= almostZero) { //and u>=l
										//side 2
										side = 2;
										flgBordering = true;

                                        //vrtx
                                        if (v <= almostZero) {
                                            vrtx = 0;
                                            flgTriVertex = true;
                                        } else if(v>=sumMax) {
                                            vrtx = 2;
                                            flgTriVertex = true;
                                        }
										goto j1;
									} else if (v <= almostZero) { //and v>=l
										//side 0
										side = 0;
										flgBordering = true;

                                        //vrtx
                                        if (u <= almostZero) {
                                            vrtx = 0;
                                            flgTriVertex = true;
                                        } else if (u >= sumMax) {
                                            vrtx = 1;
                                            flgTriVertex = true;
                                        }
                                        goto j1;
									} else if (USumV >= sumMax) { //and <=h{
										//side 1
										flgBordering = true;
										side = 1;

                                        //vrtx
                                        if (u >= sumMax) {
                                            vrtx = 1;
                                            flgTriVertex = true;
                                        } else if (v >= sumMax) {
                                            vrtx = 2;
                                            flgTriVertex = true;
                                        }
                                        goto j1;
									}
								    j1:
								    return true;
							    }
						    }
					    }
				    }
			    }
			    return false;
		    }


        }
        #endregion

        #region BorderDefinitionClass
        public class Border: System.IEquatable<Border> {
            private int[] pntsIndx;
            private Vector3[] pnts;
            private int[] indxSideInPoly = new int[2];
            private int[] indxNeighborsPoly = new int[2];
            public WrapVectBox[] CrossPntWithPlane = null;

            private int hashCode = -1;
            public Border(int i1, int i2, Vector3 v1, Vector3 v2) {
                pntsIndx = new int[] {i1, i2};
                pnts = new Vector3[] { v1, v2 };
                RefreshHashCode();
            }
            public int[] IndxSideInPoly {
                set { indxSideInPoly = value; }
                get { return indxSideInPoly; }
            }

            public int[] IndxNeighbors {
                set { indxNeighborsPoly = value; }
                get { return indxNeighborsPoly; }
            }

            public int[] PntsIndx {
                get { return pntsIndx; }
            }
            public Vector3[] Pnts {
                get { return pnts; }
            }

            public void DetermCrossPntWithPlane(int cntA) {
                CrossPntWithPlane = new WrapVectBox[cntA];
            }
            private void RefreshHashCode() {
                hashCode = pnts[0].GetHashCode() ^ pnts[1].GetHashCode();
            }
            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Border)obj);
            }
            public bool Equals(Border other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                int[] otherPntsIndx = other.pntsIndx;
                //if hashcode are coinsides then this enough:
                bool flg1 = otherPntsIndx[0] == this.pntsIndx[0] || otherPntsIndx[0] == this.pntsIndx[1];
                if (flg1) {
                    return true;
                }else {
                    return (this.pnts[0] == other.pnts[0] && this.pnts[1] == other.pnts[1]) || (this.pnts[1] == other.pnts[0] && this.pnts[0] == other.pnts[1]);
                }
            }
            public override int GetHashCode() {
                return hashCode;
            }
        }
        #endregion

        #region PointDefinitionClass
        public class Point : System.IEquatable<Point> {
            // For this class IEqualeble will be to compared by only Vector3 "V" (position in space)
            //For Slise_
//            public bool isUsedPair = false;
//            public bool isPaired = false;
//            private Vector3 cutterNorm; //Normal to sliser plane
            //_For Slise

            private int hashCode = -1;
            private int indSubMsh; //index of submesh
            private int oldIndx = -1;

            private bool isAddedToMshBody = false;

            private Vector3 v;
            private Vector3 n;      //normal
            private Vector2 uv;
            private Vector4 tng;

            private Vector2 pV;

            //            private List<Vector3> normCutter;
            public Vector3[] NormCutterContainer = new Vector3[2];
            public int bordCloseIndx = -1;
//            private int cutterIndx = 0;

            private bool isBorderedPnt = false;

            //            private int side=-1;
            public List<int> Side = null;
            private bool isTriVertex = false;
            private bool isCrossBorderedPnt = false;
            private bool isDoublePnt = false; //isTriVertex or isCrossBorderPnt
            public int[] Neighbors = null;//if crossPnt isTriVetex - index vertex in neighbors[0]; for crossPnt isCrossBorderPnt - indexes closer vertex in neighbors[0] and neighbors[1]
            private int cntNeighbors = -1;//for isTriVertesxs = 1; for isCrossBorderPnt = 2;

            private bool isCrossingPnt = false;//Used for catching of Links only
            private bool isSlisedPnt = false; //slised or sliser body?


            private bool isUsedByTranslate = false; //used by translating of previous apertuner
            private int oldApertNum = -1;
            private int newIndx = -1;
            private int curIndx = -1;
            private int[] oldIndxs = null;//for save of order of pnts after all refreshes {i,j}, i - num of side, j - num of pnt on side


            private bool isExternSidePnt = false; //external circout ordered sequence!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            private List<Point> linksToCommEdge; //Only for cross pnts
            private Dictionary<int, int> Corrector = new Dictionary<int, int>();
            private int indxOfLink = 0;

            public static List<Vector3> GetListVector3(List<Point> pnts) {
                List<Vector3> vPnts = new List<Vector3>();
                vPnts.Capacity = pnts.Capacity;
                for (int i = 0; i < pnts.Count; i++) {
                    vPnts.Add(pnts[i].V);
                }
                return vPnts;
            }
            public static List<Vector2> GetListProjVector2(List<Point> pnts) {
                List<Vector2> vPnts = new List<Vector2>();
                vPnts.Capacity = pnts.Capacity;
                for (int i = 0; i < pnts.Count; i++) {
                    vPnts.Add(pnts[i].ProjV);
                }
                return vPnts;
            }

            public bool IsAddedToMshBody {
                set { isAddedToMshBody = value; }
                get { return isAddedToMshBody; }
            }

            public Vector3 V{
				set{
					v = value;
					RefreshHashCode();
				}
				get{return v;}
			}

            public Vector2 ProjV {
                set { pV = value; }
                get { return pV; }
            }

            public Vector3 N{
				set{n = value;}
				get{return n;}
			}
            public Vector2 UV {
                set { uv = value; }
                get { return uv; }
            }
            public Vector2 Tng {
                set { tng = value; }
                get { return tng; }
            }
            public bool IsBorderedPnt {
                set { isBorderedPnt = value; }
                get { return isBorderedPnt; }
            }
            /// <summary>
            /// Setup property to true will set isTriVertex to false, and call new allocations massive for Neighbors
            /// </summary>
            public bool IsCrossBorderedPnt {//or CrossBorderedPnt or TriVertex
                set {
                    isCrossBorderedPnt = value;
                    if (isCrossBorderedPnt) {
                        cntNeighbors = 2;
                        Neighbors = new int[cntNeighbors];
                        isTriVertex = false;
                        isDoublePnt = true;
                    }
                }
                get { return isCrossBorderedPnt; }
            }
            public int CntNeighbors {
                set { cntNeighbors = value; }
                get { return cntNeighbors; }
            }
            /// <summary>
            /// Setup property to true will set isCrossBorderedPnt to false, and call new allocations massive for Neighbors
            /// </summary>
            public bool IsTriVertex {//or CrossBorderedPnt or TriVertex
                set {
                    isTriVertex = value;
                    if (isTriVertex) {
                        cntNeighbors = 1;
                        Neighbors = new int[cntNeighbors];
                        isCrossBorderedPnt = false;
                        isDoublePnt = true;
                    }
                }
                get { return isTriVertex; }
            }

            public bool IsDoublePnt{
                get{ return isDoublePnt; }
            }
            /*
            public void AddSide(int i) {
                Side.Add(i);
            }
            */
            public void SetSide(int i) {
                if(Side.Count == 0) {
                    Side.Add(i);
                }else {
                    Side[0] = i;
                }

            }
            //            public int Side {
            //                set { side = value; }
            //                get { return side; }
            //            }
            public bool IsCrossing {
                set { isCrossingPnt = value; }
                get { return isCrossingPnt; }
            }
            public bool IsSlised {
                set { isSlisedPnt = value; }
                get {return isSlisedPnt; }
            }

            public int OldIndx{
				set{ oldIndx = value;}
				get{ return oldIndx;}
			}
			public int CurIndx{
				set{ curIndx = value;}
				get{ return curIndx;}
			}

            public int[] OldIndxs {
                set { oldIndxs = value; }
                get { return oldIndxs; }
            }
            public int IndxSubMsh{
				set{ indSubMsh = value;}
				get{ return indSubMsh;}
			}
			public bool IsAroundSidePnt{
				set{ isExternSidePnt = value;}
				get{ return isExternSidePnt;}
			}
			public bool IsUsedByTranslate{
				set{ isUsedByTranslate = value;}
				get{ return isUsedByTranslate;}
			}
			public int OldApertNum{
				set{ oldApertNum = value;}
				get{ return oldApertNum;}
			}

            public Point(bool flgSliser, Vector3 _v, Vector3 _n, int i=0, int j=0, Vector2 _uv =  new Vector2(), Vector4 _tng =  new Vector4()){
                this.oldIndx = j;
                this.indSubMsh=i;
				this.v = _v;
                this.n = _n;
				this.uv = _uv;
				this.tng = _tng;
				isSlisedPnt = flgSliser;
                RefreshHashCode();
                //				linksToCommEdge = new List<PntCL> ();
                Side = new List<int>();
//                normCutter = new List<Vector3>();
            }
            public Point(Vector3 _v, bool flgSliser= false) {
                this.v = _v;
                isSlisedPnt = flgSliser;
                RefreshHashCode();
                Side = new List<int>();
//                normCutter = new List<Vector3>();
            }

            public void SetEqualAverage(Point other) {
                this.v.x += other.v.x;
                this.v.y += other.v.y;
                this.v.z += other.v.z;
                this.v /= 2;
            }

            public bool IsEqualLinks(Point p) {
                return ReferenceEquals(this, p);
            }
            public void CopyMainAttributesFrom(Point p) {
                this.n = p.n;
                this.uv = p.uv;
                this.tng = p.tng;
            }

            public Point Clone() {
                Point newPnt = new Point(this.isSlisedPnt, this.v, this.n, this.indSubMsh, this.oldIndx, this.uv, this.tng);
                newPnt.hashCode = this.hashCode;
                return newPnt;
            }

            public Point CloneEmpty() {
                Point newPnt = new Point(this.v);
                newPnt.hashCode = this.hashCode;
                return newPnt;
            }
            public List<Point> GetComEdgesPnts() {
                return linksToCommEdge;
            }
            public void AddNewLinkToEdge(Point vPnt) {
                int indxPnt = vPnt.CurIndx;
                if (indxPnt != curIndx) {
                    if (!Corrector.ContainsKey(indxPnt)) {
                        Corrector.Add(indxPnt, indxOfLink);
                        indxOfLink++;
                        linksToCommEdge.Add(vPnt);
                    } else {
                        int cIndx = Corrector[indxPnt];
                        linksToCommEdge.RemoveAt(cIndx);
                        indxOfLink--;
                        Corrector.Remove(indxPnt);
                        for (int i = cIndx; i < linksToCommEdge.Count; i++) {
                            Corrector[linksToCommEdge[i].CurIndx]--;
                        }
                    }
                }
            }

            /// <summary>
            /// Control Dictionary.ContaynsKey should be in out side
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
//            public Vector3 GetNetSecatorBySide(Point p) {
//                return netSecatorBySide[p];
//            }


            public override bool Equals(object obj) {
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((Point)obj);
			}
			public bool Equals(Point other) {
				float f = VectorExtension.rFactor;
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				bool flg1 = (Mathf.Abs(this.V.x - other.V.x) < f && Mathf.Abs(this.V.y - other.V.y) < f && Mathf.Abs(this.V.z - other.V.z) < f);
				return flg1;
			}
			public void RefreshHashCode() {
				hashCode = VectorExtension.VecRound(v, 0f).GetHashCode();
			}

			public override int GetHashCode() {
                return hashCode;
            }

            public override string ToString() {
                return string.Format("[V={0}, N={1}, UV={2}", v, n, uv);
            }

        }

        public class DirectedPoint: BasePoint{
			protected Vector3 n;
            private int hashCode = -1;

			public Vector3 N{
				set{n = value;}
				get{return n;}
			}

            public DirectedPoint(Point pnt) {
                v = pnt.V;
                n = pnt.N;
                RefreshHashCode(pnt);
            }
            public void RefreshHashCode(Point p) {
                hashCode = p.GetHashCode();
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DirectedPoint)obj);
            }
            public bool Equals(DirectedPoint other) {
                float f = VectorExtension.rFactor;
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                bool flg1 = (Mathf.Abs(this.V.x - other.V.x) < f && Mathf.Abs(this.V.y - other.V.y) < f && Mathf.Abs(this.V.z - other.V.z) < f);
                if (!flg1) {
                    return false;
                }
                return flg1 && VectorExtension.IsParalVec2(this.n, other.n);//VectorExtension.IsParalDirVec(this.n, other.n);
            }
            public override int GetHashCode() {
                return hashCode;
            }
        }

        public class BasePoint: System.IEquatable<BasePoint>{
			protected Vector3 v;
			public Vector3 V{
				set{v = value;}
				get{return v;}
			}
			public override bool Equals(object obj){
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((BasePoint) obj);
			}
			public bool Equals(BasePoint other){
				float f = VectorExtension.rFactor; //28.07.2018 была 0.001f. Для уменьшения вероятности ошибки пришлось снизить точность!!!!!!!!!!!!!!!!
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				bool flg1 = (Mathf.Abs (this.v.x-other.v.x)<f && Mathf.Abs (this.v.y-other.v.y)<f && Mathf.Abs (this.v.z-other.v.z)<f);
				return flg1;
			}
			public override int GetHashCode(){
				return VectorExtension.VecRound(v, 0f).GetHashCode();
			}
		}

        public class WrapVectBox {
            public VectBox VB;

            public WrapVectBox(VectBox nvb) {
                this.VB = nvb;
            }
        }
        public class VectBox {
            public Vector3 V;
            public bool flgCrossBordering;
            public int side;
            public bool flgIsTriVertex;
            public int NumVert;
            public List<Point> LinkToPnt = null;
            public List<List<int>> Sides = null;

            public VectBox(bool flgCrBord, bool flgIsTriV, int sd, int nVert, ref Vector3 v_) {
                flgCrossBordering = flgCrBord;
                side = sd;
                flgIsTriVertex = flgIsTriV;
                NumVert = nVert;
                V = v_;
                LinkToPnt = new List<Point>();
                Sides = new List<List<int>>();
            }
/*
            public void SetNewLinks(Point p, List<int> lst) {
                LinkToPnt.Add(p);
                Sides.Add(lst);
            }
*/  
        }
        #endregion

        public class Holes{
			public HoleAttribute BorderHoles;
			public HoleAttribute InnerHoles;
//            public List<List<Vector3>> BorderCutter;
            public List<List<int>> IndexCrossBorderPntsInner;// indexer of InnerHoles group includes CrossBorderPnts [index of  innerHoles][index of point in cur innerHoles Pnts group]
            public Holes(){
                BorderHoles = new HoleAttribute();
                InnerHoles = new HoleAttribute();
                IndexCrossBorderPntsInner = new List<List<int>>();
            }
			//Можно добавить функцию объединения коллекций в одну
		}

        public class HoleAttribute {
            public List<List<Point>> Lst;
            public List<List<Vector3[]>> NormCutts;//Direct and revers
            public int[] LeftIndxs;
            private int[] lnkToNrm;
            public HoleAttribute() {
                Lst = new List<List<Point>>();
                NormCutts = new List<List<Vector3[]>>();
            }
            public void CreateLinkToNormCutt() {
                lnkToNrm = new int[Lst.Count];
                LeftIndxs = new int[Lst.Count];
            }
            public void ReverseAt(int i) {
                Lst[i].Reverse();
                NormCutts[i].Reverse();
                lnkToNrm[i] = 1;
            }
            public int GetCurDirNormsLink(int i) {
                return lnkToNrm[i];
            }
        }

        public struct WrapSlice {
            public int BordPntIndx;
            public Vector3 sideV;
            public Vector3 curDir;
            public Slice SL;
//            public WrapSlice(int i = 0, Slice lnkSL = null, Vector3 side = new Vector3(), Vector3 dir = new Vector3()) {
            public WrapSlice(int i, Slice lnkSL, Vector3 side, Vector3 dir) {
                BordPntIndx = i;
                SL = lnkSL;
                sideV = side;
                curDir = dir;
            }

            public static void Sort(List<WrapSlice> data) {
                int last = data.Count - 1;
                if (data.Count > 0) {
                    QuickSort(data, 0, last);
                }
            }

            private static void QuickSort(List<WrapSlice> data, int first, int last) {
                if (first <= last) {
                    int pivot = (last - first) / 2 + first;
                    int i = first, j = last;
                    WrapSlice pivotS = data[pivot];
                    float pivotAngl = VectorExtension.UltAnglByVect(pivotS.sideV, pivotS.curDir);

                    int brdPntIndx = -1;
                    Slice curSlice = null;
                    for (; i <= j;) {
                        int pivIndx = pivotS.BordPntIndx;
                        while (i <= last) {
                            WrapSlice curWrap = data[i];
                            brdPntIndx = curWrap.BordPntIndx;
                            curSlice = curWrap.SL;
                            if (curSlice.p[curWrap.BordPntIndx] != pivotS.SL.p[pivIndx]) {
                                if (curSlice.d[brdPntIndx] < pivotS.SL.d[pivIndx]) {
                                    i++;
                                } else {
                                    break;
                                }
                            }else {
                                //if crossBorderPnt

                                if (!(curSlice.p[0] == curSlice.p[1] && curSlice.p[0].IsCrossBorderedPnt)) {//if cur SL - is clamping SL with crossBorder Pnt. It yet was sorted
                                    float curAnlg = VectorExtension.UltAnglByVect(curWrap.sideV, curWrap.curDir);
                                    if (curAnlg < pivotAngl) {
                                        i++;
                                    } else {
                                        break;
                                    }
                                } else {
                                    i++;
                                }
                            }
                        }
                        while (j >= first) {
                            WrapSlice curWrap = data[j];
                            brdPntIndx = curWrap.BordPntIndx;
                            curSlice = curWrap.SL;
                            if (curSlice.p[curWrap.BordPntIndx] != pivotS.SL.p[pivIndx]) {
                                if (curSlice.d[brdPntIndx] > pivotS.SL.d[pivIndx]) {
                                    j--;
                                } else {
                                    break;
                                }
                            }else {
                                //if crossBorderPnt
                                if (!(curSlice.p[0] == curSlice.p[1] && curSlice.p[0].IsCrossBorderedPnt)) {//(curSlice != pivotS.SL) {//if cur SL - is clamping SL with crossBorder Pnt. It yet was sorted
                                    float curAnlg = VectorExtension.UltAnglByVect(curWrap.sideV, curWrap.curDir);
                                    if (curAnlg > pivotAngl) {
                                        j--;
                                    } else {
                                        break;
                                    }
                                } else {
                                    j--;
                                }
                            }
                        }
                        if (i <= j) {
                            data[i].SL.IndxInLst[data[i].BordPntIndx] = j;
                            data[j].SL.IndxInLst[data[j].BordPntIndx] = i;
                            WrapSlice swiper = data[i];
                            data[i] = data[j];
                            data[j] = swiper;
                            i++;
                            j--;
                        }
                    }
                    if (j > first) QuickSort(data, first, j);
                    if (i < last) QuickSort(data, i, last);
                }
            }
        }

		public class Slice{
            public bool IsClamper = false;
			public Point [] p = new Point[2];
			public float[] d = new float[2];
            public int[] IndxInLst = new int[2]; //index in List<List<Slice>> (GetPerimeter)
            public bool IsUsed = false;
			public bool iSPntOfTriangle; //Вершина треугольника (?)
			public List<Point> Lst;//Упорядоченный список от ближайшей точки к дальней
            private List<Vector3[]> lstNrmCutt;
			public int[] i = new int[2];//Текущая сторона треугольника для точки 1 и 2
            private int curInd = -1;//Dirct
            private int posBrds = 0;
//            public Slice(Point _p1, Point _p2, float _d1, float _d2, List<Point> listArr, List<Vector3[]> listNrmCutt, int j1 , int j2){
            public Slice(Point _p1, Point _p2, float _d1, float _d2, HoleAttribute holeAttr, int curHoleIndx, int j1, int j2) {
                p[0] = _p1;
				p[1] = _p2;
				d[0] = _d1;
				d[1] = _d2;
				Lst = holeAttr.Lst[curHoleIndx];
                lstNrmCutt = holeAttr.NormCutts[curHoleIndx];
                curInd = holeAttr.GetCurDirNormsLink(curHoleIndx);
                posBrds = 0;//if reversed : lstNrmCutt.Count - 1;//reverse
                i[0] = j1;
				i[1] = j2;
            }
            public Slice(bool flgRevesed, float _d1, float _d2, HoleAttribute holeAttr, int curHoleIndx, int j1, int j2) {
                Lst = new List<Point>(holeAttr.Lst[curHoleIndx]);
                lstNrmCutt = new List<Vector3[]>(holeAttr.NormCutts[curHoleIndx]);
                if (flgRevesed) {
                    Lst.Reverse();
                    lstNrmCutt.Reverse();
                    curInd = 1;
                } else {
                    curInd = 0;
                }
                i[0] = j1;
                i[1] = j2;
                d[0] = _d1;
                d[1] = _d2;
                p[0] = Lst[0];
                p[1] = Lst[Lst.Count - 1];
                posBrds = 0;//if reversed : lstNrmCutt.Count - 1;//reverse
            }

            public Vector3 GetStartPositionNrmCutt() {
                return lstNrmCutt[posBrds][1 - curInd];//[curInd];//Vector from dir //необходимо проверить по какому индексу нужно брать!

            }

            public void ReverseBordsPnt() {
                Lst.Reverse();
                Point tmP = p[0];
                p[0] = p[1];
                p[1] = tmP;
                int ti = i[0];
                i[0] = i[1];
                i[1] = ti;
                float tmD = d[0];
                d[0] = d[1];
                d[1] = tmD;
                curInd = 1 - curInd;
                posBrds = lstNrmCutt.Count - 1;//reverse
            }
		}
		public struct FstPnt{
			public Point v;
			public Vector3 n;
			public int triNumb;
		}

        public class LinksCreator{
			List<LinksCreatorPar> Links = new List<LinksCreatorPar>();
			private List<int>[] externPnts;
			private List<List<int>[]> oldRefreshedPnts;
//			private int offst;
//			private int LastIndx=0;
			private List<List<int>[]> tmpInds;
			private bool isRefreshedOld = false;
			private int sbCnt = 0;
			List<Point> pntsLink;
			public List<List<int>[]> OldInds{
				set{oldRefreshedPnts = value;}
			}
			public List<int>[] ExternPnts{
				set{externPnts = value;}
			}	
			public bool IsRefreshedOld{
				get{ return isRefreshedOld;}
			}

			public LinksCreator(int subCnt = 3){
				sbCnt = subCnt;
				tmpInds = new List<List<int>[]> ();
				pntsLink = new List<Point>();
			}

			//The order is much necessury for 0 and 1 side only (frond and back side, without cuter-builder faces)
			private List<int> SetOrder(List<int> unOrderPnts){
				List<int> result = new List<int>();
				int cur = unOrderPnts [0];
				int nxt = -1;
				int k = 0;
				bool flg = false;
				result.Add (cur);
				Dictionary<int, int> Catched = new Dictionary<int, int> ();
				Catched.Add(cur, cur);
				do {
					List<Point> nxtPnts = pntsLink[cur].GetComEdgesPnts();
					flg = true;
					for(int i = 0; i< nxtPnts.Count;i++){
						if(!Catched.ContainsKey(nxtPnts[i].CurIndx)){
							nxt = nxtPnts[i].CurIndx;
							Catched.Add(nxt, nxt);
							nxtPnts.RemoveAt(i);
							flg = false;
							cur = nxt;
							result.Add(cur);
							break;
						}
					}
					k++;//Temporary!!!!!!!!!!!!!!!!!
				} while (!flg&& k<1000);

				return result;
			}

			public List<List<int>[]> GetOldIndxs(){
				return oldRefreshedPnts; //changed by ref
			}
			public List<int>[] GetExternIndxs(){
				return externPnts; //changed by ref
			}

		}



		public class LinksCreatorPar{
			public List<List<int>[]> OldRefreshedPnts; //will be more then 2 side (for wall)
            public List<int>[] externPnts; //will be 2 side

			public LinksCreatorPar(ref List<List<int>[]> oldInds, ref List<int>[] exPnts){
				OldRefreshedPnts = oldInds;
				externPnts = exPnts;
			}

            public LinksCreatorPar(int subMshCnt) {
                OldRefreshedPnts = new List<List<int>[]>();
                externPnts = new List<int>[2];
                for(int i = 0; i < externPnts.Length; i++) {
                    externPnts[i] = new List<int>();
                }
            }

            public LinksCreatorPar(PntsAttributes attributes) {
                OldRefreshedPnts = attributes.inds;
                externPnts = attributes.externalPnts;
            }

        }

        public class ResultInfo {
            public BodyRawAttributes bodyMsh;
            public ResultInfo(BodyRawAttributes body) {
                bodyMsh = body;
            }
        }

		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		//Триангуляция

		public class Triangulation {
			public static List<int> GetTriangles(List<Point> pnts, int nPosit, Vector3 norm){
                int left = 0;
                Matrix4x4 MT = GetM4MRotate(ref norm);
                List<Vector2> PntsV2 = GetOrthoProjectionV2(MT, ref pnts, ref left);
                return GeneralTriangulation(PntsV2, nPosit, norm, false, false, left);
            }

            public static List<int> GeneralTriangulation(List<Vector2> v, int nPosit, Vector3 nrm, bool flgKnownDirect = false, bool flgCutter = false, int ii = 0) {

                List<Vector2> copyV = new List<Vector2>(v);
                bool[] casherArr = new bool[copyV.Count];
                List<bool> casher = new List<bool>(casherArr);
                List<int> indxList = new List<int>();
                indxList.Capacity = v.Count;
                for (int i = 0; i < v.Count; i++) {
                    indxList.Add(i);
                }
                List<Vector2> copyV_t = new List<Vector2>();
                List<int> resultT = new List<int>();
                Vector2 nLeft = v[ii];//Самая левая точка

                //Сначала найдём самую левую точку, она точно будет выпуклой. В ней найдём значение векторного умножения (для выпуклых всех такое будет)
                //Так же мы могли посчитать кол-во "+" и "-" векторных умножений, тех, которых больше для фигуры, образованной
                //замкнутой ломанной больше будет именно выпуклых вершин.

                copyV_t.Add(copyV.Last());
                copyV_t.AddRange(copyV);
                copyV_t.Add(copyV[0]);
                float VectorCrosOut = 0.0f;
                bool flg = false; //!flgCutter;
                if (!flgKnownDirect) {
                    VectorCrosOut = VectorExtension.OrtCrosVect(nLeft, copyV_t[ii], nLeft, copyV_t[ii + 2]);
                    flg = ((VectorCrosOut * nrm.z) < 0);
                }else {
                    VectorCrosOut = Mathf.Sign(nrm.z);
                }
/*
                for(int i = 0; i < copyV.Count; i++) {
                    ListGO.Add(VectorExtension.CreatePntGO(i.ToString() + "_" + copyV[i].ToString(), copyV[i]));
                }
*/
                int index = 0;
                int k = 0;
                do {
                    int lastElmt = copyV.Count - 1;
                    int before = index > 0 ? index - 1 : lastElmt;
                    int i = index;
                    int after = index < lastElmt ? index + 1 : 0;

                    if (!casher[index]) {
                        bool isEar = false;
                        Vector2 curV = copyV[i];
                        float tmpOrt = VectorExtension.OrtCrosVect(curV, copyV[before], curV, copyV[after]);//Outer (cross sign is like the normal by plane
                        isEar = (Mathf.Abs(tmpOrt - VectorCrosOut) < VectorExtension.rFactor);
                        if (isEar || copyV.Count == 3) {
                            if (isIntOuter(before, i, after, copyV)) {
                                if (flg) {
                                    resultT.AddRange(new int[] { indxList[before] + nPosit, indxList[i] + nPosit, indxList[after] + nPosit });
                                } else {
                                    resultT.AddRange(new int[] { indxList[after] + nPosit, indxList[i] + nPosit, indxList[before] + nPosit });
                                }
                                //Необходимо, чтобы copyV был замкнутым списком. Чтобы "отрезаение" треугольников происходило до тех пор, пока не останется 3 вершины.
                                copyV.RemoveAt(index);
                                indxList.RemoveAt(index);

                                casher[before] = false;
                                casher[after] = false;
                                casher.RemoveAt(index);

                                index = index - 1;
                            } else {
                                casher[index] = true;
                            }
                        } else {
                            casher[index] = true;
                        }
                    }
                    index++;
                    if (index == copyV.Count) {
                        index = 0;
                    }
                    k++;//Пока оставлю, чтобы не возникало беск. цикла
                } while (copyV.Count > 2 && k < 10000);//k может помешать для очень детализированного полигона (с б-м к-ом вертексов)

                v.Clear();
                copyV.Clear();
                copyV_t.Clear();
                return resultT;
            }

            private static bool isIntOuter(int a, int b, int c, List<Vector2> v) {
                //Если треугольник, то всё ок, завершаем.
                if (v.Count == 3) {
                    return true;
                }
                Vector2 A = v[a];
                Vector2 C = v[b];
                Vector2 B = v[c];
                Vector2 AC = A - C;
                Vector2 BC = B - C;
                for (int i= 0; i< v.Count; i++) {
                    //if (i != a && i != b && i != c) {
                    if (v[i] != A && v[i] != B && v[i] != C) {
                        Vector2 D = v[i];
                        Vector2 DC = D - C;

                        float v23 = DC.y * AC.x - DC.x * AC.y;
                        float v43 = DC.y * BC.x - DC.x * BC.y;

                        float Dot1 = DC.x * AC.x + DC.y * AC.y;
                        float Dot2 = DC.x * BC.x + DC.y * BC.y;
/*
                        float DCsqrMagnitude = DC.sqrMagnitude;
                        float proj1 = Dot1 / DCsqrMagnitude;//project to DC (use ^2, becouse 0 and 1 for ^2 identical)
                        float proj2 = Dot2 / DCsqrMagnitude;//project to DC
                        float proj31 = Dot1 / AC.sqrMagnitude; //project to AC
                        float proj32 = Dot2 / BC.sqrMagnitude; //project to BC
*/
                        if (v23 * v43 < 0) {
                            float ACsqrMagnitude = AC.sqrMagnitude;
                            float BCsqrMagnitude = BC.sqrMagnitude;
                            Vector2 DacC = AC * (Dot1 / ACsqrMagnitude);//project D to AC
                            Vector2 DbcC = BC * (Dot2 / BCsqrMagnitude);//project D to BC
                            float dotACDbc = Vector2.Dot(DbcC, AC) / ACsqrMagnitude;
                            float dotBCDac = Vector2.Dot(DacC, BC) / BCsqrMagnitude;
                            float proj1 = Dot1 / ACsqrMagnitude;
                            float proj2 = Dot2 / BCsqrMagnitude;
                            if (proj1 >= dotACDbc && proj2 >= dotBCDac){//дб >=, потому что при 90 градусах будет =, но только 1 из Dot-ов сможет быть =0. Одновременно если, то 180 градусов будет мбду сторонами - не канает
                                //vertesices in area of ear not should be
                                //For S of triangles will be calculate inside pnt, or outer
                                Vector2 AB = A - B;
                                float S0 = Mathf.Abs(AB.y * AC.x - AB.x * AC.y)/2;//Mathf.Abs(v23/2)+ Mathf.Abs(v43/2);
                                float S01 = Mathf.Abs(v23 / 2); //Vector3.Cross(AC, DA).sqrMagnitude;
                                float S02 = Mathf.Abs(v43 / 2);
                                if ((S01+S02)/S0 <= 1f + VectorExtension.MFactor) {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            private static bool isOuter(Vector2 a, Vector2 b, Vector2 c, List<Vector2> v){
				//Если треугольник, то всё ок, завершаем.
				if (v.Count == 3) {
					return true;
				}
				Vector2 A = a;
				Vector2 C = b;
				Vector2 B = c;
				foreach (Vector2 D in v){
					if(D!=A&&D!=B&&D!=C){
						Vector2 DC = D - C;
						Vector2 AC = A - C;
						Vector2 BC = B - C;

						float v23 = DC.y*AC.x-DC.x*AC.y;
						float v43 = DC.y*BC.x-DC.x*BC.y;

						float Dot1 = DC.x*AC.x+DC.y*AC.y;
						float Dot2 = DC.x*BC.x+DC.y*BC.y;

						float Cross1=0;
						float Cross2=0;
						if(v23!=0){
							Cross1 = Mathf.Sign (v23);
						}
						if(v43!=0){
							Cross2 = Mathf.Sign (v43);
						}
						bool exist=false;
						if(Dot1>=0&&Dot2>=0){//дб >=, потому что при 90 градусах будет =, но только 1 из Dot-ов сможет быть =0. Одновременно если, то 180 градусов будет мбду сторонами - не канает

							//					if (Dot1 < 0 && Dot2 < 0) {
							//						return false;
							//					} else {
							exist = Mathf.Abs (Cross1 - Cross2) > 0;
						}
						//Если относительно смежной диагонали точки исследуемая вершина и несмежная вершина лежат по разные стороны, то
						if (exist) {
							Vector2 AB = A - B;
							Vector2 DB = D - B;
							Vector2 CB = C - B;

							float v12 = AB.y*DB.x-AB.x*DB.y;
							float v32 = AB.y*CB.x-AB.x*CB.y;
							//Находим соотношения косинусов (знак интересует, д. б. >0)
							float Cross3=0;
							float Cross4=0;
							if(v12!=0){
								Cross3 = Mathf.Sign(v12);
							}
							if(v32!=0){
								Cross4 = Mathf.Sign(v32);
							}
							//Если смежная диагональ не пересекается с хотя бы 1й несмежной то вершина не выпуклая
							bool exist123 = false;
							exist123 = Mathf.Abs(Cross3-Cross4)>0;
							if (!exist123) {
								return false;
							}
							//Если относительно смежной диагонали точки: исследуемая вершина и несмежная вершина лежат на 1 полуплоскости, то
						}
					}
				}
				return true;
			}

			public int[] GetTrianglesByPoligonWithHoles(Vector3 norm, Vector3[] vs, List<List<int>> holes, List<int> exPnts){
				List<int> result = new List<int> ();
				//			List<Vector2> v = GetProjection(nrm,v1); //Получаем проекцию на плоскость
				return result.ToArray();
			}
		}

        public class Matrix3{
            public float[,] m = null;

            public Matrix3() {
                m = new float[3,3];
            }
            public Vector3 Mul(Vector3 v) {
                Vector3 result = new Vector3();
                float[] t = new float[3];
                for(int i = 0; i < m.GetLength(0); i++) {
                    for (int j = 0; j < m.GetLength(1); j++) {
                        t[i] += this.m[i, j] * v[j];
                    }
                    result[i] = t[i];
                }
                return result;
            }
        }

		public static class VectorExtension{
            public static Vector3 Vx1 = new Vector3(1, 0, 0);
            public static Vector3 Vy1 = new Vector3(0, 1, 0);
            public static Vector3 Vz1 = new Vector3(0, 0, 1);
            public const float cFactor = 0.0001f;//for find cross Pnt
            public const float rFactor = 0.0001f;// на 0.00001f ошибки
            public const float MFactor = 0.00001f;
            private static float rounder = 5f;//Округление необходимо корректировать (лучше динамически)

            public static Vector2 V2ProjectToVector(Vector2 vec, Vector2 vAxisProj) {
                return vAxisProj * (Vector2.Dot(vec, vAxisProj) / (vAxisProj.sqrMagnitude));
            }

			public static bool ApproxEqual (Vector3 v1, Vector3 v2){
				if (Mathf.Approximately (v1.x, v2.x)&&Mathf.Approximately (v1.y, v2.y)&&Mathf.Approximately (v1.z, v2.z)) {
					return true;
				}
				return false;
			}

            public static float UltAnglByVect(Vector3 v1, Vector3 v2) {
                return 180 - Vector3.Angle(v2, v1);
            }

            public static float GetFullAngle(Vector3 v1, Vector3 v2, Vector3 norm) {
                Vector3 cross = Vector3.Cross(v1, v2);
                float sign = Vector3.Dot(cross, norm);
                float angl = Vector3.Angle(v1, v2);
                if (sign > 0) {
                   angl= 360f - angl;
                }
                return angl;
            }
			public static Vector3 ProjectPntToLine(Vector3 A, Vector3 B, Vector3 P){
				Vector3 AB = (B-A);
				return A+(AB).normalized*Vector3.Dot (P-A, AB)/AB.magnitude;
			}

			public static bool SidesUnCrossed(Vector3 v1, Vector3 v2, List<Point> h){
				for(int i=0; i<h.Count-1;i++){
					if(Vect3Crossed(v1,v2,h[i].V, h[i+1].V)){
						return false;
					}
				}
				return true;
			}


			public static bool Vect3Crossed(Vector3 A, Vector3 B, Vector3 C, Vector3 D){
				Vector3 tmp1 = Vector3.Cross (B-A, B-C);
				Vector3 tmp2 = Vector3.Cross (B-A, B-D);
				if(Vector3.Dot (tmp1,tmp2)<0){// общая точка в вершине не будет считаться
					Vector3 tmp3 = Vector3.Cross (D-C, A-C);
					Vector3 tmp4 = Vector3.Cross (D-C, B-C);
					return Vector3.Dot (tmp3,tmp4)<0;// общая точка в вершине не будет считаться
				}
				return false;
			}

            public static bool IsCrossHorizontalRay(Vector2 p1, Vector2 p2) {
                Vector2 ray = Vector2.left;
                float f1 = Vector3.Cross(p1, ray).z;
                float f2 = Vector3.Cross(p2, ray).z;
                bool flgDiffHalfPlanes = (Mathf.Abs(Mathf.Sign(f1) + Mathf.Sign(f2)) < rFactor);
                bool positiveSumProject = Vector2.Dot(p1.normalized, ray) + Vector2.Dot(p2.normalized, ray) > 0;
//                return (((int)((Mathf.Sign(f1) + Mathf.Sign(f2)))>>32) < rFactor);
                return positiveSumProject && flgDiffHalfPlanes;
            }

			public static bool ApproxEqVec(ref Vector3 v1, ref Vector3 v2){
				float f = rFactor;
				if(Mathf.Abs (v1.x-v2.x)<f && Mathf.Abs (v1.y-v2.y)<f && Mathf.Abs (v1.z-v2.z)<f){
					return true;
				}
				return false;
			}
			public static Vector3 VecRound(Vector3 v, float r = 4.0f){
				return new Vector3(Round(v.x, r), Round(v.y, r), Round(v.z, r));
			}

			public static float OrtCrosVect(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4){
                Vector2 V01 = v2 - v1;
                Vector2 V02 = v4 - v3;
				float fff = V01.y*V02.x-V01.x*V02.y;
                if (fff<rFactor && fff>-rFactor){
					return 0f;
				}
				return Mathf.Sign(fff);
			}

            public static bool IsCrossInsidedVect(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4) {
                Vector2 V01 = v4 - v3;
                Vector2 V02 = v1 - v3;
                Vector2 V03 = v2 - v3;
                float fff1 = Vector3.Cross(V01, V02).z;
                float fff2 = Vector3.Cross(V01, V03).z;
                if (fff1 * fff2 < 0) {
                    V01 = v2 - v1;
                    V02 = v3 - v1;
                    V03 = v4 - v1;
                    fff1 = Vector3.Cross(V01, V02).z;
                    fff2 = Vector3.Cross(V01, V03).z;
                    return (fff1 * fff2 < 0);
                }
                return false;
            }

            public static bool IsOrthoVec(Vector3 v1, Vector3 v2){
                float curSign = Vector3.Dot(v1, v2);
                if (Mathf.Abs(curSign) < rFactor){ //for 45 degree 0.707
					return true;
				}
				return false;
			}


			public static bool IsParalDirVec(Vector3 v1, Vector3 v2){
				if((1-(Vector3.Dot(v1,v2)/(v1.magnitude*v2.magnitude)))< rFactor){
					return true;
				}
				return false;
			}

			public static float SignProjectV3toV3(Vector3 v1, Vector3 v2){
				return Round(Vector3.Dot (v1,v2), rounder);
			}
			public static bool IsParalVec2(Vector3 v1, Vector3 v2){
                float f = float.MinValue;//rFactor;
                float deltaX = v1.x / v2.x;
                float deltaY = v1.y / v2.y;
                float deltaZ = v1.z / v2.z;
                float delta1 = deltaX - deltaY;
                float delta2 = deltaX - deltaZ;
                if (delta1 < f && delta1 > -f && delta2 < f && delta2 > -f) { //Если отношения координат равны
					return true;
				}
				return false;
			}

			public static float Round(float f, float n){
				float p = Mathf.Pow (10, n);
				return (Mathf.Round (f*p))/p;
			}

            public static Vector3 Substitude(Vector3 v1, Vector3 v2) {
                float x = v1.x - v2.x;
                if (x < rFactor && x > -rFactor)
                    x = 0f;
                float y = v1.y - v2.y;
                if (y < rFactor && y > -rFactor)
                    y = 0f;
                float z = v1.z - v2.z;
                if (z < rFactor && z > -rFactor)
                    z = 0f;
                return new Vector3(x, y, z);
            }

            public static Vector3 VLerP(Vector3 n1, Vector3 n2, Vector3 v1, Vector3 v2, Vector3 v3) {
                Vector3 t1 = (v2 - v1);
                Vector3 t2 = (v3 - v1);
                Vector3 t3 = Vector3.Lerp(n1, n2, (t2.magnitude) / t1.magnitude);
                return t3;
            }

            public static Vector3 VLerP(Vector3 n1, Vector3 n2, Vector3 t1, Vector3 t2) {
                Vector3 t3 = Vector3.Lerp(n1, n2, (t2.magnitude) / t1.magnitude);
                return t3;
            }

            public static Vector2 VLerP2d(Vector2 uv1, Vector2 uv2, Vector3 v1, Vector3 v2, Vector3 v3) {
                Vector3 t1 = (v2 - v1);
                Vector3 t2 = (v3 - v1);
                Vector2 t3 = Vector2.Lerp(uv1, uv2, (t2.magnitude) / t1.magnitude);
                return t3;
            }
            public static Vector2 VLerP2d(Vector2 uv1, Vector2 uv2, Vector3 t1, Vector3 t2) {
                Vector2 t3 = Vector2.Lerp(uv1, uv2, (t2.magnitude) / t1.magnitude);
                return t3;
            }
            public static Vector4 VLerP4d(Vector4 tn1, Vector4 tn2, Vector3 v1, Vector3 v2, Vector3 v3) {
                Vector3 t1 = (v2 - v1);
                Vector3 t2 = (v3 - v1);
                Vector4 t3 = Vector4.Lerp(tn1, tn2, (t2.magnitude) / t1.magnitude);
                return t3;
            }
            public static Vector4 VLerP4d(Vector4 tn1, Vector4 tn2, Vector3 t1, Vector3 t2) {
                Vector4 t3 = Vector4.Lerp(tn1, tn2, (t2.magnitude) / t1.magnitude);
                return t3;
            }

            public static GameObject CreatePntGO(string s, Vector3 pnt){
				GameObject go = GameObject.CreatePrimitive (PrimitiveType.Sphere);
				go.name = s;
				go.transform.localScale *= 0.5f;
                go.transform.position = pnt;//  +new Vector3(-50, -20, 200);
                return go;
			}
		}
	}



    /*
     * НЕ ДОДЕЛАНО кеширование "Левой точки" для "внутренних" вырезов!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
     *  * Разбиения сложены на 3 этапа:
     * -Поиск точек пересечения
     * -Поиск изменённых форм (и колва) для пересечённых полигонов
     * -Триангуляция получившихся полигонов:
     * --Самый простой случай, когда внутренных отверстий нет в полигоне. Просто триангулируем по "ушной" системе
     * --Если хотя бы одна из получившихся фигур сечения входит полностью в исследуемый полигон( в том числе в один из получившихся полигонов, на которые он разбит), нужна триангуляция
     *   полигона с произвольными отверстиями, учитывающаяя в том числе граничные условия (вершина отверстия на грани исследуемого полигона, и вершина отверстия на вершине исследуемого полигона.
     *   При этом для фигур типа бублика возможна ситуация с инвертнутыми нормалями поверхности. Поэтому эти нормали необходимо учитывать при такой триангуляции.
     * Есть граничные случае. Когда точкаа пересечения лежит на стороне полигона или в вершине.
     * оба этих случая будут попадать в разрезы, пересекающиеся с границей.
     * отдельно считаются разрзы, никак не пересекающиеся с границей (триангуляция полигона в отверстиями).
     * 
     * 
     * Возможно, было бы потимальней применять связные списки в триангуляции, когда мы бегаем по замкнутой фигуре из оставшихся вершин, а так же при формировании сортированных
     * "секций".
     * 
     * В триангуляции и при вычислении "обходов" сторон для полигона с "дыркой" (ф-я GetArrWithHole2) тоже есть похожие алгоритмы, которые
     * выполняются несколько раз. Это можно попробовать тоже объединить, чтобы не было лишних повторяющихся пересчётов (я про поиск "уха" и выпуклой вершины многоугольника).
     * можно попробовать кэшировать.
     * 
     * Необходимо добавлять новые понятия, типа "Линия" и направления поверхности тела в ней,
     *  чтобы была возможность вычитать из полигона сложные фигуры типа структурных концентричных цилиндров
     * (имеется в виду, при не граничных пересечениях, а "дырочные варианты"
     * 
     * Остаются ошибки, связанные с погрешностью расчётов в результате алгебраических операция с float, коим являются координаты в Vector3
     * эти ошибки возникают в функцие нахождения точек пересечения. В результате погрешности плохо отрабатывается поиск в словаре CrossPnt.Net. Поскольку
     * он завязан на поиске идентичных (с некоторой точностью (пока 0.001f)) точек. Даже если словарь поправлю на <Vector3, CrossPnt>, он будет так же 
     * не работать из-за погрешностей при расчётах точек пересечения.
     * 
     */
}





