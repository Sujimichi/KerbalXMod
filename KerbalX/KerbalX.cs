using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

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

		public static string screenshot_dir = Paths.joined (KSPUtil.ApplicationRootPath, "Screenshots"); //TODO make this a setting, oh and make settings.

		public static Dictionary<int, Dictionary<string, string>> existing_craft; //container for listing of user's craft already on KX and some details about them.

		//window handles (cos a window without a handle is just a pane)
		public static KerbalXConsole console 				= null;
		public static KerbalXLoginWindow login_gui 			= null;
		public static KerbalXEditorWindow editor_gui 		= null;
		public static KerbalXImageSelector image_selector 	= null;


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
			KerbalX.login_gui = this;
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
		private string mode = "upload";
		private float win_width = 410f;

		private bool first_pass = true;
		//private string image = "";

		private DropdownData craft_select;
		private DropdownData style_select;


		private Dictionary<int, string> remote_craft = new Dictionary<int, string> (); //will contain a mapping of KX database-ID to craft name
		List<int> matching_craft_ids = new List<int> ();	//will contain any matching craft names

		private Dictionary<int, string> craft_styles = new Dictionary<int, string> (){
			{0, "Ship"}, {1, "Aircraft"}, {2, "Spaceplane"}, {3, "Lander"}, {4, "Satellite"}, {5, "Station"}, {6, "Base"}, {7, "Probe"}, {8, "Rover"}, {9, "Lifter"}
		};

		GUIStyle alert_style 	= new GUIStyle();
		GUIStyle upload_button  = new GUIStyle();
		GUIStyle wrapped_button = new GUIStyle();
		GUIStyle centered 		= new GUIStyle();
		GUIStyle header_label	= new GUIStyle();

		//private Texture2D kx_logo = new Texture2D(56, 56, TextureFormat.ARGB32, false);
		//kx_logo = GameDatabase.Instance.GetTexture (Paths.joined ("KerbalX", "Assets", "KXlogo"), false);


		private void Start()
		{
			window_title = "KerbalX::Upload";
			window_pos = new Rect ((Screen.width - win_width - 100), 60, win_width, 5);
			prevent_editor_click_through = true;
			KerbalX.editor_gui = this;
			KerbalXAPI.fetch_existing_craft (() => {  //Query KX for the user's current craft (which gets stashed on KerablX.existing_craft). lambda gets called once request completes.
				remote_craft.Clear ();
				remote_craft.Add (0, "select a craft");	//remote_craft populates the select menu, ID 0 (which can't exist on KX) is used as the placeholder
				foreach(KeyValuePair<int, Dictionary<string, string>> craft in KerbalX.existing_craft){
					remote_craft.Add (craft.Key, craft.Value["name"]);
				}
			});
		}

		private void set_stylz(){
			alert_style.normal.textColor = Color.red;
			upload_button = new GUIStyle (GUI.skin.button);
			upload_button.fontSize = 20;
			upload_button.padding = new RectOffset (3, 3, 10, 10);

			wrapped_button = new GUIStyle (GUI.skin.button);
			wrapped_button.wordWrap = true;

			centered = new GUIStyle (GUI.skin.label);
			centered.alignment = TextAnchor.UpperCenter;

			header_label = new GUIStyle (GUI.skin.label);
			header_label.fontSize = 15;
			header_label.fontStyle = FontStyle.Bold;
//			GUI.skin.label.fontSize = 20;
		}

		protected override void WindowContent(int win_id)
		{
			if (first_pass) {
				first_pass = false;
				set_stylz ();//it's like we need a sorta sheet of styles, maybe one that can cascade, a cascading style sheet if you will.
			}

			//get the craft name from the editor field, but allow the user to set a alternative name to upload as without changing the editor field
			//but if the user changes the editor field then reset the craft_name to that. make sense? good, shutup. 
			if(editor_craft_name != EditorLogic.fetch.ship.shipName){
				craft_name = EditorLogic.fetch.ship.shipName;	
				check_for_matching_craft_name ();
			}
			editor_craft_name = EditorLogic.fetch.ship.shipName;

			//Perform checks to see if there is a craft,  its not untitled and a craft file for it exists.
			string trimmed_lowered_name = editor_craft_name.Trim ().ToLower ().Replace (" ", "");
			if(part_info ().Count == 0){
				GUILayout.Label ("No craft loaded. Create or load a craft to continue.", header_label);
			}else if(trimmed_lowered_name == "untitledspacecraft" || trimmed_lowered_name == EditorLogic.autoShipName){
				GUILayout.Label (editor_craft_name + "? Really?\nHow about you name the poor thing before uploading!", header_label);
			}else if(!craft_file_exists ()){
				section (win_width, w => {
					GUILayout.Label ("This craft hasn't been saved yet\nNo craft file found for " + editor_craft_name, header_label, width(w*0.7f));
					if(GUILayout.Button ("Save it now", width(w*0.3f), height (40))){
						EditorLogic.fetch.saveBtn.onClick.Invoke ();
					}
				});
			}else{
				//if checks pass continue with drawing rest of interface (TODO check for unsaved changes).

				string mode_title = new CultureInfo ("en-US", false).TextInfo.ToTitleCase (mode);
				GUILayout.Label (mode_title + " '" + craft_name + "' " + (mode == "update" ? "on" : "to") + " KerbalX", header_label);
				
				if(mode == "upload"){
					section (w => {
						GUILayout.Label ("Enter details about your craft", width(w*0.45f));
						GUILayout.Label ("OR", centered, width(w*0.1f));
						if (GUILayout.Button ("Update An Existing Craft", width(w*0.45f))) {
							change_mode("update");
							if (matching_craft_ids.Count != 1) { craft_select.id = 0;};
						}
					});
					
					section (w => {
						string current_craft_name = craft_name; //used to detect if user changes craft_name field (GUI.changed gets triggered by above button)
						GUILayout.Label ("Craft name:", width(80f));
						craft_name = GUILayout.TextField (craft_name, 255, width(w - 80));
						if(craft_name != current_craft_name){check_for_matching_craft_name (); } //check for matching existing craft
					});
					
					section (w => {
						GUILayout.Label ("Select craft type:", width(100f));
						style_select = dropdown (craft_styles, style_select, 100f, 100f);
					});
					
					section (w => {
						if (GUILayout.Button ("Edit Action Group info", width(w*0.5f), height(30)) ) {
						}					
						if (GUILayout.Button ("Add Pictures", width(w*0.5f), height (30)) ) {
							KerbalX.image_selector.toggle ();
						}					
					});
					
					
				}else if(mode == "update"){
					if (matching_craft_ids.Count > 0) {
						string label_text = "This craft's name matches the name of " + (matching_craft_ids.Count == 1 ? "a" : "several") + " craft you've already uploaded.";
						if (matching_craft_ids.Count > 1) {
							label_text = label_text + " Select which one you want to update";
						}
						GUILayout.Label (label_text);
					}
					
					section (w => {
						v_section (w*0.7f, inner_w => {
							section (inner_w, inner_w2 => { GUILayout.Label ("Select Craft on KerbalX to update:"); });
							craft_select = dropdown (remote_craft, craft_select, inner_w, 100f);
						});
						v_section (w*0.3f, inner_w => {
							section (inner_w, inner_w2 => {
								if (GUILayout.Button ("OR upload this as a new craft", wrapped_button, height (50) )) {
									change_mode("upload");
								}
							});
						});
					});
					
					if (craft_select.id > 0) {
						GUILayout.Label ("id:" + craft_select.id + ", name:" + KerbalX.existing_craft [craft_select.id] ["name"] + " - " + KerbalX.existing_craft [craft_select.id] ["url"]);
					}
				}
				
				
				if (KerbalX.alert != "") {	
					GUILayout.Label (KerbalX.alert, alert_style, width (win_width) );
				}
				if (upload_errors.Count () > 0) {
					GUILayout.Label ("errors and shit");
					foreach (string error in upload_errors) {
						GUILayout.Label (error.Trim (), alert_style, width (win_width));
					}
				}
				
				style_override = new GUIStyle();
				style_override.padding = new RectOffset (20, 20, 10, 10);
				section (w => {
					if (GUILayout.Button (mode_title + "!", upload_button)) {
						upload_craft ();
					}
				});
			}


			if(GUILayout.Button ("test")){
//				EditorLogic.fetch.newBtn.onClick.AddListener (() => {
//					Debug.Log ("NEW CLICKED");
//				});
				//EditorLogic.fetch.newBtn.Select ();
				//EditorLogic.fetch.newBtn.onClick.Invoke ();
				//EditorLogic.fetch.saveBtn.onClick.Invoke ();

//				this.visible = false;
//				Application.CaptureScreenshot ("fibble");
//				this.visible = true;
				
				window_pos.width = window_pos.width + 10;


			}

		}

		//check if craft_name matches any of the user's existing craft.  Sets matching_craft_ids to contain KX database ids of any matching craft
		//if only one match is found then craft_select.id is also set to the matched id (which them selects the craft in the select menu)
		private void check_for_matching_craft_name(){
			KerbalX.log ("checking for matching craft - " + craft_name);
			string lower_name = craft_name.Trim ().ToLower ();
			matching_craft_ids.Clear ();
			foreach(KeyValuePair<int, string> craft in remote_craft){
				string rc_lower = craft.Value.Trim ().ToLower ();
				if( lower_name == rc_lower || lower_name == rc_lower.Replace ("-", " ")){
					matching_craft_ids.Add (craft.Key);
				}
			}
			change_mode (matching_craft_ids.Count > 0 ? "update" : "upload");
			if(matching_craft_ids.Count == 1){
				craft_select.id = matching_craft_ids.First ();
			}			
		}

		private void change_mode(string new_mode){
			mode = new_mode;
			autoheight ();
		}

		//returns the craft file
		private string craft_file(){
			//return EditorLogic.fetch.ship.SaveShip ().ToString ();
			return System.IO.File.ReadAllText(craft_path ());
		}

		private bool craft_file_exists(){
			return System.IO.File.Exists (craft_path ());
		}

		//returns the path of the craft file
		private string craft_path(){
			return ShipConstruction.GetSavePath (editor_craft_name);
			//string path = Paths.joined (KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", current_editor, editor_craft_name);
			//return path + ".craft";
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

		private void upload_craft(){
			//Array.Clear (upload_errors, 0, upload_errors.Length);	//remove any previous upload errors
			upload_errors = new string[0];
			KerbalX.alert = "";

			NameValueCollection data = new NameValueCollection ();	//contruct upload data
			//data.Add ("craft_file", craft_file());
			data.Add ("craft_name", craft_name);
			data.Add ("part_data", JSONX.toJSON (part_info ()));
			KerbalXAPI.post (KerbalX.url_to ("api/craft.json"), data, (resp, code) => {
				string message = "";
				if(code == 200){
					var resp_data = JSON.Parse (resp);
					try{
						message = resp_data["message"];
					}
					catch{
						message = "failed to read response";
					}

					if(message == "uploaded"){
						KerbalX.log ("holy fuck! it uploaded");

					}else{
						KerbalX.log ("upload failed");
						string resp_errs = resp_data["errors"];
						upload_errors = resp_errs.Split (',');
						KerbalX.alert = "my fish escaped";
					}
				}else{
					message = "upload failed - server error";
					KerbalX.alert = message;
					KerbalX.log (message);
				}
			});
		}
	}


	
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXImageSelector : KerbalXWindow
	{
		public struct PicData{
			public string name;
			public FileInfo file;
			public Texture2D texture;
			public void initialize(string new_name, FileInfo new_file, Texture2D new_texture){
				name = new_name;
				file = new_file;
				texture = new_texture;
			}
		}
		private List<PicData> pictures = new List<PicData>();				//populated by load_pics, contains PicData objects for each pic 
		private List<List<PicData>> groups = new List<List<PicData>> ();	//nested list of lists - rows of pictures for display in the interface

		private int pics_per_row = 4;
		private string[] file_types = new string[]{"jpg", "png"};
		private Vector2 scroll_pos;
		private PicData selected_pic;

		GUIStyle pic_link 	 = new GUIStyle();
		GUIStyle pic_hover	 = new GUIStyle();
		GUIStyle header_label= new GUIStyle();

		Texture2D pic_highlight 	= new Texture2D(1, 1, TextureFormat.RGBA32, false);
		Texture2D scroll_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);

		private string hover_ele = "";

		private void Start(){
			window_title = "KerbalX::ScreenShots";
			float w = 610;
			window_pos = new Rect((Screen.width/2 - w/2), Screen.height/4, w, 5);
			visible = true;
			prevent_editor_click_through = true;
			KerbalX.image_selector = this;
			
			pic_highlight.SetPixel(0, 0, new Color (0.4f,0.5f,0.9f,1));
			pic_highlight.Apply ();
			
			scroll_background.SetPixel(0, 0, new Color (0.12f,0.12f,0.12f,0.7f));
			scroll_background.Apply ();

			load_pics ();
		}

		protected override void on_show(){
			load_pics ();
		}

		protected override void WindowContent(int win_id)
		{
			pic_link = new GUIStyle (GUI.skin.label);
			pic_link.padding = new RectOffset (5, 5, 5, 5);
			pic_link.margin = new RectOffset (0, 0, 0, 0);

			pic_hover = new GUIStyle (pic_link);
			pic_hover.normal.background = pic_highlight;
			pic_hover.normal.textColor = Color.black;

			header_label = new GUIStyle (GUI.skin.label);
			header_label.fontSize = 15;
			header_label.fontStyle = FontStyle.Bold;


			GUILayout.Label ("Select a picture for your craft", header_label);


			if (pictures.Count > 0) {
				scroll_pos = scroll (scroll_pos, 620f, 300f, w => {
					foreach(List<PicData> row in groups){
						style_override = new GUIStyle ();
						style_override.normal.background = scroll_background;
						section (600f, sw => {
							foreach(PicData pic in row){
								v_section (150f, w2 => {
									if(GUILayout.Button (pic.texture, (hover_ele==pic.name ? pic_hover : pic_link), width (150f), height (150f*0.75f))){
										select_pic (pic);
									}
									if(GUILayout.Button (pic.name, (hover_ele==pic.name ? pic_hover : pic_link), width(150f))){
										select_pic (pic);
									}
								});
								if(GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition)){
									hover_ele = pic.name;
								}
							}
						});
					}
				});
			}

			if(GUILayout.Button ("refresh")){
				load_pics ();
			}
			GUILayout.Label (selected_pic.name);

		}



		private void select_pic(PicData pic){
			selected_pic = pic;
		}



		private void load_pics(){
			DirectoryInfo dir = new DirectoryInfo (KerbalX.screenshot_dir);
			List<FileInfo> files = new List<FileInfo> ();

			//Get file info for all files of defined file_types within the given dir
			foreach(string file_type in file_types){
				foreach(FileInfo file in dir.GetFiles ("*." + file_type)){
					files.Add (file);
				}
			}

			pictures.Clear ();
			foreach(FileInfo file in files){
				//prepare the texture for the image
				Texture2D tex = new Texture2D (2, 2);
				byte[] pic_data = File.ReadAllBytes (file.FullName);
				tex.LoadImage (pic_data);

				//add a PicData struct for each picture into pictures (struct defines name, file and texture)
				PicData data = new PicData ();
				data.initialize (file.Name, file, tex);
				pictures.Add (data);
			}
			group_pics (); //divide pictures into "rows" of x pics_per_row 
		}

		//constructs a List of Lists containing PicData.  Divides pictures into 'rows' of x pics_per_row 
		private void group_pics(){
			groups.Clear ();							//clear any existing groups
			groups.Add (new List<PicData>());			//add first row to groups
			int i = 0;
			foreach (PicData pic in pictures) {
				groups.Last ().Add (pic);				//add picture to the last row
				i++;
				if(i >= pics_per_row){					//once a row is full (row count == pics_per_row)
					groups.Add (new List<PicData>());	//then add another row to groups 
					i = 0;								//and reset i
				}
			}
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

	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
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
				KerbalX.editor_gui.visible = !KerbalX.editor_gui.visible;
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
				DebugToolbar.toolbarShown = true;
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
