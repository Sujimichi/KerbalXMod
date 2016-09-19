﻿using System;
using UnityEngine;
using System.Collections.Generic;

namespace KerbalX
{

	public struct DropdownData{
		public int id;
		public bool show_select;
		public Vector2 scroll_pos;
		public void selected(int set_id){
			id = set_id;
		}
	}

	/* KerbalXWindow is a base class to be inherited by classes which draw GUI windows
	It provides common setup required to draw a GUI window, enabling DRY and minimal window classes.
	A class which inherits KerbalXWindow needs to override the WindowContent method to define the content of the window
	Basic Usage:
		[KSPAddon(KSPAddon.Startup.MainMenu, false)]
		public class SimpleWindow : KerbalXWindow
		{
			protected override void WindowContent(int win_id)
			{
				GUILayout.Label ("some nonsense", GUILayout.Width (60f));
			}
		}
	Attributes like the window title, size/pos, id are set, but can be overridden by defining a Start() method
	Configured Usage:
		[KSPAddon(KSPAddon.Startup.MainMenu, false)]
		public class SimpleWindow : KerbalXWindow
		{
			private void Start()
			{
				window_pos = new Rect(100,100,500,200); //defaults to new Rect() if not set
				window_title = "test window";			//defaults to "untitled window" if not set
				window_id = 42;							//defaults to the next ID in sequence (change last_window_id in the base class to change the sequence start point)
				footer = false							//defaults to true. if true adds a common set of GUI elements defined in DrawWindow
				draggable = false;						//defaults to true. makes the windows draggable, duh. 
			}

			protected override void WindowContent(int win_id)
			{
				GUILayout.Label ("some nonsense", GUILayout.Width (60f));
			}
		}	 
	KerbalXWindow also provides the handy-dandy fabtastic grid method. grid takes a width and a lambda (delegate) statement and wraps the actions defined in the
	lambda in calls to BeginHorizontal and EndHorizontal.  This ensures End is always called after a begin, and (I think) makes for clearer and more readable code.
 	*/
	public class KerbalXWindow : MonoBehaviour
	{
		protected string window_title = "untitled window";

		public Rect window_pos = new Rect();
		public bool visible = true;

		protected bool footer = true;
		protected bool draggable = true;
		protected bool prevent_editor_click_through = false;

		protected int window_id = 0;
		static int last_window_id = 0;

		protected GUIStyle style_override = null;
		protected GUIStyle section_style = new GUIStyle();
//		protected GUISkin cust_skin = null;

		private Texture2D kx_logo_small = new Texture2D(166,30, TextureFormat.ARGB32, false);


		//Definition of delegate to be passed into the section method 
		protected delegate void Content(float width);


		/* Essentially wraps the actions of a delegate (lambda) in calls to BeginHorizontal and EndHorizontal
		 * Can take an optional width float which if given will be passed to BeginHorizontal as GUILayoutOption params for Width and MaxWidth
		 * Takes a lambda statement as the delegate Content which is called inbetween calls to Begin/End Horizontal
		 * The lambda will be passed a float which is either the width supplied or is the width of the windown (minus padding and margins)
		 * Usage:
			section (400f, w => {
				// Calls to draw GUI elements inside a BeginHorizontal group ie;
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, 400f in this case
			});	
			OR without defining a width
			section (w => {
				// Calls to draw GUI elements inside a BeginHorizontal group ie;
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, window_pos.width in this case
			});	
		* In a slightly crazy approach, you can also define a GUIStyle to pass to BeginHorizontal by setting style_override before calling section, ie:
			style_override = new GUIStyle();
			style_override.padding = new RectOffset (20, 20, 10, 10);
			section (w => {
				// Calls to draw GUI elements inside a BeginHorizontal group ie;
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, window_pos.width in this case
			});	
		*/
		protected void section(Content content){ section (-1, content); } //alias (overload) for section when used without a width float, just a lambda.
		protected void section(float section_width, Content content)
		{
			//Call BeginHorizontal giving the style as either default GUIStyle or style_override and any GUILayoutOptions
			GUILayout.BeginHorizontal(style_override == null ? section_style : style_override, section_options (section_width)); 
			style_override = null;						//style_override is set back to null so it doesn't effect any other sections.
			content(callback_width (section_width));	//call the lambda and pass a width (either given width or window.pos.width)
			GUILayout.EndHorizontal ();					//ze END!
		}

		//Works in the same way as section but wraps the lambda in Begin/End Vertical instead.
		protected void v_section(float section_width, Content content){
			//Call BeginHorizontal giving the style as either default GUIStyle or style_override and any GUILayoutOptions
			GUILayout.BeginVertical(style_override == null ? section_style : style_override, section_options (section_width)); 
			style_override = null;						//style_override is set back to null so it doesn't effect any other sections.
			content(callback_width (section_width));	//call the lambda and pass a width (either given width or window.pos.width)
			GUILayout.EndVertical ();					//ze END!
		}

		protected Vector2 scroll(Vector2 scroll_pos, float scroll_width, float scroll_height, Content content){
			GUILayoutOption[] opts = new GUILayoutOption[]{ GUILayout.Width(scroll_width), GUILayout.MaxWidth (scroll_width), GUILayout.Height(scroll_height) };
			scroll_pos = GUILayout.BeginScrollView(scroll_pos, (style_override == null ? section_style : style_override), opts);
			style_override = null;	//style_override is set back to null so it doesn't effect any other sections.
			content (scroll_width);
			GUILayout.EndScrollView();
			return scroll_pos;
		}

		//Helpers for section, v_section and scroll
		//section_options returns GUILayoutOptions to pass into Begin(Horizontal/Vertical). either returns empty set of options (if width is given as -1) 
		//or a set of options defining Width and MaxWidth.
		private GUILayoutOption[] section_options(float section_width){
			GUILayoutOption[] opts = new GUILayoutOption[]{};	//GUILayoutOptions to pass onto BeginHorizontal
			if (section_width != -1){							//if width is given a -1 then no GUILayoutOptions are used
				opts = new GUILayoutOption[]{ GUILayout.Width(section_width), GUILayout.MaxWidth (section_width) };
			}
			return opts;
		}

		private float callback_width(float section_width){
			if(section_width == -1){				//if width was given as -1 then the with of the window (minus its padding and margins) is used instead
				section_width = window_pos.width - GUI.skin.window.padding.horizontal - GUI.skin.window.border.horizontal;
			}
			return section_width; //width to pass back into the lambda
		}


		protected DropdownData dropdown(Dictionary<int, string> collection, DropdownData drop_data, float outer_width, float menu_height){
			GUIStyle dropdown_field = new GUIStyle (GUI.skin.textField);
			GUIStyle dropdown_menu_item = new GUIStyle (GUI.skin.label);
			//dropdown_menu_item.normal.textColor = Color.magenta;
			dropdown_menu_item.onHover.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB
			dropdown_menu_item.hover.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB
			dropdown_menu_item.padding = new RectOffset (0, 0, 0, 0);

			string selected;
			collection.TryGetValue (drop_data.id, out selected);

			v_section (outer_width, (inner_width) => {
				section (inner_width, w2 => {
					if (GUILayout.Button (selected, dropdown_field, GUILayout.Width (inner_width - 20) )) {
						drop_data.show_select = !drop_data.show_select;	
						autoheight ();
					}
					if (GUILayout.Button ("\\/", GUILayout.Width (20f) )) {
						drop_data.show_select = !drop_data.show_select;	
						autoheight ();
					}
				});
				section (inner_width, w2 => {
					if(drop_data.show_select){
						drop_data.scroll_pos = scroll (drop_data.scroll_pos, w2, menu_height, (w3) => {
							foreach(KeyValuePair<int, string> item in collection){
								if(GUILayout.Button (item.Value, dropdown_menu_item, GUILayout.Width (w3-25))){
									drop_data.selected (item.Key);
									drop_data.show_select = false;
									autoheight ();
								}
							}
						});
					}
				});
			});
			return drop_data;
		}

		//shorthand for GUILayout.width()
		protected GUILayoutOption width(float w){
			return GUILayout.Width (w);
		}
		//shorthand for GUILayout.height()
		protected GUILayoutOption height(float h){
			return GUILayout.Height (h);
		}

//		protected float pcent(string percent, object width_in){
//			float p = float.Parse (percent.Replace ("%", ""));
//			float w = (float)width_in;
//			return (float)Math.Floor ((w / 100) * p);
//		}

		protected void prevent_click_through(string mode){
			if(mode == "editor"){
				Vector2 mouse_pos = Input.mousePosition;
				mouse_pos.y = Screen.height - mouse_pos.y;
				if(window_pos.Contains (mouse_pos)){
					EditorLogic.fetch.Lock (true, true, true, window_id.ToString ());
				}else{
					EditorLogic.fetch.Unlock (window_id.ToString ());				
				}
			}
		}

		public void autoheight(){
			window_pos.height = 5;
		}

		public void show(){
			visible = true;
			on_show ();
		}
		public void hide(){
			visible = false;
			on_hide ();
		}
		public void toggle(){
			if(visible){
				hide ();
			}else{
				show ();
			}
		}
		public bool is_visible(){
			return visible;
		}
			
		protected virtual void on_hide(){ }
		protected virtual void on_show(){ }

		protected void Awake(){
			kx_logo_small = GameDatabase.Instance.GetTexture (Paths.joined ("KerbalX", "Assets", "KXlogo_small"), false);
		}

		//called on each frame, handles drawing the window and will assign the next window id if it's not set
		protected void OnGUI()
		{
			if(window_id == 0){
				window_id = last_window_id + 1;
				last_window_id = last_window_id + 1;
			}

			if(visible){
				window_pos = GUILayout.Window (window_id, window_pos, DrawWindow, window_title, GUILayout.Width( window_pos.width ), GUILayout.MaxWidth( window_pos.width ), GUILayout.ExpandHeight (true));
			}
		}

		//Callback methods which is passed to GUILayout.Window in OnGUI.  Calls WindowContent and performs common window actions
		private void DrawWindow(int window_id)
		{
			if(prevent_editor_click_through){
				prevent_click_through ("editor");
			}

			//Draw the main content of the window as defined by WindowContent
			WindowContent (window_id);			

			//add common footer elements for all windows
			if(footer){
				GUIStyle link_label_style = new GUIStyle (GUI.skin.label);
				link_label_style.normal.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB

				section (w => {
					if(GUILayout.Button ("KerbalX.com", link_label_style, width (75f))){
						Application.OpenURL (KerbalX.site_url);
					}
					GUILayout.FlexibleSpace ();
					GUILayout.Label (kx_logo_small);
				});
				//GUILayout.Label ("window id: " + window_id);
			}

			if(draggable){
				GUI.DragWindow();
			}
		}

		//The main method which defines the content of a window.  This method is provided so as to be overridden in inherited classes
		protected virtual void WindowContent(int window_id)
		{

		}

		private void onDestroy(){
			print ("shit was destroyed yo"); 
		}
	}
}

