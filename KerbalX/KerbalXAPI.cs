using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using UnityEngine.Networking;

using System.Threading;

using SimpleJSON;


namespace KerbalX
{
	//define delegates to be used to pass lambda statement as callbacks in request methods.
	public delegate void RequestCallback(string data, int status_code);
	public delegate void ActionCallback();

	public class KerbalXAPI
	{
		private static string token = null; 

		public static bool logged_out(){
			return token == null;
		}
		public static bool logged_in(){
			return token != null;
		}


		public static void load_and_authenticate_token(){
			KerbalX.notify("Reading token from " + KerbalX.token_path);
			KerbalX.login_gui.enable_login = false;
			try{
				string token = System.IO.File.ReadAllText(KerbalX.token_path);
				KerbalXAPI.authenticate_token (token);
			}
			catch{
				KerbalX.login_gui.enable_login = true;
			}
		}

		private static void save_token(string token){
			System.IO.File.WriteAllText(KerbalX.token_path, token);
		}


		public static string temp_view_token(){ //TODO remove this - just a temp method to access the token from other classes.
			return token;
		}

		//takes partial url and returns full url to site; ie url_to("some/place") -> "http://whatever_domain_site_url_defines.com/some/place"
		public static string url_to (string path){
			if(!path.StartsWith ("/")){ path = "/" + path;}
			return KerbalX.site_url + path;
		}

		//make request to site to authenticate token 
		public static void authenticate_token(string new_token){
			KerbalX.notify("Authenticating with KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("token", new_token);
			HTTP.post (url_to ("api/authenticate"), data).send ((resp, code) => {
				if(code==200){
					token = new_token;
				}
				KerbalX.login_gui.autoheight ();
			});
		}

		//make request to site to authenticate username and password and get token back
		public static void login(string username, string password){
			KerbalX.login_gui.enable_login = false; //disable interface while logging in to prevent multiple login clicks
			KerbalX.login_gui.login_failed = false;
			KerbalX.notify("loging into KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("username", username);
			data.Add ("password", password);
			HTTP.post (url_to ("api/login"), data).send ((resp, code) => {
				if(code==200){
					var resp_data = JSON.Parse (resp);
					token = resp_data["token"];
					save_token (resp_data["token"]);
					KerbalX.login_gui.login_successful = true;
				}else{
					KerbalX.login_gui.login_failed = true;
				}
				KerbalX.login_gui.enable_login = true;
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void log_out(){
			token = null; 
			KerbalX.login_gui.enable_login = true;
			KerbalX.log ("logged out");
			//TODO delete token file.
		}


		public static void fetch_existing_craft(ActionCallback callback){
			//NameValueCollection data = new NameValueCollection (){{"lookup", "existing_craft"}};
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
				}else{
					KerbalX.alert = "An error occurred while contacting KerbalX, try again later";
				}
			});
		}
	}


	public class HTTP
	{
		private UnityWebRequest request; 

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
		private UnityWebRequest last_request;
		private RequestCallback last_callback;

		public void send_request(UnityWebRequest request, RequestCallback callback){
			StartCoroutine (transmit (request, callback));
		}

		public void try_again(){			
			send_request(last_request, last_callback);
		}

		public IEnumerator transmit(UnityWebRequest request, RequestCallback callback){
			last_request = new UnityWebRequest (request.url, request.method);					//create a copy of the request which is about to be sent
			if(request.method != "GET"){														//if the request fails because of inability to connect to site
				last_request.uploadHandler = new UploadHandlerRaw (request.uploadHandler.data);;//then try_again() can be used to fire the copied request
			}																					//and the user can carry on from where they were when connection was lost.
			last_request.downloadHandler = request.downloadHandler;								//upload and download handlers have to be duplicated too
			last_callback = callback;															//and the callback is also stuffed into a var for reuse.

			KerbalX.alert = "";
			KerbalX.failed_to_connect = false;
			KerbalX.log("sending request to: " + request.url);

			yield return request.Send ();

			if (request.isError){
				KerbalX.failed_to_connect = true;
				KerbalX.log ("request failed: " + request.error);
				KerbalX.alert = "request failed: " + request.error;
			}else{
				int status_code = (int)request.responseCode;
				if(status_code == 500){
					KerbalX.log ("request returned 500 - Server Error!");	
				}else{
					KerbalX.log ("request returned " + status_code); 
					callback (request.downloadHandler.text, status_code);
				}
				try{
					request.Dispose ();
					last_request.Dispose ();
					last_callback = null;
				}
				catch{
					//no need to catch anything here. If it can dispose of the request objects then great, if not then they've already been disposed (I think).
				}
			}
		}
	}

}
//				KerbalX.log ("response headers:");
//				Dictionary<string, string> t = request.GetResponseHeaders ();
//				foreach(KeyValuePair<string, string> r in t){
//					KerbalX.log ("header: " + r.Key + " value " + r.Value);
//				}

