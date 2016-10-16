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
        public bool prevent_click_through   = true;     //prevent clicks interacting with elements behind the window
        protected bool require_login        = false;    //set to true if the window requires user to be logged into KerbalX
        protected bool draggable            = true;     //sets the window as draggable
        public bool footer                  = true;     //sets if the set footer content should be draw (see FooterContent method)
        public bool visible                 = true;     //sets if the window is visible to start with (see show(), hide(), toggle(), on_show(), on_hide())
        protected bool gui_locked           = false;    //if true will disable interaction with the window (without changing its appearance) (see lock_ui() and unlock_ui())
        protected int window_id             = 0;        //can be set to override automatic ID assignment. If left at 0 it will be auto-assigned
        static int last_window_id           = 0;        //static track of the last used window ID, new windows will take the next value and increment this.
        public string window_title          = "untitled window";    //shockingly enough, this is the window title
        //public Rect window_pos            = new Rect()            //override in Start() to set window size/pos - default values are defined in KerbalXWindowExtension

        private bool interface_locked       = false; //not to be confused with gui_locked. interface_lock is set to true when ControlLocks are set on the KSP interface
        protected bool is_dialog            = false; //set to true in dialog windows.

        public static GUISkin KXskin        = null;  //static variable to hold the reference to the custom skin. First window created will set it up


        //show, hide and toggle - basically just change the value of the bool visible which defines whether or not OnGUI will draw the window.
        //also provides hooks (on_show and on_hide) for decendent classes to trigger actions when showing or hiding.
        public void show(){
            visible = true;
            on_show();
        }
        public void hide(){
            visible = false;
            on_hide();
            StartCoroutine(unlock_delay()); //remove any locks on the editor interface, after a slight delay.
        }
        public void toggle(){
            if(visible){
                hide();	
            } else{
                show();
            }
        }

        //unlock delay just adds a slight delay between an action and unlocking the editor.
        //in cases where a click on the window also results in closing the window (ie a close button) then the click would also get registered by whatever is behind the window
        //adding this short delay prevents that from happening.
        public IEnumerator unlock_delay(){
            yield return true;	//doesn't seem to matter what this returns
            Thread.Sleep(100);
            if(interface_locked){
                InputLockManager.RemoveControlLock(window_id.ToString());
            }
        }

        //overridable methods for gui classes to define actions which are called on hide and show
        protected virtual void on_hide(){}
        protected virtual void on_show(){}
        protected virtual void on_error(){}

        //lock_iu and unlock_ui result in GUI.enabled being set around the call to draw the contents of the window.
        //lets you disable the whole window (it also results in a change to the GUI.color which makes this change without a visual change).
        public void lock_ui(){
            gui_locked = true;
        }
        public void unlock_ui(){
            gui_locked = false;
        }

        //called after successful login IF the login was initiated from a KerbalXWindow with require_login set to true. 
        //Windows which inherit from KerbalXWindow can override this method to have specific actions performed after login
        //(ie: UploadInterface will request a fetch of existing craft).
        protected virtual void on_login(){
            GameObject.Destroy(KerbalX.login_gui);
        }
		
        //As windows will have been drawn with GUILayout.ExpandHeight(true) setting the height to a small value will cause the window to readjust its height.
        //Only call after actions which reduce the height of the content, don't call it constantly OnGUI (unless Epilepsy is something you enjoy)
        public void autoheight(){
            window_pos.height = 5;
        }


        //opens a dialog window which is populated by the lambda statement passed to show_dialog ie:
        //show_dialog((d) => {
        //	GUILayout.Label("hello I'm a dialog");
        //})
        //The dialog instance is returned by show_dialog, and it's also passed into the lambda.
        protected KerbalXDialog show_dialog(DialogContent content){
            KerbalXDialog dialog = gameObject.AddOrGetComponent<KerbalXDialog>();
            dialog.content = content;
            return dialog;
        }

        //close instance of dialog if it exists.
        protected void close_dialog(){
            KerbalXDialog.close();		
        }

        //basically just syntax sugar for a call to AddOrGetComponent for specific named windows. (unfortunatly has nothing to do with launching rockets)
        protected void launch(string type){
            if(type == "ImageSelector"){
                gameObject.AddOrGetComponent<KerbalXImageSelector>();
            } else if(type == "ActionGroupEditor"){
                gameObject.AddOrGetComponent<KerbalXActionGroupInterface>();
            }
        }

        //prevents mouse actions on the GUI window from affecting things behind it.
        protected void prevent_ui_click_through(){
            Vector2 mouse_pos = Input.mousePosition;
            mouse_pos.y = Screen.height - mouse_pos.y;
            if(window_pos.Contains(mouse_pos)){
                if(!interface_locked){
                    InputLockManager.SetControlLock(window_id.ToString());
                    interface_locked = true;
                }
            } else{
                if(interface_locked){
                    InputLockManager.RemoveControlLock(window_id.ToString());
                    interface_locked = false;
                }
            }
        }


        //MonoBehaviour methods

        //called on each frame, handles drawing the window and will assign the next window id if it's not set
        protected void OnGUI(){
            if(window_id == 0){
                window_id = last_window_id + 1;
                last_window_id = last_window_id + 1;
            }

            if(visible){
                GUI.skin = KXskin;
                window_pos = GUILayout.Window(window_id, window_pos, DrawWindow, window_title, GUILayout.Width(window_pos.width), GUILayout.MaxWidth(window_pos.width), GUILayout.ExpandHeight(true));
                GUI.skin = null;
            }
        }

        //Callback method which is passed to GUILayout.Window in OnGUI.  Calls WindowContent and performs common window actions
        private void DrawWindow(int window_id){
            if(prevent_click_through){
                prevent_ui_click_through();
            }

            //if a server error has occured display an error dialog, but don't halt drawing of interface. 
            if(KerbalX.server_error_message != null){
                List<string> messages = KerbalX.server_error_message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
                KerbalX.server_error_message = null;
                string title = messages[0];
                messages[0] = "";
                KerbalXDialog dialog = show_dialog((d) =>{
                    v_section(w =>{
                        GUILayout.Label(title, "alert.h2");
                        foreach(string message in messages){
                            if(message != ""){ GUILayout.Label(message);}
                        }
                        if(GUILayout.Button("OK", height(30))){ close_dialog();}
                    });
                });
                dialog.window_title = title;
                on_error();
            }

            //If unable to connect to KerbalX halt drawing interface and replace with "try again" button"
            if(KerbalX.failed_to_connect){
                GUILayout.Label("Unable to Connect to KerbalX.com!");
                if(GUILayout.Button("try again")){
                    RequestHandler.instance.try_again();
                }
            //If user is not logged in, halt drawing interface and show login button (unless the window is a dialog window)"
            } else if(!is_dialog && require_login && KerbalXAPI.logged_out()){
                GUILayout.Label("You are not logged in.");
                if(GUILayout.Button("Login")){
                    KerbalXLoginInterface login_window = gameObject.AddOrGetComponent<KerbalXLoginInterface>();
                    login_window.after_login_action = () =>{ on_login(); };
                }
            //If an upgrade is required, halt drawing interface and show message;
            } else if(KerbalX.upgrade_required){
                GUILayout.Label("Upgrade Required", "h3");
                GUILayout.Label("This version of the KerbalX mod is no longer compatible with KerbalX.com\nYou need to get the latest version.");
                GUILayout.Label(KerbalX.upgrade_required_message);
                on_error();
            //otherwse all is good, draw the main content of the window as defined by WindowContent
            } else{
                if(gui_locked){
                    GUI.enabled = false;
                    GUI.color = new Color(1, 1, 1, 2); //This enables the GUI to be locked from input, but without changing it's appearance. 
                }
                WindowContent(window_id);	//oh hey, finally, actually drawing the window content. 
                GUI.enabled = true;
                GUI.color = Color.white;
            }

            //add common footer elements for all windows if footer==true
            if(footer){
                FooterContent(window_id);
            }

            //enable draggable window if draggable == true.
            if(draggable){
                GUI.DragWindow();
            }
        }


        //The main method which defines the content of a window.  This method is provided so as to be overridden in inherited classes
        protected virtual void WindowContent(int window_id){

        }

        //Default Footer for all windows, can be overridden only called if footer==true
        protected virtual void FooterContent(int window_id){
            section(w =>{
                if(GUILayout.Button("KerbalX.com", "hyperlink.footer", width(75f), height(30f))){
                    Application.OpenURL(KerbalX.site_url);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(StyleSheet.assets["logo_small"]);
            });
        }

        protected virtual void OnDestroy(){
            InputLockManager.RemoveControlLock(window_id.ToString()); //ensure control locks are released when GUI is destroyed
        }
    }



    //Base class used in KX GUIs.  Provides a set of helper methods for GUILayout calls. these helpers take lambda statements and wraps
    //them in calls to GUILayout methods.
    public class KerbalXWindowExtension : MonoBehaviour
    {
        public Rect window_pos = new Rect((Screen.width / 2 - 500f / 2), 200, 500f, 5);
        protected GUIStyle style_override = null;
        protected GUIStyle section_style = new GUIStyle();

        //anchors are used by the ComboBox. Each anchor is a named reference to a Rect obtained from GetLastRect
        public Dictionary<string, Rect> anchors = new Dictionary<string, Rect>();


        //shorthand for GUILayout.width()
        protected GUILayoutOption width(float w){
            return GUILayout.Width(w);
        }
        //shorthand for GUILayout.height()
        protected GUILayoutOption height(float h){
            return GUILayout.Height(h);
        }


        //Essential for any window which needs to make web requests.  If a window is going to trigger web requests then it needs to call this method on its Start() method
        //The RequestHandler handles sending requests asynchronously (so delays in response time don't lag the interface).  In order to do that it uses Coroutines 
        //which are inherited from MonoBehaviour (and therefore can't be triggered by the static methods in KerbalXAPI).
        protected void enable_request_handler(){
            if(RequestHandler.instance == null){
                KerbalX.log("starting web request handler");
                RequestHandler request_handler = gameObject.AddOrGetComponent<RequestHandler>();
                RequestHandler.instance = request_handler;
            }
        }


        //Definition of delegate to be passed into the section, v_section and scroll methods
        protected delegate void Content(float width);
        protected delegate void ContentNoArgs();


        /* section essentially wraps the actions of a delegate (lambda) in calls to BeginHorizontal and EndHorizontal
		 * Can take an optional width float which if given will be passed to BeginHorizontal as GUILayoutOption params for Width and MaxWidth
		 * Takes a lambda statement as the delegate Content which is called inbetween calls to Begin/End Horizontal
		 * The lambda will be passed a float which is either the width supplied or is the width of the windown (minus padding and margins)
		 * Usage:
			section (400f, w => {
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, 400f in this case
			});	
			OR without defining a width
			section (w => {
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, in this case the window width
			});	
		* In a slightly crazy approach, you can also define a GUIStyle to pass to BeginHorizontal by setting style_override before calling section, ie:
			style_override = new GUIStyle();
			style_override.padding = new RectOffset (20, 20, 10, 10);
			section (w => {
				// GUILayout.Label ("some nonsense", GUILayout.Width (w*0.5f)); //use w to get the inner width of the section, window_pos.width in this case
			});	
		*/
        protected void section(Content content){
            GUILayout.BeginHorizontal(get_section_style()); 
            content(win_width_without_padding());
            GUILayout.EndHorizontal();                  
        } 
        protected void section(float section_width, Content content){
            GUILayout.BeginHorizontal(get_section_style(), GUILayout.Width(section_width), GUILayout.MaxWidth(section_width)); 
            content(section_width);
            GUILayout.EndHorizontal();
        }


        //Works in the just the same way as section() but wraps the lambda in Begin/End Vertical instead.
        protected void v_section(Content content){
            GUILayout.BeginVertical(get_section_style()); 
            content(win_width_without_padding());
            GUILayout.EndVertical();
        }
        protected void v_section(float section_width, Content content){
            GUILayout.BeginVertical(get_section_style(), GUILayout.Width(section_width), GUILayout.MaxWidth(section_width));
            content(section_width);
            GUILayout.EndVertical();
        }
        protected void v_section(float section_width, float section_height, Content content){
            GUILayout.BeginVertical(get_section_style(), GUILayout.Width(section_width), GUILayout.MaxWidth(section_width), GUILayout.Height(section_height));
            content(section_width);
            GUILayout.EndVertical();
        }

        //Very similar to section() and v_section(), but requires a Vector2 to track scroll position and two floats for width and height as well as the content lamnbda
        //Essentially just the same as section() it wraps the call to the lamba in BeginScrollView/EndScrollView calls.
        //The Vector2 is also returned so it can be passed back in in the next pass of OnGUI
        protected Vector2 scroll(Vector2 scroll_pos, float scroll_width, float scroll_height, Content content){
            scroll_pos = GUILayout.BeginScrollView(scroll_pos, get_section_style(), GUILayout.Width(scroll_width), GUILayout.MaxWidth(scroll_width), GUILayout.Height(scroll_height));
            content(scroll_width);
            GUILayout.EndScrollView();
            return scroll_pos;
        }

        //Helper for above section(), v_section() and scroll() methods.  Returns a GUIStyle, either a default GUIStyle() or if section_override has been
        //set then it returns that.  It also sets section_override back to null 
        private GUIStyle get_section_style(){
            GUIStyle style = style_override == null ? section_style : style_override;
            style_override = null;
            return style;
        }
        //Get the window width minus horizontal padding and border (used in above section(), v_section() and scroll() methods when they're not supplied a width)
        private float win_width_without_padding(){
            return window_pos.width - GUI.skin.window.padding.horizontal - GUI.skin.window.border.horizontal;
        }


        protected void begin_group(Rect container, ContentNoArgs content){
            GUI.BeginGroup(container, get_section_style());
            content();
            GUI.EndGroup();
        }


        //Uses the ComboBox class to setup a drop down menu.
        protected void combobox(string combo_name, Dictionary<int, string> select_options, int selected_id, float list_width, float list_height, KerbalXWindow win, ComboResponse resp){
            section(list_width, w =>{
                float h = 22f + select_options.Count * 17;
                if(h > list_height){
                    h = list_height;
                }
                if(GUILayout.Button(select_options[selected_id], GUI.skin.textField, width(w - 20f))){
                    gameObject.AddOrGetComponent<ComboBox>().open(combo_name, select_options, anchors[combo_name], h, win, resp);
                }
                track_rect(combo_name, GUILayoutUtility.GetLastRect());
                if(GUILayout.Button("\\/", width(20f))){
                    gameObject.AddOrGetComponent<ComboBox>().open(combo_name, select_options, anchors[combo_name], h, win, resp);
                }
            });		
        }

        protected void track_rect(string name, Rect rect){
            if(rect.x != 0 && rect.y != 0){
                if(!anchors.ContainsKey(name)){
                    anchors[name] = rect;
                }
            }
        }

    }
}

