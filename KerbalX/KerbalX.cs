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
using UnityEngine.EventSystems;



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


		public static Dictionary<int, Dictionary<string, string>> existing_craft; //container for listing of user's craft already on KX and some details about them.

		//window handles (cos a window without a handle is just a pane)
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
				section (310f, e => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
				});

				section (310f, e => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.PasswordField (password, '*', 255, GUILayout.Width(250f));
				});
				GUI.enabled = true;
			}

			if (KerbalX.notice != "") {
				GUILayout.Label (KerbalX.notice, GUILayout.Width (310f));
			}

			if (KerbalX.alert != "") {	
				GUILayout.Label (KerbalX.alert, alert_style, GUILayout.Width (310f) );
			}

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
		private string editor_craft_name = "";
		private string[] upload_errors = new string[0];

		//private string image = "";

		private DropdownData craft_select;
		private DropdownData style_select;


		private Dictionary<int, string> remote_craft = new Dictionary<int, string> (); //will contain a mapping of KX database ID to craft name
		List<int> matching_craft_ids = new List<int> ();	//will contain any matching craft names

		private Dictionary<int, string> craft_styles = new Dictionary<int, string> (){
			{0, "Ship"}, {1, "Aircraft"}, {2, "spaceplane"}, {3, "lander"}, {4, "satellite"}, {5, "station"}, {6, "base"}, {7, "probe"}, {8, "rover"}, {9, "lifter"}
		};

		GUIStyle alert_style = new GUIStyle();

		private void Start()
		{
			window_title = "KerbalX::EditorInterface";
			window_pos = new Rect(250, 400, 310, 5);
			alert_style.normal.textColor = Color.red;
			KerbalX.editor_gui = this;
			KerbalXAPI.fetch_existing_craft (() => {
				remote_craft.Clear ();
				foreach(KeyValuePair<int, Dictionary<string, string>> craft in KerbalX.existing_craft){
					remote_craft.Add (craft.Key, craft.Value["name"]);
				}
			});
		}

		protected override void WindowContent(int win_id)
		{
			GUILayout.Label ("Yo fat ass is in the " + current_editor);
			//get the craft name from the editor field, but allow the user to set a alternative name to upload as without changing the editor field
			//but if the user changes the editor field then reset the craft_name to that. make sense? good, shutup. 
			if(editor_craft_name != EditorLogic.fetch.shipNameField.text){
				craft_name = EditorLogic.fetch.shipNameField.text;
			}
			editor_craft_name = EditorLogic.fetch.shipNameField.text;

			section (310f , width => {
				GUILayout.Label ("craft name", GUILayout.Width (60f));
				craft_name = GUILayout.TextField (craft_name, 255, GUILayout.Width (width - 60));
			});

			if(GUI.changed){
				string lower_name = craft_name.Trim ().ToLower ();
				matching_craft_ids.Clear ();
				foreach(KeyValuePair<int, string> craft in remote_craft){
					string rc_lower = craft.Value.Trim ().ToLower ();
					if( lower_name == rc_lower || lower_name == rc_lower.Replace ("-", " ")){
						matching_craft_ids.Add (craft.Key);
					}
				}
				if(matching_craft_ids.Count == 1){
					craft_select.id = matching_craft_ids.First ();
				}

			}
										
			if(matching_craft_ids.Count > 0){
				GUILayout.Label ("match found");
			}

			craft_select = dropdown (remote_craft, craft_select, 310f, 100f);

			style_select = dropdown (craft_styles, style_select, 100f, 80f);
			
			GUILayout.Label ("id:" + craft_select.id + ", name:" + craft_select.value);




			//string image = GUILayout.TextField (image, 255);

			if (KerbalX.alert != "") {	
				GUILayout.Label (KerbalX.alert, alert_style, GUILayout.Width (310f) );
			}
			if (upload_errors.Length > 0) {
				GUILayout.Label ("errors and shit");
				foreach (string error in upload_errors) {
					GUILayout.Label (error, alert_style, GUILayout.Width (310f));
				}
			}


			if (GUILayout.Button ("upload")) {
				upload_craft ();
			}

		}

		//returns the craft file
		private string craft_file(){
			//return EditorLogic.fetch.ship.SaveShip ().ToString ();
			return System.IO.File.ReadAllText(craft_path ());
		}

		//returns the path of the craft file
		private string craft_path(){
			string path = Paths.joined (KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", current_editor, craft_name);
			return path + ".craft";
		}

		//returns a unique set of the craft's parts and data about each part;
		//{"partname1" => {"mod" => "mod_name"}, "partname2" => {"mod" => "mod_name"}, ....} #yeah, explained in Ruby hash notation, cos...it's terse. 
		private Dictionary<string, object> part_info(){
			Dictionary<string, object> part_data = new Dictionary<string, object>();
			var part_list = EditorLogic.fetch.ship.parts;
			foreach(Part part in part_list){
				if (!part_data.ContainsKey (part.name)) {
					Dictionary<string, object> part_detail = new Dictionary<string, object>();
					part_detail.Add ("mod", part.partInfo.partUrl.Split ('/') [0]);
					//part.partInfo.partConfig
					part_data.Add (part.name, part_detail);
				}
			}
			return part_data;
		}

		//returns a list of unique part names in the craft
		private string[] craft_part_names(){
			List<string> part_names_list = new List<string> ();
			var part_list = EditorLogic.fetch.ship.parts;
			foreach(Part part in part_list){
				part_names_list.Add (part.name);
			}
			return part_names_list.Distinct ().ToArray ();
		}

		private void upload_craft(){
			Array.Clear (upload_errors, 0, upload_errors.Length);	//remove any previous upload errors

			NameValueCollection data = new NameValueCollection ();	//contruct upload data
			//data.Add ("craft_file", craft_file());
			data.Add ("craft_name", craft_name);
			data.Add ("part_data", JSONX.toJSON (part_info ()));

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
	}


	



	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class EditorActions : MonoBehaviour
	{
		private bool set_state = true;
		public string editor = null;

		private void Update(){
			if(set_state){
				set_state = false;
				KerbalX.console.window_pos = new Rect(250, 10, 310, 5);
				KerbalX.editor_gui.current_editor = EditorLogic.fetch.ship.shipFacility.ToString ();
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
			section (300f, e => { GUILayout.Label (KerbalX.last_log ());	});
			section (300f, e => { GUILayout.Label (KerbalXAPI.token); 	});

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

			if (GUILayout.Button ("test method")) {
				Debug.Log ("Editor:" + EditorLogic.fetch.ship.shipFacility.ToString ());

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
