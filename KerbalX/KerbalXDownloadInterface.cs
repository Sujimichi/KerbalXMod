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
    public class KerbalXDownloadController : KerbalXWindowExtension
    {
        public static KerbalXDownloadController instance = null;
//        public int download_queue_count = 0;
        private  Dictionary<int, Dictionary<string, string>> download_queue = new Dictionary<int, Dictionary<string, string>>();

        private Rect container = new Rect(Screen.width/2 - 500/2, -90, 500, 80);
        private int disable_link = 0;
        private long start_time = 0;
        private int startup_delay = 1500;


        private void Start(){
            enable_request_handler();
            start_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            instance = this;
            GameEvents.OnAppFocus.Add(app_focus);
            fetch_download_queue();
        }

        private void OnDestroy(){
            GameEvents.OnAppFocus.Remove(app_focus);
        }

        public void app_focus(bool e){
            KerbalX.log("app focus " + e.ToString());
            fetch_download_queue((KerbalX.download_gui && KerbalX.download_gui.visible && KerbalX.download_gui.mode == "Download Queue"));
        }


        public void fetch_download_queue(){
            fetch_download_queue(false);
        }
        public void fetch_download_queue(bool show_list){
            if(KerbalXAPI.logged_in()){
                KerbalXAPI.fetch_download_queue((craft_data) =>{
                    download_queue.Clear();
                    download_queue = craft_data;
                    if(show_list){
                        show_download_list();
                    }
                });
            }
        }

        public void show_download_list(){
            KerbalX.download_gui.visible = true;
            if(KerbalX.download_gui.visible){
                KerbalX.download_gui.set_list(download_queue, "Download Queue");
            }
        }

        public static void get(string type){
            if(type == "download_queue"){
                KerbalXDownloadController.instance.fetch_download_queue(true);
            }else if(type == "past_downloads"){
                KerbalXDownloadController.instance.fetch_past_downloads();
            }else if(type == "users_craft"){
                KerbalXDownloadController.instance.fetch_users_craft();
            }
        }

        //Fetch craft the user has downloaded in the past
        private void fetch_past_downloads(){
            if(KerbalXAPI.logged_in()){
                KerbalXAPI.fetch_past_downloads((craft_data) =>{
                    KerbalX.download_gui.set_list(craft_data, "Past Downloads");
                });
            }
        }

        //Fetch craft uploaded by the user
        private void fetch_users_craft(){
            if(KerbalXAPI.logged_in()){
                KerbalXAPI.fetch_users_craft((craft_data) =>{
                    KerbalX.download_gui.set_list(craft_data, "Your Craft");
                });
            }
        }


        private long millisecs(){
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private void OnGUI(){
            GUI.skin = KerbalXWindow.KXskin;
            if(KerbalXAPI.logged_in()){
                
                if(millisecs() - start_time > startup_delay){
                    if(download_queue.Count > 0){
                        if(container.y < -10){
                            container.y+=5;
                        }
                        style_override = new GUIStyle(GUI.skin.window);
                        begin_group(container, () =>{
                            v_section(500, w =>{
                                GUILayout.Space(20);
                                GUILayout.Label("You have " + download_queue.Count + " craft to download", "h2.centered");
                                GUILayout.Label("(click to view)", "centered");
                            });
                        });
                        if(disable_link <= 0 && container.Contains(Event.current.mousePosition) && Input.GetKeyDown(KeyCode.Mouse0) && Input.GetMouseButtonDown(0)){
                            show_download_list();
                            disable_link = 5;
                        }
                        disable_link--;
                    } else{
                        if(container.y > -10){
                            container.y-=1;
                        }
                    }
                    
                }
            }
            GUI.skin = null;
        }
    }



    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KerbalXDownloadInterface : KerbalXWindow
    {

        private Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();

        public string mode = "";
        private int[] craft_ids;
        private Vector2 scroll_pos;
        private float scroll_height;
        private float win_top = 200f;

        private bool only_version_compatible = true;
        private string ksp_ver;


        private void Start(){
            KerbalX.download_gui = this;
            window_title = "KerbalX::Downloader";
            window_pos = new Rect(Screen.width / 2 - 500 / 2, win_top, 500, 5);
            require_login = true;
            visible = false;
            ksp_ver = Versioning.GetVersionString();

        }

        //Called after a succsessful login, if the login dialog was opened from this window.
        protected override void on_login(){
            base.on_login();        //inherits call to hide login window
            KerbalXDownloadController.instance.fetch_download_queue(true);
        }

        private long timer = 0;
        private bool readjust_height = false;


        protected override void WindowContent(int win_id){
            if(win_top != window_pos.y){
                timer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                win_top = window_pos.y;
                readjust_height = true;
            }
            if(readjust_height && (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - timer > 500){
                readjust_height = false;
                adjust_scroll_height();
            }

            section(w =>{
                if(GUILayout.Button("Download Queue", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.get("download_queue");
                }
                if(GUILayout.Button("Past Downloads", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.get("past_downloads");
                }
                if(GUILayout.Button("Your Craft", "button.bold", width(w * 0.33f))){
                    KerbalXDownloadController.get("users_craft");
                }
            });

            GUILayout.Label(mode, "h2");

            only_version_compatible = GUILayout.Toggle(only_version_compatible, " Only show KSP " + ksp_ver + " craft");
            if(GUI.changed){
                adjust_scroll_height(); //adjust the scroll height when the above toggle is changed.
            }

            scroll_pos = scroll(scroll_pos, 500f, scroll_height, sw =>{
                if(craft_list.Count > 0){
                    foreach(int id in craft_ids){
                        
                        GUI.enabled = !(mode == "Download Queue" && (craft_list[id]["status"] == "removed" || craft_list[id]["status"] == "Downloaded"));
                        if(ksp_ver == craft_list[id]["version"] || !only_version_compatible){
                            style_override = GUI.skin.GetStyle("background.dark.margin");
                            section(w =>{
                                v_section(w * 0.7f, w2 =>{
                                    GUILayout.Label(craft_list[id]["name"], "h3");
                                    GUILayout.Label(craft_list[id]["type"] + " | made in KSP:" + craft_list[id]["version"]);
                                });
                                v_section(w * 0.3f, w2 =>{
                                    section(w2, w3 => {
                                        if(GUILayout.Button("download", "button.bold")){
                                            download_craft(id);
                                        }
                                        if(mode == "Download Queue"){
                                            if(GUILayout.Button("X", "remove_link", width(10f), height(25f))){
                                                KerbalXAPI.remove_from_queue(id);
                                                KerbalXDownloadController.instance.fetch_download_queue();
                                                craft_list[id]["status"] = "removed";
                                            }
                                        }
                                    });
                                    GUILayout.Label(craft_list[id]["status"], "align.right");
                                });
                            });
                        }
                        GUI.enabled = true;
                    }
                } else{
                    GUILayout.Label("No craft do display for \"" + mode + "\"");
                }
            });
        }


        public void set_list(Dictionary<int, Dictionary<string, string>> craft_data, string mode_name){
            mode = mode_name;
            craft_list = new Dictionary<int, Dictionary<string, string>>();
            foreach(KeyValuePair<int, Dictionary<string, string>> data in craft_data){
                craft_list.Add(data.Key, new Dictionary<string, string>(data.Value));
            }
            craft_ids = craft_list.Keys.ToArray();
            foreach(int id in craft_ids){
                craft_list[id].Add("dir", path_for_craft(id, "dir"));           //Folder the craft will be downloaded into
                craft_list[id].Add("path", path_for_craft(id));                 //full path that the craft will be downloaded to
                craft_list[id].Add("short_path", path_for_craft(id, "short"));  //path from saves folder
                craft_list[id].Add("status", "");
                if(File.Exists(craft_list[id]["path"])){
                    craft_list[id]["status"] = "In folder";
                }
            }
            adjust_scroll_height();
            scroll_pos.y = 0;
        }


        //set scroll height according to number of craft and window position relative to bottom of the screen.
        private void adjust_scroll_height(){
            scroll_height = display_count() * 67;
            float max_height = Screen.height - (window_pos.y + 200);
            if(scroll_height > max_height){
                scroll_height = max_height;
            }
            if(scroll_height < 67){
                scroll_height = 67f;
            }
            autoheight();
        }

        //returns count of craft taking version filter into account
        private int display_count(){
            int count = 0;
            foreach(int id in craft_ids){
                if(ksp_ver == craft_list[id]["version"] || !only_version_compatible){
                    count++;
                }
            }
            return count;
        }




        private string path_for_craft(int id){
            return path_for_craft(id, "");
        }
        private string path_for_craft(int id, string type){
            if(type == "short"){
                return Paths.joined(HighLogic.SaveFolder, "Ships", craft_list[id]["type"], craft_list[id]["name"] + ".craft");
            }
            if(type == "dir"){
                return Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list[id]["type"]);
            }
            return Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list[id]["type"], craft_list[id]["name"] + ".craft");
        }


        private void download_craft(int id){
            download_craft(id, false);
        }
        private void download_craft(int id, bool force_download){

            if(force_download){
                get_craft_file(id);
            }else{
                if(File.Exists(craft_list[id]["path"])){
                    KerbalXDialog dialog = show_dialog(d =>{
                        style_override = GUI.skin.GetStyle("background.dark");
                        v_section(w =>{
                            GUILayout.Label("A craft with this name already exists", "h2");
                            GUILayout.Label("You have a craft at: " + craft_list[id]["short_path"], "small");
                        });
                        section(w =>{
                            GUILayout.FlexibleSpace();
                            if(GUILayout.Button("Cancel", "button.bold", width(w * 0.2f))){
                                close_dialog();
                            }
                            if(GUILayout.Button("Replace", "button.bold", width(w * 0.2f))){
                                get_craft_file(id);
                                close_dialog();
                            }
                        });
                    }); 
                    dialog.window_pos.width = 400f;
                    dialog.window_pos.y = Screen.height - Input.mousePosition.y - 42; //42 just seemed like the answer, for some reason.
                    dialog.window_pos.x = (window_pos.x + window_pos.width / 2) - dialog.window_pos.width / 2;
                    dialog.window_title = "Replace Existing?";
                    
                } else{
                    get_craft_file(id);
                }
            }

        }

        private void get_craft_file(int id){
            KerbalXAPI.download_craft(id, (craft_file_string, code) =>{
                if(code == 200){
                    write_file(id, craft_file_string);
                    KerbalXDownloadController.instance.fetch_download_queue();
                }
            });
        }

        private void write_file(int craft_id, string craft_file){
            Directory.CreateDirectory(craft_list[craft_id]["dir"]); //ensure directorys exist (usually just subassembly folder which is missing). 
            File.WriteAllText(craft_list[craft_id]["path"], craft_file);
            craft_list[craft_id]["status"] = "Downloaded";
        }


    }
}

