using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.IO;
using System.Threading;

using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;


namespace KerbalX
{
	public class KerbalX
	{
		public static string token_path = Path.Combine (KSPUtil.ApplicationRootPath, "KerbalX.key");
		public static List<string> log_data = new List<string>();
		public static string notice = "";
		public static string alert = "";
		public static bool show_login = false;
		public static string site_url = "http://localhost:3000";

		//window handles
		public static KerbalXConsole console = null;
		public static KerbalXEditorWindow editor_gui = null;

		//methodical things
		//takes partial url and returns full url to site; ie url_to("some/place") -> "http://whatever_domain_site_url_defines.com/some/place"
		public static string url_to (string path){
			if(!path.StartsWith ("/")){ path = "/" + path;}
			return site_url + path;
		}

		//logging stuf, not suitable for lumberjacks
		public static void log (string s){ 
			s = "[KerbalX] " + s;
			log_data.Add (s); 
			Debug.Log (s);
		}
		public static string last_log()
		{
			if(log_data.Count != 0){
				return log_data [log_data.Count - 1];
			}else{
				return "nothing logged yet";
			}
		}
		public static void show_log(){
			foreach (string l in log_data) { Debug.Log (l); }
		}
		public static void notify(string s){
			notice = s;
			log (s);
		}

		public static void load_token(){
			KerbalX.notify("Reading token from " + token_path);
			try{
				string token = System.IO.File.ReadAllText(token_path);
				KerbalXAPI.authenticate_token (token);
			}
			catch{
				KerbalX.notify("Enter your KerbalX username and password");
				KerbalX.show_login = true;
			}
		}
		public static void save_token(string token){
			System.IO.File.WriteAllText(token_path, token);
		}
	}



	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{
		private string username = "";
		private string password = "";
		public static bool enable_login = true;  //used to toggle enabled/disabled state on login fields and button
		GUIStyle alert_style = new GUIStyle();


		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 310/2),100, 310, 5);
			alert_style.normal.textColor = Color.red;
			KerbalX.show_login = false;
			if (KerbalXAPI.token == null) {
				KerbalX.load_token ();
			}
		}

		protected override void WindowContent(int win_id)
		{
			if(KerbalX.show_login == true){					
				GUI.enabled = enable_login;
				grid (310f, window => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
				});

				grid (310f, window => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.PasswordField (password, '*', 255, GUILayout.Width(250f));
				});
				GUI.enabled = true;
			}

			if (KerbalX.notice != "") {
				GUILayout.Label (KerbalX.notice, GUILayout.Width (310f));
			};

			if (KerbalX.alert != "") {	
				GUILayout.Label (KerbalX.alert, alert_style, GUILayout.Width (310f) );
			};

			GUI.enabled = enable_login;
			if (KerbalX.show_login == true) {
				if (GUILayout.Button ("Login")) {				
					KerbalX.alert = "";
					enable_login = false;
					KerbalXAPI.login (username, password);
				}
			}else{
				if (GUILayout.Button ("Log out")) {
					KerbalX.show_login = true;
					KerbalXAPI.token = null;
					KerbalX.notify ("logged out");
				}				
			}
			GUI.enabled = true;
		}
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXEditorWindow : KerbalXWindow
	{
		public string current_editor = null;
		private string craft_name = null;
		private string[] part_names;
		private string[] upload_errors = new string[0];

		GUIStyle alert_style = new GUIStyle();

		private void Start()
		{
			window_title = "KerbalX::EditorInterface";
			window_pos = new Rect(250, 400, 310, 5);
			alert_style.normal.textColor = Color.red;
			KerbalX.editor_gui = this;
		}

		protected override void WindowContent(int win_id)
		{
			craft_name = EditorLogic.fetch.shipNameField.text;
			GUILayout.Label (craft_name);
			GUILayout.Label ("Yo fat ass is in the " + current_editor);


			var part_list = EditorLogic.fetch.ship.parts;
			List<string> part_names_list = new List<string> ();
			foreach(Part part in part_list){
				part_names_list.Add (part.name);
			}
			part_names = part_names_list.Distinct ().ToArray ();
			foreach(string part_name in part_names){
				GUILayout.Label (part_name);
			}

			if (upload_errors.Length > 0) {
				GUILayout.Label ("errors and shit");
				foreach (string error in upload_errors) {
					GUILayout.Label (error, alert_style, GUILayout.Width (310f));
				}
			}


			if (GUILayout.Button ("test")) {
				string path = craft_path ();
				KerbalX.log (path);
				//EditorLogic.fetch.ship.SaveShip ().Save (path);
				Debug.Log (EditorLogic.fetch.ship.SaveShip ());


			}

			if (GUILayout.Button ("upload")) {
				upload_craft ();
			}

		}

		private void upload_craft(){
			Array.Clear (upload_errors, 0, upload_errors.Length);
			NameValueCollection data = new NameValueCollection ();
			data.Add ("craft_file", craft_file());
			data.Add ("craft_name", craft_name);
			KerbalXAPI.post (KerbalX.url_to ("api/craft.json"), data, (resp, code) => {
				if(code == 200){
					var resp_data = JSON.Parse (resp);
					string message = resp_data["message"];
					KerbalX.log ("the message was: '" + message + "'");

					if(message.Equals ("uploaded", StringComparison.Ordinal)){
						KerbalX.log ("holy fuck! it uploaded");
						KerbalX.log (resp);
					}else{
						KerbalX.log ("upload failed");
						KerbalX.log(resp);
						string resp_errs = resp_data["errors"];
						upload_errors = resp_errs.Split (',');
					}
				}else{
					KerbalX.log ("upload failed - server error");
				}
			});
		}

		private string craft_file(){
			//return EditorLogic.fetch.ship.SaveShip ().ToString ();
			return System.IO.File.ReadAllText(craft_path ());
		}

		private string craft_path(){
			string path = Paths.joined (KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", current_editor, craft_name);
			return path + ".craft";
		}

	}

	public class Paths{
		static public string joined(params string[] paths){
			string path = paths [0];
			for(int i=1; i<paths.Length; i++){
				path = Path.Combine (path, paths[i]);
			}
			return path;
		}
	}

	
	[KSPAddon(KSPAddon.Startup.EditorVAB, false)]
	public class WhoEditVAB : WhoEdit
	{ 
		private void Start()
		{ 
			editor = "VAB";
		} 
	}

//	[KSPAddon(KSPAddon.Startup.EditorSPH, false)]
//	public class WhoEditSPH : WhoEdit
//	{ 
//		private void Start(){ 
//			editor = "SPH";
//		}
//	}

	public class WhoEdit : MonoBehaviour
	{
		private bool set_state = true;
		public string editor = null;

		private void Update(){
			if(set_state){
				set_state = false;
				KerbalX.console.window_pos = new Rect(250, 10, 310, 5);
				KerbalX.log ("WhoEdit says: " + editor	);
				KerbalX.editor_gui.current_editor = editor;
			}
		}
	}

	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class KerbalXConsole : KerbalXWindow
	{
		private void Start()
		{
			window_title = "test window";
			KerbalX.console = this;
		}

		protected override void WindowContent(int win_id)
		{
			grid (300f, window => { GUILayout.Label (KerbalX.last_log ());	});
			grid (300f, window => { GUILayout.Label (KerbalXAPI.token); 	});

			if (GUILayout.Button ("print log to console")) { KerbalX.show_log (); }

			if (GUILayout.Button ("test fetch http")) {
				KerbalXAPI.get ("http://kerbalx-stage.herokuapp.com/katateochi.json", (resp, code) => {
					KerbalX.notify ("callback start, got data");
					Debug.Log ("code: " + code);
					var data = JSON.Parse (resp);
					KerbalX.notify (data["username"]);
				});
			}
			
			if (GUILayout.Button ("test fetch https")) {
				KerbalXAPI.get ("https://kerbalx.com/katateochi.json", (resp, code) => {
					KerbalX.notify ("callback start, got data");
					Debug.Log ("code: " + code);
					var data = JSON.Parse (resp);
					KerbalX.notify (data["username"]);
				});
			}

			if (GUILayout.Button ("test api/craft")) {
				KerbalXAPI.get (KerbalX.url_to ("api/craft.json"), (resp, code) => {
					if(code==200){
						KerbalX.log (resp);
					}
				});
			}
		}
	}

		
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class JumpStart : MonoBehaviour
	{
		public static bool autostart = true;
		public static string save_name = "default";
		public static string craft_name = "testy";

		public void Start()
		{
			if(autostart){
				HighLogic.SaveFolder = save_name;
				HighLogic.fetch.showConsole = true;
				var editor = EditorFacility.VAB;
				GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
				if(craft_name != null || craft_name != ""){
					string path = Path.Combine (KSPUtil.ApplicationRootPath, "saves/" + save_name + "/Ships/VAB/" + craft_name + ".craft");
					EditorDriver.StartAndLoadVessel (path, editor);
				}else{
					EditorDriver.StartEditor (editor);
				}

			}
		}
	}


}
