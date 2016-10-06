using System;
using System.Collections.Generic;

using UnityEngine;

namespace KerbalX
{
    public delegate void ComboResponse(int selected);

    //The Combobox...actually it's a filterable dropdown menu, is a GUI helper to add in a textfield with a button that acts as a drop down menu.
    //actually, the textfield is a lie, it's a button too, just styled to look like a textfield, but when the menu content opens it overlays a real 
    //textfield onto of it.  The real text field an be typed into to filter the content of the menu, moving the mouse over the items in the menu 
    //highlight them and populate the text field and clicking on any of the items closes the menu and populates the fake textfield with the value of the item.
    //It requires a bit of extra setup in the GUI where it is to be used; One key aspect is the anchor.  The anchor is a Rect which defines the position 
    //where the menu needs to open at and they are set by calls to GetLastRect, but as multiple comboboxs might exist in the same GUI, the rects returned 
    //have to be tracked (and named) 
    //This needs to be added to a window class in which you want to use a combobox.
    //
    //public Dictionary<string, Rect> anchors = new Dictionary<string, Rect>();
    //
    //protected void combobox(string combo_name, Dictionary<int, string> select_options, int selected_id, float list_width, float list_height, KerbalXWindow win, ComboResponse resp){
    //    section(list_width, w =>{
    //        float h = 22f + select_options.Count * 17;
    //        if(h > list_height){
    //            h = list_height;
    //        }
    //        if(GUILayout.Button(select_options[selected_id], GUI.skin.textField, width(w - 20f))){
    //            gameObject.AddOrGetComponent<ComboBox>().open(combo_name, select_options, anchors[combo_name], h, win, resp);
    //        }
    //        track_rect(combo_name, GUILayoutUtility.GetLastRect());
    //        if(GUILayout.Button("\\/", width(20f))){
    //            gameObject.AddOrGetComponent<ComboBox>().open(combo_name, select_options, anchors[combo_name], h, win, resp);
    //        }
    //    });     
    //}
    //
    //protected void track_rect(string name, Rect rect){
    //    if(rect.x != 0 && rect.y != 0){
    //        if(!anchors.ContainsKey(name)){
    //            anchors[name] = rect;
    //        }
    //    }
    //}
    //
    //Then it can be used like this;
    //
    //combobox("combo_name", list_content, selected_item_id, width, height, parent_window, id =>{
    //  selected_item_id = id;
    //});
    //
    //The 1st string arg is used as the name for the anchor for this combobox, so needs to be uniq.
    //list_content is a Dictionary<int, string> the value component is the content for each menu item, and the key it's id, which is returned once a item is selected
    //The 3rd arg is an int, the id of the currently selected menu item.
    //4th and 5th args are floats to define the width and height of the drop down menu.  
    //6th argument is the parent window which contains the combobox and it needs to response to window_pos with a Rect that defines its position.
    //7th arg is a lambda statement which takes an int arg. This is used to return the selected menu item id back to the scope of the window it is called from.
    //simple right!
    //
    public class ComboBox : KerbalXWindowExtension
    {
        public static ComboBox instance;

        public string active_anchor = null;
        public Rect anchor_rect = new Rect(0, 0, 100, 100);
        public Rect container = new Rect(0, 0, 100, 100);
        public float list_height = 150;
        public KerbalXWindow parent_window;
        public Dictionary<int, string> sel_options = new Dictionary<int, string>();
        public ComboResponse response;
        public Vector2 scroll_pos;
        public int gui_depth = 0;
        public string select_field = "";
        public string filter_string = "";
        int on_hover = 0;

        void Start(){
            instance = this;
        }

        public void open(string combo_name, Dictionary<int, string> select_options, Rect anchor, float height, KerbalXWindow parent_win, ComboResponse selection_callback){
            if(active_anchor != combo_name){
                active_anchor = combo_name;
                sel_options = select_options;
                response = selection_callback;
                anchor_rect = anchor;
                parent_window = parent_win;
                list_height = height;
            }
        }

        public void close(){
            parent_window.unlock_ui();
            active_anchor = null;
            GameObject.Destroy(ComboBox.instance);
        }

        void OnGUI(){
            //set the container to track it's anchors position (which is relative to the parent window), relative to the screen. 
            //ie offset from screen 0,0 of the window + the offset from the window of the anchor. (#when-comments-make-less-sense-than-code)
            container.x = anchor_rect.x + parent_window.window_pos.x;
            container.y = anchor_rect.y + parent_window.window_pos.y;
            container.width = anchor_rect.width + 26f; //the 26 accouts for the 20f wide button + 3f padding either side of it.
            container.height = list_height;

            GUI.skin = KerbalXWindow.KXskin;
            GUI.depth = gui_depth;

            //If the mouse is NOT over the combobox and its list AND the user clicks, the close the combobox.  Otherwise set the parent window to be locked (GUI.enabled = false)
            if(!container.Contains(Event.current.mousePosition) && Input.GetKeyDown(KeyCode.Mouse0) && Input.GetMouseButtonDown(0)){
                close();
            } else{
                parent_window.lock_ui();
            }

            begin_group(container, () =>{
                style_override = GUI.skin.GetStyle("background.dark");
                v_section(container.width, w =>{
                    section(w2 =>{
                        GUI.SetNextControlName("combobox.filter_field");
                        select_field = GUILayout.TextField(select_field, 255, "combobox.filter_field", width(w - 26f));
                        if(GUILayout.Button("\\/", "combobox.bttn", GUILayout.Width(20f))){
                            close();
                        }
                        if(GUI.changed){
                            filter_string = select_field;
                        }
                        GUI.FocusControl("combobox.filter_field");
                    });

                    scroll_pos = scroll(scroll_pos, w, container.height - 22f, sw =>{
                        foreach(KeyValuePair<int, string> option in sel_options){
                            if(filter_string.Trim() == "" || option.Value.ToLower().Contains(filter_string.Trim().ToLower())){
                                if(GUILayout.Button(option.Value, on_hover == option.Key ? "combobox.option.hover" : "combobox.option")){
                                    response(option.Key);
                                    close();
                                }
                                if(GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){
                                    on_hover = option.Key;
                                    if(filter_string.Trim() == ""){
                                        select_field = option.Value;
                                    }
                                }
                            }
                        }
                    });

                });
            });
            GUI.skin = null;
        }
    }
}

