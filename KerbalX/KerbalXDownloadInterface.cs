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
        private string mode = "";
        private int[] craft_ids;
        private Vector2 scroll_pos;
        private float scroll_height;
        private float win_top = 200f;

        private bool only_version_compatible = true;
        private string ksp_ver;

        private string craft_file;

        private void Start(){
            KerbalX.download_gui = this;
            window_title = "KerbalX::Downloader";
            window_pos = new Rect(Screen.width / 2 - 500 / 2, win_top, 500, 5);
            require_login = true;
            visible = true;
            ksp_ver = Versioning.GetVersionString();
            enable_request_handler();
            fetch_download_queue();
        }

        //Called after a succsessful login, if the login dialog was opened from this window.
        protected override void on_login(){
            base.on_login();        //inherits call to hide login window
            fetch_download_queue(); //fetch list of craft to download
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
                    fetch_download_queue();
                }
                if(GUILayout.Button("Past Downloads", "button.bold", width(w * 0.33f))){
                    fetch_past_downloads();
                }
                if(GUILayout.Button("Your Craft", "button.bold", width(w * 0.33f))){
                    fetch_users_craft();
                }
            });

            GUILayout.Label(mode, "h2");

            only_version_compatible = GUILayout.Toggle(only_version_compatible, " Only show KSP " + ksp_ver + " craft");

            if(GUI.changed){
                adjust_scroll_height();
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

        //returns conut of craft taking version filter into account
        private int display_count(){
            int count = 0;
            foreach(int id in craft_ids){
                if(ksp_ver == craft_list[id]["version"] || !only_version_compatible){
                    count++;
                }
            }
            return count;
        }

        //Fetch the current download queue
        private void fetch_download_queue(){
            mode = "Download Queue";
            KerbalXAPI.fetch_download_queue((craft_data) =>{
                craft_list = craft_data;
                after_fetch_actions();
            });
        }

        //Fetch craft the user has downloaded in the past
        private void fetch_past_downloads(){
            mode = "Past Downloads";
            KerbalXAPI.fetch_past_downloads((craft_data) =>{
                craft_list = craft_data;
                after_fetch_actions();
            });
        }

        //Fetch craft uploaded by the user
        private void fetch_users_craft(){
            mode = "Your Craft";
            KerbalXAPI.fetch_users_craft((craft_data) =>{
                craft_list = craft_data;
                after_fetch_actions();
            });
        }

        //called after each fetch, adds additional data to each craft
        private void after_fetch_actions(){
            craft_ids = craft_list.Keys.ToArray();
            foreach(int id in craft_ids){
                craft_list[id].Add("dir", dir_for_craft(id));   //Folder the craft will be downloaded into
                craft_list[id].Add("path", path_for_craft(id)); //full path that the craft will be downloaded to
                craft_list[id].Add("short_path", path_for_craft(id).Replace(Paths.joined(KSPUtil.ApplicationRootPath, "saves"), "")); //path starting at save folder
                craft_list[id].Add("status", "");
                if(File.Exists(craft_list[id]["path"])){
                    craft_list[id]["status"] = "In folder";
                }
            }
            adjust_scroll_height();
            scroll_pos.y = 0;
        }


        private string path_for_craft(int id){
            return Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list[id]["type"], craft_list[id]["name"] + ".craft");
        }

        private string dir_for_craft(int id){
            return Paths.joined(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", craft_list[id]["type"]);
        }



        private void download_craft(int id){
            KerbalXAPI.download_craft(id, (craft_file_string, code) =>{
                if(code == 200){
                    Directory.CreateDirectory(craft_list[id]["dir"]); //ensure directorys exist (usually just subassembly folder which is missing). 
                    craft_file = craft_file_string;
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
                                    write_file(id);
                                    close_dialog();
                                }
                            });
                        });	
                        dialog.window_pos.width = 400f;
                        dialog.window_pos.y = Event.current.mousePosition.y - 42; //42 just seemed like the answer, for some reason.
                        dialog.window_pos.x = (window_pos.x + window_pos.width / 2) - dialog.window_pos.width / 2;
                        dialog.window_title = "Replace Existing?";

                    } else{
                        write_file(id);
                    }
                }
            });
        }

        private void write_file(int craft_id){
            File.WriteAllText(craft_list[craft_id]["path"], craft_file);
            craft_list[craft_id]["status"] = "Downloaded";
        }


    }
}

