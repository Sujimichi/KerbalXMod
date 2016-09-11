using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;


namespace KerbalX
{

	public class Paths{
		static public string joined(params string[] paths){
			string path = paths [0];
			for(int i=1; i<paths.Length; i++){
				path = Path.Combine (path, paths[i]);
			}
			return path;
		}
	}

	//The most redneck implementation of a JSON serializer ever!
	public class JSONX{
		public static string toJSON(Dictionary<string, object> data){
			List<string> objects = new List<string> ();
			foreach(KeyValuePair<string, object> entry in data){

				if (entry.Value is Dictionary<string, object>){
					var t = (Dictionary<string, object>)data [entry.Key];
					objects.Add (String.Format ("\"{0}\":{1}", entry.Key, JSONX.toJSON (t)));
				}else if(entry.Value is String){
					objects.Add (String.Format ("\"{0}\":\"{1}\"", entry.Key, entry.Value));
				}else{	//mutha of asumptions: if it's not a string or dict then it's a numeric
					try{
						objects.Add (String.Format ("\"{0}\":{1}", entry.Key, entry.Value));
					}
					catch{
						objects.Add (String.Format ("\"{0}\":\"{1}\"", entry.Key, entry.Value.ToString ()));
					}					
				}
			}

			//Poor man's String.join becuase I couldn't get the line below to function in unity
			//string json_string = "{" + String.Join (",", objects) + "}";
			string json_string = "{";
			foreach (string obj in objects) {				
				json_string = json_string + obj;
				if (obj != objects.Last ()) {json_string = json_string + ",";}
			}
			json_string = json_string + "}";
			return json_string;
		}

	}

}

