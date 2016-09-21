using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;

using SimpleJSON;
using UnityEngine;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXUploadInterface : KerbalXWindow
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

		public List<PicData> pictures = new List<PicData> ();

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
			enable_request_handler ();
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

					v_section (w => {
						section (w2 => {
							foreach(PicData pic in pictures){
								v_section (80f, w3 => {
									if(pic.file != null){
										GUILayout.Label (pic.texture, width (w3), height (w3*0.75f));
										if(GUILayout.Button ("remove")){
											pictures.Remove (pic);
											this.autoheight ();
										}
									}
								});
							}
						});
						foreach(PicData pic in pictures){
							section(w2 => {
								if(pic.url != null){
									GUILayout.Label (pic.url, width (w2-80f));
									if(GUILayout.Button ("remove", width (80f))){
										pictures.Remove (pic);
										this.autoheight ();
									}
								}
							});
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
			data.Add ("craft_file", craft_file());
			data.Add ("craft_name", craft_name);
			data.Add ("part_data", JSONX.toJSON (part_info ()));
			HTTP.post (KerbalX.url_to ("api/craft.json"), data).set_header ("token", KerbalXAPI.temp_view_token ()).send ((resp, code) => {

				string message = "";
				if(code == 200){
					var resp_data = JSON.Parse (resp);
					KerbalX.log ("holy fuck! it uploaded");

				}else if(code == 422){
					var resp_data = JSON.Parse (resp);
					KerbalX.log ("upload failed");
					KerbalX.log (resp);
					string resp_errs = resp_data["errors"];
					upload_errors = resp_errs.Split (',');
					KerbalX.alert = "Craft Failed to Upload";

				}else{
					message = "upload failed - server error";
					KerbalX.alert = message;
					KerbalX.log (message);
				}
			});
		}
	}
}

