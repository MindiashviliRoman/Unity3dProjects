
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

public class LogToTXT {
	public static void Main(string path,string text){
		
		// Delete the file if it exists.
		if (!File.Exists(path))
		{
			FileStream fs1=File.Create(path);
			fs1.Close();
		}
		
		//Create the file.
		using (FileStream fs = File.Open(path, FileMode.Append, FileAccess.Write))
		{
			AddText(fs, text + "\r\n");
		}
		
		//Open the stream and read it back.
		using (FileStream fs = File.OpenRead(path))
		{
			byte[] b = new byte[1024];
//			UTF8Encoding temp = new UTF8Encoding(true);
			while (fs.Read(b,0,b.Length) > 0)
			{
				//	Console.WriteLine(temp.GetString(b));
			}
		}
	}
	
	public static void AddText(FileStream fs, string value)
	{
		byte[] info = new UTF8Encoding(true).GetBytes(value);
		fs.Write(info, 0, info.Length);
		
	}
}