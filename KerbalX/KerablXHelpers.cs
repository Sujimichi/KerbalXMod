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
	//it can take a Dictionary<string, object> and the objects in the dictionary can be a string, a numeric, or a nested Dictionary<string, object>
	//it can also an optional second argument which is either a bool or an int.  If given as (bool)true then it will generate JSON with spacing and indentation 
	//(the same arg is also used to pass the level of indentation on when called recusively)
	//handle with caution, if swallowed seek medical attention.
	public class JSONX{
		public static string toJSON(Dictionary<string, object> data, params object[] opts){
			int indent = 0;
			bool do_indent = false;
			if(opts.Length == 1){
				if (opts [0] is int) {
					indent = (int)opts [0];
					do_indent = true;
				}else{
					do_indent = (bool)opts [0];
				}
			}
			indent++;
			object arg;
			if(do_indent){arg = (int)indent;}else{arg = (bool)do_indent;}  //yes it's a if else block on one line, deal with it, C# refused to let me use a ternary with mixed types, fussy lang.

			List<string> objects = new List<string> ();

			foreach(KeyValuePair<string, object> entry in data){
				if (entry.Value is Dictionary<string, object>){
					var t = (Dictionary<string, object>)data [entry.Key];
					objects.Add (String.Format ("\"{0}\":{1}", entry.Key, JSONX.toJSON (t, arg)));
				}else if(entry.Value is String){
					objects.Add (String.Format ("\"{0}\":\"{1}\"", entry.Key, entry.Value));
				}else{	
					//mutha of asumptions: if it's not a string or dict then it's a numeric, cos seriously C#, you don't have a numeric class? You want me to test for each 
					//numerical type individualy? yeah....I'm to lazy for that...oh Ruby...I miss you so.
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
			//also adds in spaces and new lines of do_indent is true.
			string json_string = "{";
			foreach (string obj in objects) {	
				if (do_indent) {
					json_string = json_string + "\n";
					for (int i = 0; i < indent; i++) {json_string = json_string + "    ";}
				}
				json_string = json_string + obj;
				if (obj != objects.Last ()) {json_string = json_string + ",";}
			}
			if(do_indent){
				json_string = json_string + "\n";
				for (int i = 0; i < indent-1; i++) {json_string = json_string + "    ";}
			}
			json_string = json_string + "}";

			return json_string; //hey look, some JSON!
		}

	}

}

