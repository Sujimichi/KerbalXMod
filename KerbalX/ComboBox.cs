using System;
using System.Collections.Generic;

using UnityEngine;

namespace KerbalX
{
    public delegate void ComboResponse(int selected);

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

