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

		public static bool token_not_loaded(){
			return token == null;
		}
		public static bool token_loaded(){
			return token != null;
		}


		public static void load_and_authenticate_token(){
			KerbalX.notify("Reading token from " + KerbalX.token_path);
			try{
				string token = System.IO.File.ReadAllText(KerbalX.token_path);
				KerbalXAPI.authenticate_token (token);
			}
			catch{
				KerbalX.login_gui.show_login = true;
			}
		}

		private static void save_token(string token){
			System.IO.File.WriteAllText(KerbalX.token_path, token);
		}

		public static void clear_token(){
			token = null; 
			//TODO delete token file.
		}

		public static string temp_view_token(){ //TODO remove this - just a temp method to access the token from other classes.
			return token;
		}

		//make request to site to authenticate token 
		public static void authenticate_token(string new_token){
			KerbalX.notify("Authenticating with KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("token", new_token);
			HTTP.post (KerbalX.url_to ("api/authenticate"), data).send ((resp, code) => {
				if(code==200){
					token = new_token;
					KerbalX.login_gui.show_login = false;
				}else{
					KerbalX.login_gui.show_login = true;
				}
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");
			NameValueCollection data = new NameValueCollection ();
			data.Add ("username", username);
			data.Add ("password", password);
			HTTP.post (KerbalX.url_to ("api/login"), data).send ((resp, code) => {
				if(code==200){
					var resp_data = JSON.Parse (resp);
					token = resp_data["token"];
					save_token (resp_data["token"]);
					KerbalX.login_gui.show_login = false;
					KerbalX.login_gui.login_successful = true;
				}else{
					KerbalX.login_gui.show_login = true;
					KerbalX.login_gui.login_failed = true;
				}
				KerbalX.login_gui.enable_login = true;
				KerbalX.login_gui.autoheight ();
			});
		}

		public static void fetch_existing_craft(ActionCallback callback){
			//NameValueCollection data = new NameValueCollection (){{"lookup", "existing_craft"}};
			HTTP.get (KerbalX.url_to ("api/existing_craft.json")).set_header ("token", KerbalXAPI.token).send ((resp, code) => {
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

		public void send_request(UnityWebRequest request, RequestCallback callback){
			StartCoroutine (transmit (request, callback));
		}

		public IEnumerator transmit(UnityWebRequest request, RequestCallback callback){
			KerbalX.log("sending request to: " + request.url);

			yield return request.Send ();

			if (request.isError){
				KerbalX.log ("request failed: " + request.error);
//				KerbalX.log ("response headers:");
//				Dictionary<string, string> t = request.GetResponseHeaders ();
//				foreach(KeyValuePair<string, string> r in t){
//					KerbalX.log ("header: " + r.Key + " value " + r.Value);
//				}
			}else{
				KerbalX.log ("request successfull"); 
				callback (request.downloadHandler.text, (int)request.responseCode);
			}
		}
	}

}

