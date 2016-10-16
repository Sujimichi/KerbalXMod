using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;


namespace KerbalX
{
    //define delegates to be used as callbacks in request methods.
    internal delegate void RequestCallback(string data,int status_code);
    internal delegate void ImageUrlCheck(string content_type);
    internal delegate void ActionCallback();
    internal delegate void CraftListCallback(Dictionary<int, Dictionary<string, string>> craft_data);

    //The KerbalXAPI class handles all interaction with KerbalX.com and is responsible for holding the authentication token
    //The class depends on there being an instance of the RequestHandler class present (which handles the actual send/receive process and error handling).
    internal class KerbalXAPI
    {
        private static string token = null;
        private static string kx_username = null; //not used for any authentication, just for being friendly!

        internal static bool logged_out(){
            return token == null;
        }

        internal static bool logged_in(){
            return token != null;
        }

        internal static string logged_in_as(){
            return kx_username;
        }

        internal static void save_token(string token){
            File.WriteAllText(KerbalX.token_path, token);
        }
        
        //takes partial url and returns full url to site; ie url_to("some/place") -> "http://whatever_domain_site_url_defines.com/some/place"
        internal static string url_to(string path){
            if(!path.StartsWith("/")){
                path = "/" + path;
            }
            return KerbalX.site_url + path;
        }
        
        //Check if Token file exists and if so authenticate it with KerbalX. Otherwise instruct login window to display login fields.
        internal static void load_and_authenticate_token(){
            KerbalX.login_gui.enable_login = false;
            try{
                if(File.Exists(KerbalX.token_path)){
                    KerbalX.log("Reading token from " + KerbalX.token_path);
                    string token = File.ReadAllText(KerbalX.token_path);
                    KerbalXAPI.authenticate_token(token);
                } else{
                    KerbalX.login_gui.enable_login = true;
                }
            } catch{
                KerbalX.login_gui.enable_login = true;
            }
        }

        //Authentication POST requests

        //make request to site to authenticate token. If token authentication fails, no error message is shown, it just sets the login window to show u-name/password fields.
        internal static void authenticate_token(string current_token){
            KerbalX.log("Authenticating with KerbalX.com...");
            NameValueCollection data = new NameValueCollection() { { "token", current_token } };
            RequestHandler.show_401_message = false; //don't show standard 401 error dialog
            HTTP.post(url_to("api/authenticate"), data).send((resp, code) =>{
                if(code == 200){
                    var resp_data = JSON.Parse(resp);
                    kx_username = resp_data["username"];
                    token = current_token;
                    KerbalX.login_gui.after_login_action();
                    KerbalX.login_gui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                } else{
                    KerbalX.login_gui.enable_login = true;
                }
                KerbalX.login_gui.enable_login = true;
                KerbalX.login_gui.autoheight();
            });
        }

        //make request to site to authenticate username and password and get token back
        internal static void login(string username, string password){
            KerbalX.login_gui.enable_login = false; //disable interface while logging in to prevent multiple login clicks
            KerbalX.login_gui.login_failed = false;
            KerbalX.log("loging into KerbalX.com...");
            NameValueCollection data = new NameValueCollection() { { "username", username }, { "password", password } };
            RequestHandler.show_401_message = false; //don't show standard 401 error dialog
            HTTP.post(url_to("api/login"), data).send((resp, code) =>{
                if(code == 200){
                    var resp_data = JSON.Parse(resp);
                    token = resp_data["token"];
                    save_token(resp_data["token"]);
                    kx_username = resp_data["username"];
                    KerbalX.login_gui.login_successful = true;
                    KerbalX.login_gui.after_login_action();
                    KerbalX.login_gui.show_upgrade_available_message(resp_data["update_available"]); //triggers display of update available message if the passed string is not empty
                } else{
                    KerbalX.login_gui.login_failed = true;
                    KerbalX.login_gui.enable_login = true;
                }
                KerbalX.login_gui.enable_login = true;
                KerbalX.login_gui.autoheight();
            });
        }

        //nukes the authentication token and user variables and sets the login gui to enable login again.
        internal static void log_out(){
            File.Delete(KerbalX.token_path);
            token = null; 
            kx_username = null;
            KerbalX.login_gui.enable_login = true;
            KerbalX.login_gui.login_successful = false;
            KerbalX.log("logged out");
        }




        //Settings POST requests

        //Tells KerbalX not to bug this user about the current minor/patch version update available
        //There is no callback for this request.
        internal static void dismiss_current_update_notification(){
            HTTP http = HTTP.get(url_to("api/dismiss_update_notification")).set_header("token", KerbalXAPI.token);
            http.request.method = "POST";
            http.send((resp, code) => { });
        }



        //Craft GET requests

        //Fetches data on the users current craft on the site.  This is kept in a Dictionary of craft_id => Dict of key value pairs....here let me explain it in Ruby;
        //{craft_id => {:id => craft.id, :name => craft.name, :version => craft.ksp_version, :url => craft.unique_url}, ...}
        internal static void fetch_existing_craft(ActionCallback callback){
            HTTP.get(url_to("api/existing_craft.json")).set_header("token", KerbalXAPI.token).send((resp, code) =>{
                if(code == 200){
                    KerbalX.existing_craft = process_craft_data(resp, "id", "name", "version", "url");
                    callback();
                }
            });
        }

        //Get the craft the user has tagged for download
        internal static void fetch_download_queue(CraftListCallback callback){
            fetch_craft_list("api/download_queue.json", callback);
        }

        //Get the craft the user has previously downloaded
        internal static void fetch_past_downloads(CraftListCallback callback){
            fetch_craft_list("api/past_downloads.json", callback);
        }

        //Get the craft the user has uploaded (really rather similar to fetch_existing_craft, just slightly different info, will try to unify these two at some point)
        internal static void fetch_users_craft(CraftListCallback callback){
            fetch_craft_list("api/user_craft.json", callback);
        }

        //Remove a craft from the list of craft the user has tagged for download
        internal static void remove_from_queue(int craft_id){
            HTTP.get(url_to("api/remove_from_queue/" + craft_id)).set_header("token", KerbalXAPI.token).send((resp, code) =>{
                KerbalXDownloadController.instance.fetch_download_queue();
            });
        }

        //Does exactly what is says on the tin, it fetches a craft by ID from KerbalX.
        //Just to note though, the ID must be for a craft that is either in the users download queue, has been downloaded before or is one of the users craft
        internal static void download_craft(int id, RequestCallback callback){
            HTTP.get(url_to("api/craft/" + id)).set_header("token", KerbalXAPI.token).send(callback);
        }

        //handles fetching a list of craft from KerbalX, processes the response for certain craft attributes and
        //assembles a Dictionary which is passed into the callback.
        private static void fetch_craft_list(string path, CraftListCallback callback){
            HTTP.get(url_to(path)).set_header("token", KerbalXAPI.token).send((resp, code) =>{
                if(code == 200){
                    callback(process_craft_data(resp, "id", "name", "version", "type"));
                }
            });
        }

        //Takes craft list JSON data from the site and converts it into a nested Dictionary of craft.id => { various craft attrs }
        //the attrs it reads out of the JSON from the site is determined by the strings passed in after the JSON.
        private static Dictionary<int, Dictionary<string, string>> process_craft_data(string craft_data_json, params string[] attrs){
            JSONNode craft_data = JSON.Parse(craft_data_json);
            Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();
            for(int i = 0; i < craft_data.Count; i++){
                var c = craft_data[i];
                int id = int.Parse((string)c["id"]);
                Dictionary<string,string> cd = new Dictionary<string,string>();
                foreach(string attr in attrs){
                    cd.Add(attr, c[attr]);                            
                }
                craft_list.Add(id, cd);
            }
            return craft_list;
        }



        //Craft POST and PUT requests

        //Send new craft to Mun....or KerbalX.com as a POST request
        internal static void upload_craft(WWWForm craft_data, RequestCallback callback){
            HTTP http = HTTP.post(url_to("api/craft"), craft_data);
            http.set_header("token", KerbalXAPI.token);
            http.set_header("Content-Type", "multipart/form-data");
            http.send(callback);
        }

        //Update existing craft on KerbalX as a PUT request with the KerbalX database ID of the craft to be updated
        internal static void update_craft(int id, WWWForm craft_data, RequestCallback callback){
            HTTP http = HTTP.post(url_to("api/craft/" + id), craft_data);
            http.request.method = "PUT"; //because unity's PUT method doesn't take a form, so we create a POST with the form and then change the verb.
            http.set_header("token", KerbalXAPI.token);
            http.set_header("Content-Type", "multipart/form-data");
            http.send(callback);
        }
    }




    //The HTTP class is basically a wrapper around UnityWebRequest and enables chaining calls ie:
    //HTTP.get("http://some_url.com").send((resp, code) =>{ } );
    //OR
    //HTTP.get("http://some_url.com").set_header("header key", "header value").send((resp, code) =>{ } );
    //OR for POST requests; 
    //HTTP.get("http://some_url.com", form_data).set_header("header key", "header value").send((resp, code) =>{ } );
    //form data can either be a WWWForm or a NameValueCollection
    //When calling send it can take a lambda as show above, or a RequestCallback delegate. Into which will be passed the response body string and the status code
    //send hands off to the RequestHandler to handle the actual send/receive process as a Coroutine and deal with error codes
    //The only slightly special method is verify_image which takes an ImageUrlCheck delegate instead of the RequestCallback.
    internal class HTTP
    {
        internal UnityWebRequest request;


        internal static HTTP get(string url){
            HTTP http = new HTTP();
            http.request = UnityWebRequest.Get(url);
            return http;
        }

        internal static HTTP post(string url, NameValueCollection data){
            WWWForm form_data = new WWWForm();
            foreach(string key in data){
                form_data.AddField(key, data[key]);
            }
            HTTP http = new HTTP();
            http.request = UnityWebRequest.Post(url, form_data);
            return http;
        }

        internal static HTTP post(string url, WWWForm form_data){
            HTTP http = new HTTP();
            http.request = UnityWebRequest.Post(url, form_data);
            return http;
        }

        //This differs from the other HTTP static methods in that is doesn't return anything and only fetches the HEADER info from the url
        //It also uses a different method in the RequestHandler which doesn't deal with status codes and only returns the Content-Type into the callback.
        //This is the one route which will make calls to other sites, but only to urls entered by the user for images
        internal static void verify_image(string url, ImageUrlCheck callback){
            HTTP http = new HTTP();
            http.request = UnityWebRequest.Get(url);
            http.request.method = "HEAD";
            http.send(callback);
        }


        internal HTTP set_header(string key, string value){
            if(key == "token" && String.IsNullOrEmpty(value)){
                throw new Exception("[KerbalX] Unable to make request - User not logged in");
            }
            request.SetRequestHeader(key, value);
            return this;
        }

        internal void send(RequestCallback callback){
            set_header("MODCLIENT", "KerbalXMod");
            set_header("MODCLIENTVERSION", KerbalX.version);
            if(RequestHandler.instance == null){
                throw new Exception("[KerbalX] RequestHandler is not ready, unable to make request");
            } else{
                RequestHandler.instance.send_request(request, callback);
            }
        }

        //override for send when using ImageUrlCheck callback
        internal void send(ImageUrlCheck callback){
            RequestHandler.instance.send_request(request, callback);
        }
    }




    //The RequestHandler is used to handel sending and receiving requests.  It does this inside a Coroutine so delays in response 
    //don't lag the interface.  As such it has to inherit MonoBehaviour and needs to be an instance (can't be used as static).
    //It provides a send_request method which take a UnityWebRequest object and a Callback.  In most cases the callback is a RequestCallback
    //except for the special case using a ImageUrlCheck callback.  
    //When using the RequestCallback (as all interaction with KerbalX does) the RequestHandler will perform different actions based on 
    //the status code returned by the request.
    internal class RequestHandler : MonoBehaviour
    {
        internal static RequestHandler instance = null;
        private static NameValueCollection status_codes = new NameValueCollection(){ 
            { "200", "OK" }, { "401", "Unauthorized" }, { "404", "Not Found" }, { "500", "Server Error!" } 
        };


        internal static bool show_401_message = true;

        private UnityWebRequest last_request = null;
        private RequestCallback last_callback = null;

        internal void try_again(){        
            send_request(last_request, last_callback);
        }

        internal bool can_retry(){
            return last_request != null;
        }


        //Used to fetch Content-Type Header info for urls entered by user for an image (to check if image is an image)
        internal void send_request(UnityWebRequest request, ImageUrlCheck callback){
            StartCoroutine(transmit(request, callback));
        }
        //Used in request to url entered by user for image, returns just the content type header info
        private IEnumerator transmit(UnityWebRequest request, ImageUrlCheck callback){
            KerbalX.log("sending request to: " + request.url);
            yield return request.Send();
            callback(request.GetResponseHeaders()["Content-Type"]);
        }
        

        //Used in all requests to KerablX
        internal void send_request(UnityWebRequest request, RequestCallback callback){
            StartCoroutine(transmit(request, callback));
        }

        //Used in all interacton with KerbalX, called from a Coroutine and handles the response error codes from the site
        private IEnumerator transmit(UnityWebRequest request, RequestCallback callback){

            last_request = null;
            last_callback = null;
            KerbalX.server_error_message = null;
            KerbalX.failed_to_connect = false;
            KerbalX.upgrade_required = false;

            KerbalX.log("sending request to: " + request.url);
            yield return request.Send();


            if(request.isError){                                                            //Request Failed, most likely due to being unable to get a response, therefore no status code
                KerbalX.failed_to_connect = true;
                KerbalX.log("request failed: " + request.error);
                
                last_request = new UnityWebRequest(request.url, request.method);                    //  \ create a copy of the request which is about to be sent
                if(request.method != "GET"){                                                        //  | if the request fails because of inability to connect to site
                    last_request.uploadHandler = new UploadHandlerRaw(request.uploadHandler.data);  // <  then try_again() can be used to fire the copied request
                }                                                                                   //  | and the user can carry on from where they were when connection was lost.
                last_request.downloadHandler = request.downloadHandler;                             //  | upload and download handlers have to be duplicated too
                last_callback = callback;                                                           // /  and the callback is also stuffed into a var for reuse.
                
            } else{
                int status_code = (int)request.responseCode;                                //server responded - get status code
                KerbalX.log("request returned " + status_code + " " + status_codes[status_code.ToString()]);                         
                
                if(status_code == 500){                                                     //KerbalX server error
                    string error_message = "KerbalX server error!!\n" +                     //default error message incase server doesn't come back with something more helpful
                            "An error has occurred on KerbalX (it was probably Jebs fault)";
                    var resp_data = JSON.Parse(request.downloadHandler.text);               //read response message and assuming there is one change the error_message
                    if(!(resp_data["error"] == null || resp_data["error"] == "")){
                        error_message = "KerbalX server error!!\n" + resp_data["error"];
                    }
                    KerbalX.log(error_message);
                    KerbalX.server_error_message = error_message;                           //Set the error_message on KerbalX, any open window will pick this up and render error dialog
                    callback(request.downloadHandler.text, status_code);                    //Still call the callback, assumption is all callbacks will test status code for 200 before proceeding, this allows for further handling if needed
                    
                } else if(status_code == 426){                                              //426 - Upgrade Required, only for a major version change that makes past versions incompatible with the site's API
                    KerbalX.upgrade_required = true;
                    var resp_data = JSON.Parse(request.downloadHandler.text);    
                    KerbalX.upgrade_required_message = resp_data["upgrade_message"];
                    
                } else if(status_code == 401){                                              //401s (Unauthorized) - response to the user's token not being recognized.
                    if(RequestHandler.show_401_message == true){                            //In the case of login/authenticate, the 401 message is not shown (handled by login dialog)
                        KerbalX.server_error_message = "Authorization Failed\nKerbalX did not recognize your authorization token, perhaps you were logged out.";
                        KerbalXAPI.log_out();
                    } else{
                        callback(request.downloadHandler.text, status_code);
                    }

                } else if(status_code == 200 || status_code == 400 || status_code == 422){  //Error codes returned for OK and failed validations which are handled by the requesting method
                    callback(request.downloadHandler.text, status_code);                    
                    
                } else{                                                                     //Unhandled error codes - All other error codes. 
                    KerbalX.server_error_message = "Unknown Error!!\n" + request.downloadHandler.text;
                    callback(request.downloadHandler.text, status_code);
                }
                request.Dispose();
                RequestHandler.show_401_message = true;
            }
        }
    }
}


