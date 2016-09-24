using System;
using System.Linq;
using System.Text;

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;


namespace KerbalX
{
	//define delegates to be used to pass lambda statement as callbacks in request methods.
	public delegate void RequestCallback(string data, int status_code);
	public delegate void ActionCallback();

	public class KerbalXAPI
	{
		private static string token = null; 
		private static string kx_username = null; //not used for any authentication, just for being friendly!

		public static bool logged_out(){
			return token == null;
		}
		public static bool logged_in(){
			return token != null;
		}
		public static string logged_in_as(){
			return kx_username;
		}

		//Check if Token file exists and if so authenticate it with KerbalX. Otherwise instruct login window to display login fields.
		public static void load_and_authenticate_token(){
			KerbalX.login_gui.enable_login = false;
			try{
				if (File.Exists (KerbalX.token_path)){
					KerbalX.log("Reading token from " + KerbalX.token_path);
					string token = File.ReadAllText(KerbalX.token_path);
					KerbalXAPI.authenticate_token (token);
				}else{
					KerbalX.login_gui.enable_login = true;
				}
			}
			catch{
				KerbalX.login_gui.enable_login = true;
			}
		}

		private static void save_token(string token){
			File.WriteAllText(KerbalX.token_path, token);
		}

		//takes partial url and returns full url to site; ie url_to("some/place") -> "http://whatever_domain_site_url_defines.com/some/place"
		public static string url_to (string path){
			if(!path.StartsWith ("/")){ path = "/" + path;}
			return KerbalX.site_url + path;
		}

		//make request to site to authenticate token 
		public static void authenticate_token(string current_token){
			KerbalX.log("Authenticating with KerbalX.com...");
			NameValueCollection data = new NameValueCollection (){{"token", current_token}};
			HTTP.post (url_to ("api/authenticate"), data).send ((resp, code) => {
				if(code==200){
					var resp_data = JSON.Parse (resp);
					kx_username = resp_data["username"];
					token = current_token;
					KerbalX.login_gui.after_login_action();
				}else{
					KerbalX.login_gui.enable_login = true;
				}
				KerbalX.login_gui.autoheight ();
			});
		}

		//make request to site to authenticate username and password and get token back
		public static void login(string username, string password){
			KerbalX.login_gui.enable_login = false; //disable interface while logging in to prevent multiple login clicks
			KerbalX.login_gui.login_failed = false;
			KerbalX.log("loging into KerbalX.com...");
			NameValueCollection data = new NameValueCollection (){{"username", username}, {"password", password}};
			HTTP.post (url_to ("api/login"), data).send ((resp, code) => {
				if(code==200){
					var resp_data = JSON.Parse (resp);
					token = resp_data["token"];
					save_token (resp_data["token"]);
					kx_username = resp_data["username"];
					KerbalX.login_gui.login_successful = true;
					KerbalX.login_gui.after_login_action();
				}else{
					KerbalX.login_gui.login_failed = true;
				}
				KerbalX.login_gui.enable_login = true;
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void log_out(){
			token = null; 
			kx_username = null;
			KerbalX.login_gui.enable_login = true;
			KerbalX.log ("logged out");
			//TODO delete token file.
		}


		public static void fetch_existing_craft(ActionCallback callback){
			HTTP.get (url_to ("api/existing_craft.json")).set_header ("token", KerbalXAPI.token).send ((resp, code) => {
				if(code==200){
					JSONNode craft_data = JSON.Parse (resp);
					Dictionary<int, Dictionary<string, string>> craft_list = new Dictionary<int, Dictionary<string, string>>();
					for(int i=0; i<craft_data.Count; i++ ){
						var c = craft_data[i];
						int id = int.Parse((string)c["id"]);
						Dictionary<string,string> cd = new Dictionary<string,string>(){
							{"id", c["id"]},
							{"name", c["name"]},
							{"version", c["version"]},
							{"url", c["url"]}
						};
						craft_list.Add (id, cd);
					}
					KerbalX.existing_craft = craft_list;
					callback();
				}
			});
		}

		public static void upload_craft(WWWForm craft_data, RequestCallback callback){
			HTTP http = HTTP.post (url_to ("api/craft"), craft_data);
			http.set_header ("token", KerbalXAPI.token);
			http.request.SetRequestHeader ("Content-Type", "multipart/form-data");
			http.send (callback);
		}
	}


	public class HTTP
	{
		public UnityWebRequest request; 

		public static HTTP get(string url){
			HTTP http = new HTTP ();
			http.request = UnityWebRequest.Get (url);
			return http;
		}

		public static HTTP post(string url, NameValueCollection data){
			WWWForm form_data = new WWWForm();
			foreach(string key in data){
				form_data.AddField (key, data[key]);
			}
			HTTP http = new HTTP ();
			http.request = UnityWebRequest.Post (url, form_data);
			return http;
		}

		public static HTTP post(string url, WWWForm form_data){
			HTTP http = new HTTP ();
			http.request = UnityWebRequest.Post (url, form_data);
			return http;
		}

		public HTTP set_header(string key, string value){
			request.SetRequestHeader (key, value);
			return this;
		}

		public void send(RequestCallback callback){
			if(RequestHandler.instance == null){
				throw new Exception ("[KerbalX] RequestHandler is not ready, unable to make request");
			}else{
				RequestHandler.instance.send_request (request, callback);
			}
		}
	}


	//[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class RequestHandler : MonoBehaviour
	{
		public static RequestHandler instance = null;
		private UnityWebRequest last_request  = null;
		private RequestCallback last_callback = null;

		public void send_request(UnityWebRequest request, RequestCallback callback){
			StartCoroutine (transmit (request, callback));
		}

		public void try_again(){		
			send_request(last_request, last_callback);
		}

		public bool can_retry(){
			return last_request != null;
		}

		public IEnumerator transmit(UnityWebRequest request, RequestCallback callback){

			last_request  = null;
			last_callback = null;
			KerbalX.server_error_message = null;
			KerbalX.failed_to_connect = false;
			KerbalX.log("sending request to: " + request.url);

			yield return request.Send ();

			if (request.isError){															//Request Failed, most likely due to being unable to get a response, therefore no status code
				KerbalX.failed_to_connect = true;
				KerbalX.log ("request failed: " + request.error);

				last_request = new UnityWebRequest (request.url, request.method);					//  \ create a copy of the request which is about to be sent
				if(request.method != "GET"){														//  | if the request fails because of inability to connect to site
					last_request.uploadHandler = new UploadHandlerRaw (request.uploadHandler.data);;// <  then try_again() can be used to fire the copied request
				}																					//  | and the user can carry on from where they were when connection was lost.
				last_request.downloadHandler = request.downloadHandler;								//  | upload and download handlers have to be duplicated too
				last_callback = callback;															// /  and the callback is also stuffed into a var for reuse.
				
			}else{
				int status_code = (int)request.responseCode;								//server responded - get status code
				if(status_code == 500){														//KerbalX server error
					string error_message = "An error has occurred on KerbalX " +			//default error message incase server doesn't come back with something more helpful
						"(it was probably Jebs fault) - Error 500";
					var resp_data = JSON.Parse (request.downloadHandler.text);				//read response message and assuming there is one change the error_message
					if(!(resp_data["error"] == null || resp_data["error"] == "")){
						error_message = "KerbalX server error:\n" + resp_data["error"];
					}

					KerbalX.log ("request returned 500 - Server Error!");
					KerbalX.log (error_message);
					KerbalX.server_error_message = error_message;							//Set the error_message on KerbalX, any open window will pick this up and render error dialog
					callback (request.downloadHandler.text, status_code);					//Still call the callback, assumption is all callbacks will test status code for 200 before proceeding, this allows for further handling if needed
				}else{
					KerbalX.log ("request returned " + status_code); 						//All other status codes - will prob add separate handling of 404 when downloads get added TODO
					callback (request.downloadHandler.text, status_code);					//401s (Unauthorized) will get handled by login/authenticate methods 

				}
				request.Dispose ();
			}
		}
	}

}
//				KerbalX.log ("response headers:");
//				Dictionary<string, string> t = request.GetResponseHeaders ();
//				foreach(KeyValuePair<string, string> r in t){
//					KerbalX.log ("header: " + r.Key + " value " + r.Value);
//				}

