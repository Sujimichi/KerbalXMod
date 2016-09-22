using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.IO;

using UnityEngine;

namespace KerbalX
{

	public struct PicData{
		public string name;
		public string url;
		public FileInfo file;
		public Texture2D texture;
		public void initialize(string new_name, FileInfo new_file, Texture2D new_texture){
			name = new_name;
			file = new_file;
			texture = new_texture;
		}
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KerbalXImageSelector : KerbalXWindow
	{
		private List<PicData> pictures = new List<PicData>();				//populated by load_pics, contains PicData objects for each pic 
		private List<List<PicData>> groups = new List<List<PicData>> ();	//nested list of lists - rows of pictures for display in the interface

		private string mode = "pic_selector";
		private int pics_per_row = 4;
		private string[] file_types = new string[]{"jpg", "png"};
		private Vector2 scroll_pos;
		private int file_count = 0;

		Texture2D pic_highlight 	= new Texture2D(1, 1, TextureFormat.RGBA32, false);
		Texture2D scroll_background = new Texture2D(1, 1, TextureFormat.RGBA32, false);


		private string pic_url = "";
		private string hover_ele = "";

		private void Start(){
			window_title = "KerbalX::ScreenShots";
			float w = 610;
			window_pos = new Rect((Screen.width/2 - w/2), Screen.height/4, w, 5);
			visible = false;
			prevent_editor_click_through = true;
			KerbalX.image_selector = this;

			pic_highlight.SetPixel(0, 0, new Color (0.4f,0.5f,0.9f,1));
			pic_highlight.Apply ();

			scroll_background.SetPixel(0, 0, new Color (0.12f,0.12f,0.12f,0.7f));
			scroll_background.Apply ();
		}

		protected override void on_show(){
			change_mode ("pic_selector");
			pic_url = "";
			int count = picture_files ().Count;
			if(count != file_count){
				prepare_pics ();
			}
		}

		protected override void WindowContent(int win_id)
		{
			pic_hover.normal.background = pic_highlight;

			if(mode == "url_entry"){
				v_section (w => {
					GUILayout.Label ("Enter the URL to your image", header_label);
					GUILayout.Label ("note: one of 'em urls what end with an extension ie .jpg");
					section(w2 => {
						pic_url = GUILayout.TextField (pic_url, width (w2-100f));
						if(GUILayout.Button ("Add url", width (100f))){
							PicData pic = new PicData();
							pic.url = pic_url;
							KerbalX.editor_gui.pictures.Add (pic);
							this.hide ();
						};	
					});
					if(GUILayout.Button ("or pic a pic from your pics, erm.", height (40f))){
						change_mode ("pic_selector");
					};
				});

			}else{
				section (w => {
					GUILayout.Label ("Select a picture for your craft", header_label, width (w-100f));
					if(GUILayout.Button ("or enter a url", width (100f))){
						change_mode ("url_entry");
					};
				});

				if (pictures.Count > 0) {
					scroll_pos = scroll (scroll_pos, 620f, 300f, w => {
						foreach(List<PicData> row in groups){
							style_override = new GUIStyle ();
							style_override.normal.background = scroll_background;
							section (600f, sw => {
								foreach(PicData pic in row){
									v_section (150f, w2 => {
										if(GUILayout.Button (pic.texture, (hover_ele==pic.name ? pic_hover : pic_link), width (w2), height (w2*0.75f))){
											select_pic (pic);
										}
										if(GUILayout.Button (pic.name, (hover_ele==pic.name ? pic_hover : pic_link), width(w2))){
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
					prepare_pics ();
				}

			}
		}

		private void change_mode(string new_mode){
			mode = new_mode;
			autoheight ();
		}

		private void select_pic(PicData pic){
			KerbalX.editor_gui.pictures.Add (pic);
			this.hide ();
		}

		private List<FileInfo> picture_files(){
			DirectoryInfo dir = new DirectoryInfo (KerbalX.screenshot_dir);
			List<FileInfo> files = new List<FileInfo> ();

			//Get file info for all files of defined file_types within the given dir
			foreach(string file_type in file_types){
				foreach(FileInfo file in dir.GetFiles ("*." + file_type)){
					files.Add (file);
				}
			}
			return files;
		}

		private void prepare_pics(){
			List<FileInfo> files = picture_files ();
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
			file_count = files.Count;
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




}

