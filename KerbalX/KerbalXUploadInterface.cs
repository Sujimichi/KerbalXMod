﻿using System;

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
//using System.Collections.Specialized;

using SimpleJSON;
using UnityEngine;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXUploadInterface : KerbalXWindow
	{
		private string craft_name = null;						//craft_name is the (adjustable) name used in the upload interface
		private string editor_craft_name = "";					//editor_craft_name is the craft's name as taken from the editor

		private List<string> errors = new List<string>();		//container for an errors which occur and need displaying to the user
		private string upload_progress = null;					//message to display while uploaded/updating
		private int upload_ticker_pos = 0;						//used in upload progress ticker
		private int frame_count = 0;

		private string mode 		= "upload";					//Interface mode ("upload" || "update")
		private float win_width 	= 410f;						//window with
		private int max_pics 		= 3;						//the max number of pics that can be uploaded

		private bool show_upload_bttn 	= true;					//bool for whether or not to show the upload button
		private bool enable_upload_bttn = true;					//bool to enable the upload button to be disabled (during upload/update)

		public List<PicData> pictures = new List<PicData> ();	//container to hold the selected pictures to be uploaded

		private DropdownData craft_select;
		private DropdownData style_select;


		private Dictionary<int, string> remote_craft = new Dictionary<int, string> (); 	//used to contain a mapping of KX database-ID to craft name
		List<int> matching_craft_ids = new List<int> ();								//used to hold any matching craft ids

		private Dictionary<int, string> craft_styles = new Dictionary<int, string> (){	//list of craft styles used on KerbalX (might make this auto populate from the site at some point)
			{0, "Ship"}, {1, "Aircraft"}, {2, "Spaceplane"}, {3, "Lander"}, {4, "Satellite"}, {5, "Station"}, {6, "Base"}, {7, "Probe"}, {8, "Rover"}, {9, "Lifter"}
		};




		private void Start()
		{
			KerbalX.editor_gui = this;
			window_title = "KerbalX::Upload";
			window_pos = new Rect ((Screen.width - win_width - 20), 50, win_width, 5);
			require_login = true;
			prevent_editor_click_through = true;
			enable_request_handler ();
			fetch_existing_craft ();

			//bind events to happen when the editor loads a saved craft or when new craft is clicked
			GameEvents.onEditorLoad.Add 	(this.on_editor_load);	
			GameEvents.onEditorRestart.Add 	(this.on_editor_new );
		}

		//Callback for when the editor loads a craft
		public void on_editor_load(ShipConstruct a, KSP.UI.Screens.CraftBrowserDialog.LoadType b){
			KerbalX.editor_gui.reset ();
		}
		//Callback for when then editor resets (new craft)
		public void on_editor_new(){
			KerbalX.editor_gui.reset ();
		}
		//Called after a succsessful login, if the login dialog was opened from this window.
		protected override void on_login ()
		{
			base.on_login ();		//inherits call to hide login window
			fetch_existing_craft ();//run check for users craft on KerablX
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy (); //releases any UI locks on the editor
			GameEvents.onEditorLoad.Remove 	  (this.on_editor_load);//unbind editor events
			GameEvents.onEditorRestart.Remove (this.on_editor_new);
			if(KerbalX.image_selector != null){						//destroy the ImageSelector if it's open
				GameObject.Destroy (KerbalX.image_selector);		
			}
		}

		//Main interface! woooo!
		protected override void WindowContent(int win_id)
		{

			show_upload_bttn = true;
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
				GUILayout.Label ("No craft loaded. Create or load a craft to continue.", "h3");
			}else if(trimmed_lowered_name == "untitledspacecraft" || trimmed_lowered_name == EditorLogic.autoShipName){
				GUILayout.Label (editor_craft_name + "? Really?\nHow about you name the poor thing before uploading!", "h3");
			}else if(!craft_file_exists ()){
				section (w => {
					GUILayout.Label ("This craft hasn't been saved yet\nNo craft file found for " + editor_craft_name, "h3", width(w*0.7f));
					if(GUILayout.Button ("Save it now", width(w*0.3f), height (40))){
						EditorLogic.fetch.saveBtn.onClick.Invoke ();
					}
				});
			}else{
				//if checks pass continue with drawing rest of interface

				string mode_title = new CultureInfo ("en-US", false).TextInfo.ToTitleCase (mode);
				GUILayout.Label (mode_title + " '" + craft_name + "' " + (mode == "update" ? "on" : "to") + " KerbalX", "h3");

				if(mode == "upload"){
					section (w => {
						GUILayout.Label ("Enter details about your craft", width(w*0.45f));
						GUILayout.Label ("OR", "centered", width(w*0.1f));
						if (GUILayout.Button ("Update An Existing Craft", width(w*0.45f))) {
							change_mode("update");
							if (matching_craft_ids.Count != 1) { craft_select.id = 0;};
						}
					});

					//textfield for craft name, if value is changed it is checked against existing craft (and if match, mode is switched to update).
					section (w => {
						string current_craft_name = craft_name; //used to detect if user changes craft_name field (GUI.changed gets triggered by above button)
						GUILayout.Label ("Craft name:", width(80f));
						craft_name = GUILayout.TextField (craft_name, 255, width(w - 80));
						if(craft_name != current_craft_name){check_for_matching_craft_name (); } //check for matching existing craft
					});

					//drop down select for craft type
					section (w => {
						GUILayout.Label ("Select craft type:", width(100f));
						combobox ("craft_style_select", craft_styles, style_select.id, 100f, 150f, this, id => {style_select.id = id;});
					});

					//buttons for setting action groups and adding pictures.
					section (w => {
						if (GUILayout.Button ("Edit Action Group info", width(w*0.5f), height(30)) ) {
						}					
						if (GUILayout.Button ("Add Pictures", width(w*0.5f), height (30)) ) {
							if(KerbalX.image_selector == null){
								launch ("ImageSelector");
							}else{
								KerbalX.image_selector.toggle ();
							}
						}					
					});

					//Display of selected pictures (and/or image urls)
					v_section (w => {
						section (w2 => {
							foreach(PicData pic in pictures){	//display selected pictures
								v_section (80f, w3 => {
									if(pic.file != null){
										GUILayout.Label (pic.texture, width (w3), height (w3*0.75f));
										if(GUILayout.Button ("remove")){ remove_picture (pic); }
									}
								});
							}
						});
						foreach(PicData pic in pictures){		//display entered image urls.
							section(w2 => {
								if(pic.url != null){
									GUILayout.Label (pic.url, width (w2-80f));
									if(GUILayout.Button ("remove", width (80f))){ remove_picture (pic); }
								}
							});
						}
					});


				}else if(mode == "update"){

					if(KerbalX.existing_craft.Count == 0){ //show message if the user doesn't have any craft on KerbalX yet.
						show_upload_bttn = false;
						GUILayout.Label ("You haven't uploaded any craft yet", "h2");
						GUILayout.Label ("Once you've uploaded craft they will appear here and you'll be able to select them to update");
						section (w => {
							GUILayout.FlexibleSpace ();
							if(GUILayout.Button ("refresh", width (60f))){
								fetch_existing_craft ();
							}
						});
						if(GUILayout.Button ("Upload a Craft", height (30f))){
							change_mode ("upload");
						}
					}else{

						//Show message if the current craft name matches the name of one of the users craft on KerablX
						if (matching_craft_ids.Count > 0) {
							string label_text = "This craft's name matches the name of " + (matching_craft_ids.Count == 1 ? "a" : "several") + " craft you've already uploaded.";
							if (matching_craft_ids.Count > 1) {
								label_text = label_text + " Select which one you want to update"; //in the case of more than one match.
							}
							GUILayout.Label (label_text);
						}
						
						section (w => {
							v_section (w*0.7f, inner_w => {
								section (inner_w, inner_w2 => { GUILayout.Label ("Select Craft on KerbalX to update:"); });
								combobox ("craft_select", remote_craft, craft_select.id, inner_w, 150f, this, id => {
									craft_select.id = id;
									autoheight ();
								});
							});
							section (w*0.3f, inner_w2 => {
								if (GUILayout.Button ("OR upload this as a new craft", "button.wrapped", height (50) )) {
									change_mode("upload");
								}
							});
						});
						
						if (craft_select.id > 0) {
							style_override = GUI.skin.GetStyle ("background.dark.margin");
							v_section (w => {
								GUILayout.Label ("Pressing Update will update the this craft on KerbalX:", "h3");
								GUILayout.Label (KerbalX.existing_craft [craft_select.id] ["name"] + " (id: " + craft_select.id + ")", "h3");
								string craft_url = KerbalXAPI.url_to (KerbalX.existing_craft [craft_select.id] ["url"]);
								if(GUILayout.Button (craft_url, "hyperlink.h3")){
									Application.OpenURL (craft_url);
								}
								GUILayout.Label ("Make sure this is the craft you want update!");
							});
						}
					}
				}

				//Display any errors preventing upload
				if (errors.Count () > 0) {
					style_override = GUI.skin.GetStyle ("background.dark.margin");
					v_section (w => {
						foreach (string error in errors) {
							GUILayout.Label (error.Trim (), "alert", width (w));
						}
					});
				}

				//The great big ol' thing what you whack to make stuff happen
				if (show_upload_bttn) {
					GUI.enabled = enable_upload_bttn;
					section (w => {
						if (GUILayout.Button (mode_title + "!", "button.upload")) {
							if (mode == "update") {
								update_craft ();
							} else {
								upload_craft ();
							}
						}
					});
					GUI.enabled = true;
				}

				//display some feedback to show that upload/update is happening
				if(upload_progress != null){
					v_section (w => {
						GUILayout.Label (upload_progress, "h2");
						progress_spinner (w, 5, 50);
					});
				}
			}
		}



		//Go-No-Go checks before upload or update. returns bool which will either block or allow the upload/update action 
		//Any reasons to stop are added to errors and displayed
		private bool ok_to_send(){
			bool go_no_go = true;

			if(mode == "upload" && pictures.Count == 0){ 
				errors.Add ("You need to add at least 1 picture.");
				go_no_go = false;
			}

			if(mode == "update" && craft_select.id == 0){
				errors.Add ("You need to select a craft to update");
				go_no_go = false;
			}

			if(!craft_is_saved ()){
				errors.Add ("This craft has unsaved changes");
				go_no_go = false;
			}
			return go_no_go;
		}

		//common actions to perform before uploading/updating (take a message string to display)
		private void before_upload(string message){
			upload_progress = message;
			enable_upload_bttn = false;	//disable upload button to prevent multiple requests being fired at once.
			if(KerbalX.image_selector != null){KerbalX.image_selector.hide ();}
			upload_ticker_pos = 0;//this and frame_count used in the upload in progress....thing, in progess animation
			frame_count = 0;  		
		}

		//common actions to perform after upload/update
		private void after_upload(){
			enable_upload_bttn = true;
			upload_progress = null;
			autoheight ();
		}

		//Prepare craft data to upload as new craft on KerbalX
		private void upload_craft(){
			clear_errors ();
			if(ok_to_send ()){
				before_upload ("Uploading....");

				WWWForm craft_data = new WWWForm();
				craft_data.AddField ("craft_name", 	craft_name);
				craft_data.AddField ("craft_style", craft_styles [style_select.id]);
				craft_data.AddField ("craft_file", 	craft_file());
				craft_data.AddField ("part_data", 	JSONX.toJSON (part_info ()));
				
				int pic_count = 0; int url_count = 0;
				foreach(PicData pic in pictures){
					if(pic.file != null){
						craft_data.AddField ("images[image_" + pic_count++ + "]", Convert.ToBase64String (read_as_jpg (pic)));
					}else{
						craft_data.AddField ("image_urls[url_" + url_count++ + "]", pic.url);
					}
				}

				KerbalXAPI.upload_craft (craft_data, (resp, code) => {
					var resp_data = JSON.Parse (resp);
					if(code == 200){
						KerbalX.log ("craft uploaded OK");
						show_upload_compelte_dialog (resp_data["url"]);
						reset ();
						fetch_existing_craft();
					}else if(code == 422){
						KerbalX.log ("craft upload failed!");
						KerbalX.log (resp);
						string resp_errs = resp_data["errors"];
						errors = resp_errs.Split (',').ToList ();
					}
					after_upload ();
				});
			}
		}

		//prepare craft data to upload as an update to an existing craft on KerbalX
		private void update_craft(){
			clear_errors ();
			if(ok_to_send ()){
				before_upload ("Updating....");
				int craft_id = craft_select.id;	

				WWWForm craft_data = new WWWForm();
				craft_data.AddField ("craft_name", 	craft_name);
				craft_data.AddField ("craft_file", 	craft_file());
				craft_data.AddField ("part_data", 	JSONX.toJSON (part_info ()));

				KerbalXAPI.update_craft (craft_id, craft_data, (resp, code) => {
					var resp_data = JSON.Parse (resp);
					if (code == 200) {
						KerbalX.log ("craft update OK");
					} else if (code == 422) {
						KerbalX.log ("craft update failed!");
						KerbalX.log (resp);
						string resp_errs = resp_data["errors"];
						errors = resp_errs.Split (',').ToList ();
					}
					after_upload ();
				});
			}
		}

		//Dialog box to display once an upload has compelted.
		public void show_upload_compelte_dialog(string craft_path){  //TODO re-privatise
			KerbalXDialog dialog = show_dialog ((d) => {
				v_section (w => {
					GUILayout.Label ("Your craft has uploaded!", "h1");
					string craft_url = KerbalXAPI.url_to (craft_path);
					GUILayout.Space (10f);
					if(GUILayout.Button (craft_url, "hyperlink.h2", width (500f))){
						Application.OpenURL (craft_url);
					}
					if(GUILayout.Button (StyleSheet.assets["logo_large"], "no_style", width (500f), height (90.36f))){
						Application.OpenURL (craft_url);
					}
					GUILayout.Label ("Click the link (or logo) to view your craft.\nIf you want to the page layout click the \"Edit Craft\" link at the top of the page.", "small");
//					GUILayout.Label (d.window_pos.ToString ());
					section (w2 => {
						GUILayout.FlexibleSpace ();
						if(GUILayout.Button ("close", width (50f))){
							close_dialog ();
						}
					});

				});
			});
			dialog.prevent_editor_click_through = true;
			dialog.window_title = "";
			dialog.window_pos = new Rect((Screen.width/2 - 528f/2), Screen.height/4, 528f, 5);
		}

		//Makes a GET to KerbalX to return info about the users uploaded craft.  Full craft data is stored on KerbalX.existing_craft
		//minimal info (craft id and name) is stached on remote_craft which is used to populate the select menu and check for matching craft.
		private void fetch_existing_craft(){
			KerbalXAPI.fetch_existing_craft (() => {  //Query KX for the user's current craft (which gets stashed on KerablX.existing_craft). lambda gets called once request completes.
				remote_craft.Clear ();
				remote_craft.Add (0, "select a craft");	//remote_craft populates the select menu, ID 0 (which can't exist on KX) is used as the placeholder
				foreach(KeyValuePair<int, Dictionary<string, string>> craft in KerbalX.existing_craft){
					remote_craft.Add (craft.Key, craft.Value["name"]);
				}
				check_for_matching_craft_name ();
			});
		}

		//Called from the ImageSelector to add a picture (as PicData) to the list of pictures to upload.
		public void add_picture(PicData picture){
			errors.Clear ();
			int pic_count = 0;
			foreach(PicData pic in pictures){					//count up how many pictures with file data have been added
				if(pic.file != null){pic_count++;};
			}
			if(pic_count < max_pics || picture.file == null){ 	//if the count is under max_pics or the pic doesn't have a file (ie it's url) then add it.
				pictures.Add (picture);
			}else{
				errors = new List<string> (){ 					//Otherwise show error message.
					"You can only add " + max_pics + " pictures for upload, (bandwidth limitations, sorry!)", 
					"You can add as many image urls as you like though." 
				};
			}
		}

		//removes a selected picture from pictures. Does this by creating a new List<PicData> and adding all other pics into it
		//because using pictures.Remove () causes an error to be shown in the console, (about breaking enumeration).
		public void remove_picture(PicData picture){
			List<PicData> new_list = new List<PicData> ();
			foreach(PicData pic in pictures){
				if(!pic.Equals (picture)){ 
					new_list.Add (pic);
				}
			}
			pictures = new_list;
			clear_errors ();
		}

		//reset any error
		public void clear_errors(){
			errors.Clear ();
			autoheight ();
		}

		//reset interface
		public void reset(){
			KerbalX.log ("Resetting UploadInterface");
			clear_errors ();
			pictures.Clear ();
			style_select.id = 0;
			craft_select.id = 0;
		}

		//switch interface mode ( "upload" || "update" ) 
		private void change_mode(string new_mode){
			mode = new_mode;
			clear_errors ();
		}
		
		//returns the craft file
		private string craft_file(){
			return System.IO.File.ReadAllText(craft_path ());
		}

		//returns the full path of the craft file
		private string craft_path(){
			return ShipConstruction.GetSavePath (editor_craft_name);
		}

		private string short_path(){
			return craft_path ().Replace (Paths.joined (KSPUtil.ApplicationRootPath, "saves"), "");
		}

		//Check if craft file exists
		private bool craft_file_exists(){
			return System.IO.File.Exists (craft_path ());
		}

		//Check if the current craft in the editor has unsaved changes. 
		//Kinda horrible appoach involing a temporary file, some C4 and a Swedish chiropractor (well not really, but it's kinda convoluted).
		private bool craft_is_saved(){
			if(craft_file_exists ()){
				string temp_path = Paths.joined (KSPUtil.ApplicationRootPath, "GameData", "KerbalX", "temp.craft");	//set temp place to save current craft
				EditorLogic.fetch.ship.SaveShip().Save(temp_path);													//tell editor to save craft to temp location
				bool is_saved = Checksum.compare (File.ReadAllText(temp_path), craft_file ());						//compare checksums of the two craft 
				File.Delete (temp_path);																			//remove temp craft
				return is_saved;
			}else{
				return false;
			}
		}



		//check if craft_name matches any of the user's existing craft.  Sets matching_craft_ids to contain KX database ids of any matching craft
		//if only one match is found then craft_select.id is also set to the matched id (which them selects the craft in the select menu)
		private void check_for_matching_craft_name(){
			if(craft_name != "" || craft_name != null){
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
		}

		//returns a unique set of the craft's parts and data about each part;
		//{"partname1" => {"mod" => "mod_name"}, "partname2" => {"mod" => "mod_name"}, ....} #yeah, explained in Ruby hash notation, cos...it's terse
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


		//Takes a PicData object and reads the image bytes. 
		//If the image is already a jpg then it just returns the bytes, otherwise it is converted into a jpg first
		private byte[] read_as_jpg(PicData pic){
			byte[] original_image = File.ReadAllBytes (pic.file.FullName);
			if(pic.file.Extension.ToLower () == ".jpg"){
				return original_image;
			}else{
				KerbalX.log ("compressing: " + pic.file.Name);
				Texture2D converter = new Texture2D (2, 2);
				converter.LoadImage (original_image);
				return converter.EncodeToJPG ();
			}
		}

		//Derpy little upload waiting thing.
		//takes a container width, number of boxes to draw and how fast to cycle (lower is faster)
		private void progress_spinner(float w, int box_count, int speed){
			GUIStyle centered_container = new GUIStyle();
			float box_size = 15f;
			float space_size = 20;
			int p = (int)(w-((box_count-1)*space_size + box_count*box_size))/2;
			centered_container.padding = new RectOffset(p,p,0,0);
			style_override = centered_container;
			section (w2 => {
				frame_count++;
				if(frame_count > speed){
					upload_ticker_pos++;
					if(upload_ticker_pos > box_count){upload_ticker_pos=0;}
					frame_count = 0;
				}
				for(int i=0; i < box_count; i++){
					GUILayout.Label ("", (upload_ticker_pos == i ? "box.blue" : "box"), width (box_size), height (box_size));
					if(i != box_count-1){
						GUILayout.Space (20f);
					}
				}
			});
		}
	}
}

