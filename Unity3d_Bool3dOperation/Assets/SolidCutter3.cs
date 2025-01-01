using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Diagnostics;

namespace Solid3{
	public class SolidCutter: MonoBehaviour{
		public static bool OptimazeMode = true;
        public static bool OldTrisLinksGetMode = false;

        private static Dictionary<Point, List<Poligon>>[] PoligonsByPoint = new Dictionary<Point, List<Poligon>>[2]; //0 - slised body, 1 - sliser body
        private static List<Point>[] LVects = new List<Point>[2];
//        private static int[][][][] crsBorders = new int[2][][][];//for control of adding crossPnt of two triangles //[bodyNum][elementOfTrianglesArray][numBorderThisTriang][numBorderOtherTriang]

        public static int kCount=0;
		public static int triCount1=0;
		public static int triCount2=0;
		public static int CountCross;
		public static int[] offset;
        public static int kk = 0;

        private static List<GameObject> ListGO = new List<GameObject>();



        public static Mesh DeductValue(Mesh m3_new, GameObject Basic, GameObject go, ref Material[] mats, ref PntsAttributes connectVerts){
            foreach(GameObject g in ListGO){
                Destroy(g);
            }
            ListGO.Clear();
//            VectorExtension.CreatePntGO("49.2, 72.5, 37.0", new Vector3(49.2f, 72.5f, 37.0f));
//            VectorExtension.CreatePntGO("52.6, 73.7, 39.9", new Vector3(52.6f, 73.7f, 39.9f));
            kk = 0;
			CountCross=0;
			int oldCntAperture = connectVerts.inds.Count;

			kCount=0;
			Mesh m1 = Basic.GetComponent<MeshFilter>().sharedMesh;
//			Mesh m3_new = m1;
            Mesh m2 = go.GetComponent<MeshFilter>().sharedMesh;

			Vector3[] v1 = m1.vertices;
			Vector3[] v2 = m2.vertices;
			Vector3[] n1 = m1.normals;
			Vector3[] n2 = m2.normals;
			Vector4[] tan1 = m1.tangents;
			Vector4[] tan2 = m2.tangents;
			Vector2[] uv1 = m1.uv;
			Vector2[] uv2 = m2.uv;


			v1 = TrmMshPntToWrdPnt (v1, Basic);
			v2 = TrmMshPntToWrdPnt(v2, go);

			for (int i=0; i< n2.Length; i++){
				n2[i] = go.transform.TransformDirection(n2[i]);
			}

			PoligonsByPoint[0] = new Dictionary<Point, List<Poligon>>();//Для отсечения лишний треугольников
            PoligonsByPoint[0].Clear();
            //Задел для обработки многоматериальных тел
            int subMshCnt1 = m1.subMeshCount;
			List<Poligon> TrisList1 = new List<Poligon>();
			Point[] Pnts1 = new Point[v1.Length];
            Point curPnt = null;
			for(int itri =0; itri<subMshCnt1; itri++){
                Dictionary<int, Point> newPntsMaker = new Dictionary<int, Point>();
                int[] tri_ = m1.GetTriangles(itri);
				for(int i=0;i<tri_.Length;i+=3){
                    List<Point> pnts = new List<Point>();
                    bool[] flgBoost = new bool[] { false, false, false }; //for acceleration of Adding AddToBodyTriangles
                    for (int z = 0; z < 3; z++) {
                        int j = tri_[i+z];
                        if (newPntsMaker.ContainsKey(j)){
                            curPnt = newPntsMaker[j];
                            flgBoost[z] = true;
                        } else {
                            curPnt = new Point(true, v1[j], n1[j], j, itri, uv1[j], tan1[j]);
                            Pnts1[j] = curPnt;
                            newPntsMaker.Add(j, curPnt);
                        }
                        pnts.Add(curPnt);
                    }
					Poligon newPoligon  = new Poligon(pnts, itri, true);
					newPoligon.AddToBodyTriangles(ref PoligonsByPoint[0], flgBoost);
					TrisList1.Add(newPoligon);
                }
            }
            LinksCreatorPar newLinks = null;
            if (OldTrisLinksGetMode) {
                newLinks = new LinksCreatorPar(connectVerts);
				for (int i = 0; i < connectVerts.externalPnts.Length; i++) {
					for (int j = 0; j < connectVerts.externalPnts [i].Count; j++) {
						int z = connectVerts.externalPnts [i] [j];
						Pnts1 [z].IsAroundSidePnt = true;
						Pnts1 [z].OldIndxs = new int[]{ i, j }; //for save order in lists
					}
				}
                for (int i = 0; i < connectVerts.inds.Count; i++) {
                    for (int j = 0; j < connectVerts.inds[i].Length; j++) {
                        for (int z = 0; z < connectVerts.inds[i][j].Count; z++) {
                            int tmpInd = connectVerts.inds[i][j][z]; //kill link
                            Pnts1[tmpInd].IsUsedByTranslate = true;
                            Pnts1[tmpInd].OldApertNum = i;
                            Pnts1[tmpInd].OldIndxs = new int[] { j, z }; //for save order in lists
                        }
                    }
                }
            }

			PoligonsByPoint[1] = new Dictionary<Point, List<Poligon>>();//Для отсечения лишний треугольников
			int subMshCnt2 = m2.subMeshCount;
			List<Poligon> TrisList2 = new List<Poligon>();
			Point[] Pnts2 = new Point[v2.Length];
			for(int itri =0; itri<subMshCnt2; itri++){
                Dictionary<int, Point> newPntsMaker = new Dictionary<int, Point>();
                int[] tri_ = m2.GetTriangles(itri);
				for(int i=0;i<tri_.Length;i+=3){
                    List<Point> pnts = new List<Point>();
                    bool[] flgBoost = new bool[] { false, false, false }; //for acceleration of Adding AddToBodyTriangles
                    for (int z = 0; z < 3; z++) {
                        int j = tri_[i+z];
                        if (newPntsMaker.ContainsKey(j)) {
                            curPnt = newPntsMaker[j];
                            flgBoost[z] = true;
                        } else {
                            curPnt = new Point(true, v2[j], -n2[j], itri, j, uv2[j], tan2[j]);
                            Pnts2[j] = curPnt;
                            newPntsMaker.Add(j, curPnt);
                        }
                        pnts.Add(curPnt);
                    }
					Poligon newPoligon  = new Poligon(pnts, itri, false);
					newPoligon.AddToBodyTriangles(ref PoligonsByPoint[1], flgBoost);
					TrisList2.Add(newPoligon);
                }
            }

            triCount1 = TrisList1.Count;
			triCount2 = TrisList2.Count;

			//Создаём единичную матрицу, соответсвующую трансформу без поворотов со скейлом = 1
			Matrix4x4 MT  = new Matrix4x4();
			MT[0,0] = 1;
			MT[1,1] = 1;
			MT[2,2] = 1;
			MT[3,3] = 1;

            //CountCross=0;
            bool flgIsCrossed = false;
			ResultInfo Ri = GetMeshInterSectGO(TrisList1, TrisList2, subMshCnt1, subMshCnt2, newLinks, ref flgIsCrossed);
			if (flgIsCrossed) {

                LinksCreator listLinks = new LinksCreator ();
                listLinks.OldInds = connectVerts.inds;//= oldInds;
                listLinks.ExternPnts = connectVerts.externalPnts;

                m3_new = Ri.bodyMsh.MeshRefresh(m3_new);
                Vector3[] newV1 = m3_new.vertices;
				newV1 = TrmWrdPntToMshPnt (newV1, Basic);//, roundScale);
				m3_new.vertices = newV1;
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

		public static ResultInfo GetMeshInterSectGO(List<Poligon> t1, List<Poligon> t2, int subMshCnt1, int subMshCnt2, LinksCreatorPar links, ref bool flgIsCrossedBody) {
            /*
             * Можно сразу отслеживать отсечённые треугольники попробовать....
             * И если они уже отсечены, с ними расчётов не проделывать!!!!!!!!!!
             */
            LVects[0] = new List<Point>();
            LVects[1] = new List<Point>();
            int allSubMshCnt = subMshCnt1 + subMshCnt2;
            Body newBody = new Body(allSubMshCnt, OptimazeMode, links);
            bool flgSide = true;//body 1
            for (int i = 0; i < t1.Count; i++) {
                kk++;
                for (int j = 0; j < t2.Count; j++) {

                    t1[i].CrossToPoligon(t2[j], i, j);
                    flgIsCrossedBody = flgIsCrossedBody || t1[i].Nets.Count > 0;
                }

                if (flgIsCrossedBody) {
                    GetCrossedFaces(flgSide, 0, 0, t1[i], ref newBody);
                }

            }
			if (flgIsCrossedBody) {
                GetOnCrossedFaces(flgSide, 0, 0, ref newBody);


                //Body 2
                flgSide = false;
	            for (int i = 0; i < t2.Count; i++) {
                    GetCrossedFaces(flgSide, 1, subMshCnt1, t2[i], ref newBody);
                }
                GetOnCrossedFaces(flgSide, 1, subMshCnt1, ref newBody);

            }
            ResultInfo RI = new ResultInfo(newBody.BodyGenerate());

            return RI;
		}

        private static void GetCrossedFaces(bool flgSide, int numBody, int offsetSubMshNum, Poligon pol, ref Body nBody) {
            if (pol.NetKeys.Count > 0) {
                List<Point> SegsOfIntersect = pol.NetKeys;
                Holes SegsOfIntsct = SortSegs(SegsOfIntersect, pol);

                List<Point>[] PerTriPnts = null;
                if (SegsOfIntsct.BorderHoles.Count > 0) {
                    PerTriPnts = GetPerimetrPnts(pol, SegsOfIntsct.BorderHoles, flgSide, numBody);
                }
                List<Point>[] PerTriPntsHoles = null;
                if (SegsOfIntsct.InnerHoles.Count > 0) {
                    if (SegsOfIntsct.BorderHoles.Count == 0) {
                        PerTriPntsHoles = GetArrWithHole(SegsOfIntsct.InnerHoles.ToArray(), pol, flgSide, numBody);
                    }else {

                    }
                    for (int j = 0; j < PerTriPntsHoles.Length; j++) {
                        for (int z = 0; z < PerTriPntsHoles[j].Count; z++) {
                            ListGO.Add(VectorExtension.CreatePntGO(j.ToString() + "_" + z.ToString(), PerTriPntsHoles[j][z].V));
                        }
                    }
                }

                int BordCount = -1;
                if (PerTriPnts != null) {
                    BordCount = PerTriPnts.Length;
                }
                for (int ii = 0; ii < BordCount; ii++) {
                    if (PerTriPnts[ii].Count > 0) {
                        PerTriPnts[ii].RemoveAt(0);
                        List<Vector3> tmpVectors = Point.GetListVector3(PerTriPnts[ii]);//PerTriPnts[ii].Select(info => info.V).ToList();//Можно сразу формировать массив в функциях обработки....!!!!
                        List<int> tris = Triangulation.GetTriangles(tmpVectors, 0, pol.Norm);
                        pol.IsCrossed = true;
                        int indx = pol.IndSubMsh + offsetSubMshNum;
                        nBody.AddNewPoligonAttributes(indx, tris, PerTriPnts[ii]);
                    }
                }
                int HoleCount = -1;
                if (PerTriPntsHoles != null) {
                    HoleCount = PerTriPntsHoles.Length;
                }
                for (int ii = 0; ii < HoleCount; ii++) {
                    if (PerTriPntsHoles[ii].Count > 0) {
                        PerTriPntsHoles[ii].RemoveAt(0);
                        if (!flgSide) {
                            PerTriPntsHoles[ii].Reverse();
                        }
                        List<Vector3> tmpVectors = Point.GetListVector3(PerTriPntsHoles[ii]);//PerTriPntsHoles[ii].Select(info => info.V).ToList();
                        List<int> tris = Triangulation.GetTriangles(tmpVectors, 0, pol.Norm);
                        pol.IsCrossed = true;
                        int indx = pol.IndSubMsh + offsetSubMshNum;
                        nBody.AddNewPoligonAttributes(indx, tris, PerTriPntsHoles[ii]);
                    }
                }
            }
        }

        private static void GetOnCrossedFaces(bool flgSide, int numBody, int offsetSubMshNum, ref Body nBody) {
            Point[] vBad = LVects[numBody].ToArray();
            while (vBad.Length > 0) {
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
                    if (flgSide) {
                        tris = new List<int>(new int[] { 0, 1, 2 });
                    } else {
                        tris = new List<int>(new int[] { 0, 2, 1 });
                    }
                    int indx = pols[i].IndSubMsh + offsetSubMshNum;
                    nBody.AddNewPoligonAttributes(indx, tris, pols[i].Pnts);
                }
            }
        }

		public static Point[] NxtvBads(Point[] v, int indBody){
			List<Point> tmpLV = new List<Point>();
            Dictionary<Point, List<Poligon>> curDict = PoligonsByPoint[indBody];
            for (int i = 0; i<v.Length;i++){
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
			return tmpLV.ToArray ();
		}

		public static Holes SortSegs(List<Point> LastS, Poligon pol){
            //При таком подходе не обязательно иметь список пар точек... поскольку ссылки уже в словаре !!!!!!!
            bool flgHole = false;
			Holes Hol = new Holes();
            List<int> indxsGroup = null;
            int curPosition = -1;
            int offsetCntr = -1;
			List<Point> Seq = new List<Point>();
			do{
                indxsGroup = new List<int>();
                curPosition = 0;
                offsetCntr = 0;
                flgHole = false;
                Point nxtPnt = LastS[0];
//				Seq.Add (nxtPnt);
                AddToSeq(1, nxtPnt, ref Seq, ref indxsGroup, ref curPosition, ref offsetCntr);
                LastS.RemoveAt(0); //LastS.Remove(nxtPnt); //191208
				List<Point> bonds = GetnxtPnts(nxtPnt, pol, true);
				if(bonds!=null){
					bool flgBordered = nxtPnt.IsBorderedPnt;//Возможно, не хватает проверки на точках bounds[i]
					int bndCnt = bonds.Count;
					if(bndCnt>0){
						//nxtbonds = список списков на случай, когда в 1 точке стыкуются больше 2-х линий (пока не продумано)
						List<List<Point>> nxtbonds = new List<List<Point>>();
						for(int i=0; i<bndCnt;i++){
                            AddToSeq(i, bonds[i], ref Seq, ref indxsGroup, ref curPosition, ref offsetCntr);
                            if (!flgBordered){
								flgBordered = bonds[i].IsBorderedPnt;
							}
							LastS.Remove(bonds[i]);
							List<Point> tmpLbonds = new List<Point>();
							tmpLbonds.Add (bonds[i]);
							nxtbonds.Add(tmpLbonds);
							DelonDictNxtPnt(nxtPnt, bonds[i], pol);//Чистим словарь
						}
						Point[] LastPnt = new Point[]{nxtPnt, nxtPnt};//Предыдущая точка
						bool flgGoNxt=false;
						do{
							for(int i = 0; i<nxtbonds.Count;i++){
								if(nxtbonds[i].Count>0){
									//Пока не нашли граничной точки, делаем проверку.
									if(!flgBordered){
										flgBordered = nxtbonds[i][0].IsBorderedPnt;
									}
									DelonDictNxtPnt(nxtbonds[i][0], LastPnt[i], pol);//Чистим словарь
									LastPnt[i] = nxtbonds[i][0];//Предыдущая точка
									nxtbonds[i] = GetnxtPnts(nxtbonds[i][0], pol, false);
								}
								if(nxtbonds[i].Count>0){
									if(!flgGoNxt){
                                        AddToSeq(i, nxtbonds[i][0], ref Seq, ref indxsGroup, ref curPosition, ref offsetCntr);
                                        LastS.Remove(nxtbonds[i][0]);
										DelonDictNxtPnt(LastPnt[i], nxtbonds[i][0], pol);//Чистим словарь
									}
									//Отделяем дырки, не пересекающие границу
									if(nxtbonds.Count>1){
										if(nxtbonds[i].Count>0){
											if(nxtbonds[nxtbonds.Count-1-i].Count>0){
												if((nxtbonds[i][0] == nxtbonds[nxtbonds.Count-1-i][0])){
													flgGoNxt=true;
                                                    flgHole = true;
                                                    break;
												}
											}
										}
									}
								}
								//Отделяем дырки, пересекающие границу
								if(nxtbonds[i].Count==0&&nxtbonds[nxtbonds.Count-1-i].Count==0){
									flgGoNxt=true;
									break;
								}
							}
						}while(!flgGoNxt);
						for(int i = 0; i<nxtbonds.Count;i++){
							if(nxtbonds[i].Count>0){
								DelonDictNxtPnt(nxtbonds[i][0], LastPnt[i], pol);//Чистим словарь
								if(!flgBordered){
									flgBordered = nxtbonds[i][0].IsBorderedPnt;
								}
							}
						}
					} 
					//Добавляем в список "дырок" список точек контура очередной "дырки"
					Point[] tSeq = new Point[Seq.Count];
					List<Point> tlSeq = new List<Point>();
					Seq.CopyTo(tSeq);
					tlSeq.AddRange(tSeq);
                    //					if(flgBordered){

                    if (indxsGroup.Count == 0) {
                        indxsGroup = null;
                    }else {
                        for(int i=0; i< indxsGroup.Count; i++) {
                            indxsGroup[i] += offsetCntr;
                        }
                        pol.bHoles.indexCrossBorderPntsInner.Add(indxsGroup);
                    }

                    if (!flgHole) {
						pol.bHoles.BorderHoles.Add(tlSeq);
					}
					else{
                        pol.bHoles.InnerHoles.Add(tlSeq);
					}
					Seq.Clear();
				}
			}while(LastS.Count>0); 

			return pol.bHoles;
		}

        private static void AddToSeq(int i, Point pnt, ref List<Point> Seq, ref List<int> indxsGroup, ref int curPosition, ref int offsets) {
            if (i == 0) {
                Seq.Insert(0, pnt);
                offsets++;
            } else {
                Seq.Add(pnt);
            }
            if (pnt.IsCrossBorderedPnt) {//write start position
                indxsGroup.Add(curPosition);
                curPosition++;
            }
        }

		//FirstPnt возможно они и не нужен!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		public static List<Point> GetnxtPnts(Point pnt, Poligon pol, bool FirstPnt){
			List<Point> pnts = new List<Point>();
			int indxList= -1;
			if(pol.Nets.ContainsKey(pnt)){
				indxList = pol.Nets[pnt];
                pnts = new List<Point>(pol.NetLinks[indxList]); //!!!!!!!!!!!! или заменить на pnts.AddRange(pol.NetLinks[indxList])????
                if (pnts.Count>2){
					pnts = GetSolidBond(pnts, pol.Norm);
				}
				else{
					float fff=0;
				}
			}
			return pnts;
		}
		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Необходимо будет править
		public static List<Point> GetSolidBond(List<Point> AllPnts, Vector3 NormTri){
			List<Point> tmp = new List<Point>();
			for (int i = 0; i < AllPnts.Count; i++) {//191208
				if(VectorExtension.IsParalDirVec(AllPnts[i].N, NormTri)){
					tmp.Add (AllPnts[i]);
				}
			}
			//			tmp.Add (AllPnts[0]);
			//			tmp.Add (AllPnts[1]);
			return tmp;
		}

		public static void DelonDictNxtPnt(Point cPnt, Point lastcPnts, Poligon pol){
			//Скорее всего эту проверку на содержание ключа можно убрать!!!!!!!!!!!!!!!!
			if(pol.Nets.ContainsKey(cPnt)){
				int indxList = pol.Nets[cPnt];
				List<Point> netLinks = pol.NetLinks[indxList];
				netLinks.Remove(lastcPnts);
				//HashSets deleting is not necessary
				//Attantion. Not delete element of pol.NetLinks
				//It will be to impact to index of List<point> in it
				if(netLinks.Count==0){
                    pol.Nets.Remove(cPnt);
				}
			}
		}

		public static float AreaOfTriangleByV(Vector3 v1, Vector3 v2, Vector3 v3){
            return Vector3.Cross(v2 - v1, v3 - v1).magnitude;
		}

        public static List<Point>[] GetPerimetrPnts (Poligon pol, List<List<Point>> BordPnts, bool flg, int indBody){
            int pntsCnt = pol.Pnts.Count;
            List<Point> StaydTri = new List<Point>(pol.Pnts);
			Point[] triCleaner = new Point[pntsCnt+1];
            pol.Pnts.CopyTo(triCleaner);//Ввожу дополнительный массив, которым буду удалять всё.
            triCleaner[pntsCnt]= triCleaner[0];
            Point[] tri = new Point[]{pol.Pnts[0], pol.Pnts[1], pol.Pnts[2], pol.Pnts[0]};//данный массив в этой функции изменяться не должен
			List<List<Slice>> LSlices = new List<List<Slice>>();

			List<List<Point>> LPerPnts = new List<List<Point>>();//объект (треугольник) может быть разбит на несколько подобъектов (многоугольников)

			List<List<float>> LDisnts = new List<List<float>>();//Сортировку делаем только по ближайшим точкам
			Dictionary<float, List<Slice>>[] dDistToSlise = new Dictionary<float, List<Slice>>[tri.Length-1];//По расстояниям получаем объект среза
			Dictionary<Point, int> dPntToSideTri = new Dictionary<Point, int>();//Ссылка по точке на сторону треугольника

			for (int i=0; i< tri.Length-1;i++){	
				dDistToSlise[i] = new Dictionary<float, List<Slice>>();//Инициируем все словари, они нам все сразу нужны будут даже в первом цикле
				LDisnts.Add (new List<float>());
			}

			for (int i=0; i< tri.Length-1;i++){	//Цикл по всем точкам данного треугольника
				for(int k=BordPnts.Count-1; k >= 0;k--){  //Идем циклом по всем граничным "срезам" (отдельным линиям пересечения, пересекающим границу дан. треугольника)
					int cntPnts = (BordPnts[k].Count-1);//Индекс последней точки в текущем срезе
					int ck = 0;
					List<Point> ListPnts = new List<Point>();//Список начальной и конечной точки
					for(int j=0; j<2;j++){ //Берем начальную и конечную точки граничного "среза"
                        for (int z = 0; z < BordPnts[k][j * cntPnts].Side.Count; z++) {
                            if (i == BordPnts[k][j * cntPnts].Side[z]) {//Если точка "среза" на текущей стороне треугольника
                                ck++;
                                ListPnts.Add(BordPnts[k][j * cntPnts]);
                            }
                            if (BordPnts[k][j * cntPnts].Side[z] < 0) {
                                string sss = "123";
                            }
                        }
					}
					if(ck>0){//Если точек  на границе > 0
						Vector3[] triVec = new Vector3[]{tri[0].V,tri[1].V,tri[2].V,tri[3].V};
						Slice SL = NearPoint1(triVec, ListPnts, BordPnts[k], i, BordPnts);
						if (SL == null || LDisnts[i]==null) {
							string sss = "123";
                            /*
                            for (int z = 0; z < BordPnts.Count; z++) {
                                if (BordPnts[z].Count > 1) {
                                    for (int zz = 0; zz < BordPnts[z].Count - 1; zz++) {
                                        ActionLines.DrawLine(BordPnts[z][zz].V, BordPnts[z][zz + 1].V, Color.green);
                                        VectorExtension.CreatePntGO(BordPnts[z][zz].IsBorderedPnt.ToString(), BordPnts[z][zz].V);
                                    }
                                    VectorExtension.CreatePntGO(BordPnts[z][BordPnts[z].Count - 1].V.ToString(), BordPnts[z][BordPnts[z].Count - 1].V);
                                }
                            }
                            for(int z =0; z<pol.Pnts.Count; z++) {
                                VectorExtension.CreatePntGO(pol.Pnts[z].ToString(), pol.Pnts[z].V);
                            }
                            */
                        }
                        LDisnts[i].Add(SL.d1);
						if(dDistToSlise[i].ContainsKey(SL.d1)){
							dDistToSlise[i][SL.d1].Add (SL);
						}else{
							List<Slice> tmpSlice = new List<Slice>();
							tmpSlice.Add (SL);
							dDistToSlise[i].Add (SL.d1, tmpSlice);
						}
						if(!SL.p2.isUsedPair){
							SL.p2.isUsedPair=true;
						}
						if (ck>1){
							SL.p1.isPaired = true;
							SL.p2.isPaired = true;
						}
					}
				}
				//Упорядычиваем в списке текущего расстояния все  линии из 1й точки (по их угловому отклонению от стороны треугольника)
				foreach(float j in dDistToSlise[i].Keys){
					if(dDistToSlise[i][j].Count>1){
						dDistToSlise[i][j] = MultySliceSort(dDistToSlise[i][j], tri[i+1].V-tri[i].V);
					}
				}
				//Сортируем всё в LSlices
				LDisnts[i].Sort();
				for (int x=0; x<LDisnts[i].Count;x++){
					LSlices.Add (dDistToSlise[i][LDisnts[i][x]]);
				}
			}

			List<Point> tmpPnts = new List<Point>();
			int cnt=0;
			FstPnt FirstPnt;
			FirstPnt.v = new Point(flg, new Vector3(), new Vector3());
			FirstPnt.n = new Vector3();
			FirstPnt.triNumb = -1;

			while (LSlices.Count>0&&cnt>-1){ //&& cnt<LSlices.Count){
				bool reverced=false;
				Vector3 v1 = tri[LSlices[cnt][0].i1].V;
				Vector3 v2 = tri[LSlices[cnt][0].i1+1].V;

				int tmpflgSign=0;
				if(flg){
					tmpflgSign=1;
				}
				else{
					tmpflgSign=-1;
				}
				if((tmpflgSign * VectorExtension.SignProjectV3toV3(v2-v1, LSlices[cnt][0].p1.CutterNorm))>0){
					//Идём по прямой дальше
					if(FirstPnt.triNumb==-1){
						//Если точка первая, добавим начальную вершину треугольника
						tmpPnts.Add (tri[LSlices[cnt][0].i1]);

						//Удаляем из списка удалённых вершин треугольника
						StaydTri.Remove(tri[LSlices[cnt][0].i1]);

						FirstPnt.triNumb = LSlices[cnt][0].i1;
						FirstPnt.v = tmpPnts[0];
					}
					tmpPnts.AddRange (LSlices[cnt][0].lst.Select (info => info).ToList());

					//Удаляем из списка дистанций текущую дистанцию
					LDisnts[LSlices[cnt][0].i1].Remove(LSlices[cnt][0].d1);
				}else{
					//Вставляем точки сзади (до точки лежащей на другой стороне)

					LSlices[cnt][0].lst.Reverse();
					reverced=true;

					tmpPnts.InsertRange (0,LSlices[cnt][0].lst.Select (info => info).ToList());

					if(FirstPnt.triNumb==-1){
						//Если точка первая, добавим начальную вершину треугольника
						FirstPnt.triNumb = LSlices[cnt][0].i1;
						FirstPnt.v = tmpPnts[0];
					}

					int itemp = LSlices[cnt][0].i2;//i1 Ищу вершину до первой точки
					float tmpF = NearestPrevSecondPnt(LDisnts[itemp].ToArray(),LSlices[cnt][0].d2);
					if(tmpF>0){
						tmpPnts.Insert(0, dDistToSlise[itemp][tmpF][0].p1);
						//Удаляем из списка дистанций текущую дистанцию
						LDisnts[LSlices[cnt][0].i2].Remove(tmpF);
					}else{
						tmpPnts.Insert(0, tri[LSlices[cnt][0].i2]);

						//Удаляем из списка оставшихся вершин треугольника
						StaydTri.Remove(tri[LSlices[cnt][0].i2]);
					}
				}

				if(VectorExtension.ApproxEqual(tmpPnts[0].V, tmpPnts.Last ().V)){
					tmpPnts[0] = tmpPnts.Last ();

					NxtList(tmpPnts, LPerPnts);
					cnt=0;

					FirstPnt.triNumb = -1;

					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					int tmpdd2 = LSlices[cnt][0].i2;
					float tmpff_2 = LSlices[cnt][0].d2;
					List<Slice> ddd2 = null;
					if(dDistToSlise[tmpdd2].ContainsKey(tmpff_2)){
						ddd2 = dDistToSlise[LSlices[cnt][0].i2][LSlices[cnt][0].d2];
						LSlices.Remove(ddd2);
					}
					//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
					LSlices.RemoveAt(cnt);
					continue;
				}

				int itemp_1 = 0;
				int itemp_2 = 0;
				float d_1 = 0f;
				float d_2 = 0f;

				if(!reverced){
					itemp_1 = LSlices[cnt][0].i1;
					itemp_2 = LSlices[cnt][0].i2;
					d_1 = LSlices[cnt][0].d1;
					d_2 = LSlices[cnt][0].d2;
					v1 = tri[LSlices[cnt][0].i2].V;
					v2 = tri[LSlices[cnt][0].i2+1].V;
				}else{
					itemp_1 = LSlices[cnt][0].i2;
					itemp_2 = LSlices[cnt][0].i1;
					d_1 = LSlices[cnt][0].d2;
					d_2 = LSlices[cnt][0].d1;
					v1 = tri[LSlices[cnt][0].i1].V;
					v2 = tri[LSlices[cnt][0].i1+1].V;
				}
				//Нельзя искать только пока LSlices.Count>0, поскольку если разбито на несколько полигонов, они все ещё останутся

				//Попробуем найти следующую точку на данной стороне
				float tmpFf =0;

//				Vector3 Norm  = LSlices[cnt][0].p2.N;

				//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				int tmpdd = LSlices[cnt][0].i2;
				float tmpff_ = LSlices[cnt][0].d2;
				List<Slice> ddd = null;
				if(dDistToSlise[tmpdd].ContainsKey(tmpff_)){
					ddd = dDistToSlise[LSlices[cnt][0].i2][LSlices[cnt][0].d2];
					LSlices.Remove(ddd);
				}
				//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				if(LSlices.Count>0){
					LSlices.RemoveAt(cnt);
				}

				tmpFf = NearestSecondPnt(LDisnts[itemp_2].ToArray(),d_2);
				if(tmpFf==0f){//LDisnts[itemp1].Count==0){
					int itemp2 = itemp_2+1;
					do{
						if(itemp2>2){
							itemp2 = 0;//Замыкаем по кругу
						}
						//Добавляем первую точку, если мы вернулись на ту же сторону и на ней не осталось срезов
						if(FirstPnt.triNumb==itemp2 && LDisnts[itemp2].Count==0){
							tmpPnts.Add (FirstPnt.v);
							break;
						}
						//Добавляем следующую вершину треугольника
						tmpPnts.Add(tri[itemp2]);

						//Удаляем из списка оставшихся вершин треугольника
						StaydTri.Remove(tri[itemp2]);

						if(LDisnts[itemp2].Count>0){
							cnt = LSlices.IndexOf (dDistToSlise[itemp2][LDisnts[itemp2][0]]);
							break;
						}
						//Если точки на стороне закончились, идём на следующую сторону
						itemp2++;
					}while(tmpPnts[0]!=tmpPnts.Last());
				}else{
					//Иначе добавляем следующие точки
					cnt = LSlices.IndexOf (dDistToSlise[itemp_2][tmpFf]);
				}

				//отправляем список в список списков (как следующий элемент) и начинаем по новой

				if(VectorExtension.ApproxEqual(tmpPnts[0].V, tmpPnts.Last ().V)){
					tmpPnts[0] = tmpPnts.Last ();
					NxtList(tmpPnts, LPerPnts);
					cnt=0;
				}
			}

			for(int i=0; i<StaydTri.Count;i++){
				FillUpVTries(StaydTri[i], triCleaner, indBody);
			}
			DelCurTriangle(pol, indBody);
			NxtList(tmpPnts, LPerPnts);
			return LPerPnts.ToArray();
		}

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

		public static List<Slice> MultySliceSort(List<Slice> listSl, Vector3 sideTr){
			List<float> listAngle = new List<float>();
			Dictionary<float, Slice> dAnglToSlice = new Dictionary<float, Slice>();
			for(int i=0; i<listSl.Count; i++){
				float Angl = UltAnglByVect(listSl[i].p2.V-listSl[i].p1.V, sideTr);
				listAngle.Add (Angl);
				dAnglToSlice.Add (Angl, listSl[i]);
			}
			listAngle.Sort();
			List<Slice> OutListSlice = new List<Slice>();
			for(int i=0; i<listAngle.Count;i++){
				OutListSlice.Add (dAnglToSlice[listAngle[i]]);
			}
			return OutListSlice;
		}

		public static float UltAnglByVect(Vector3 v1, Vector3 v2){
			return 180-Vector3.Angle(v2,v1);
		}

		public static void NxtList(List<Point> lst, List<List<Point>> listlstPnts){
			List <Point> tmppVec = new List<Point>(lst);
			listlstPnts.Add (tmppVec);
			lst.Clear();
		}

		//Функция нахождения ближайшего расстояния из словаря (поиск рядом стоящей первой точки другого среза по второй точке текущего)
		public static float NearestSecondPnt(float[] ddist, float Slice2Dist){
			float min=0;
			float curVal=0;
			for(int i=0; i<ddist.Length; i++){
				float tmpf = ddist[i]-Slice2Dist;
				if(min<tmpf && tmpf>0){
					min = tmpf;
					curVal = ddist[i];
					break;
				}
			}
			return curVal;
		}
		//Функция нахождения ближайшего расстояния из словаря (поиск рядом стоящей первой точки другого среза по второй точке текущего)
		public static float NearestPrevSecondPnt(float[] ddist, float Slice2Dist){
			float curVal=0;
			for(int i=0; i<ddist.Length; i++){
				float tmpf = ddist[i]-Slice2Dist;
				if(tmpf>=0){
					break;
				}else{
					curVal = ddist[i];
				}
			}
			return curVal;
		}

		public static Slice NearPoint1(Vector3[] v, List<Point> PntsOnCurSide, List<Point> lstCrPnt, int j, List<List<Point>> sdsd){
			List<float> LDistance = new List<float>();
			int j1=j;
			int j2=j;
			Dictionary<float, Point> dDistances = new Dictionary<float, Point>();
			for(int i=0; i< PntsOnCurSide.Count;i++){
				float fDict = (PntsOnCurSide[i].V-v[j]).sqrMagnitude;
				LDistance.Add (fDict);
				if(!dDistances.ContainsKey(fDict)){
					dDistances.Add (fDict, PntsOnCurSide[i]);
				}
			}
			LDistance.Sort ();

			//Создаём направление
			if(dDistances[LDistance[0]]!=lstCrPnt[0]){
				List<Point> lstCrPntRev = new List<Point>(lstCrPnt);
				lstCrPntRev.Reverse();
				lstCrPnt = lstCrPntRev;
			}
			if(PntsOnCurSide.Count==1){//Если на текущей стороне треугольника только одна точка
                Point tmpSecPnt = lstCrPnt[lstCrPnt.Count-1];//another end pnt (border pnt)
				for (int i=0; i< v.Length-1;i++){	//Цикл по всем точкам данного треугольника
                    for (int z = 0; z < tmpSecPnt.Side.Count; z++) {
                        if (i == tmpSecPnt.Side[z]) {//Если точка "среза" на текущей стороне треугольника
                            LDistance.Add((v[i] - tmpSecPnt.V).sqrMagnitude);//Добавляем в Slice расстояние 2й точки среза до соотвествующей точки соотвествующей стороны треугольника
                            dDistances.Add(LDistance.Last(), tmpSecPnt);
                            j2 = i;
                        } else if (tmpSecPnt.Side[z] < 0 || tmpSecPnt.Side[z] > v.Length - 1) {
                            Debug.Log("Do not catch nearPnt");
                        }
                    }
				}
			}
			Slice tmpSl=null;
			try{
				tmpSl = new Slice(dDistances[LDistance[0]], dDistances[LDistance[1]], LDistance[0], LDistance[1], lstCrPnt, j1, j2);
			}
			catch(System.ArgumentOutOfRangeException e){
				/*
				for(int i=0; i<sdsd.Count;i++){
					for(int jj = 0; jj< sdsd[i].Count-1;jj++){
						ActionLines.DrawLine(sdsd[i][jj].Pnt,sdsd[i][jj+1].Pnt, Color.red);
					}
				}
				ActionLines.DrawLine(v[0], v[1], Color.yellow);
				ActionLines.DrawLine(v[1], v[2], Color.yellow);
				ActionLines.DrawLine(v[2], v[0], Color.yellow);
				for(int i = 0; i<lstCrPnt.Count-1;i++){
					ActionLines.DrawLine(lstCrPnt[i].Pnt, lstCrPnt[i+1].Pnt, Color.red);
				}
*/
				Debug.Log (e.ToString());
			}

			return tmpSl;
		}

		public static Vector3[] TrmWrdPntToMshPnt (Vector3[] v, GameObject go){//, float f){
			//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
				v[i] = go.transform.InverseTransformPoint(v[i]);
			}
			return v;
		}

		public static Vector3[] TrmMshPntToWrdPnt (Vector3[] v, GameObject go){//, float f){
			//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
				v[i] = go.transform.TransformPoint(v[i]);
			}
			return v;
		}

		public static List<Point>[] GetArrWithHole(List<Point>[] HolesPnts, Poligon pol, bool flg, int indBody){
            if (flg){
                DelCurTriangle(pol, indBody); //If Slised body
            }
            else{
                LVects[1].Add(pol.Pnts[0]); // if Sliser body
            }
            List<Point>[] Result = new List<Point>[] { new List<Point>(), new List<Point>() };
//            List<Point>[] Result = new List<Point>[2 * HolesPnts.Length];!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (flg) {
                for (int i = 0; i < HolesPnts.Length; i++) {
//                    PntsV[i].RemoveAt(PntsV[i].Count - 1);//Нет нужны, поскольку благодаря тому, что теперь Pnts лишние не создаются, корректно равенство происходит в SortSegs (не формируется лишней точки)
                    //Для начала, найдём "ушную" вершину и определим правильный обход вырезу (путём нахождения ближайшей вершины к вершине треугольника)
                    int Lft = 0;
                    float dist = 0;

                    List<int> IsCrossBorders = new List<int>();//////////////////////////////it is necessary ???!!!

                    for (int ii = 0; ii < HolesPnts[i].Count - 1; ii++) {
                        if (i == 0) {
                            if (HolesPnts[i][ii].IsCrossBorderedPnt) {
                                IsCrossBorders.Add(ii);//////////////////////////////it is necessary ???!!!
                            }
                        }
                        float tmp = (HolesPnts[i][ii].V - VectorExtension.ProjectPntToLine(pol.Pnts[1].V, pol.Pnts[0].V, HolesPnts[i][ii].V)).sqrMagnitude;//SqrMagnitude!!!!
                        if (dist == 0) {
                            dist = tmp;
                            Lft = ii;
                        } else if (dist > tmp && Vector3.Cross(HolesPnts[i][ii - 1].V - HolesPnts[i][ii].V, HolesPnts[i][ii + 1].V - HolesPnts[i][ii].V).sqrMagnitude > 0) {//!!!!!!!!!!!!!!!!!!WTF distance is >0!!!!!!!!!
                            dist = tmp;
                            Lft = ii;
                        }
//                        if (HolesPnts[i][ii].IsCrossBorderedPnt) {
//                            Lft = ii;
//                            break;
//                        }
                    }
                    HolesPnts[i].RemoveAt(HolesPnts[i].Count - 1);


                    List<Point> PntsV_C = new List<Point>(HolesPnts[i]);

                    PntsV_C.Add(PntsV_C[0]);
                    PntsV_C.Insert(0, HolesPnts[i].Last());

                    if (!VectorExtension.IsParalDirVec(pol.Norm, Vector3.Cross(PntsV_C[Lft].V - PntsV_C[Lft + 1].V, PntsV_C[Lft + 2].V - PntsV_C[Lft + 1].V))) {
                        HolesPnts[i].Reverse();
                        Lft = HolesPnts[i].Count - Lft - 1;
                    }
                    List<Point> Rotate = new List<Point>();
                    Rotate.AddRange(HolesPnts[i]);
                    List<Point> tmP = new List<Point>();
                    tmP.AddRange(Rotate.GetRange(0, Lft));
                    Rotate.RemoveRange(0, Lft);
                    Rotate.AddRange(tmP);

                    HolesPnts[i] = new List<Point>(Rotate);
                    Rotate.Clear();

                    HolesPnts[i].Add(HolesPnts[i][0]);

                    PntsV_C = new List<Point>(HolesPnts[i]);

                    List<Point> tmpResult = new List<Point>();
                    //					int tmpInt=0;
                    for (int j = 0; j < pol.Pnts.Count; j++) {
                        for (int ii = 0; ii < PntsV_C.Count; ii++) {
                            if (VectorExtension.SidesUnCrossed(pol.Pnts[j].V, PntsV_C[ii].V, HolesPnts[i])) {
                                if (j == 0) {
                                    tmpResult.Add(pol.Pnts[j]);
                                    tmpResult.Add(PntsV_C[ii]);
                                    PntsV_C.Remove(PntsV_C[ii]);
                                    break;
                                } else {
                                    tmpResult.Add(PntsV_C[ii]);
                                    tmpResult.Add(pol.Pnts[j]);
                                    tmpResult.Add(tmpResult[0]);
                                    Result[0].AddRange(tmpResult);
                                    tmpResult.Clear();

                                    Result[1].Add(pol.Pnts[j]);
                                    for (int jj = j + 1; jj < pol.Pnts.Count; jj++) {
                                        Result[1].Add(pol.Pnts[jj]);
                                    }
                                    Result[1].Add(Result[0][0]);
                                    for (int jj = PntsV_C.Count - 1; jj >= ii; jj--) {
                                        Result[1].Add(PntsV_C[jj]);
                                    }
                                    Result[1].Add(Result[1][0]);
                                    Result[0].Reverse();
                                    return Result;
                                }
                            } else {
                                tmpResult.Add(PntsV_C[ii]);
                            }

                        }
                    }
                }
            } else {
/*
            if(VectorExtension.IsParalDirVec(pol.Norm, Vector3.Cross(PntsV_C[Lft].V-PntsV_C[Lft+1].V, PntsV_C[Lft+2].V-PntsV_C[Lft+1].V))){
                PntsV[i].Reverse();
            }
            Result = new List<Point>[1];
            Result[0] = new List<Point>();
            for(int j=0;j<PntsV[i].Count;j++){
                Result[0].Add (PntsV[i][j]);
            }
*/
                Result = HolesPnts;//For Sliser body this will be figures, that are cupped surfaces

            }
			return Result;
		}

//		public static List<int> GetCrossPnts(int ofst, List<Point> pnts){
//			return pnts.Select ((item, index) => new {Item = item, Index = index}).Where (n => n.Item.IsCrossing).Select(n=>n.Index+ofst).ToList();
//		}

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
        public class BodyAttributes {
            public List<int[]> tris = null;
            public Vector3[] v = null;
            public Vector3[] n = null;
            public Vector2[] uv = null;
            public Vector4[] tng = null;
            public int subMshCnt;

            public BodyAttributes(int pntsCnt, int SetSubMshCnt) {
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
            public Body(int sbMshCnt, bool flg=false, LinksCreatorPar oldlinks = null) {
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

            public BodyAttributes BodyGenerate() {
                List<int>[] nwAperture = new List<int>[subMshCnt];//for update links

                OptimalContol = null;
                int vertCnt = 0;
                int vertOffst = 0;
                int trisOffst = 0;
                for (int i = 0; i < subMshCnt; i++) {
                    vertCnt += pnts[i].Count;
                }

                BodyAttributes ba = new BodyAttributes(vertCnt, subMshCnt);
                for (int i = 0; i < subMshCnt; i++) {
                    nwAperture[i] = new List<int>();
                    MergeMsh(i, ref trisOffst, ref vertOffst, ref ba, ref links, ref nwAperture[i]);
                }
                if (OldTrisLinksGetMode) {
                    links.OldRefreshedPnts.Add(nwAperture);
                }
                return ba;
            }

            private void MergeMsh(int subMsh, ref int trisOffst, ref int vertOffst, ref BodyAttributes ba, ref LinksCreatorPar links, ref List<int> newLink) {
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
//            private int[] crsPntsControl;//For crossing one triangle with another max 2 cross pnts
            public Dictionary<Point, int> Nets;
            //NetKeys, NetLinks, controlNets are have equal elements quantity = Nets.count
            public List<Point> NetKeys;
            public List<List<Point>> NetLinks;
			public List<HashSet<Point>> ControlNets;
            
            private static HashSet<Border>[] borders;//array by subMshCnt!!!!!!!!
            private static HashSet<DirectedPoint>[] genPnts;//general points for optimization count of vertex for each submesh

            private int indSubMsh = -1; //index of submesh
			private int hashCode=-1;
			private List<Point> pnts;
			public List<Point> LinkPnts;//for deleting links to another
			private Vector3 norm;//this normal direction will be indipendent to type plane
            private Vector3 normSecator;//normal of plane. It is indipendent to of type plane (Slising or Sliser)
            private bool isCrossed = false;
            private bool isSliser = false;

            private Dictionary<Point, Point> crossPnts = null;

            public Holes bHoles;

            //used in NxtvBads (IsFiltred)
            private bool isFiltredInPntToTrisDict = false;//this for optimization in triangle cutting algoritm (cutting of redundant triangles)


            private List<Vector2> OrthoPnts; //pnts projections to plane of poligon (Necessary reate function via rotate matrix)
            private List<Vector2> OrthoPrjPnts; //pnts projections to plane one of 3 ortho planes (XY, YZ, ZX)
            private List<Vector2> OrthoPrjInHoles; //pnts projections to plane one of 3 ortho planes (XY, YZ, ZX)

            private bool CutterAction; //Cutter or cutting

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
				
			public Poligon(List<Point> points, int iSubMsh, bool forvardDirect = true){
				pnts = points;
				LinkPnts = new List<Point>(pnts);
				indSubMsh = iSubMsh;
                normSecator = Vector3.Cross(points[0].V - points[1].V, points[2].V - points[1].V);
                if (forvardDirect){
					norm = normSecator;
                }
                else{
					norm = -normSecator;
				}
                isSliser = !forvardDirect;

                crossPnts = new Dictionary<Point, Point>();
                Nets = new Dictionary<Point, int>();
                NetKeys = new List<Point>();
                NetLinks = new List<List<Point>>();
				ControlNets = new List<HashSet<Point>>();

                bHoles = new Holes();
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

/*
            public void CreateControCrossController(int anotherCount) {
                crsPntsControl = new int[anotherCount];
            }
*/

/*
            public void GetProjectionPnts(){
                if (!VectorExtension.IsParalVec(norm, Vector3.Cross(VectorExtension.Vx1, VectorExtension.Vy1))) {
                    for (int i = 0; i <Pnts.Count; i++) {
                        OrthoPrjPnts.Add(new Vector2(Pnts[i].V.x, Pnts[i].V.y));
                    }
                    for (int i =0; i<innerHoles.Count; i++) {
                        for (int j = 0; j < innerHoles[i].Count; j++) {
                            OrthoPrjInHoles.Add(new Vector2(innerHoles[i][j].V.x, innerHoles[i][j].V.y));
                        }
                    }
                    norm = new Vector3(0f, 0f, norm.z);
                } else if (!VectorExtension.IsParalVec(norm, Vector3.Cross(VectorExtension.Vx1, VectorExtension.Vz1))) {
                    for (int i = 0; i < Pnts.Count; i++) {
                        OrthoPrjPnts.Add(new Vector2(Pnts[i].V.x, Pnts[i].V.z));
                    }
                    for (int i = 0; i < innerHoles.Count; i++) {
                        for (int j = 0; j < innerHoles[i].Count; j++) {
                            OrthoPrjInHoles.Add(new Vector2(innerHoles[i][j].V.x, innerHoles[i][j].V.z));
                        }
                    }
                    //Меняем параметры нормали (чтобы корректно был расчёт на параллельность)
                    norm = new Vector3(0f, 0f, -norm.y);
                } else if (!VectorExtension.IsParalVec(norm, Vector3.Cross(VectorExtension.Vy1, VectorExtension.Vz1))) {
                    for (int i = 0; i < Pnts.Count; i++) {
                        OrthoPrjPnts.Add(new Vector2(Pnts[i].V.y, Pnts[i].V.z));
                    }
                    for (int i = 0; i < innerHoles.Count; i++) {
                        for (int j = 0; j < innerHoles[i].Count; j++) {
                            OrthoPrjInHoles.Add(new Vector2(innerHoles[i][j].V.y, innerHoles[i][j].V.z));
                        }
                    }
                    norm = new Vector3(0f, 0f, norm.x);
                } else {
                    for (int i = 0; i < Pnts.Count; i++) {
                        OrthoPrjPnts.Add(new Vector2(Pnts[i].V.x, Pnts[i].V.y));
                    }
                    for (int i = 0; i < innerHoles.Count; i++) {
                        for (int j = 0; j < innerHoles[i].Count; j++) {
                            OrthoPrjInHoles.Add(new Vector2(innerHoles[i][j].V.x, innerHoles[i][j].V.y));
                        }
                    }
                    norm = new Vector3(0f, 0f, norm.z);
                }
            }
*/

            public void GetOrthoPnts(){
                for (int i = 0; i < Pnts.Count; i++){
//                    OrthoPnts.Add(Pnts.)
                }
            }

            private void ChangeHash(){
				for (int i = 0; i < Pnts.Count; i++) {
					hashCode += VectorExtension.VecRound (Pnts[i].V, 0f).GetHashCode();
				}
				hashCode = hashCode >> Pnts.Count;
			}
				
			public bool Equals(Poligon other){
				//Примем что в идентичных треугольниках и порядок следования вершин должен совпадать, тогда не нужно перебирать варианты
				float f = VectorExtension.rFactor; //28.07.2018 была 0.001f. Для уменьшения вероятности ошибки пришлось снизить точность
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				bool flg = true;
				for (int i = 0; i < Pnts.Count; i++) {
					bool flg2 = this.Pnts[i].V.Equals(other.Pnts[i].V);
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
            public bool CrossToPoligon(Poligon other, int indexThis, int indexOther) {
                //index - connect other poligon with this poligon
//                List<BasePoint> crsPnts = new List<BasePoint>();
                HashSet<BasePoint> cPnt = new HashSet<BasePoint>();//C помощью словаря избавимся от задваивания в случае, когда треугольники пересекутся своей стороной
                int k = 0;
                int cnt = this.Pnts.Count;
                Vector3[] tt1 = new Vector3[cnt + 1];
                Point[] pts1 = new Point[cnt + 1];
                for (int i=0; i< cnt; i++) {
                    tt1[i] = this.Pnts[i].V;//////////////////////нужно избавиться от этих tt1. Создать массив!!!
                    pts1[i] = this.Pnts[i];
                }
                tt1[cnt] = tt1[0];
                pts1[cnt] = pts1[0];

                cnt = other.Pnts.Count;
                Vector3[] tt2 = new Vector3[cnt + 1];
                Point[] pts2 = new Point[cnt + 1];
                for (int i = 0; i < cnt; i++) {
                    tt2[i] = other.Pnts[i].V;
                    pts2[i] = other.Pnts[i];
                }
                int[] indxs = new int[] { 0, 1, 2, 0 };
                tt2[cnt] = tt2[0];
                pts2[cnt] = pts2[0];

                /*
                  Необходимо искать точки граничные и внутренние в разных циклах, поскольку иначе может получиться, что будет 1 и та же точка 2 раза 
                  (если она лежит на границе) она будет в обоих циклах. Может попасться оба раза за 1 цикл и тогда точку исключим.
                  но нужно помнить, что у 2х треугольников общие точки лежат на максимум 1 отрезке (а минимум 1 точка).
                */
                List<Point>[] crPnts = new List <Point>[]{ new List<Point>(), new List<Point>()};

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


                for (int i = 0; i < tt1.Length - 1; i++) {
                    flgCrossBordering = false;
                    flgIsTriVertex = false;
                    if (CrossTrianglBySegment(ref cP, tt2, new Vector3[] { tt1[i], tt1[i + 1] }, ref flgCrossBordering, ref side, ref flgIsTriVertex, ref NumVert)) {
                        if (indexOther == 0) {
                            string s = "";
                        }
                        t1 = (pts1[i+1].V - pts1[i].V);
                        t2 = (cP - pts1[i].V);
                        //This will be search crossed point on border for t1 and not sure for t2
                        //Интерполируем нормаль, создавая объект PntCL
                        Point cPcL = new Point(flgSlised, cP, VectorExtension.VLerP(pts1[i].N, pts1[i + 1].N, t1, t2));
                        cPcL.IsCrossing = true;
                        if (!this.crossPnts.ContainsKey(cPcL)) {
                            //this
//                            cPcL.Side = i;
                            cPcL.AddSide(i);
                            cPcL.IsBorderedPnt = true;
                            cPcL.UV = VectorExtension.VLerP2d(pts1[i].UV, pts1[i + 1].UV, t1, t2);
                            cPcL.Tng = VectorExtension.VLerP4d(pts1[i].Tng, pts1[i + 1].Tng, t1, t2);
                            cPcL.IndxSubMsh = this.indSubMsh;
                            cPcL.CutterNorm = other.normSecator;
                            this.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[0], cPcL);
                        } else {
                            cPcL = this.crossPnts[cPcL];
                            if (!cPcL.IsCrossBorderedPnt) {//If was be added from t2 cicle
                                cPcL.AddSide(i);
                                cPcL.IsCrossBorderedPnt = true;
                            }
                            cPcL.IsBorderedPnt = true;
                            AddCrossPnt(crPnts[0], cPcL);
                        }
                        if (!flgCrossBordering) {//if bordering then it will be added in cicle by t2
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
                                if (flgCrossBordering) {
                                    cPcL.CutterNorm = this.normSecator;
                                    cPcL.IsBorderedPnt = true;
                                    cPcL.Neighbors[0] = indxs[side];
                                    cPcL.Neighbors[1] = indxs[side + 1];
                                    cPcL.AddSide(side);
                                }
                                other.crossPnts.Add(cPcL, cPcL);
                                AddCrossPnt(crPnts[1], cPcL);
                            } else {
                                cPcL = other.crossPnts[cPcL];
                                AddCrossPnt(crPnts[1], cPcL);
                            }
                        }
                        //в словарь other.crossPnts можно добавлять без проверки, поскольку у них точки пересечения общие и всё должно в словарях совпадать с небольшой разницей по принадлежности
                    }
                }
                //По-хорошему, нужно убирать только те треуголники, которые реальное пересечение имеют между собой
                for (int i = 0; i < tt2.Length - 1; i++) {
                    flgCrossBordering = false;
                    if (CrossTrianglBySegment(ref cP, tt1, new Vector3[] { tt2[i], tt2[i + 1] }, ref flgCrossBordering, ref side, ref flgIsTriVertex, ref NumVert)) {
                        if (indexOther == 0) {
                            string s = "";
                        }
                        t1 = (pts2[i + 1].V - pts2[i].V);
                        t2 = (cP - pts2[i].V);
                        //This will be search crossed point on border for t2 and not sure for t1
                        Point cPcL = new Point(cP, flgSlised);
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
                            if (flgCrossBordering) {
                                cPcL.CutterNorm = other.normSecator;
                                cPcL.IsBorderedPnt = true;
                                cPcL.Neighbors[0] = indxs[side];
                                cPcL.Neighbors[1] = indxs[side + 1];
                                cPcL.AddSide(side);
                            }
                            this.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[0], cPcL);
                        } else {
                            cPcL = this.crossPnts[cPcL];
                            AddCrossPnt(crPnts[0], cPcL);
                        }

                        if (!other.crossPnts.ContainsKey(cPcL)) {
                            //other
                            cPcL = cPcL.CloneEmpty();
//                            cPcL.Side = i;
                            cPcL.AddSide(i);
                            cPcL.IsCrossing = true;
                            cPcL.IsBorderedPnt = true;
                            cPcL.IsSlised = false;
                            cPcL.N = VectorExtension.VLerP(pts2[i].N, pts2[i + 1].N, t1, t2);
                            cPcL.UV = VectorExtension.VLerP2d(pts2[i].UV, pts2[i + 1].UV, t1, t2);
                            cPcL.Tng = VectorExtension.VLerP4d(pts2[i].Tng, pts2[i + 1].Tng, t1, t2);
                            cPcL.IndxSubMsh = other.indSubMsh;
                            cPcL.CutterNorm = this.normSecator;
                            other.crossPnts.Add(cPcL, cPcL);
                            AddCrossPnt(crPnts[1], cPcL);
                        } else {
                            cPcL = other.crossPnts[cPcL];
                            if (!cPcL.IsCrossBorderedPnt) {//If was be added from t1 cicle
                                cPcL.AddSide(i);
                                cPcL.IsCrossBorderedPnt = true;
                            }
                            cPcL.IsBorderedPnt = true;
                            AddCrossPnt(crPnts[1], cPcL);
                        }
                    }
                }
                //Не будем учитывать прикосновения поверхностей 1й точкой
                //Сразу полагаем, что всего точек две. И для каждой линии разреза, когда не произошло Connect, может быть только одна из этих двух точек
                for (int ii = 0; ii < crPnts.Length; ii++) {
//                    if (crPnts[ii].Count == 2) {
                    if (crPnts[ii].Count > 1) {
						if(ii==0){
							AddDict (this, crPnts[ii]);
						} else{
							AddDict (other, crPnts[ii]);
						}
                    }
                }

                return crPnts[0].Count > 0;
            }

            private void AddCrossPnt(List<Point> crPnts, Point cPcL) {
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
                    }
                }else {
                    crPnts.Add(cPcL);
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

            private void AddDict(Poligon pol, List<Point> pnts) {
                List<Point> lstPnts = null;
                Point ctrlPnt = null;
                HashSet<Point> control = null;
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
                                    control.Add(ctrlPnt);
                                }
                            }
                        }
                    } else {
                        lstPnts = new List<Point>();
                        control = new HashSet<Point>();
                        for (int j = 0; j < pnts.Count; j++) {
                            if (j != i) {
                                ctrlPnt = pnts[j];//otherPnt
                                lstPnts.Add(ctrlPnt);
                                control.Add(ctrlPnt);
                            }
                        }
                        pol.Nets.Add(pnts[i], pol.NetLinks.Count); //Point - index in NetLinks
                        pol.NetKeys.Add(pnts[i]);
                        pol.NetLinks.Add(lstPnts);//pol.NetLinks.Count == pol.controlNets.Count!
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

			    Vector3 p1 = D-A;
			    Vector3 p2 = D-E;
			    Vector3 p3 = B-A;
			    Vector3 p4 = C-A;

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
									if(USumV>= sumMax && (v <= almostZero||u <= almostZero)){
										string s ="";
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
            private Point[] pnts;
            private int hashCode = -1;
            public Border(Point p1, Point p2) {
                pnts = new Point[] {p1, p2};
                RefreshHashCode();
            }
            public Point[] Pnts{
                get { return pnts;}
                set {
                    pnts = value;
                    RefreshHashCode();
                }
            }
            private void RefreshHashCode() {
                hashCode = VectorExtension.VecRound(pnts[0].V, 0f).GetHashCode()>>1+ VectorExtension.VecRound(pnts[1].V, 0f).GetHashCode()>>1;  
            }
            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Border)obj);
            }
            public bool Equals(Border other) {
                float f = VectorExtension.rFactor; //28.07.2018 была 0.001f. Для уменьшения вероятности ошибки пришлось снизить точность!!!!!!!!!!!!!!!!
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                bool flg1 = (Mathf.Abs(this.Pnts[0].V.x - other.Pnts[0].V.x) < f && Mathf.Abs(this.Pnts[0].V.y - other.Pnts[0].V.y) < f && Mathf.Abs(this.Pnts[0].V.z - other.Pnts[0].V.z) < f);
                bool flg2 = (Mathf.Abs(this.Pnts[1].V.x - other.Pnts[0].V.x) < f && Mathf.Abs(this.Pnts[1].V.y - other.Pnts[0].V.y) < f && Mathf.Abs(this.Pnts[1].V.z - other.Pnts[0].V.z) < f);
                if (!(flg1) && !(flg2)) {
                    return false;
                }
                flg1 = flg1 && (Mathf.Abs(this.Pnts[1].V.x - other.Pnts[1].V.x) < f && Mathf.Abs(this.Pnts[1].V.y - other.Pnts[1].V.y) < f && Mathf.Abs(this.Pnts[1].V.z - other.Pnts[1].V.z) < f);
                if (flg1) {
                    return true;
                }
                flg2 = flg2 && (Mathf.Abs(this.Pnts[0].V.x - other.Pnts[1].V.x) < f && Mathf.Abs(this.Pnts[0].V.y - other.Pnts[1].V.y) < f && Mathf.Abs(this.Pnts[0].V.z - other.Pnts[1].V.z) < f);
                if (flg2) {
                    return true;
                }
                return false;
            }
            public override int GetHashCode() {
                return hashCode;
            }
        }
        #endregion

        #region PointDefinitionClass
		public class Point: System.IEquatable<Point>{
            // For this class IEqualeble will be to compared by only Vector3 "V" (position in space)
            //For Slise_
            public bool isUsedPair = false;
            public bool isPaired = false;
            private Vector3 cutterNorm; //Normal to sliser plane
            //_For Slise

            private int hashCode= -1;
            private int indSubMsh; //index of submesh
            private int oldIndx = -1;

            private bool isAddedToMshBody = false;

            private Vector3 v;
			private Vector3 n;		//normal
			private Vector2 uv;		
			private Vector4 tng;
            private bool isBorderedPnt = false;//if pnt on borderside of this poligon

//            private int side=-1;
            public List<int> Side = null;
            private bool isTriVertex = false;
            private bool isCrossBorderedPnt = false;//if general pnt for this poligon and another poligon
            public int[] Neighbors = null;//if crossPnt isTriVetex - index vertex in neighbors[0]; for crossPnt isCrossBorderPnt - indexes closer vertex in neighbors[0] and neighbors[1]
            private int cntNeighbors = -1;//for isTriVertesxs = 1; for isCrossBorderPnt = 2; 

            private bool isCrossingPnt = false;//Used for catching of Links only
			private bool isSlisedPnt = false; //slised or sliser body?


			private bool isUsedByTranslate = false; //used by translating of previous apertuner
			private int oldApertNum = -1;
			private int newIndx =-1;
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
            public Vector3 CutterNorm {
                set { cutterNorm = value; }
                get { return cutterNorm; }
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
            public bool IsCrossBorderedPnt {//or CrossBorderedPnt or TriVertex
                set {
                    isCrossBorderedPnt = value;
                    if (isCrossBorderedPnt) {
                        cntNeighbors = 2;
                        isTriVertex = false;
                    }
                }
                get { return isCrossBorderedPnt; }
            }
            public int CntNeighbors {
                set { cntNeighbors = value; }
                get { return cntNeighbors; }
            }
            public bool IsTriVertex {//or CrossBorderedPnt or TriVertex
                set {
                    isTriVertex = value;
                    if (isTriVertex) {
                        cntNeighbors = 1;
                        isCrossBorderedPnt = false;
                    }
                }
                get { return isTriVertex; }
            }

            public void AddSide(int i) {
                Side.Add(i);
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
                Neighbors = new int[2];
            }
            public Point(Vector3 _v, bool flgSliser= false) {
                this.v = _v;
                isSlisedPnt = flgSliser;
                RefreshHashCode();
                Side = new List<int>();
                Neighbors = new int[2];
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
                return flg1 && VectorExtension.IsParalDirVec(this.n, other.n);
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
        #endregion

        public class Holes{
			public List<List<Point>> BorderHoles;
			public List<List<Point>> InnerHoles;
            public List<List<int>> indexCrossBorderPntsInner;// indexer of InnerHoles group includes CrossBorderPnts [index of  innerHoles][index of point in cur innerHoles Pnts group]
			public Holes(){
				BorderHoles = new List<List<Point>>();
				InnerHoles = new List<List<Point>>();
			}
			public Holes(Holes H){
				BorderHoles = new List<List<Point>>(H.BorderHoles);
				InnerHoles = new List<List<Point>>(H.InnerHoles);
                indexCrossBorderPntsInner = new List<List<int>>();
            }
			//Можно добавить функцию объединения коллекций в одну
		}
		public class Slice{
			public Point p1;
			public Point p2;
			public float d1;
			public float d2;
			public bool iSPntOfTriangle; //Вершина треугольника (?)
			public List<Point> lst;//Упорядоченный список от ближайшей точки к дальней
			public int i1;//Текущая сторона треугольника для точки 1
			public int i2;//Текущая сторона треугольника для точки 2
			public Slice(Point _p1, Point _p2, float _d1, float _d2, List<Point> listArr, int j1 , int j2){
				p1 = _p1;
				p2 = _p2;
				d1 = _d1;
				d2 = _d2;
				lst = listArr;
				i1=j1;
				i2=j2;
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
			private int offst;
			private int LastIndx=0;
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
            public BodyAttributes bodyMsh;
            public ResultInfo(BodyAttributes body) {
                bodyMsh = body;
            }
        }

		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		//Триангуляция

		public class Triangulation {
			public static int[] GetTrianglesInds(List<Vector3> v1, List<int> inds, int nPosit, Vector3 nrm, bool flg=true){
				List<Vector2> v =GetProjection(ref nrm, v1); //Получаем проекцию на плоскость
//				bool ClockDirCalculated = false;//Используем определение по часовой или против для поворота плоскости в нужном нам направлении
				List <Vector2> copyV = new List<Vector2>(v);
				List <Vector2> copyV_t = new List<Vector2>();
				List<int> resultT = new List<int> ();
				Vector2 nLeft = new Vector2();//Самая левая точка
				//Сначала найдём самую левую точку, она точно будет выпуклой. В ней найдём значение векторного умножения (для выпуклых всех такое будет)
				//Так же мы могли посчитать кол-во "+" и "-" векторных умножений, тех, которых больше для фигуры, образованной
				//замкнутой ломанной больше будет именно выпуклых вершин.
				for (int i = 0; i < copyV.Count; i++) {
					if (i == 0) {
						nLeft = v[i];
					}
					else{
						if(nLeft.y>v[i].y){
							nLeft = v[i];
						}
					}
				}
				copyV_t.Insert(0,copyV.Last());
				copyV_t.AddRange(copyV);
				copyV_t.Add(copyV[0]);

				float VectorCrosOut = VectorExtension.OrtCrosVect (nLeft, copyV_t[v.IndexOf(nLeft)], nLeft, copyV_t[v.IndexOf(nLeft)+2]);
				int index = 0;
				int k = 0;
				float tmpOrt;
				do{
					//Сделаю пока от 0 до count.
					//Но по-хорошему, надо замкнуть и сам цикл сделать бесконечным, пока не останется лишь триугольник
					int i_before =  index>0 ? (index+copyV.Count-1) % copyV.Count: copyV.Count-1;
					int i_n = index;
					int i_after = index < copyV.Count-1  ? (index+1) % copyV.Count : 0;

					Vector2 nbefore=  copyV [i_before];
					Vector2 n = copyV [i_n];
					Vector2 nafter = copyV[i_after];

					tmpOrt = VectorExtension.OrtCrosVect (n, nbefore, n, nafter);
					if (Mathf.Abs(tmpOrt-VectorCrosOut)<VectorExtension.rFactor||copyV.Count==3) {
						if(isOuter(nbefore, n, nafter, copyV)){
							if(flg){
								resultT.AddRange (new int[]{ inds[i_before]+nPosit, inds[i_n]+nPosit, inds[i_after]+nPosit});		
							}else{
								resultT.AddRange (new int[]{ inds[i_after]+nPosit, inds[i_n]+nPosit, inds[i_before]+nPosit});	
							}
							//Необходимо, чтобы copyV был замкнутым списком. Чтобы "отрезаение" треугольников происходило до тех пор, пока не останется 3 вершины.
							copyV.RemoveAt (index);
							index=0;
						}
					}
					if (index==copyV.Count-1){
						index=0;
					}
					index++;
					k++;//Пока оставлю, чтобы не возникало беск. цикла
				} while(copyV.Count>2&&k<10000);//k может помешать для очень детализированного полигона (с б-м к-ом вертексов)

				v.Clear ();
				copyV.Clear ();
				copyV_t.Clear();
				return resultT.ToArray ();
			}
			public static List<int> GetTriangles(List<Vector3> v1, int nPosit, Vector3 nrm){
				List<Vector2> v = GetProjection(ref nrm, v1); //Получаем проекцию на плоскость

//				bool ClockDirCalculated = false;//Используем определение по часовой или против для поворота плоскости в нужном нам направлении
				List <Vector2> copyV = new List<Vector2>(v);
				List <Vector2> copyV_t = new List<Vector2>();
				List<int> resultT = new List<int> ();
				Vector2 nLeft = new Vector2();//Самая левая точка
				//Сначала найдём самую левую точку, она точно будет выпуклой. В ней найдём значение векторного умножения (для выпуклых всех такое будет)
				//Так же мы могли посчитать кол-во "+" и "-" векторных умножений, тех, которых больше для фигуры, образованной
				//замкнутой ломанной больше будет именно выпуклых вершин.
                int ii = -1;
				for (int i = 0; i < copyV.Count; i++) {
					if (i == 0) {
						nLeft = v[i];
                        ii = i;

                    }
					else{
						if(nLeft.y>v[i].y){
							nLeft = v[i];
                            ii = i;

                        }
					}
				}
				copyV_t.Insert(0,copyV.Last());
				copyV_t.AddRange(copyV);
				copyV_t.Add(copyV[0]);

				float VectorCrosOut = VectorExtension.OrtCrosVect (nLeft, copyV_t[ii], nLeft, copyV_t[ii + 2]);
                bool flg = ((VectorCrosOut * nrm.z)<0);

                int index = 0;
				int k = 0;
				float tmpOrt;
				do{
					//Сделаю пока от 0 до count.
					//Но по-хорошему, надо замкнуть и сам цикл сделать бесконечным, пока не останется лишь триугольник
					Vector2 nbefore=  index>0 ? copyV [(index+copyV.Count-1) % copyV.Count]: copyV[copyV.Count-1];
					Vector2 n = copyV [index];
					Vector2 nafter = index < copyV.Count-1  ? copyV [(index+1) % copyV.Count] : copyV[0];

					tmpOrt = VectorExtension.OrtCrosVect (n, nbefore, n, nafter);
					if (Mathf.Abs(tmpOrt-VectorCrosOut)<VectorExtension.rFactor||copyV.Count==3) {
						if(isOuter(nbefore, n, nafter, copyV)){
							if(flg){
								resultT.AddRange (new int[]{ v.IndexOf (nbefore)+nPosit, v.IndexOf (n)+nPosit, v.IndexOf (nafter)+nPosit });		
							}else{
								resultT.AddRange (new int[]{ v.IndexOf (nafter)+nPosit, v.IndexOf (n)+nPosit,  v.IndexOf (nbefore)+nPosit});	
							}
							//Необходимо, чтобы copyV был замкнутым списком. Чтобы "отрезаение" треугольников происходило до тех пор, пока не останется 3 вершины.
							copyV.RemoveAt (index);
							index=0;
						}
					}
					if (index==copyV.Count-1){
						index=0;
					}
					index++;
					k++;//Пока оставлю, чтобы не возникало беск. цикла
				} while(copyV.Count>2&&k<10000);//k может помешать для очень детализированного полигона (с б-м к-ом вертексов)

				v.Clear ();
				copyV.Clear ();
				copyV_t.Clear();
				return resultT;
			}
			public static List<Vector2> GetProjection(ref Vector3 nrm, List<Vector3> v1){
				List<Vector2> v = new List<Vector2>();
                v.Capacity = v1.Capacity;

                if (!VectorExtension.IsParalVec(nrm, Vector3.Cross (VectorExtension.Vx1, VectorExtension.Vy1))){
					for(int i=0;i<v1.Count;i++){
						v.Add (new Vector2(v1[i].x, v1[i].y));
					}
					nrm = new Vector3(0f,0f, nrm.z);
				}else if(!VectorExtension.IsParalVec(nrm, Vector3.Cross (VectorExtension.Vx1, VectorExtension.Vz1))){
					for(int i=0;i<v1.Count;i++){
						v.Add (new Vector2(v1[i].x,v1[i].z));
					}
					//Меняем параметры нормали (чтобы корректно был расчёт на параллельность)
					nrm=new Vector3(0f,0f, -nrm.y);
				}else if(!VectorExtension.IsParalVec(nrm, Vector3.Cross (VectorExtension.Vy1, VectorExtension.Vz1))){
					for(int i=0;i<v1.Count;i++){
						v.Add (new Vector2(v1[i].y,v1[i].z));
					}
					nrm = new Vector3(0f,0f, nrm.x);
				}else{
					for(int i=0;i<v1.Count;i++){
						v.Add (new Vector2(v1[i].x,v1[i].y));
					}
                    nrm = new Vector3(0f, 0f, nrm.z);
                }
				return v;
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

		public static class VectorExtension{
            public static Vector3 Vx1 = new Vector3(1, 0, 0);
            public static Vector3 Vy1 = new Vector3(0, 1, 0);
            public static Vector3 Vz1 = new Vector3(0, 0, 1);

            public static float rFactor = 0.01f;// на 0.00001f ошибки
            private static float rounder = 5f;//Округление необходимо корректировать (лучше динамически)

			public static bool ApproxEqual (Vector3 v1, Vector3 v2){
				if (Mathf.Approximately (v1.x, v2.x)&&Mathf.Approximately (v1.y, v2.y)&&Mathf.Approximately (v1.z, v2.z)) {
					return true;
				}
				return false;
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

			public static bool IsParalVec(Vector3 v1, Vector3 v2){
				if(Mathf.Abs(Vector3.Dot(v1,v2)/(v1.magnitude*v2.magnitude))<rFactor){
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
				float f = rFactor;
				if(Mathf.Abs(v1.x/v2.x-v1.y/v2.y)<f && Mathf.Abs(v1.x/v2.x-v1.z/v2.z)<f){ //Если отношения координат равны
					return true;
				}
				return false;
			}

			public static float Round(float f, float n){
				float p = Mathf.Pow (10, n);
				return (Mathf.Round (f*p))/p;
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
				go.transform.localScale *= 5.5f;
                go.transform.position = pnt;//  +new Vector3(-50, -20, 200);
                return go;
			}
		}
	}



/*
 * Разбиения сложены на 3 этапа:
 * -Поиск точек пересечения
 * -Поиск изменённых форм (и колва) для пересечённых полигонов
 * -Триангуляция получившихся полигонов:
 * --Самый простой случай, когда внутренных отверстий нет в полигоне. Просто триангулируем по "ушной" системе
 * --Если хотя бы одна из получившихся фигур сечения входит полностью в исследуемый полигон( в том числе в один из получившихся полигонов, на которые он разбит), нужна триангуляция
 *   полигона с произвольными отверстиями, учитывающаяя в том числе граничные условия (вершина отверстия на грани исследуемого полигона, и вершина отверстия на вершине исследуемого полигона.
 *   При этом для фигур типа бублика возможна ситуация с инвертнутыми нормалями поверхности. Поэтому эти нормали необходимо учитывать при такой триангуляции.
 *
 * 
 * 
 * 
 * 
 * 
 * 
 * Есть возможность некоторой оптимизации (по скорости (думаю, порядка 30% можно скорости прибавить, правда памяти больше будем хавать)), 
 * поскольку на данный момент имеет место быть пересчёт одного и того же по несколько раз:
 * Сначала прохожусь по телу Basic и телу go (ищу вырезы на теле Basic), потом вновь прохожусь по телу go и телу Basic, когда ищу вырезы уже на теле go
 * Кроме того, в триангуляции и при вычислении "обходов" сторон для полигона с "дыркой" (ф-я GetArrWithHole2) тоже есть похожие алгоритмы, которые
 * выполняются несколько раз. Это можно попробовать тоже объединить, чтобы не было лишних повторяющихся пересчётов (я про поиск "уха" и выпуклой вершины многоугольника).
 * 
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





