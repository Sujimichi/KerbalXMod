using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Threading;

using UnityEngine;


namespace KerbalX
{

    public struct PicData{
        public string name;
        public string url;
        public FileInfo file;
        public Texture2D texture;
        public int id;

        public void initialize(string new_name, FileInfo new_file, Texture2D new_texture, int set_id){
            name = new_name;
            file = new_file;
            texture = new_texture;
            id = set_id;
        }
    }

    //The ImageSelector provides a GUI for browing the screenshot directory and selecting pics to upload. It also enables grabbing screenshots of the current view.
    public class KerbalXImageSelector : KerbalXWindow
    {
        private List<PicData> pictures = new List<PicData>();           //populated by load_pics, contains PicData objects for each pic
        private List<List<PicData>> groups = new List<List<PicData>>(); //nested list of lists - rows of pictures for display in the interface
        private bool[] loaded_pics;                                     //array of bools used to track which pictures have had their textures loaded.


        private List<KerbalXWindow> open_windows = new List<KerbalXWindow>();

        private string mode = "pic_selector";
        private int pics_per_row = 4;
        private float pics_scroll_height = 300f;
        private string[] file_types = new string[] { "jpg", "png" };

        private Vector2 scroll_pos;
        private int file_count = 0;

        private string pic_url = "";
        private bool show_content_type_error = false;
        private string hover_ele = "";
        private bool minimized = false;
        private Rect normal_size;

        private void Start(){
            window_title = "KerbalX::ScreenShots";
            float w = 640;
            window_pos = new Rect(275, 95, w, 5);
            normal_size = new Rect(window_pos);
            KerbalX.image_selector = this;
            this.show(); //perform on_show actions when started
        }


        protected override void on_show(){
            change_mode("pic_selector");
            maximize();
            pic_url = "";
            int count = picture_files().Count;
            if(count != file_count){
                prepare_pics();
            }
        }

        protected override void on_hide(){
            KerbalX.upload_gui.clear_errors();
        }

        public static void close(){
            if(KerbalX.image_selector){
                GameObject.Destroy(KerbalX.image_selector);
            }
        }

        protected override void WindowContent(int win_id){

            if(mode == "url_entry"){
                v_section(w =>{
                    section(w2 =>{
                        GUILayout.Label("Enter the URL to your image", "h2", width(w2 - 100f));
                        if(GUILayout.Button("close", width(100f), height(30))){
                            this.hide();
                        }
                    });
                    GUILayout.Label("note: one of 'em urls what end with an extension ie .jpg");

                    section(w2 =>{
                        pic_url = GUILayout.TextField(pic_url, width(w2 - 100f));
                        if(GUILayout.Button("Add url", width(100f))){
                            show_content_type_error = false;
                            HTTP.verify_image(pic_url, (content_type) =>{
                                Debug.Log("resp: " + content_type);
                                if(content_type.StartsWith("image/")){
                                    PicData pic = new PicData();
                                    pic.url = pic_url;
                                    KerbalX.upload_gui.add_picture(pic);
                                    this.hide();
                                } else{
                                    show_content_type_error = true;
                                }
                            });
                        }
                        ;    
                    });

                    if(show_content_type_error){
                        GUILayout.Label("The entered URL does not return the content-type for an image", "alert");
                    }

                    if(GUILayout.Button("or pic a pic from your pics, erm.", height(40f))){
                        change_mode("pic_selector");
                    }
                });

            } else{
                if(!minimized){
                    section(w =>{
                        v_section(w - 100f, w2 =>{
                            GUILayout.Label("Select a picture for your craft", "h2", width(w2));
                            GUILayout.Label("Click on pics below to add them", width(w2));
                        });
                        v_section(100f, w2 =>{
                            if(GUILayout.Button("or enter a url", width(w2), height(30))){
                                change_mode("url_entry");
                            }
                            if(GUILayout.Button("close", width(w2), height(30))){
                                this.hide();
                            }
                        });
                    });
                    
                }

                section(w =>{
                    if(GUILayout.Button("Take Screenshot now", "button.screenshot", width(w - 40f))){
                        grab_screenshot();
                    }
                    if(GUILayout.Button((minimized ? ">>" : "<<"), "button.screenshot.bold", width(40f))){
                        minimized = !minimized;
                        if(minimized){
                            minimize();
                        } else{
                            maximize();
                        }
                    }
                });

                if(!minimized){
                    GUILayout.Label("Grabs a screen shot of the current view (KX windows will hide while taking the pic).", "small");
                }



                //Display picture selector - scrolling container of selectable pictures.
                //picture files will have already been selected and sorted (by prepare_pics()) and then grouped into rows of 4 pics per row (by group_pics())
                //but the files won't have been read yet, so the picture textures haven't been set.  Trying to load all picture textures on load is very time consuming.
                //so instead pictures are loaded and have their texture set row by row, on demand as the user scrolls down.
                if(pictures.Count == 0 && !minimized){
                    GUILayout.Label("You don't have a screen shots in your screen shots folder", "h3");
                    GUILayout.Label("Click Take Screenshot to take one now");
                }
                if(pictures.Count > 0 && !minimized){
                    int n = 0;
                    foreach(bool p in loaded_pics){
                        if(p){n++;}
                    }

                    List<string> files = new List<string>();
                    foreach(PicData selected_pic in KerbalX.upload_gui.pictures){
                        files.Add(selected_pic.file.FullName);
                    }

                    section(w =>{
                        GUILayout.Label("loaded " + n + " of " + pictures.Count.ToString() + " pictures");
                        if(GUILayout.Button("refresh", width(100f))){
                            prepare_pics();
                        }
                    });

                    pics_scroll_height = pictures.Count <= 4 ? 150f : 300f; //adjust image selector height if 4 or less images

                    scroll_pos = scroll(scroll_pos, 620f, pics_scroll_height, w =>{
                        int row_n = 1;
                        foreach(List<PicData> row in groups){

                            //On demand loading of textures.  As each row comes into focus it's picture's textures are loaded
                            //row_n * 150 (the height of each row) defines it's bottom offset. when that value minus the current scroll pos is less than
                            //the threshold (height of scroll container 300, plus 100 so images load before their in full view), then load the pictures on this row.
                            if((row_n * 150) - scroll_pos.y <= 400){    
                                foreach(PicData pic in row){
                                    if(loaded_pics[pic.id] != true){
                                        loaded_pics[pic.id] = true;        //bool array used to track which pictures have been loaded. checking the texture isn't good enough because of the coroutine
                                        StartCoroutine(load_image(pic.file.FullName, pic.texture));  //Use a Coroutine to load the picture texture with as little interface lag as possible.
                                    }
                                }
                            }
                            row_n++;//increment row count

                            //Draw each row, regardless of wheter picture textures have been loaded (will prob add a 'not yet loaded' texture in at some point TODO)
                            style_override = GUI.skin.GetStyle("background.dark");
                            section(600f, sw =>{    //horizontal section....sorry, BeginHorizontal container for the row (slightly narrower than outter container to account for scroll bar)
                                foreach(PicData pic in row){
                                    v_section(150f, w2 =>{  //vertical section, ahem, BeginVertical container for each pic.  Contains two restyled buttons, each will call select_pic.
                                        var style = (hover_ele == pic.file.FullName ? "pic.hover" : "pic.link"); //flip-flop style depending on hover_ele, being == to file name (because I can't figure out how to make style.hover work yet)

                                        if (files.Contains(pic.file.FullName)){
                                            style = (hover_ele == pic.file.FullName ? "pic.selected.highlighted" : "pic.selected");
                                        }

                                        if(GUILayout.Button(pic.texture, style, width(w2), height(w2 * 0.75f))){
                                            toggle_pic(pic);
                                        }
                                        if(GUILayout.Button(pic.name, style, width(w2), height(37.5f))){
                                            toggle_pic(pic);
                                        }
                                    });
                                    if(GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)){ //detect if mouse is over the last vertical section 
                                        hover_ele = pic.file.FullName;                                        //and hover_ele to that pics filename. used to set on hover style
                                    }
                                }
                            });
                        }
                    });
                }
            }
        }

        //Grab a screenshot of the craft (KX windows will be hidden for a 100ms while screenshot is taken).
        private void grab_screenshot(){
            string filename = "screenshot - " + string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now) + ".png";
            KerbalX.log("grabbing screenshot: " + filename);

            //track which windows are currently open in open_windows
            open_windows.Clear();
            if(KerbalX.upload_gui && KerbalX.upload_gui.visible){
                open_windows.Add(KerbalX.upload_gui);
            }
            if(KerbalX.action_group_gui && KerbalX.action_group_gui.visible){
                open_windows.Add(KerbalX.action_group_gui);
            }
            open_windows.Add(this);
            //hide all the open windows
            foreach(KerbalXWindow win in open_windows){
                win.hide();
            }

            maximize();
            Application.CaptureScreenshot(filename);
            StartCoroutine(shutter(filename));        //shutter re-opens the windows. well, it's kinda the exact opposite of what a shutter does, but yeah....whatever
        }

        public IEnumerator shutter(string filename){    
            yield return true;                          //doesn't seem to matter what this returns
            Thread.Sleep(50);                           //delay before re-opening windows
            //Application.CaptureScreenshot seems insistant on plonking the picture in KSP_Data, so this next bit relocates the pic to join it's friends in the screenshot folder
            string origin_path = Paths.joined(KSPUtil.ApplicationRootPath, "KSP_Data", filename);    //location where screenshot is created (as a png)
            string png_path = Paths.joined(KerbalX.screenshot_dir, filename);                        //location where it will be moved to
            if(File.Exists((origin_path))){
                KerbalX.log("moving file: " + origin_path + " to: " + png_path);
                File.Move(origin_path, png_path);
            } else{                                                                                       //TODO find less hacky way of solving which Data folder to look in
                origin_path = Paths.joined(KSPUtil.ApplicationRootPath, "KSP_x64_Data", filename);        //location where screenshot is created (as a png)
                if(File.Exists((origin_path))){
                    KerbalX.log("moving file: " + origin_path + " to: " + png_path);
                    File.Move(origin_path, png_path);
                }
            }

            //re-open the KX windows (after the file has been moved so the ImageSelector will notice it).
            foreach(KerbalXWindow win in open_windows){
                win.show();
            }
        }

        public void minimize(){
            minimized = true;
            window_pos.height = 60f;
            window_pos.width = 250f;
            footer = false;
        }

        public void maximize(){
            minimized = false;
            window_pos.height = normal_size.height;
            window_pos.width = normal_size.width;
            footer = true;
        }

        private void change_mode(string new_mode){
            mode = new_mode;
            show_content_type_error = false;
            autoheight();
        }

        //adds pic to list of selected pics on UploadInterface
        private void toggle_pic(PicData pic){
            List<string> files = new List<string>();
            foreach(PicData p in KerbalX.upload_gui.pictures){
                files.Add(p.file.FullName);
            }

            if (files.Contains(pic.file.FullName)){
                KerbalX.upload_gui.remove_picture(pic);
            }else{
                KerbalX.upload_gui.add_picture(pic);
            }
        }

        //Get file info for all files of defined file_types within the screenshot dir
        private List<FileInfo> picture_files(){
            DirectoryInfo dir = new DirectoryInfo(KerbalX.screenshot_dir);
            List<FileInfo> files = new List<FileInfo>();

            foreach(string file_type in file_types){
                foreach(FileInfo file in dir.GetFiles ("*." + file_type)){
                    files.Add(file);
                }
            }
            return files;
        }

        //sort pic files from picture_files (by date) and for each on initialise a PicData struct which will contain the name FileInfo and a blank texture (to be loaded on demand later)
        private void prepare_pics(){
            List<FileInfo> files = picture_files();

            FileInfo[] sorted_files = files.ToArray();
            Array.Sort(sorted_files, delegate(FileInfo f1, FileInfo f2){
                return f2.CreationTime.CompareTo(f1.CreationTime);
            });
                
            pictures.Clear();
            loaded_pics = new bool[files.Count];
            Texture2D placeholder = (Texture2D)StyleSheet.assets["image_placeholder"];
            scroll_pos.y = 0;
            int i = 0;
            foreach(FileInfo file in sorted_files){
                //add a PicData struct for each picture into pictures (struct defines name, file, texture and id) 
                //id is used as index in loaded_pics, used for tracking which have had textures loaded 
                PicData data = new PicData();
                data.initialize(file.Name, file, Instantiate(placeholder), i);
                pictures.Add(data);
                i++;
            }
            file_count = files.Count;
            group_pics(); //divide pictures into "rows" of x pics_per_row 
        }

        //Does the loading of the picture onto the Texture2D object, returns IEnumerator as this is called in a Coroutine.
        public IEnumerator load_image(string path, Texture2D texture){
            yield return true;                          //doesn't seem to matter what this returns
            byte[] pic_data = File.ReadAllBytes(path);  //read image file
            texture.LoadImage(pic_data);                //wop it all upside the texture.
        }


        //constructs a List of Lists containing PicData.  Divides pictures into 'rows' of x pics_per_row
        private void group_pics(){
            groups.Clear();                             //clear any existing groups
            groups.Add(new List<PicData>());            //add first row to groups
            int i = 0;
            foreach(PicData pic in pictures){
                groups.Last().Add(pic);                 //add picture to the last row
                i++;
                if(i >= pics_per_row){                  //once a row is full (row count == pics_per_row)
                    groups.Add(new List<PicData>());    //then add another row to groups 
                    i = 0;                              //and reset i
                }
            }
        }
    }
}

