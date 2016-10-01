using System;
using System.Linq;
using System.Threading;
//using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalX
{



	/* KerbalXWindow is a base class to be inherited by classes which draw GUI windows. It inherits from KerbalXWindowExtension which in turn inherits from MonoBehaviour
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
	public class KerbalXWindow : KerbalXWindowExtension
	{

		//Window Config variables. Change these in Start() in descendent classes.
		//public Rect window_pos					= new Rect()			//override in Start() to set window size/pos - default values defined in KerbalXWindowExtension
		public string window_title 					= "untitled window";	//shockingly enough, this is the window title
		public bool prevent_editor_click_through 	= false;				//only set to true in editor windows - prevents clicks interacting with elements behind the window
		protected bool require_login 				= false;				//set to true if the window requires user to be logged into KerbalX
		protected bool draggable 					= true;					//sets the window as draggable
		protected bool footer 						= true;					//sets if the set footer content should be draw (see FooterContent method)
		public bool visible 						= true;					//sets if the window is visible to start with (see show(), hide(), toggle(), on_show(), on_hide())
		protected bool gui_locked 					= false;				//if true will disable interaction with the window (without changing its appearance) (see lock_ui() and unlock_ui())
		protected int window_id   					= 0;					//can be set to override automatic ID assignment. If left at 0 it will be auto-assigned

		static int last_window_id = 0;			//static track of the last used window ID, new windows will take the next value and increment this.
		public static GUISkin KXskin = null;	//static variable to hold the reference to the custom skin. First window created will set it up

		protected bool is_dialog = false;


		//show, hide and toggle - basically just change the value of the bool visible which defines whether or not OnGUI will draw the window.
		//also provides hooks (on_show and on_hide) for decendent classes to trigger actions when showing or hiding. 
		public void show(){
			visible = true;
			on_show ();
		}
		public void hide(){
			visible = false;
			on_hide ();
			StartCoroutine (unlock_delay ()); //remove any locks on the editor interface, after a slight delay.
		}
		public void toggle(){
			if (visible) {
				hide ();	
			} else {
				show ();
			}
		}


		//unlock delay just adds a slight delay between an action and unlocking the editor.
		//incases where a click on the window also result in closing the window (ie a close button) then the click would also get registered by whatever is behind the window
		//adding this short delay prevents that from happening.
		public IEnumerator unlock_delay(){
			yield return true;	//doesn't seem to matter what this returns
			Thread.Sleep (100);
			EditorLogic.fetch.Unlock (window_id.ToString ()); //ensure any locks on the editor interface are release when hiding.
		}

		//overridable methods for class which inherit this class to define actions which are called on hide and show
		protected virtual void on_hide(){ }
		protected virtual void on_show(){ }

		protected virtual void on_error(){ }

		//lock_iu and unlock_ui result in GUI.enabled being set around the call to draw the contents of the window.
		//lets you disable the whole window (it also results in a change to the GUI.color which makes this change without a visual change).
		public void lock_ui(){
			gui_locked = true;
		}
		public void unlock_ui(){
			gui_locked = false;
		}

		//called after successful login IF the login was initiated from a KerbalXWindow. Windows which inherit from KerbalXWindow can override this 
		//method to have specific actions performed after login (ie: UploadInterface will request a fetch of existing craft).
		protected virtual void on_login(){
			GameObject.Destroy (KerbalX.login_gui);
		}
		
		//As window will have been drawn with GUILayout.ExpandHeight(true) setting the height to a small value will cause the 
		//window to readjust its height.  Only call after actions which reduce the height of the content, don't call it constantly OnGUI (unless Epilepsy is something you enjoy)
		public void autoheight(){
			window_pos.height = 5;
		}


		//Essential for any window which needs to make web requests.  If a window is going to trigger web requests then it needs to call this method on its Start() method
		//The Request handler handles sending requests asynchronously (so delays in response time don't lag the interface).  In order to do that it 
		//uses Coroutines which are a MonoBehaviour concept, hence this calls in a decendent of MonoBehaviour can't be started by the static methods on the API class.
		protected void enable_request_handler(){
			if(RequestHandler.instance == null){
				KerbalX.log ("starting web request handler");
				RequestHandler request_handler = gameObject.AddOrGetComponent<RequestHandler> ();
				RequestHandler.instance = request_handler;
			}
		}

		//opens a dialog window which is populated by the lambda statement passed to show_dialog ie:
		//show_dialog((d) => {
		//	GUILayout.Label("hello I'm a dialog");
		//})
		//The dialog instance is returned by show_dialog, and it's also passed into the lambda.
		protected KerbalXDialog show_dialog(DialogContent content){
			KerbalXDialog dialog = gameObject.AddOrGetComponent<KerbalXDialog> ();
			dialog.content = content;
			return dialog;
		}
		protected void close_dialog(){
			KerbalXDialog.close ();		//close instance of dialog if it exists.
		}

		//basically just syntax sugar for a call to AddOrGetComponent for specific named windows. (unfortunatly has nothing to do with launching rockets)
		protected void launch(string type){
			if(type == "ImageSelector"){
				gameObject.AddOrGetComponent<KerbalXImageSelector> ();
			}else if(type == "ActionGroupEditor"){
				gameObject.AddOrGetComponent<KerbalXActionGroupInterface> ();
			}
		}

		//prevents mouse actions on the GUI window from affecting things behind it.  Only works in the editors at present.
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


		//MonoBehaviour methods

		//called on each frame, handles drawing the window and will assign the next window id if it's not set
		protected void OnGUI()
		{
			if(window_id == 0){
				window_id = last_window_id + 1;
				last_window_id = last_window_id + 1;
			}

			if(visible){
				GUI.skin = KXskin;
				window_pos = GUILayout.Window (window_id, window_pos, DrawWindow, window_title, GUILayout.Width( window_pos.width ), GUILayout.MaxWidth( window_pos.width ), GUILayout.ExpandHeight (true));
				GUI.skin = null;
			}
		}

		//Callback methods which is passed to GUILayout.Window in OnGUI.  Calls WindowContent and performs common window actions
		private void DrawWindow(int window_id)
		{
			if(prevent_editor_click_through){
				prevent_click_through ("editor");
			}

			//if a server error has occured display error in dialog, but don't halt drawing of interface. 
			if(KerbalX.server_error_message != null){
				List<string> messages = KerbalX.server_error_message.Split (new string[] { Environment.NewLine }, StringSplitOptions.None).ToList ();
				KerbalX.server_error_message = null;
				string title = messages [0];
				messages [0] = "";

				KerbalXDialog dialog = show_dialog((d) => {
					v_section (w => {
						GUILayout.Label (title, "alert.h2");
						foreach(string message in messages){
							if(message != ""){GUILayout.Label (message);}
						}
						if(GUILayout.Button ("OK", height (30))){close_dialog (); }
					});
				});
				dialog.window_title = title;
				on_error ();
			}


			//If unable to connect to KerbalX halt drawing interface and replace with "try again" button"
			if (KerbalX.failed_to_connect) {
				GUILayout.Label ("Unable to Connect to KerbalX.com!");
				if (GUILayout.Button ("try again")) {
					RequestHandler.instance.try_again ();
				}
			//If user is not logged in halt drawing interface and show login button (unless the window is a dialog window)"
			} else if (!is_dialog && require_login && KerbalXAPI.logged_out ()) {
				GUILayout.Label ("You are not logged in.");
				if (GUILayout.Button ("Login")) {
					KerbalXLoginWindow login_window = gameObject.AddOrGetComponent<KerbalXLoginWindow> ();
					login_window.after_login_action = () => {
						on_login ();
					};
				}

			//If an upgrade is required, halt drawing interface and show message;
			} else if (KerbalX.upgrade_required) {
				GUILayout.Label ("Upgrade Required", "h3");
				GUILayout.Label ("This version of the KerbalX mod is no longer compatible with KerbalX.com\nYou need to get the latest version.");
				GUILayout.Label (KerbalX.upgrade_required_message);
				on_error ();
			//otherwse all is good, draw the main content of the window as defined by WindowContent
			}else{
				if(gui_locked){
					GUI.enabled = false;
					GUI.color = new Color (1, 1, 1, 2); //This enables the GUI to be locked from input, but without changing it's appearance. 
				}
				WindowContent (window_id);	
				GUI.enabled = true;
				GUI.color = Color.white;
			}

			//add common footer elements for all windows if footer==true
			if(footer){
				FooterContent (window_id);
			}

			if(draggable){
				GUI.DragWindow();
			}
		}


		//The main method which defines the content of a window.  This method is provided so as to be overridden in inherited classes
		protected virtual void WindowContent(int window_id)
		{

		}

		//Default Footer for all windows, can be overridden only called if footer==true
		protected virtual void FooterContent(int window_id){
			section (w => {
				if(GUILayout.Button ("KerbalX.com", "hyperlink.footer", width (75f), height (30f))){
					Application.OpenURL (KerbalX.site_url);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.Label (StyleSheet.assets["logo_small"]);
			});
		}

		protected virtual void OnDestroy(){
			EditorLogic.fetch.Unlock (window_id.ToString ());
		}
	}




	public class KerbalXWindowExtension : MonoBehaviour
	{
		public Rect window_pos = new Rect((Screen.width/2 - 500f/2), 200, 500f, 5);
		protected GUIStyle style_override = null;
		protected GUIStyle section_style = new GUIStyle();

		public Dictionary<string, Rect> anchors = new Dictionary<string, Rect> ();	//anchors are used by the ComboBox. Each anchor is a named reference to a Rect obtained from GetLastRect

		//shorthand for GUILayout.width()
		protected GUILayoutOption width(float w){
			return GUILayout.Width (w);
		}
		//shorthand for GUILayout.height()
		protected GUILayoutOption height(float h){
			return GUILayout.Height (h);
		}



		//Definition of delegate to be passed into the section, v_section and scroll methods
		protected delegate void Content(float width);
		protected delegate void ContentNoArgs();


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
		protected void v_section(Content content){ v_section (-1, content); } //alias (overload) for v_section when used without a width float, just a lambda.
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
		//either simply returns the given width, unless it's given as -1 in which case it returns the window width (minus padding and margin)
		private float callback_width(float section_width){
			if(section_width == -1){				//if width was given as -1 then the with of the window (minus its padding and margins) is used instead
				section_width = window_pos.width - GUI.skin.window.padding.horizontal - GUI.skin.window.border.horizontal;
			}
			return section_width; //width to pass back into the lambda
		}

		protected void begin_group(Rect container, ContentNoArgs content){
			GUI.BeginGroup (container);
			content ();
			GUI.EndGroup ();
		}


		protected void combobox(string combo_name, Dictionary<int, string> select_options, int selected_id, float list_width, float list_height, KerbalXWindow win, ComboResponse resp){
			section (list_width, w => {
				float h = 22f + select_options.Count * 17;
				if(h > list_height){h = list_height;}
				if (GUILayout.Button (select_options [selected_id], GUI.skin.textField, width (w-20f))) {
					gameObject.AddOrGetComponent<ComboBox> ().open (combo_name, select_options, anchors[combo_name], h, win, resp);
				}
				track_rect (combo_name, GUILayoutUtility.GetLastRect ());
				if (GUILayout.Button ("\\/", width (20f))) {
					gameObject.AddOrGetComponent<ComboBox> ().open (combo_name, select_options, anchors[combo_name], h, win, resp);
				}
			});		
		}

		protected void track_rect(string name, Rect rect){
			if (rect.x != 0 && rect.y != 0) {
				if (!anchors.ContainsKey (name)) {
					anchors [name] = rect;
				}
			}
		}


//		public struct DropdownData{
//			public int id;
//			public bool show_select;
//			public Vector2 scroll_pos;
//			public void selected(int set_id){
//				id = set_id;
//			}
//		}
//		protected DropdownData dropdown(Dictionary<int, string> collection, DropdownData drop_data, float outer_width, float menu_height){
//			GUIStyle dropdown_field = new GUIStyle (GUI.skin.textField);
//			GUIStyle dropdown_menu_item = new GUIStyle (GUI.skin.label);
//			//dropdown_menu_item.normal.textColor = Color.magenta;
//			dropdown_menu_item.onHover.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB
//			dropdown_menu_item.hover.textColor = new Color (0.4f,0.5f,0.9f,1); //color also known as KerbalX Blue - #6E91EB
//			dropdown_menu_item.padding = new RectOffset (0, 0, 0, 0);
//
//			string selected;
//			collection.TryGetValue (drop_data.id, out selected);
//
//			v_section (outer_width, (inner_width) => {
//				section (inner_width, w2 => {
//					if (GUILayout.Button (selected, dropdown_field, GUILayout.Width (inner_width - 20) )) {
//						drop_data.show_select = !drop_data.show_select;	
//						autoheight ();
//					}
//					if (GUILayout.Button ("\\/", GUILayout.Width (20f) )) {
//						drop_data.show_select = !drop_data.show_select;	
//						autoheight ();
//					}
//				});
//				section (inner_width, w2 => {
//					if(drop_data.show_select){
//						drop_data.scroll_pos = scroll (drop_data.scroll_pos, w2, menu_height, (w3) => {
//							foreach(KeyValuePair<int, string> item in collection){
//								if(GUILayout.Button (item.Value, dropdown_menu_item, GUILayout.Width (w3-25))){
//									drop_data.selected (item.Key);
//									drop_data.show_select = false;
//									autoheight ();
//								}
//							}
//						});
//					}
//				});
//			});
//			return drop_data;
//		}
	}
}

