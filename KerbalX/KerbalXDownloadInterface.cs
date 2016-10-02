using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace KerbalX
{

	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class KerbalXDownloadInterface : KerbalXWindow
	{

		private Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();
		private string title = "";
		private int[] craft_ids;
		private Vector2 scroll_pos;
		private float scroll_height;
		private float win_top = 200f;
		private float max_scroll_height;

		private void Start(){
			KerbalX.download_gui = this;
			window_title = "KerbalX::Downloader";
			window_pos = new Rect (Screen.width / 2 - 500 / 2, win_top, 500, 5);
			max_scroll_height = Screen.height - (win_top + 200);
			require_login = true;
			enable_request_handler ();
			fetch_download_queue ();
		}

		//Called after a succsessful login, if the login dialog was opened from this window.
		protected override void on_login ()
		{
			base.on_login ();		//inherits call to hide login window
			fetch_download_queue ();//fetch list of craft to download
		}

		protected override void WindowContent(int win_id){
			section (w => {
				if(GUILayout.Button ("Download Queue", "button.bold", width (w*0.33f))){fetch_download_queue();}
				if(GUILayout.Button ("Past Downloads", "button.bold", width (w*0.33f))){fetch_past_downloads();}
				if(GUILayout.Button ("Your Craft", 	   "button.bold", width (w*0.33f))){fetch_users_craft();}
			});

			GUILayout.Label (title, "h2");
			scroll_pos = scroll (scroll_pos, 500f, scroll_height, sw => {
				foreach(int id in craft_ids){
					style_override = GUI.skin.GetStyle ("background.dark.margin");
					section (w => {
						v_section (w2 => {
							GUILayout.Label (craft_list[id]["name"], "h3");
							GUILayout.Label (craft_list[id]["type"] + " | made in KSP:" + craft_list[id]["version"]);
						});
						v_section (w2 => {
							if(GUILayout.Button ("download")){
								download_craft (id);
							}
							GUILayout.Label (craft_list[id]["download_status"]);
						});
					});

				}
			});
		}

		private void fetch_download_queue(){
			title = "Download Queue";
			KerbalXAPI.fetch_download_queue ((craft_data) => {
				craft_list = craft_data;
				add_paths_to_data();
			});
		}

		private void fetch_past_downloads(){
			title = "Past Downloads";
			KerbalXAPI.fetch_past_downloads ((craft_data) => {
				craft_list = craft_data;
				add_paths_to_data();
			});
		}

		private void fetch_users_craft(){
			title = "Your Craft";
			KerbalXAPI.fetch_users_craft ((craft_data) => {
				craft_list = craft_data;
				add_paths_to_data();
			});
		}

		private void add_paths_to_data(){
			craft_ids = craft_list.Keys.ToArray ();
			scroll_height = craft_list.Count * 62;
			if(scroll_height > max_scroll_height){scroll_height = max_scroll_height;}
			foreach(int id in craft_ids){
				craft_list[id].Add ("dir",  dir_for_craft (id));
				craft_list[id].Add ("path", path_for_craft (id));
				craft_list[id].Add ("short_path", path_for_craft (id).Replace (Paths.joined (KSPUtil.ApplicationRootPath, "saves"), ""));
				craft_list[id].Add ("download_status", "");
			}
			autoheight ();
		}


		private string path_for_craft(int id){
			return Paths.joined (KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list [id] ["type"], craft_list[id]["name"] + ".craft");
		}
		private string dir_for_craft(int id){
			return Paths.joined (KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list [id] ["type"]);
		}

		private string craft_file;

		private void download_craft(int id){
			KerbalXAPI.download_craft (id, (craft_file_string, code) => {
				if(code == 200){
					Directory.CreateDirectory (craft_list[id]["dir"]); //ensure directorys exist (usually just subassembly folder which is missing). 
					craft_file = craft_file_string;
					if(File.Exists (craft_list[id]["path"])){
						KerbalXDialog dialog = show_dialog (d => {
							GUILayout.Label ("A craft with this name already exists", "h2");
							GUILayout.Label ("You have a craft at: " + craft_list[id]["short_path"], "small");
							section (w => {
								GUILayout.FlexibleSpace ();
								if(GUILayout.Button ("Cancel", width (w*0.2f))){
									close_dialog ();
								}
								if(GUILayout.Button ("Replace", width (w*0.2f))){
									write_file (id);
									close_dialog ();
								}
							});
						});					
						dialog.window_pos.y = Event.current.mousePosition.y;
						dialog.window_pos.x = window_pos.x + window_pos.width + 10;
						dialog.window_pos.width = 400f;
						dialog.window_title = "Replace Existing?";

					}else{
						write_file (id);
					}
				}
			});
		}

		private void write_file(int craft_id){
			File.WriteAllText(craft_list [craft_id] ["path"], craft_file);
			craft_list [craft_id]["download_status"] = "Downloaded";
		}


	}
}

