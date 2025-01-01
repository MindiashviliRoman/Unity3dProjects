using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; //для MyList.Select(info => info.Name).ToList();

namespace DVName{
	public class DV:MonoBehaviour{
		public static float rounder = 5f;//Округление необходимо корректировать (лучше динамически)
		public static int kCount=0;
		public static int triCount1=0;
		public static int triCount2=0;
		public static int CountCross;
        //		private static Mesh m3_new = new Mesh();

        private static List<GameObject> ListGO = new List<GameObject>();

        public static int kk = 0;
		public static Mesh DeductValue(Mesh m3_new, GameObject Basic, GameObject go, ref Material[] mats){
            foreach (GameObject g in ListGO) {
                Destroy(g);
            }

            ListGO.Clear();

            kk = 0;
            CountCross =0;
			//Обнуляем в случае предыдущего прерывания (из-за try-catch)
			CrossPntSt.LVects = new List<PntCL>();
			CrossPntSt.Tris = new Dictionary <Vector3,List<TrSrCL>>();
			CrossPntSt.Nets = new Dictionary<CrossPnt,List<CrossPnt>>();
			CrossPntSt.cPntIsBord = new Dictionary<Vector3, CrossPnt>();

			kCount=0;
			Mesh m1 = Basic.GetComponent<MeshFilter>().sharedMesh;
			Mesh m2 = go.GetComponent<MeshFilter>().sharedMesh;
			Vector3[] v1 = m1.vertices;
			Vector3[] v2 = m2.vertices;
			Vector3[] n1 = m1.normals;
			Vector3[] n2 = m2.normals;
			Vector4[] tan1 = m1.tangents;
			Vector4[] tan2 = m2.tangents;
			Vector2[] uv1 = m1.uv;
			Vector2[] uv2 = m2.uv;

			//roundScale - масштабирую всю систему, чтобы сместить чуть-чуть 
			//десятичную точку вправо. необходимо для повышения точности, при уменьшении
			//погрешности
			float roundScale=10f;//1/tmpMinBound.magnitude;
//			Mesh m3_new = new Mesh();
			try{

                v1 = TrmMshPntToWrdPnt(v1, Basic);//, roundScale);
                v2 = TrmMshPntToWrdPnt(v2, go);//, roundScale);
				
				for (int i=0; i< n2.Length; i++){
					n2[i] = go.transform.TransformDirection(n2[i]);//.TransformPoint(n2[i]);
				}

				//Задел для обработки многоматериальных тел

				int subMshCnt1 = m1.subMeshCount;
				List<TriCL> TrisList1 = new List<TriCL>();
				for(int itri =0; itri<subMshCnt1; itri++){
					int[] tri_ = m1.GetTriangles(itri);
					for(int i=0;i<tri_.Length;i++){
						TrisList1.Add (new TriCL(itri, tri_[i]));
					}
				}
				PntCL[] Pnts1 = new PntCL[v1.Length];
				for(int i =0; i<v1.Length; i++){
					Pnts1[i] = new PntCL(v1[i], n1[i], uv1[i], tan1[i]);
				}
				int subMshCnt2 = m2.subMeshCount;
				List<TriCL> TrisList2 = new List<TriCL>();
			
				for(int itri =0; itri<subMshCnt2; itri++){
					int[] tri_ = m2.GetTriangles(itri);
					for(int i=0;i<tri_.Length;i++){
						TrisList2.Add (new TriCL(itri, tri_[i]));
					}
				}

				PntCL[] Pnts2 = new PntCL[v2.Length];
				for(int i =0; i<v2.Length; i++){
					Pnts2[i] = new PntCL(v2[i], -n2[i], uv2[i], tan2[i]);
				}
				int k=0;

				TriCL[] t1 = TrisList1.ToArray();
				TriCL[] t2 = TrisList2.ToArray();
				triCount1 = t1.Length;
				triCount2 = t2.Length;

				//Создаём единичную матрицу, соответсвующую трансформу без поворотов со скейлом = 1
				Matrix4x4 MT  = new Matrix4x4();
				MT[0,0] = 1;
				MT[1,1] = 1;
				MT[2,2] = 1;
				MT[3,3] = 1;

				CountCross=0;

				List<CombineInstance>[] tmpMshAttributes1 = GetMeshInterSectGO(t1,t2,Pnts1,Pnts2, true, subMshCnt1);
				CombineInstance[] totalMesh1 = new CombineInstance[tmpMshAttributes1.Length];

				for(int i=0; i<tmpMshAttributes1.Length;i++){
					totalMesh1[i].mesh = new Mesh();
					totalMesh1[i].mesh.CombineMeshes(tmpMshAttributes1[i].ToArray(), true, false);
					totalMesh1[i].transform = MT;

				}
				CombineInstance[] totalMesh;
				if(CountCross>0){
					List<CombineInstance>[] tmpMshAttributes2 = GetMeshInterSectGO(t2,t1,Pnts2,Pnts1, false, subMshCnt2);
					CombineInstance[] totalMesh2 = new CombineInstance[tmpMshAttributes2.Length];
					for(int i=0; i<tmpMshAttributes2.Length;i++){
						totalMesh2[i].mesh = new Mesh();
						totalMesh2[i].mesh.CombineMeshes(tmpMshAttributes2[i].ToArray(), true, false);
						totalMesh2[i].transform = MT;
					}

					List<CombineInstance> tmpInst = new List<CombineInstance>();
					tmpInst.AddRange(totalMesh1);
					tmpInst.AddRange(totalMesh2);
					totalMesh = tmpInst.ToArray();
				}else{
					totalMesh = totalMesh1;
				}

				//Назначаем меш
				m3_new.CombineMeshes(totalMesh, false);
				Vector3[] newV1 = m3_new.vertices;
                newV1 = TrmWrdPntToMshPnt(newV1, Basic);//, roundScale);
				m3_new.vertices = newV1;
				m3_new.RecalculateBounds();


				//Очищаем лишние меши
				for(int i=0; i<totalMesh.Length;i++){
					Destroy(totalMesh[i].mesh);
				}


				//Возвращаем все получившиеся материалы
				List<Material> mat = new List<Material>();
				mats = Basic.GetComponent<MeshRenderer>().sharedMaterials;
				//Используем subMshCnt1, а не mats.Length,
				//чтобы правильно забрать материалы, которые на реально существующих сабмешах
				//ошибка может получиться, когда вручную назначишь большее число материал, чем реально сабмешей
				for(int i=0; i<subMshCnt1;i++){
					mat.Add (mats[i]);
				}
				if(CountCross>0){
					mats = go.GetComponent<MeshRenderer>().sharedMaterials;
					for(int i=0; i<subMshCnt2;i++){
						mat.Add (mats[i]);
					}
				}
				mats = mat.ToArray();



			}catch(System.Exception e){
				
			}
			return m3_new;
		}

		public static List<CombineInstance>[] GetMeshInterSectGO(TriCL[] t1, TriCL[] t2, PntCL[] pntV1, PntCL[] pntV2, bool flgSide, int subMshCnt){
			int[] NewTri;
			int NumbOfTrianglVert=0;

			List<CombineInstance>[] ArrCombine = new List<CombineInstance>[subMshCnt];
			for(int i=0; i<subMshCnt;i++){
				ArrCombine[i]= new List<CombineInstance>();		
			}

			for (int i=0;i<t1.Length;i=i+3){
                kk++;

                List<PntCL> trii = new List<PntCL>();
				trii.AddRange (new PntCL[]{pntV1[t1[i].t],pntV1[t1[i+1].t],pntV1[t1[i+2].t]});
				TrSrCL Triangl = new TrSrCL();
				Triangl.IndSide=t1[i].indSubMsh;
				Triangl.tPnts = trii;

				//Создаём ссылочные коллекции всех треугольников по всем вершинам.
				for(int j=0;j<3;j++){
					if(!CrossPntSt.Tris.ContainsKey(pntV1[t1[i+j].t].v)){
						List<TrSrCL> tmpTriis = new List<TrSrCL>();
						tmpTriis.Add (Triangl);
						CrossPntSt.Tris.Add (pntV1[t1[i+j].t].v, tmpTriis);
					}else{
						CrossPntSt.Tris[pntV1[t1[i+j].t].v].Add (Triangl);
					}
				}
				Vector3 NormTri = Vector3.Cross(pntV1[t1[i].t].v-pntV1[t1[i+1].t].v,pntV1[t1[i+2].t].v-pntV1[t1[i+1].t].v); //Нормаль к плоскости треугольника
				if(!flgSide){
					NormTri = new Vector3 (-NormTri.x, -NormTri.y, -NormTri.z);
				}
				List<CrossPnt[]> PntsOfBorder = new List<CrossPnt[]>();
				int BordCount=0;
				for(int j=0; j<t2.Length;j=j+3){
					List<CrossPnt> crsPts1 = new List<CrossPnt>();
					//получаем все точки (пары точек) пересечения текущего треугольника секущего объёма(t2) с текущим треугольником исходного объёма (t1)
					//пара может лежать как на отрезках, образующих треугольник исходного объёма, так и внутри этого множества точек, на плосктости исх. треуг.
					crsPts1 = CrossTriang(ref PntsOfBorder, new PntCL[]{pntV1[t1[i].t],pntV1[t1[i+1].t],pntV1[t1[i+2].t]}, new PntCL[]{pntV2[t2[j].t],pntV2[t2[j+1].t], pntV2[t2[j+2].t]}, NormTri);
					if(crsPts1.Count>0){
						CountCross=+crsPts1.Count;
						//Для текущего треугольника секущего объёма по всем точкам пересечения с текущим треугольником исходного объёма
						//Сократим отрезки треугольника исходного объёма до отрезков с общей точкой пересечения на именно этих граничных отрезках...
						for (int f=0; f<crsPts1.Count;f++){
							crsPts1[f].Norm = Vector3.Cross (pntV2[t2[j].t].v-pntV2[t2[j+1].t].v,pntV2[t2[j+2].t].v-pntV2[t2[j+1].t].v);
						}
					}
				}			
				List<CrossPnt> SegsOfIntersect = new List<CrossPnt>();
				if(CrossPntSt.Nets.Keys.Count>0){
                    SegsOfIntersect.AddRange(CrossPntSt.Nets.Keys);

                    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    //SortSegs неверно отрабатывает для InnerHoles. Добавляет один лишний конечный сегмент!!!!!!!!!!!!!!
                    Holes SegsOfIntsct = SortSegs(SegsOfIntersect, NormTri);

					List<PntCL>[] PerTriPnts=null;
													

					if(SegsOfIntsct.BorderHoles.Count>0){
						PerTriPnts = GetPerimetrPnts1(Triangl, SegsOfIntsct.BorderHoles, flgSide);
					}
					List<PntCL>[] PerTriPntsHoles= null;
					if(SegsOfIntsct.InnerHoles.Count>0){
						PerTriPntsHoles = GetArrWithHole2(SegsOfIntsct.InnerHoles.ToArray(), Triangl, NormTri, flgSide);
                        for (int j = 0; j < PerTriPntsHoles.Length; j++) {
                            for (int z = 0; z < PerTriPntsHoles[j].Count; z++) {
                                ListGO.Add(VectorExtension.CreatePntGO("Body1" + j.ToString() + "_" + z.ToString(), PerTriPntsHoles[j][z].v));
                            }
                        }
                    }
					if(PerTriPnts!=null){
						BordCount = PerTriPnts.Length;
					}
					for(int ii = 0; ii<BordCount;ii++){
						if(PerTriPnts[ii].Count>0){
							CombineInstance Tempcombine = new CombineInstance();
							PerTriPnts[ii].RemoveAt(0);
							if(!flgSide){
								PerTriPnts[ii].Reverse();
							}
							Vector2[] tmpUV = PerTriPnts[ii].Select(info => info.uv).ToArray();
							Vector3[] tmpNormals = PerTriPnts[ii].Select(info => info.n).ToArray();
							List<Vector3> tmpVectors = PerTriPnts[ii].Select(info => info.v).ToList();
							Vector4[] tmpTngs = PerTriPnts[ii].Select(info => info.tng).ToArray();
							Tempcombine.mesh = PolCreate(tmpVectors.ToArray(), tmpNormals,tmpUV, Triangulation.GetTriangles(tmpVectors, 0, NormTri), tmpTngs);

							ArrCombine[t1[i].indSubMsh].Add (Tempcombine);
							Destroy(Tempcombine.mesh);
						}
					}
					int HoleCount =0;
					if(PerTriPntsHoles!=null){
						HoleCount = PerTriPntsHoles.Length;
					}
					for(int ii = 0; ii<HoleCount;ii++){
						if(PerTriPntsHoles[ii].Count>0){
							CombineInstance Tempcombine = new CombineInstance();
							PerTriPntsHoles[ii].RemoveAt(0);
							if(!flgSide){
								PerTriPntsHoles[ii].Reverse();
							}
							Vector2[] tmpUV = PerTriPntsHoles[ii].Select(info => info.uv).ToArray();
							Vector3[] tmpNormals = PerTriPntsHoles[ii].Select(info => info.n).ToArray();
							List<Vector3> tmpVectors = PerTriPntsHoles[ii].Select(info => info.v).ToList();
							Vector4[] tmpTngs = PerTriPntsHoles[ii].Select(info => info.tng).ToArray();
							Tempcombine.mesh = PolCreate(tmpVectors.ToArray(), tmpNormals, tmpUV, Triangulation.GetTriangles(tmpVectors, 0, NormTri), tmpTngs);

							ArrCombine[t1[i].indSubMsh].Add (Tempcombine);
							Destroy(Tempcombine.mesh);
						}
					}
				}
			}
			PntCL[] vBad = CrossPntSt.LVects.Distinct().ToArray();
			while(vBad.Length>0){
				vBad = NxtvBads(vBad);
			}
			List<List<TrSrCL>> tmpTrs = CrossPntSt.Tris.Values.ToList();
			List<Vector3> ttrs = new List<Vector3>();
			List<TrSrCL> tmpTrs2 = tmpTrs.SelectMany(a => a).ToList();
			TrSrCL[] Trs = tmpTrs2.Distinct().ToArray();
			for(int i=0;i<Trs.Length;i++){
				if(Trs[i].tPnts.Count>2){
					Vector2[] tmpUV = new Vector2[]{Trs[i].tPnts[0].uv,Trs[i].tPnts[1].uv,Trs[i].tPnts[2].uv};
					Vector3[] tmpNorm = new Vector3[]{Trs[i].tPnts[0].n,Trs[i].tPnts[1].n,Trs[i].tPnts[2].n};
					Vector3[] tmpV = new Vector3[]{Trs[i].tPnts[0].v,Trs[i].tPnts[1].v,Trs[i].tPnts[2].v};
					Vector4[] tmpTng = new Vector4[]{Trs[i].tPnts[0].tng,Trs[i].tPnts[1].tng,Trs[i].tPnts[2].tng};
					CombineInstance Tempcombine = new CombineInstance();
					MshAttributes tmpMshAttributes = null;
					if(flgSide){
						tmpMshAttributes= new MshAttributes(tmpV,tmpNorm, tmpUV,tmpTng, new int[]{0,1, 2});
						Tempcombine.mesh = PolCreate(tmpV,tmpNorm,tmpUV,new int[]{0,1, 2}, tmpTng);
					}else{
						tmpMshAttributes = new MshAttributes(tmpV,tmpNorm, tmpUV,tmpTng, new int[]{0,2, 1});
						Tempcombine.mesh = PolCreate(tmpV,tmpNorm,tmpUV,new int[]{0,2, 1}, tmpTng);
					}

					ArrCombine[Trs[i].IndSide].Add (Tempcombine);
					Destroy(Tempcombine.mesh);
				}
			}
			CrossPntSt.LVects.Clear ();
			CrossPntSt.Tris.Clear ();
			CrossPntSt.Nets.Clear ();
			CrossPntSt.cPntIsBord.Clear ();
			return ArrCombine;
		}

		public static PntCL[] NxtvBads(PntCL[] v){
			List<PntCL> tmpLV = new List<PntCL>();
			for(int i = 0; i<v.Length;i++){
				if(CrossPntSt.Tris.ContainsKey(v[i].v)){
					List<TrSrCL> tmpTris = CrossPntSt.Tris[v[i].v];
					for(int x = 0; x<tmpTris.Count;x++){
						tmpTris[x].tPnts.Remove(v[i]);
						tmpLV.AddRange (tmpTris[x].tPnts);
						//Если из треугольника удалена крайняя точка, то треугольник и сам необходимо удалить
//						if(tmpTris[x].tPnts.Count==0){
//							CrossPntSt.Tris.Remove (v[i].v);
//						}
					}
					CrossPntSt.Tris.Remove (v[i].v);
				}
			}
			return tmpLV.Distinct().ToArray();
		}

		public static Holes SortSegs(List<CrossPnt> LastS, Vector3 NormTri){
			//При таком подходе не обязательно иметь список пар точек... поскольку ссылки уже в словаре !!!!!!!
			Holes Hol = new Holes();
			List<CrossPnt> Seq = new List<CrossPnt>();
			do{
				CrossPnt nxtPnt = LastS[0];
				Seq.Add (nxtPnt);
				LastS.Remove(nxtPnt);
				List<CrossPnt> bonds = GetnxtPnts(nxtPnt, NormTri, true);
				if(bonds!=null){
					bool flgBordered = nxtPnt.isBordering;//Возможно, не хватает проверки на точках bounds[i]
					if(bonds.Count>0){
						//nxtbonds = список списков на случай, когда в 1 точке стыкуются больше 2-х линий (пока не продумано)
						List<List<CrossPnt>> nxtbonds = new List<List<CrossPnt>>();
						for(int i=0; i<bonds.Count;i++){
							if(i==0){
								Seq.Insert(0, bonds[i]);
							}
							else{
								Seq.Add (bonds[i]);
							}
							if(!flgBordered){
								flgBordered = bonds[i].isBordering;
							}
							LastS.Remove(bonds[i]);
							List<CrossPnt> tmpLbonds = new List<CrossPnt>();
							tmpLbonds.Add (bonds[i]);
							nxtbonds.Add(tmpLbonds);
							DelonDictNxtPnt(nxtPnt, bonds[i]);//Чистим словарь
						}
						CrossPnt[] LastPnt = new CrossPnt[]{nxtPnt, nxtPnt};//Предыдущая точка
						bool flgGoNxt=false;
						do{
							for(int i = 0; i<nxtbonds.Count;i++){
								if(nxtbonds[i].Count>0){
									//Пока не нашли граничной точки, делаем проверку.
									if(!flgBordered){
										flgBordered = nxtbonds[i][0].isBordering;
									}
									DelonDictNxtPnt(nxtbonds[i][0], LastPnt[i]);//Чистим словарь
									LastPnt[i] = nxtbonds[i][0];//Предыдущая точка
									nxtbonds[i] = GetnxtPnts(nxtbonds[i][0], NormTri, false);
								}
								if(nxtbonds[i].Count>0){
									if(!flgGoNxt){
										if(i==0){
											Seq.Insert(0, nxtbonds[i][0]);
										}
										else{
											Seq.Add (nxtbonds[i][0]);
										}
										LastS.Remove(nxtbonds[i][0]);
										DelonDictNxtPnt(LastPnt[i], nxtbonds[i][0]);//Чистим словарь
									}
									//Отделяем дырки, не пересекающие границу
									if(nxtbonds.Count>1){
										if(nxtbonds[i].Count>0){
											if(nxtbonds[nxtbonds.Count-1-i].Count>0){
												if((nxtbonds[i][0] == nxtbonds[nxtbonds.Count-1-i][0])){
													flgGoNxt=true;
												}
											}
										}
									}
								}
								//Отделяем дырки, пересекающие границу
								if(nxtbonds[i].Count==0&&nxtbonds[nxtbonds.Count-1-i].Count==0){
									flgGoNxt=true;
								}
							}
						}while(!flgGoNxt);
						for(int i = 0; i<nxtbonds.Count;i++){
							if(nxtbonds[i].Count>0){
								DelonDictNxtPnt(nxtbonds[i][0], LastPnt[i]);//Чистим словарь
								if(!flgBordered){
									flgBordered = nxtbonds[i][0].isBordering;
								}
							}
						}
					} 
					//Добавляем в список "дырок" список точек контура очередной "дырки"
					CrossPnt[] tSeq = new CrossPnt[Seq.Count];
					List<CrossPnt> tlSeq = new List<CrossPnt>();
					Seq.CopyTo(tSeq);
					tlSeq.AddRange(tSeq);
					if(flgBordered){
						Hol.BorderHoles.Add(tlSeq);
					}
					else{
						Hol.InnerHoles.Add(tlSeq);
					}
					Seq.Clear();
				}
			}while(LastS.Count>0); //(CrossPntSt.Nets.Count>1);
			return Hol;
		}

		//FirstPnt возможно они и не нужен!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		public static List<CrossPnt> GetnxtPnts(CrossPnt pnt, Vector3 NormTri, bool FirstPnt){
			List<CrossPnt> pnts = new List<CrossPnt>();
			if(CrossPntSt.Nets.ContainsKey(pnt)){
				CrossPnt[] tmppnts = new CrossPnt[CrossPntSt.Nets[pnt].Count];
				CrossPntSt.Nets[pnt].CopyTo(tmppnts);
				pnts.AddRange(tmppnts);
				if(pnts.Count>2){
					pnts = GetSolidBond(pnts, NormTri);
				}
				else{
					float fff=0;
				}
			}
			return pnts;
		}
		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Необходимо будет править
		public static List<CrossPnt> GetSolidBond(List<CrossPnt> AllPnts, Vector3 NormTri){
			List<CrossPnt> tmp = new List<CrossPnt>();
			tmp.Add (AllPnts[0]);
			tmp.Add (AllPnts[1]);
			return tmp;
		}

		public static void DelonDictNxtPnt(CrossPnt cPnt, CrossPnt lastcPnts){
			//Скорее всего эту проверку на содержание ключа можно убрать!!!!!!!!!!!!!!!!
			if(CrossPntSt.Nets.ContainsKey(cPnt)){
				CrossPntSt.Nets[cPnt].Remove(lastcPnts);
				if(CrossPntSt.Nets[cPnt].Count==0){
					CrossPntSt.Nets.Remove(cPnt);
				}
			}
		}

		public static List<CrossPnt> CrossTriang(ref List<CrossPnt[]> POfBr,PntCL[] tr1, PntCL[] tr2, Vector3 norm1){
			List<CrossPnt> crsPnts = new List<CrossPnt>();
			Dictionary<Vector3, int> cPnt = new Dictionary<Vector3, int>();//C помощью словаря избавимся от задваивания в случае, когда треугольники пересекутся своей стороной
			int k =0;
			Vector3[] tt1= new Vector3[]{tr1[0].v, tr1[1].v, tr1[2].v, tr1[0].v};
			Vector3[] tt2= new Vector3[]{tr2[0].v, tr2[1].v, tr2[2].v, tr2[0].v};
			PntCL[] ttr1 = new PntCL[]{tr1[0], tr1[1], tr1[2], tr1[0]};
			PntCL[] ttr2 = new PntCL[]{tr2[0], tr2[1], tr2[2], tr2[0]};

	/*
	 *	Необходимо искать точки граничные и внутренние в разных циклах, поскольку иначе может получиться, что будет 1 и та же точка 2 раза 
	 *  (если она лежит на границе) она будет в обоих циклах. Может попасться оба раза за 1 цикл и тогда точку исключим.
	 *  но нужно помнить, что у 2х треугольников общие точки лежат на максимум 1 отрезке (а минимум 1 точка).
	 */
			for(int i=0; i<tt1.Length-1;i++){
				Vector3 cP = new Vector3();

				if(CrossTrianglBySegment (ref cP, tt2, new Vector3[]{tt1[i], tt1[i+1]})){
					//Интерполируем нормаль, создавая объект PntCL
					PntCL cPcL = new PntCL(cP, VLerP(ttr1[i].n, ttr1[i+1].n, ttr1[i].v, ttr1[i+1].v, cP));
					cPcL.uv = VLerP2d(ttr1[i].uv, ttr1[i+1].uv, ttr1[i].v, ttr1[i+1].v, cP);
					cPcL.tng = VLerP4d(ttr1[i].tng, ttr1[i+1].tng, ttr1[i].v, ttr1[i+1].v, cP);
					//Округляем значения координат
					CrossPnt tmpCrPnt = new CrossPnt(cPcL, true, new Vector3(), i);
					if(!cPnt.ContainsKey(cP)){
						if(!CrossPntSt.cPntIsBord.ContainsKey(cP)){
							//Записываю в глобальный словарь все точки, лежащие на стороне треуг-ка даже если точка уже есть,
							CrossPntSt.cPntIsBord.Add (cP, tmpCrPnt);
						}
						cPnt.Add (cP, k);
						crsPnts.Add (tmpCrPnt);
						k++;
					}
					else{
						if(!CrossPntSt.cPntIsBord.ContainsKey(cP)){
							//Записываю в глобальный словарь все точки, лежащие на стороне треуг-ка даже если точка уже есть,
							//ведь она может там появиться при просчете в следующем цикле , с false
							//Мб следует тут ещё раз принудительно .isBordering поставить в true,
							//поскольку прохода с исправлением в другой раз может не быть. (Тогда возм-но придётся создать словарь CrossPnt-ов
							CrossPntSt.cPntIsBord.Add (cP, tmpCrPnt);
							crsPnts[0].isBordering=true;
						}
					}
				}
				if(CrossTrianglBySegment (ref cP, tt1, new Vector3[]{tt2[i], tt2[i+1]})){
					//найдём барицентрические координаты в треугольнике через площади. по ним осуществим интерполяцию

					float S = AreaOfTriangleByV(ttr1[0].v,ttr1[1].v,ttr1[2].v);
					float Sa = AreaOfTriangleByV(ttr1[1].v,ttr1[2].v,cP)/S;
					float Sb = AreaOfTriangleByV(ttr1[2].v,ttr1[0].v,cP)/S;
					float Sc = AreaOfTriangleByV(ttr1[0].v,ttr1[1].v,cP)/S;

					Vector3 tmpVec3 = Sa*ttr1[0].n+Sb*ttr1[1].n+Sc*ttr1[2].n;
					PntCL cPcL = new PntCL(cP, tmpVec3);

					cPcL.uv = Sa*ttr1[0].uv+Sb*ttr1[1].uv+Sc*ttr1[2].uv;

					cPcL.tng = Sa*ttr1[0].tng+Sb*ttr1[1].tng+Sc*ttr1[2].tng;

					if(!cPnt.ContainsKey(cP)){
						cPnt.Add (cP, k);
						if(CrossPntSt.cPntIsBord.ContainsKey(cP)){
							//Если точка граничная
							crsPnts.Add(CrossPntSt.cPntIsBord[cP]);
						}
						else{
							//Если точка не граничная
							crsPnts.Add (new CrossPnt(cPcL, false, new Vector3()));
						}
						k++;
					}
				}
			}
			//Не будем учитывать точки (тоесть прикосновения поверхностей 1й точкой
			if (crsPnts.Count < 2){
				crsPnts.Clear();
			}else{
				AddDict(crsPnts[0], crsPnts[1]);
				AddDict(crsPnts[1], crsPnts[0]);
			}
			return crsPnts;
		}
		public static float AreaOfTriangleByV(Vector3 v1, Vector3 v2, Vector3 v3){
			return Vector3.Cross (v2-v1,v3-v1).magnitude;
		}

		public static Vector3 VLerP(Vector3 n1, Vector3 n2, Vector3 v1, Vector3 v2, Vector3 v3){
			Vector3 t1= (v2-v1);
			Vector3 t2= (v3-v1);
			Vector3 t3 = Vector3.Lerp(n1,n2,(t2.magnitude)/t1.magnitude);
			return t3;
		}

		public static Vector2 VLerP2d(Vector2 uv1, Vector2 uv2, Vector3 v1, Vector3 v2, Vector3 v3){
			Vector3 t1= (v2-v1);
			Vector3 t2= (v3-v1);
			Vector2 t3 = Vector2.Lerp(uv1,uv2,(t2.magnitude)/t1.magnitude);
			return t3;
		}

		public static Vector2 VLerP4d(Vector4 tn1, Vector4 tn2, Vector3 v1, Vector3 v2, Vector3 v3){
			Vector3 t1= (v2-v1);
			Vector3 t2= (v3-v1);
			Vector4 t3 = Vector4.Lerp(tn1,tn2,(t2.magnitude)/t1.magnitude);
			return t3;
		}

		public static void AddDict(CrossPnt CPnt0, CrossPnt CPnt1){
			float f = 0.001f; //Этот параметр отвечает за точность
			//Если точки совпадают
			if(ApproxEqVec(ref CPnt0.Pnt.v, ref CPnt1.Pnt.v,f)){
			}else{
				if(CrossPntSt.Nets.ContainsKey(CPnt0)){
					CrossPntSt.Nets[CPnt0].Add (CPnt1);
				}
				else{
					List<CrossPnt> tmpList = new List<CrossPnt>();
					tmpList.Add (CPnt1);
					CrossPntSt.Nets.Add (CPnt0, tmpList);
				}
				if(CrossPntSt.Nets.Count>2){
					string s = "";
				}
			}
		}

		public static bool ApproxEqVec(ref Vector3 vtmp1, ref Vector3 vtmp2, float f){
			if(Mathf.Abs (vtmp1.x-vtmp2.x)<f&&
			   Mathf.Abs (vtmp1.y-vtmp2.y)<f&&Mathf.Abs (vtmp1.z-vtmp2.z)<f){
				vtmp2 = vtmp1;
				return true;
			}
			return false;
		}

		public static List<PntCL>[] GetPerimetrPnts1 (TrSrCL tri_, List<List<CrossPnt>> BordPnts, bool flg){
			List<PntCL> StaydTri = new List<PntCL>(tri_.tPnts);
			List<PntCL> tmpTri = new List<PntCL>(tri_.tPnts);
			tmpTri.Add (tmpTri[0]);
			PntCL[] tri1 = new PntCL[tmpTri.Count];
			tmpTri.CopyTo(tri1);//Ввожу дополнительный массив, которым буду удалять всё.
			PntCL[] tri = new PntCL[]{tmpTri[0],tmpTri[1],tmpTri[2],tmpTri[3]};//данный массив в этой функцие изменяться не должен

//			List<List<CrossPnt>> copyBordPnts = new List<List<CrossPnt>>(BordPnts);
			List<List<Slice>> LSlices = new List<List<Slice>>();

			List<List<PntCL>> LPerPnts = new List<List<PntCL>>();//объект (треугольник) может быть разбит на несколько подобъектов (многоугольников)

			List<List<float>> LDisnts = new List<List<float>>();//Сортировку делаем только по ближайшим точкам
			Dictionary<float, List<Slice>>[] dDistToSlise = new Dictionary<float, List<Slice>>[tri.Length-1];//По расстояниям получаем объект среза
			Dictionary<CrossPnt, int> dPntToSideTri = new Dictionary<CrossPnt, int>();//Ссылка по точке на сторону треугольника

			for (int i=0; i< tri.Length-1;i++){	
				dDistToSlise[i] = new Dictionary<float, List<Slice>>();//Инициируем все словари, они нам все сразу нужны будут даже в первом цикле
				LDisnts.Add (new List<float>());
			}

			for (int i=0; i< tri.Length-1;i++){	//Цикл по всем точкам данного треугольника
				for(int k=BordPnts.Count-1; k >= 0;k--){  //Идем циклом по всем граничным "срезам" (отдельным линиям пересечения, пересекающим границу дан. треугольника)
					int cntPnts = (BordPnts[k].Count-1);//Индекс последней точки в текущем срезе
					int ck = 0;
					List<CrossPnt> ListPnts = new List<CrossPnt>();//Список начальной и конечной точки
					for(int j=0; j<2;j++){ //Берем начальную и конечную точки граничного "среза"
						if(i==BordPnts[k][j*cntPnts].nSide1){//Если точка "среза" на текущей стороне треугольника
							ck++;
							ListPnts.Add(BordPnts[k][j*cntPnts]);
						}
					}
					if(ck>0){//Если точек  на границе > 0
						Vector3[] triVec = new Vector3[]{tri[0].v,tri[1].v,tri[2].v,tri[3].v};
						Slice SL = NearPoint1(triVec, ListPnts.ToArray(), BordPnts[k], i, BordPnts);
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
						dDistToSlise[i][j] = MultySliceSort(dDistToSlise[i][j], tri[i+1].v-tri[i].v);
					}
				}
				//Сортируем всё в LSlices
				LDisnts[i].Sort();
				for (int x=0; x<LDisnts[i].Count;x++){
					LSlices.Add (dDistToSlise[i][LDisnts[i][x]]);
				}
			}
			
			List<PntCL> tmpPnts = new List<PntCL>();
			int cnt=0;
			FstPnt FirstPnt;
			FirstPnt.v = new PntCL(new Vector3(), new Vector3());
			FirstPnt.n = new Vector3();
			FirstPnt.triNumb = -1;
			int itemp3=0;//Текущая вершина

			while (LSlices.Count>0&&cnt>-1){ //&& cnt<LSlices.Count){
				bool reverced=false;
				Vector3 v1 = tri[LSlices[cnt][0].i1].v;
				Vector3 v2 = tri[LSlices[cnt][0].i1+1].v;

				int tmpflgSign=0;
				if(flg){
					tmpflgSign=1;
				}
				else{
					tmpflgSign=-1;
				}
				if((tmpflgSign*SignProjectV3toV3(v2-v1, LSlices[cnt][0].p1.Norm))>0){
					//Идём по прямой дальше
					if(FirstPnt.triNumb==-1){
						//Если точка первая, добавим начальную вершину треугольника
						tmpPnts.Add (tri[LSlices[cnt][0].i1]);

						//Удаляем из списка удалённых вершин треугольника
						StaydTri.Remove(tri1[LSlices[cnt][0].i1]);

						FirstPnt.triNumb = LSlices[cnt][0].i1;
						FirstPnt.v = tmpPnts[0];
					}
					tmpPnts.AddRange (LSlices[cnt][0].lst.Select (info => info.Pnt).ToList());

					//Удаляем из списка дистанций текущую дистанцию
					LDisnts[LSlices[cnt][0].i1].Remove(LSlices[cnt][0].d1);
				}else{
					//Вставляем точки сзади (до точки лежащей на другой стороне)

					LSlices[cnt][0].lst.Reverse();
					reverced=true;

					tmpPnts.InsertRange (0,LSlices[cnt][0].lst.Select (info => info.Pnt).ToList());

					if(FirstPnt.triNumb==-1){
						//Если точка первая, добавим начальную вершину треугольника
						FirstPnt.triNumb = LSlices[cnt][0].i1;
						FirstPnt.v = tmpPnts[0];
					}

					int itemp = LSlices[cnt][0].i2;//i1 Ищу вершину до первой точки
					float tmpF = NearestPrevSecondPnt(LDisnts[itemp].ToArray(),LSlices[cnt][0].d2);
					if(tmpF>0){
						tmpPnts.Insert(0, dDistToSlise[itemp][tmpF][0].p1.Pnt);
						//Удаляем из списка дистанций текущую дистанцию
						LDisnts[LSlices[cnt][0].i2].Remove(tmpF);
					}else{
						tmpPnts.Insert(0, tri[LSlices[cnt][0].i2]);

						//Удаляем из списка оставшихся вершин треугольника
						StaydTri.Remove(tri1[LSlices[cnt][0].i2]);
					}
				}

				if(ApproxEqual(tmpPnts[0].v, tmpPnts.Last ().v)){
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
					v1 = tri[LSlices[cnt][0].i2].v;
					v2 = tri[LSlices[cnt][0].i2+1].v;
				}else{
					itemp_1 = LSlices[cnt][0].i2;
					itemp_2 = LSlices[cnt][0].i1;
					d_1 = LSlices[cnt][0].d2;
					d_2 = LSlices[cnt][0].d1;
					v1 = tri[LSlices[cnt][0].i1].v;
					v2 = tri[LSlices[cnt][0].i1+1].v;
				}
				//Нельзя искать только пока LSlices.Count>0, поскольку если разбито на несколько полигонов, они все ещё останутся

				//Попробуем найти следующую точку на данной стороне
				float tmpFf =0;

				Vector3 Norm  = LSlices[cnt][0].p2.Norm;

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
						StaydTri.Remove(tri1[itemp2]);

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

				if(ApproxEqual(tmpPnts[0].v, tmpPnts.Last ().v)){
					tmpPnts[0] = tmpPnts.Last ();
					NxtList(tmpPnts, LPerPnts);
					cnt=0;
				}
			}

			for(int i=0; i<StaydTri.Count;i++){
				FillUpVTries(StaydTri[i], tri1);
			}
			DelCurTriangle(tri_);
			NxtList(tmpPnts, LPerPnts);
			return LPerPnts.ToArray();
		}

		public static void DelCurTriangle(TrSrCL tri){
			for(int i = 0; i<tri.tPnts.Count;i++){
				CrossPntSt.Tris[tri.tPnts[i].v].Remove(tri);
			}
		}
		//Во всех оставшихся вершинах удаляю ссылку на удалённую вершину треугольника
		public static void FillUpVTries(PntCL tri, PntCL[] triVss){
			for(int x = 0; x<CrossPntSt.Tris[tri.v].Count;x++){
				for(int y=0; y<triVss.Length;y++){
					CrossPntSt.Tris[tri.v][x].tPnts.Remove(triVss[y]);
				}
			}
            CrossPntSt.LVects.Add(tri);
        }

		public static List<Slice> MultySliceSort(List<Slice> listSl, Vector3 sideTr){
			List<float> listAngle = new List<float>();
			Dictionary<float, Slice> dAnglToSlice = new Dictionary<float, Slice>();
			for(int i=0; i<listSl.Count; i++){
				float Angl = UltAnglByVect(listSl[i].p2.Pnt.v-listSl[i].p1.Pnt.v, sideTr);
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

		public static void NxtList(List<PntCL> lst, List<List<PntCL>> listlst){
			List <PntCL> tmppVec = new List<PntCL>(lst);
			listlst.Add (tmppVec);
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

		public static Slice NearPoint1(Vector3[] v, CrossPnt[] Pnts, List<CrossPnt> lstCrPnt, int j, List<List<CrossPnt>> sdsd){
			List<float> LDistance = new List<float>();
			int j1=j;
			int j2=j;
			Dictionary<float, CrossPnt> dDistances = new Dictionary<float, CrossPnt>();
			for(int i=0; i< Pnts.Length;i++){
				float fDict = (Pnts[i].Pnt.v-v[j]).sqrMagnitude;
				LDistance.Add (fDict);
				if(!dDistances.ContainsKey(fDict)){
					dDistances.Add (fDict,Pnts[i]);
				}
			}
			LDistance.Sort ();

			//Создаём направление
			if(dDistances[LDistance[0]]!=lstCrPnt[0]){
				List<CrossPnt> lstCrPntRev = new List<CrossPnt>(lstCrPnt);
				lstCrPntRev.Reverse();
				lstCrPnt = lstCrPntRev;
			}
			if(Pnts.Length==1){//Если на текущей стороне треугольника только одна точка
				CrossPnt tmpSecPnt = lstCrPnt[lstCrPnt.Count-1];
				for (int i=0; i< v.Length-1;i++){	//Цикл по всем точкам данного треугольника
					if(i==tmpSecPnt.nSide1){//Если точка "среза" на текущей стороне треугольника
						LDistance.Add ((v[i]-tmpSecPnt.Pnt.v).sqrMagnitude);//Добавляем в Slice расстояние 2й точки среза до соотвествующей точки соотвествующей стороны треугольника
						dDistances.Add (LDistance.Last (),tmpSecPnt);
						j2=i;
					}
				}
			}
			Slice tmpSl=null;
			try{
				tmpSl = new Slice(dDistances[LDistance[0]], dDistances[LDistance[1]], LDistance[0], LDistance[1], lstCrPnt, j1, j2);
			}
			catch(System.ArgumentOutOfRangeException e){


			}

			return tmpSl;
		}

		public static float SignProjectV3toV3(Vector3 v1, Vector3 v2){
			return Round(Vector3.Dot (v1,v2),rounder);
		}
		public static bool IsParalVec(Vector3 v1, Vector3 v2){
			if((1-(Vector3.Dot(v1,v2)/(v1.magnitude*v2.magnitude)))<0.0000001){
				return true;
			}
			return false;
		}

		public static bool CrossTrianglBySegment(ref Vector3 CrPoint, Vector3[] tri, Vector3[] seg){
			Vector3 A = tri[0];
			Vector3 B = tri[1];
			Vector3 C = tri[2];
			Vector3 D = seg[0];
			Vector3 E = seg[1];

			Vector3 p1 = D-A;
			Vector3 p2 = D-E;
			Vector3 p3 = B-A;
			Vector3 p4 = C-A;

			float Delta = Vector3.Dot(p2, Vector3.Cross (p3,p4));
			float t = 0f;
			float u = 0f;
			float v = 0f;

			if (Delta==0f){
				//Отрезок параллелен плоскости
			}
			else{
				t = Vector3.Dot (p1,Vector3.Cross(p3,p4))/Delta;
				if(t>=0&&t<1){
					u = Vector3.Dot (p2,Vector3.Cross(p1,p4))/Delta;
					if(u>=0&&u<1){
						v = Vector3.Dot (p2,Vector3.Cross(p3,p1))/Delta;
						if(v>=0&&v<1){
							if((u+v)<=1){
								CrPoint = A+p3*u+p4*v;
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		public static Vector3[] TrmWrdPntToMshPnt(Vector3[] v, GameObject go, float f=-1){
//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
                if (f > -1) {
                    v[i] = new Vector3(v[i].x / f, v[i].y / f, v[i].z / f);
                }
				v[i] = go.transform.InverseTransformPoint(v[i]);
			}
			return v;
		}

		public static Vector3[] TrmMshPntToWrdPnt(Vector3[] v, GameObject go, float f=-1f){
//			float f = 100f;//Увеличиваю точность при расчётах (с учётом моих округлений в будущем)
			for (int i=0; i< v.Length; i++){
				v[i] = go.transform.TransformPoint(v[i]);
                if (f > -1) {
                    v[i] = v[i] * f;
                }
			}
			return v;
		}

		public static float Round(float f, float n){
			float p = Mathf.Pow (10,n);
			return (Mathf.Round (f*p))/p;
		}

		public static bool ApproxEqual (Vector3 v1, Vector3 v2){
			if (Mathf.Approximately (v1.x, v2.x)&&Mathf.Approximately (v1.y, v2.y)&&Mathf.Approximately (v1.z, v2.z)) {
				return true;
			}
			return false;
		}

		public static List<PntCL>[] GetArrWithHole2(List<CrossPnt>[] PntsV, TrSrCL Triangl, Vector3 nrm, bool flg){
			DelCurTriangle(Triangl);


			List<PntCL>[] Result=new List<PntCL>[]{new List<PntCL>(), new List<PntCL>()};
			for(int i=0; i<PntsV.Length;i++){
				PntsV[i].RemoveAt (PntsV[i].Count-1);

				//Для начала, найдём "ушную" вершину и определим правильный обход вырезу (путём нахождения ближайшей вершины к вершине треугольника)
				int Lft=0;
				float dist=0;

/*
				for(int ii=0; ii<PntsV[i].Count-1;ii++){
					float tmp = (PntsV[i][ii].Pnt.v-Triangl.tPnts[0].v).magnitude;
					if (dist == 0){
						dist = tmp;
						Lft = ii;
					}else if(dist>tmp){
						dist=tmp;
						Lft = ii;
					}
				}
*/

				for(int ii=0; ii<PntsV[i].Count-1;ii++){
					float tmp = (PntsV[i][ii].Pnt.v-ProjectPntToLine(Triangl.tPnts[1].v,Triangl.tPnts[0].v, PntsV[i][ii].Pnt.v)).magnitude;
					if (dist == 0){
						dist = tmp;
						Lft = ii;
					}else if(dist>tmp && Vector3.Cross (PntsV[i][ii-1].Pnt.v-PntsV[i][ii].Pnt.v, PntsV[i][ii+1].Pnt.v-PntsV[i][ii].Pnt.v).magnitude>0){
						dist=tmp;
						Lft = ii;
					}
				}
				PntsV[i].RemoveAt (PntsV[i].Count-1);

				List<CrossPnt> PntsV_C = new List<CrossPnt>(PntsV[i]);

				PntsV_C.Add (PntsV_C[0]);
				PntsV_C.Insert(0,PntsV[i].Last ());

				if(flg){
					if(!IsParalVec(nrm, Vector3.Cross(PntsV_C[Lft].Pnt.v-PntsV_C[Lft+1].Pnt.v, PntsV_C[Lft+2].Pnt.v-PntsV_C[Lft+1].Pnt.v))){
						PntsV[i].Reverse();
						Lft=PntsV[i].Count-Lft-1;
					}
					List<CrossPnt> Rotate = new List<CrossPnt>();
					Rotate.AddRange(PntsV[i]);
					List<CrossPnt> tmP = new List<CrossPnt>();
					tmP.AddRange(Rotate.GetRange(0,Lft));
					Rotate.RemoveRange(0, Lft);
					Rotate.AddRange(tmP);

					PntsV[i]= new List<CrossPnt>(Rotate);
					Rotate.Clear ();

					PntsV[i].Add (PntsV[i][0]);

					PntsV_C = new List<CrossPnt>(PntsV[i]);

					List<PntCL> tmpResult= new List<PntCL>();
					int tmpInt=0;
					for(int j=0; j< Triangl.tPnts.Count;j++){
						for(int ii=0; ii<PntsV_C.Count;ii++){
							if(SidesUnCrossed(Triangl.tPnts[j].v, PntsV_C[ii].Pnt.v, PntsV[i])){
								if(j==0){
									tmpResult.Add (Triangl.tPnts[j]);
									tmpResult.Add (PntsV_C[ii].Pnt);
									PntsV_C.Remove(PntsV_C[ii]);
									break;
								}else{
									tmpResult.Add (PntsV_C[ii].Pnt);
									tmpResult.Add (Triangl.tPnts[j]);
									tmpResult.Add (tmpResult[0]);
									Result[0].AddRange (tmpResult);
									tmpResult.Clear ();

									Result[1].Add(Triangl.tPnts[j]);
									for(int jj=j+1; jj< Triangl.tPnts.Count;jj++){
										Result[1].Add(Triangl.tPnts[jj]);
									}
									Result[1].Add (Result[0][0]);
									for(int jj =PntsV_C.Count-1; jj>=ii;jj--){
										Result[1].Add(PntsV_C[jj].Pnt);
									}
									Result[1].Add (Result[1][0]);
									Result[0].Reverse ();
		
									return Result;
								}
							}else{
								tmpResult.Add (PntsV_C[ii].Pnt);
							}

						}
					}
				}
				else{
					if(IsParalVec(nrm, Vector3.Cross(PntsV_C[Lft].Pnt.v-PntsV_C[Lft+1].Pnt.v, PntsV_C[Lft+2].Pnt.v-PntsV_C[Lft+1].Pnt.v))){
						PntsV[i].Reverse();
					}
					Result = new List<PntCL>[1];
					Result[0] = new List<PntCL>();
					for(int j=0;j<PntsV[i].Count;j++){
						Result[0].Add (PntsV[i][j].Pnt);
					}
					
				}
			}
			return Result;
		}

		public static Vector3 ProjectPntToLine(Vector3 A, Vector3 B, Vector3 P){
			Vector3 AB = (B-A);
			return A+(AB).normalized*Vector3.Dot (P-A, AB)/AB.magnitude;
		}

		public static bool SidesUnCrossed(Vector3 v1, Vector3 v2, List<CrossPnt> h){
			for(int i=0; i<h.Count-1;i++){
				if(Vect3Crossed(v1,v2,h[i].Pnt.v, h[i+1].Pnt.v)){
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


		public static Mesh PolCreate(Vector3[] V0, Vector3[] N0, Vector2[] UV0, int[] TRI, Vector4[] TNGs){
			Mesh mesh = new Mesh();
			mesh.vertices = V0;
			mesh.triangles = TRI;
			mesh.normals = N0;
			mesh.uv = UV0;
			mesh.tangents = TNGs;
//			mesh.RecalculateBounds();
//			mesh.RecalculateNormals();
			return mesh;
		}

	}


//!!!!!!!!______________________________другие классы_________________________!!!!!!!!!!!!!!!!!!!!!
	public class Holes{
		public List<List<CrossPnt>> BorderHoles;
		public List<List<CrossPnt>> InnerHoles;
		public Holes(){
			BorderHoles = new List<List<CrossPnt>>();
			InnerHoles = new List<List<CrossPnt>>();
		}
		public Holes(Holes H){
			BorderHoles = new List<List<CrossPnt>>(H.BorderHoles);
			InnerHoles = new List<List<CrossPnt>>(H.InnerHoles);
		}
		//Можно добавить функцию объединения коллекций в одну
	}
	public class Slice{
		public CrossPnt p1;
		public CrossPnt p2;
		public float d1;
		public float d2;
		public bool iSPntOfTriangle; //Вершина треугольника (?)
		public List<CrossPnt> lst;//Упорядоченный список от ближайшей точки к дальней
		public int i1;//Текущая сторона треугольника для точки 1
		public int i2;//Текущая сторона треугольника для точки 2
		public Slice(CrossPnt _p1, CrossPnt _p2, float _d1, float _d2, List<CrossPnt> listArr, int j1 , int j2){
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
		public PntCL v;
		public Vector3 n;
		public int triNumb;
	}
	public class MshAttributes{
		public int[] trs;
		public Vector3[] v;
		public Vector3[] n;
		public Vector2[] uv;
		public Vector4[] tng;
		public MshAttributes(Vector3[] _v, Vector3[] _n, Vector2[] _uv, Vector4[] _tng, int[] _trs){
			this.v = _v;
			this.n = _n;
			this.uv = _uv;
			this.tng = _tng;
			this.trs = _trs;
		}
	}
	public class PntCL{
		public int indSubMsh; //Индекс субмеша, которому вертекс принадлежит
		public Vector3 v;
		public Vector3 n;
		public Vector2 uv;
		public Vector4 tng;

		public PntCL(Vector3 _v, Vector3 _n, Vector2 _uv =  new Vector2(), Vector4 _tng =  new Vector4(), int i=0){
			this.indSubMsh=i;
			this.v = _v;
			this.n = _n;
			this.uv = _uv;
			this.tng = _tng;
		}

        public override string ToString() {
            return string.Format("[V={0}, N={1}, UV={2}", v, n, uv);
        }
    }
	public class TriCL{
		public int indSubMsh=0; //Индекс субмеша, которому вертекс принадлежит
		public int t = 0;
		
		public TriCL(int iSub, int tInd){
			this.indSubMsh = iSub;
			this.t = tInd;
		}
	}

	//Не ясно, что лучше, класс или структура!!!!!!!!!!!!
	public class TrSrCL{
		public List<PntCL> tPnts;
		public int IndSide;
	}

	public static class CrossPntSt{
		public static Dictionary<Vector3, CrossPnt> cPntIsBord = new Dictionary<Vector3, CrossPnt>();//Словарь точек на границе.
		public static Dictionary <CrossPnt,List<CrossPnt>> Nets = new Dictionary <CrossPnt,List<CrossPnt>>(); //Словарь ссылок по точкам на соседние по рёбрам точки.
		public static Dictionary <Vector3,List<TrSrCL>> Tris;//= new Dictionary <Vector3,List<TrSrCL>>(); //Словарь треугольников по точкам (ссылка каждой вершины на свой треугольник)
		public static List <PntCL> LVects = new List <PntCL>();//Список первоначальных точек, по которым удаляем следующие лишние треугольники

	}

	public class CrossPnt: System.IEquatable<CrossPnt>{
		public bool isBordering;
		public bool isPaired = false;//Конечные точки "среза" обе лежат на 1 границе треугольника (true) 
		public bool isUsedPair = false;
		public PntCL Pnt;
		public Vector3 Norm;
		public int nSide1;//Строна треугольника первого объекта

//		public static Dictionary<Vector3, CrossPnt> cPntIsBord = new Dictionary<Vector3, CrossPnt>();//Словарь точек на границе.
//		public static Dictionary <CrossPnt,List<CrossPnt>> Nets = new Dictionary <CrossPnt,List<CrossPnt>>(); //Словарь ссылок по точкам на соседние по рёбрам точки.
//		public static Dictionary <Vector3,List<TrSrCL>> Tris;//= new Dictionary <Vector3,List<TrSrCL>>(); //Словарь треугольников по точкам (ссылка каждой вершины на свой треугольник)
//		public static List <PntCL> LVects = new List <PntCL>();//Список первоначальных точек, по которым удаляем следующие лишние треугольники

		public void ChangeValue(Vector3 v){
			this.Pnt.v.Set (v.x,v.y,v.z);
		}
		public CrossPnt(PntCL v, bool flg, Vector3 n, int i=-1){
			isBordering = flg;
			Pnt = v;
			Norm = n; //Необходим только для точек пересечения
			nSide1 = i;
		}

		public bool Equals(CrossPnt other){
			//Уменьшая значение f мы уменьшим точность. Зато с этим уменьшатся и кол-во ошибочных пропаданий изображения
			//Потом необходимо доработать этот параметр
			float f = 0.01f; //28.07.2018 была 0.001f. Для уменьшения вероятности ошибки пришлось снизить точность
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return (Mathf.Abs (this.Pnt.v.x-other.Pnt.v.x)<f&&Mathf.Abs (this.Pnt.v.y-other.Pnt.v.y)<f&&
			        Mathf.Abs (this.Pnt.v.z-other.Pnt.v.z)<f);
		}

		public override bool Equals(object obj){
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((CrossPnt) obj);
		}

		public override int GetHashCode(){
//			unchecked
//			{
			//Такое, как я прочитал уменьшает скорость GetHashCode
			//В нальнейшем, необходимо доработать эту вещь.
			return 0;//this.Pnt.GetHashCode();//Pnt.GetHashCode();
//			}
		}

        public override string ToString() {
            return string.Format("[V={0}, N={1}, UV={2}", Pnt.v, Pnt.n, Pnt.uv);
        }
    }

//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//Триангуляция

	public class Triangulation {
		public static int[] GetTriangles(List<Vector3> v1, int nPosit, Vector3 nrm){
			List<Vector2> v =GetProjection(nrm,v1); //Получаем проекцию на плоскость
			
			bool ClockDirCalculated = false;//Используем определение по часовой или против для поворота плоскости в нужном нам направлении
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
			
			float VectorCrosOut = OrtCrosVect (nLeft, copyV_t[v.IndexOf(nLeft)], nLeft, copyV_t[v.IndexOf(nLeft)+2]);
			
			int index = 0;
			int k = 0;
			float tmpOrt;
			do{
				//Сделаю пока от 0 до count.
				//Но по-хорошему, надо замкнуть и сам цикл сделать бесконечным, пока не останется лишь триугольник
				Vector2 nbefore=  index>0 ? copyV [(index+copyV.Count-1) % copyV.Count]: copyV[copyV.Count-1];
				Vector2 n = copyV [index];
				Vector2 nafter = index < copyV.Count-1  ? copyV [(index+1) % copyV.Count] : copyV[0];
				
				tmpOrt = OrtCrosVect (n, nbefore, n, nafter);
				if (Mathf.Abs(tmpOrt-VectorCrosOut)<0.001f||copyV.Count==3) {
					if(isOuter(nbefore, n, nafter, copyV)){
						resultT.AddRange (new int[]{ v.IndexOf (nbefore)+nPosit, v.IndexOf (n)+nPosit, v.IndexOf (nafter)+nPosit });			
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
		public static List<Vector2> GetProjection(Vector3 nrm, List<Vector3> v1){
			List<Vector2> v = new List<Vector2>();
			bool fff = DVName.DV.IsParalVec(nrm, new Vector3(0,0,1));
			if(!IsParralVec(nrm, Vector3.Cross (new Vector3(1,0,0), new Vector3(0,1,0)))){
				for(int i=0;i<v1.Count;i++){
					v.Add (new Vector2(v1[i].x, v1[i].y));
				}
				nrm = new Vector3(0f,0f, nrm.z);
			}else if(!IsParralVec(nrm, Vector3.Cross (new Vector3(1,0,0), new Vector3(0,0,1)))){
				for(int i=0;i<v1.Count;i++){
					v.Add (new Vector2(v1[i].x,v1[i].z));
				}
				//Меняем параметры нормали (чтобы корректно был расчёт на параллельность)
				nrm=new Vector3(0f,0f,-nrm.y);
			}else if(!IsParralVec(nrm, Vector3.Cross (new Vector3(0,1,0), new Vector3(0,0,1)))){
				for(int i=0;i<v1.Count;i++){
					v.Add (new Vector2(v1[i].y,v1[i].z));
				}
				nrm = new Vector3(0f,0f, nrm.x);
			}else{
				for(int i=0;i<v1.Count;i++){
					v.Add (new Vector2(v1[i].x,v1[i].y));
				}	
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
					float v23 = (D-C).y*(A-C).x-(D-C).x*(A-C).y;
					float v43 = (D-C).y*(B-C).x-(D-C).x*(B-C).y;
					
					float Dot1 = (D-C).x*(A-C).x+(D-C).y*(A-C).y;
					float Dot2 = (D-C).x*(B-C).x+(D-C).y*(B-C).y;
					
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
						exist=Mathf.Abs(Cross1-Cross2)>0;
					}
					//Если относительно смежной диагонали точки исследуемая вершина и несмежная вершина лежат по разные стороны, то
					if (exist) {
						float v12 = (A-B).y*(D-B).x-(A-B).x*(D-B).y;
						float v32 = (A-B).y*(C-B).x-(A-B).x*(C-B).y;
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
		
		private static float OrtCrosVect(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4){
			float fff = ((v2-v1).y*(v4-v3).x-(v2-v1).x*(v4-v3).y);
			if(fff==0){
				return 0f;
			}
			return Mathf.Sign((v2-v1).y*(v4-v3).x-(v2-v1).x*(v4-v3).y);
		}
		
		public static bool IsParralVec(Vector3 v1, Vector3 v2){
			if(Mathf.Abs(Vector3.Dot(v1,v2)/(v1.magnitude*v2.magnitude))<0.000001f){
				return true;
			}
			return false;
		}
	}
    public static class VectorExtension {
        public static Vector3 Vx1 = new Vector3(1, 0, 0);
        public static Vector3 Vy1 = new Vector3(0, 1, 0);
        public static Vector3 Vz1 = new Vector3(0, 0, 1);

        public static float rFactor = 0.0001f;// на 0.00001f ошибки
        private static float rounder = 5f;//Округление необходимо корректировать (лучше динамически)

        public static bool ApproxEqual(Vector3 v1, Vector3 v2) {
            if (Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y) && Mathf.Approximately(v1.z, v2.z)) {
                return true;
            }
            return false;
        }


        public static Vector3 ProjectPntToLine(Vector3 A, Vector3 B, Vector3 P) {
            Vector3 AB = (B - A);
            return A + (AB).normalized * Vector3.Dot(P - A, AB) / AB.magnitude;
        }



        public static bool Vect3Crossed(Vector3 A, Vector3 B, Vector3 C, Vector3 D) {
            Vector3 tmp1 = Vector3.Cross(B - A, B - C);
            Vector3 tmp2 = Vector3.Cross(B - A, B - D);
            if (Vector3.Dot(tmp1, tmp2) < 0) {// общая точка в вершине не будет считаться
                Vector3 tmp3 = Vector3.Cross(D - C, A - C);
                Vector3 tmp4 = Vector3.Cross(D - C, B - C);
                return Vector3.Dot(tmp3, tmp4) < 0;// общая точка в вершине не будет считаться
            }
            return false;
        }


        public static bool ApproxEqVec(ref Vector3 v1, ref Vector3 v2) {
            float f = rFactor;
            if (Mathf.Abs(v1.x - v2.x) < f && Mathf.Abs(v1.y - v2.y) < f && Mathf.Abs(v1.z - v2.z) < f) {
                return true;
            }
            return false;
        }
        public static Vector3 VecRound(Vector3 v, float r = 4.0f) {
            return new Vector3(Round(v.x, r), Round(v.y, r), Round(v.z, r));
        }

        public static float OrtCrosVect(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4) {
            Vector2 V01 = v2 - v1;
            Vector2 V02 = v4 - v3;
            float fff = V01.y * V02.x - V01.x * V02.y;
            if (fff < rFactor && fff > -rFactor) {
                return 0f;
            }
            return Mathf.Sign(fff);
        }

        public static bool IsParalVec(Vector3 v1, Vector3 v2) {
            if (Mathf.Abs(Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude)) < rFactor) {
                return true;
            }
            return false;
        }

        public static bool IsParalDirVec(Vector3 v1, Vector3 v2) {
            if ((1 - (Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude))) < rFactor) {
                return true;
            }
            return false;
        }

        public static float SignProjectV3toV3(Vector3 v1, Vector3 v2) {
            return Round(Vector3.Dot(v1, v2), rounder);
        }
        public static bool IsParalVec2(Vector3 v1, Vector3 v2) {
            float f = rFactor;
            if (Mathf.Abs(v1.x / v2.x - v1.y / v2.y) < f && Mathf.Abs(v1.x / v2.x - v1.z / v2.z) < f) { //Если отношения координат равны
                return true;
            }
            return false;
        }

        public static float Round(float f, float n) {
            float p = Mathf.Pow(10, n);
            return (Mathf.Round(f * p)) / p;
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

        public static GameObject CreatePntGO(string s, Vector3 pnt) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = s;
            go.transform.localScale *= 5.5f;
            go.transform.position = pnt + new Vector3(-50, -20, 200);
            return go;
        }
    }

/*
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





