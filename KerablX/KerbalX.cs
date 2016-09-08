using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Experimental.Networking;
using KerablXWindow;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;


namespace KerbalX
{
	public class KerbalX
	{
		public static List<string> log_data = new List<string>();
		public static string notification = "";
		public static bool logged_in = false;

		public static void log (string s){ 
			s = "[KerbalX] " + s;
			log_data.Add (s); 
			Debug.Log (s);
		}
		public static string last_log()
		{
			if(log_data.Count != 0){
				return log_data [log_data.Count - 1];
			}else{
				return "nothing logged yet";
			}
		}
		public static void show_log(){
			foreach (string l in log_data) { Debug.Log (l); }
		}
		public static void notify(string s){
			notification = s;
			log (s);
		}

		public static void load_token(){
			//attempt to read the token file from the root of KSP
			//read token file if present
			KerbalX.notify("Reading token from file");
			KerbalXAPI.authenticate_token ("bob's pyjamas");
		}
	}

	public class KerbalXAPI
	{
		public static string token = null; 
		public static string json = "";

		public static void set_token(string new_token){
			token = new_token;
			//and write token to KerablX.key in KSP root dir
		}

		public static bool authenticate_token(string new_token){
			//make request to site to authenticate token and return true or false
			KerbalX.notify("Authenticating token with KerbalX.com...");
			if(new_token == "bob's pyjamas"){
				token = new_token;
				KerbalX.logged_in = true;
				KerbalX.notify("logged in");
				return true;
			}else{				
				token = null;
				KerbalX.logged_in = false;
				KerbalX.notify("token was invalid");
				return false;
			}
		}

		public static void login(string username, string password){
			//make request to site to authenticate username and password and get token back
			KerbalX.notify("loging into KerbalX.com...");

			//succeed
			set_token("lsjdlkfjslkdjflskjdflksjdfl"); //will be token returned from site
			KerbalX.logged_in = true;
			KerbalX.notify("login succsessful, yay!");
		}

		//define delegate to be used to pass lambda statement as a callback to get, post and request methods.
		public delegate void RequestCallback(string data, int status_code);

		/*Perform simple GET request 
		* Usage:
		*	KerbalXAPI.get ("http://some_website.com/path/to/stuff", (resp, code) => {
		*		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		*	});
		*/
		public static void get(string url, RequestCallback callback){
			request ("GET", url, new NameValueCollection(), callback);
		}

		/*Perform GET request with query 
		* Usage:
		* NameValueCollection query = new NameValueCollection ();
		* query.Add ("username", "foobar");
		*	KerbalXAPI.get ("http://some_website.com/path/to/stuff", query, (resp, code) => {
		*		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		*	});	
		*/
		public static void get(string url, NameValueCollection query, RequestCallback callback){
			request ("GET", url, query, callback);
		}

		/*Perform POST request
		* Usage:
		* NameValueCollection query = new NameValueCollection ();
		* query.Add ("username", "foobar");
		*	KerbalXAPI.post ("http://some_website.com/path/to/stuff", query, (resp, code) => {
		*		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		*	});	
		*/
		public static void post(string url, NameValueCollection query, RequestCallback callback){
			request ("POST", url, query, callback);
		}

		/* Performs HTTP GET and POST requests - takes a method ('GET' or 'POST'), a url, query args and a callback delegate
		* The request is performed in a thread to facilitate asynchronous handling
		* Usage:
		* NameValueCollection query = new NameValueCollection ();
		* query.Add ("username", "foobar");
		*	KerbalXAPI.request ("GET", "http://some_website.com/path/to/stuff", query, (resp, code) => {
		*		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		*	});	
		* OR
		*	KerbalXAPI.request ("POST", "http://some_website.com/path/to/stuff", query, (resp, code) => {
		*		//actions to perform after request completes. code provides the status code int and resp provides the returned string
		*	});	
		*/
		public static void request(string method, string url, NameValueCollection query, RequestCallback callback)
		{
			string response_data = null;
			int status_code = 500;

			var thread = new Thread (() => {
				try{
					using (var client = new System.Net.WebClient()) {
						KerbalX.log("sending request to: " + url);
						if(method == "GET"){
							client.QueryString = query;	
							response_data = client.DownloadString (url);
						}else if(method == "POST"){
							response_data = Encoding.Default.GetString(client.UploadValues (url, query));
						}
						status_code = 200;
					}
				}
				catch(WebException e){
					HttpWebResponse resp = (System.Net.HttpWebResponse)e.Response;
					KerbalX.log ("request failed with " + resp.StatusCode + "-" + (int)resp.StatusCode);
					status_code = (int)resp.StatusCode;
				}
				callback(response_data, status_code); //call the callback method and pass in the response and status code.
			});
			thread.Start ();
		}
	}




	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalXLoginWindow : KerbalXWindow
	{

		private string username = "";
		private string password = "";


		private void Start(){
			window_title = "KerbalX::Login";
			window_pos = new Rect((Screen.width/2 - 310/2),100, 310, 5);
			KerbalX.log ("starting");
			KerbalX.load_token ();
		}

		protected override void WindowContent(int win_id)
		{
			if(KerbalX.logged_in == false){					
				grid (310f, window => {
					GUILayout.Label ("username", GUILayout.Width (60f));
					username = GUILayout.TextField (username, 255, GUILayout.Width (250f));
				});

				grid (310f, window => {
					GUILayout.Label ("password", GUILayout.Width(60f));
					password = GUILayout.TextField (password, 255, GUILayout.Width(250f));
				});
			}

			GUILayout.Label (KerbalX.notification, GUILayout.Width(310f));

			if (KerbalX.logged_in == false) {
				if (GUILayout.Button ("Login")) {
					
					KerbalXAPI.login (username, password);
				}
			}else{
				if (GUILayout.Button ("Log out")) {
					KerbalX.logged_in = false;
					KerbalX.notify ("logged out");
				}				
			}
		}
	}


	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class KXTest : KerbalXWindow
	{
		private void Start()
		{
			window_title = "test window";
		}

		protected override void WindowContent(int win_id)
		{
			grid (300f, window => {
				GUILayout.Label (KerbalX.last_log ());
			});

			if (GUILayout.Button ("print log to console")) {
				KerbalX.show_log ();
			}

			if (GUILayout.Button ("test fetch")) {
				KerbalXAPI.get ("http://localhost:3000/katateochi.json", (resp, code) => {
					KerbalX.notify ("callback start, got data");
					Debug.Log ("code: " + code);
					var data = JSON.Parse (resp);
					KerbalX.notify (data["username"]);
				});
			}

			if (GUILayout.Button ("test api/craft")) {
				KerbalXAPI.get ("http://localhost:3000/api/craft.json", (resp, code) => {
					if(code==200){
						KerbalX.notify (resp);
						var data = JSON.Parse (resp);
						Debug.Log (data["controller"]);
					}
					//KerbalX.notify (data["username"]);
				});
			}
			if (GUILayout.Button ("test api/login")) {
				NameValueCollection queries = new NameValueCollection ();
				queries.Add ("username", "barbar");
				queries.Add ("password", "someshit");
				KerbalXAPI.post ("http://localhost:3000/api/login", queries, (resp, code) => {
					if(code==200){
						KerbalX.notify (resp);
						var data = JSON.Parse (resp);
						Debug.Log (data["controller"]);
					}
					//KerbalX.notify (data["username"]);
				});
			}

		}
	}
		
}
