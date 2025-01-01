using UnityEngine;
using System.Collections;
using System;
using System.Diagnostics;

public class Main : MonoBehaviour {
	public static float tn;//Время выполнения операции
	public GameObject Basic;
	public GameObject Operator;
    public GameObject Operator1;
    public static string Angle ="0,0,0";
	public static string tempAngle="0,0,0";
	public float[] out_= new float[]{0,0,0};
	public static GUIStyle style2;
	public static Vector3 V0;
	public static bool flgCutter;
	public static bool flgStart = false;
    public static bool flgType = false;
	public static GameObject go111;
	public static Vector3 tmpPosition;
	public static Quaternion tmpRot;
	public static Vector3 tmpScl;

	public static float X,Y,X_,Y_;
	public static float ScaleValue, tScl, delta; 
	public static Vector3 scle;
	public static Material[] mats =null;
	public static PntsAttributes tmp = new PntsAttributes();

    private static int intMode = 0;
    private static string sMode = "0";

    // Use this for initialization

    void Start () {
		Basic = GameObject.Find ("Cube1");
		Operator1 = GameObject.Find ("Cube2");

        Operator = Operator1;

		Vector3 El = Operator.transform.rotation.eulerAngles;
		Angle = El.x.ToString()+","+El.y.ToString()+","+El.z.ToString();
		tempAngle=Angle;
		V0 = Operator.transform.position;
		go111 = new GameObject();
		go111.AddComponent<MeshFilter>();
		go111.AddComponent<MeshRenderer>();
		go111.GetComponent<MeshRenderer>().material = Basic.GetComponent<MeshRenderer>().material;
		go111.transform.localScale = Basic.transform.localScale;
		go111.transform.rotation = Basic.transform.rotation;
		go111.name = "Cube3_new";
		go111.transform.position = Basic.transform.position+new Vector3(-50,-20,200);
		tmpPosition=Operator.transform.position;
		tmpRot=Operator.transform.rotation;
		tmpScl=Operator.transform.localScale;
		X_=50;
		Y_=0;
		X=Operator.transform.position.x+X_;
		Y=Operator.transform.position.y+Y_;
		delta = 0.5f;
		ScaleValue = 0.1f;
		tScl=ScaleValue;
		scle = tmpScl;
		go111.GetComponent<MeshFilter>().mesh = new Mesh();
        go111.transform.rotation = Basic.transform.rotation;
        go111.transform.localScale = Basic.transform.localScale;
        go111.transform.position = Basic.transform.position + new Vector3(-50, -20, 200);
    }

	
	// Update is called once per frame
	void Update () {
        int.TryParse(sMode, out intMode);
        if (Input.GetKeyDown (KeyCode.Escape)) {
			Application.Quit();
		}
		if(tScl!=ScaleValue){
			Operator.transform.localScale = ScaleValue*scle;
			tScl=ScaleValue;
		}
		if(X-X_!=Operator.transform.position.x){	
			Operator1.transform.position=new Vector3(X-X_,Operator1.transform.position.y,Operator1.transform.position.z);
		}
		if(Y-Y_!=Operator.transform.position.y){	
			Operator1.transform.position=new Vector3(Operator1.transform.position.x,Y-Y_,Operator1.transform.position.z);
		}
        if (flgStart) 
		{
            Stopwatch sw = new Stopwatch();
            sw.Start();
            tmpPosition = Operator.transform.position;
            tmpRot = Operator.transform.rotation;
            tmpScl = Operator.transform.localScale;

            Mesh shMesh = go111.GetComponent<MeshFilter>().sharedMesh;
            if (!flgType) {
                go111.GetComponent<MeshFilter>().sharedMesh = Solid.SolidCutter.DeductValue(shMesh, Basic, Operator, ref mats, ref tmp, intMode);
            } else {
                go111.GetComponent<MeshFilter>().sharedMesh = Solid5.SolidCutter.DeductValue(shMesh, Basic, Operator, ref mats, ref tmp, intMode); 
            }
			go111.GetComponent<MeshRenderer>().sharedMaterials = mats;

			sw.Stop();
			tn=sw.ElapsedMilliseconds;

			String msg = GC.GetTotalMemory(true).ToString("0,0");
			UnityEngine.Debug.Log (msg);
		}
	}
	void OnGUI(){
		if (style2==null){
			style2 = new GUIStyle (GUI.skin.textArea);
			style2.fontSize = 14;
			style2.alignment = TextAnchor.MiddleCenter;
			style2.normal.background = ColorS(100,220,43,255f);
			style2.normal.textColor = Color.black;
			style2.fontStyle = FontStyle.Bold;
		}
		if(GUI.Button (new Rect(0f,0f, 150, 20), "Вычесть")){
			flgStart=!flgStart;
		}

        if (GUI.Button(new Rect(0f, 50f, 150, 20), flgType.ToString())) {
            flgType = !flgType;
        }
        GUI.Label(new Rect(Screen.width-50-3*Screen.width/18,0f, 50, 20), tn.ToString());
		GUI.Label(new Rect(Screen.width-50-3*Screen.width/18,20f, 50, 20), DVName.DV.kCount.ToString());
		GUI.Label(new Rect(Screen.width-50-3*Screen.width/18,40f, 50, 20), DVName.DV.triCount1.ToString());
		GUI.Label(new Rect(Screen.width-50-3*Screen.width/18,60f, 50, 20), DVName.DV.triCount2.ToString());
		GUI.Label(new Rect(Screen.width-50-3*Screen.width/18,100f, 50, 20), ScaleValue.ToString());
		X = GUI.VerticalScrollbar(new Rect(Screen.width-Screen.width/18,0,Screen.width/18,Screen.height), X, 5, 150, 30f);
		Y = GUI.VerticalScrollbar(new Rect(Screen.width-2*Screen.width/18,0,Screen.width/18,Screen.height), Y, 5, 150, 30f);
		ScaleValue = GUI.VerticalScrollbar(new Rect(Screen.width-3*Screen.width/18,0,Screen.width/18,Screen.height), ScaleValue, 0.1f, 0.0f, 30f);
		if(GUI.Button (new Rect(370f,0f, 70, 20), "Рестарт")){
			DVName.CrossPntSt.Nets.Clear();
			DVName.CrossPntSt.Tris.Clear ();
			DVName.CrossPntSt.cPntIsBord.Clear ();
			DVName.CrossPntSt.LVects.Clear ();
			Application.LoadLevel("123");
		}
        sMode = GUI.TextField(new Rect(0f, 22f, 170, 20), intMode.ToString());

		Angle = GUI.TextArea (new Rect(200f,0f, 150, 20), Angle, style2);

		if(tempAngle!=Angle)
		{
			tempAngle=Angle;
			float[] anglxyz = new float[3];
			string[] sAngl = Angle.Split (new[] {","}, StringSplitOptions.RemoveEmptyEntries);
			Vector3 AngV = new Vector3();
			for(int i=0; i<sAngl.Length;i++){
				if(float.TryParse(sAngl[i], out out_[i])){
					
				}
			}
			AngV = new Vector3(out_[0],out_[1],out_[2]);
			Operator.transform.rotation = Quaternion.Euler(AngV);
		}
	}
	public static Texture2D ColorS(int r,int g, int b, float al)
	{
		Texture2D texture = new Texture2D (1,1);//Screen.width,Screen.height);
		Color color = new Color(r/255f, g/255f,  b/255f, al);
		for (int y = 0; y < texture.height; ++y)
		{
			for (int x = 0; x < texture.width; ++x)
			{
				texture.SetPixel(x,y,color);
			}
		}
		texture.Apply();
		return texture;
		
	}
}
