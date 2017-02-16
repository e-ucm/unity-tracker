/*
 * Copyright 2016 e-UCM (http://www.e-ucm.es/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0 (link is external)
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
/// <summary>
/// Gleaner Tracker Unity implementation.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System;
using System.IO;
using RAGE.Analytics.Formats;
using RAGE.Analytics.Storages;

namespace RAGE.Analytics
{
	public class Tracker : MonoBehaviour
	{
		public static DateTime START_DATE = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public interface ITraceFormatter
		{
			string Serialize (List<string> traces);

			void StartData (JSONNode data);
		}

		public interface IGameObjectTracker
		{
			void setTracker(Tracker tracker);
		}

		private Storage mainStorage;
		private LocalStorage backupStorage;

		public bool rawCopy;
		private string rawFilePath;

        public bool strictMode = true;

		private ITraceFormatter traceFormatter;
		private bool sending;
		private bool connected;
		private bool connecting;
		private bool flushRequested;
		private bool useMainStorage;
		protected List<string> queue = new List<string> ();
		private List<string> sent = new List<string> ();
		private List<string> allTraces = new List<string>();
		private float nextFlush;
		public float flushInterval = -1;
		[Range(3, int.MaxValue)]
		public float checkInterval = 3;
		private float nextCheck;
		public string storageType = "local";
		public string traceFormat = "csv";
		public string host;
		public string trackingCode;
		public Boolean debug = false;
		private StartListener startListener;
		private FlushListener flushListener;
		private static Tracker tracker;
		private String filePath;
		private StartLocalStorageListener startLocalStorageListener;
		private Dictionary<string, System.Object> extensions = new Dictionary<string, System.Object>();

		public CompletableTracker completable;
		public AlternativeTracker alternative;
		public AccessibleTracker accessible;
		public GameObjectTracker trackedGameObject;

		public static Tracker T
		{
			get { return tracker; }
		}

		public Tracker ()
		{
			flushListener = new FlushListener (this);
			startListener = new StartListener (this);
			startLocalStorageListener = new StartLocalStorageListener(this);
			completable = new CompletableTracker();
			completable.setTracker(this);
			alternative = new AlternativeTracker();
			alternative.setTracker(this);
			accessible = new AccessibleTracker();
			accessible.setTracker(this);
			trackedGameObject = new GameObjectTracker();
			trackedGameObject.setTracker(this);
			tracker = this;
		}

		void Awake ()
		{
			if(tracker == null)
			{
				tracker = this;
			}
		}

		public ITraceFormatter GetTraceFormatter ()
		{
			return this.traceFormatter;
		}

		private void SetMainStorageConnected (bool connected)
		{
			useMainStorage = connected;
			SetConnected (connected || this.connected);
		}

		private void SetConnected (bool connected)
		{
			this.connected = connected;
			connecting = false;
		}
		
        /// <summary>
        /// DONT USE THIS METHOD. UNITY INTERNAL MONOBEHAVIOUR.
        /// </summary>
		public void Start ()
		{
			switch (traceFormat) {
			case "json":
				this.traceFormatter = new SimpleJsonFormat ();
				break;
			case "xapi":
				this.traceFormatter = new XApiFormat ();
				break;
			default:
				this.traceFormatter = new DefaultTraceFromat ();
				break;
			}
			filePath = GeneratePath ();
			rawFilePath = filePath + "Raw.csv";

			switch (storageType) {
			case "net":
				filePath += "Pending";
				mainStorage = new NetStorage (this, host, trackingCode);
				mainStorage.SetTracker (this);
				backupStorage = new LocalStorage (filePath);
				backupStorage.SetTracker (this);
				break;
			default:
				mainStorage = new LocalStorage (filePath);
				mainStorage.SetTracker (this);
				break;
			}
			
			this.startListener.SetTraceFormatter (this.traceFormatter);
			this.Connect ();
			this.nextFlush = flushInterval;

			UnityEngine.Object.DontDestroyOnLoad (this);
		}

        /// <summary>
        /// Generates the path used for storing the traces in.
        /// </summary>
        /// <returns>The path.</returns>
		public string GeneratePath ()
		{
			String path = Application.persistentDataPath;
	#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaObject env = new AndroidJavaObject ("android.os.Environment");
			AndroidJavaObject file = env.CallStatic<AndroidJavaObject> ("getExternalStorageDirectory");
			path = file.Call<String> ("getAbsolutePath");
	#endif
			if (!path.EndsWith ("/")) {
				path += "/";
			}
			path += "traces";
			if (debug) {
				Debug.Log ("Storing traces in " + path);
			}

			return path;
		}
		
        /// <summary>
        /// DONT USE THIS METHOD. UNITY INTERNAL MONOBEHAVIOUR.
        /// </summary>
		public void Update ()
		{
			float delta = Time.deltaTime;
			if (flushInterval >= 0) {
				nextFlush -= delta;
				if (nextFlush <= 0) {
					flushRequested = true;
				}
				while (nextFlush <= 0) {
					nextFlush += flushInterval;
				}
			}

			if (checkInterval >= 0) {
				nextCheck -= delta;
				if (!useMainStorage && !connecting && nextCheck <= 0 && mainStorage.IsAvailable ()) {
					connecting = true;
					if (debug) {
						Debug.Log ("Starting main storage");
					}
					mainStorage.Start (startListener);
				}
				while (nextCheck <= 0) {
					nextCheck += checkInterval;
				}
			}

			if (connected && flushRequested) {
				Flush ();
			}
		}

		/// <summary>
		/// Flush the traces queue in the next update.
		/// </summary>
		public void RequestFlush ()
		{
			flushRequested = true;
		}

		private void Connect ()
		{
			if (!connected && !connecting) {
				connecting = true;
				if (debug) {
					Debug.Log ("Starting local storage ");
				}
				
				if (mainStorage.IsAvailable ()) {
					mainStorage.Start (startListener);
				} 
				if (backupStorage != null) {
					backupStorage.Start (startLocalStorageListener);
				}
				if (rawCopy)
				{
					WriteRawCopy(true);
				}
			}
		}
		
		private void Flush ()
		{
			if (!connected && !connecting) {
				if (debug) {
					Debug.Log ("Not connected. Trying to connect");
				}
				Connect ();
			} else if (queue.Count > 0 && !sending) {
				if (debug) {
					Debug.Log ("Flushing...");
				}
				sending = true;
				sent.AddRange (queue);
				queue.Clear ();
				flushRequested = false;
				string data = "";
				if (useMainStorage == false && backupStorage != null) {
					if (debug) {
						Debug.Log ("Sending traces via aux storage");
					}
					backupStorage.Send (GetRawTraces (), flushListener);
				} else {
					if (debug) {
						Debug.Log ("Sending traces via main storage");
					}
					allTraces.Clear();
					allTraces.AddRange (sent);
					if(backupStorage!=null)
						allTraces.AddRange (backupStorage.RecoverData ());
					data = traceFormatter.Serialize (allTraces);
					mainStorage.Send(data, flushListener);
				}
				if (debug) {
					Debug.Log(data);
				}
			}
		}

		private string GetRawTraces()
		{
			return GetRawTraces("");
		}

		private string GetRawTraces (string separator)
		{
			string data = "";
			foreach (String trace in sent)
			{
				data += trace + ";" + separator;
			}
			return data;
		}

		private void Sent (bool error)
		{
			if (!error) {
				if (debug) {
					Debug.Log ("Traces received by storage.");
				}
				if (rawCopy)
				{
					WriteRawCopy(false);
				}
				sent.Clear ();
				if (useMainStorage && backupStorage != null) {
					backupStorage.CleanFile();
				}
			} else {
				if (debug) {
					Debug.LogError ("Traces dispatch failed");
				}
				if (useMainStorage && backupStorage != null) {
					useMainStorage = false;
					backupStorage.Send (GetRawTraces(), flushListener);
				}
			}
			sending = false;
		}

		private void WriteRawCopy(bool newSession)
		{
			string data = "";
			if (newSession)
			{
				string now = System.DateTime.Now.ToString().Replace('/', '_').Replace(':', '_');
				data = "\n" + "--session--," + now + "\n";
			}
			else
			{
				data = GetRawTraces("\n");
			}

	#if UNITY_WEBGL
			requestListener.Error ("Impossible to use LocalStorage in WebGL version");
	#elif UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
			try
			{
				File.AppendAllText(rawFilePath, data);
			}
			catch (Exception e)
			{
				Debug.Log("Error writting raw copy. Exception: " + e);
			}
	#endif
		}

		public class StartLocalStorageListener : Net.IRequestListener
		{
			protected Tracker tracker;
			private ITraceFormatter traceFormatter;

			public StartLocalStorageListener (Tracker tracker)
			{
				this.tracker = tracker;
			}

			public void Result(string data)
			{
				if (tracker.debug)
				{
					Debug.Log ("Start local storage successfull");
				}
				tracker.SetConnected (true);
			}

			public void Error(string error)
			{
				if (tracker.debug)
				{
					Debug.Log("Error " + error);
				}
				tracker.SetConnected (false);
			}
		}

		public class StartListener : Net.IRequestListener
		{
			protected Tracker tracker;
			private ITraceFormatter traceFormatter;

			public StartListener (Tracker tracker)
			{
				this.tracker = tracker;
			}

			public void SetTraceFormatter (ITraceFormatter traceFormatter)
			{
				this.traceFormatter = traceFormatter;
			}

			public void Result (string data)
			{
				if (tracker.debug) {
					Debug.Log ("Start main storage successfull");
				}
				if (!String.IsNullOrEmpty(data)) {
					try {
						JSONNode dict = JSONNode.Parse (data);
						this.ProcessData (dict);
					} catch (Exception e) {
						Debug.LogError (e);
					}
				}
				tracker.SetMainStorageConnected (true);
			}

			public void Error (string error)
			{
				if (tracker.debug) {
					Debug.Log ("Error " + error);
				}
				tracker.SetMainStorageConnected (false);
			}

			protected virtual void ProcessData (JSONNode data)
			{
				traceFormatter.StartData (data);
			}
		}

		public class FlushListener : Net.IRequestListener
		{

			private Tracker tracker;

			public FlushListener (Tracker tracker)
			{
				this.tracker = tracker;
			}

			public void Result (string data)
			{
				tracker.Sent (false);
			}

			public void Error (string error)
			{
				tracker.Sent (true);
			}
		}

		/* Traces */

		/// <summary>
		/// Adds a trace to the queue.
		/// </summary>
		/// <param name="trace">A comma separated string with the values of the trace</param>
		[Obsolete("Use ActionTrace instead. Never intended to be public. Has to receive a csv with a specific format.")]
		public void Trace (string trace)
		{
            if (strictMode) {
                Debug.LogWarning ("Tracker: Trace() method is Obsolete. Ignoring");
                return;
            }

			EnqueueTrace (trace);
		}

		private void EnqueueTrace(string trace){
			trace = Math.Round (System.DateTime.Now.ToUniversalTime ().Subtract (START_DATE).TotalMilliseconds) + "," + trace;
			if (debug) {
				Debug.Log ("'" + trace + "' added to the queue.");
			}
			if(extensions.Count > 0)
			{
				string extContent = "";
				foreach (KeyValuePair<string, System.Object> e in extensions)
				{
					string key = e.Key.ToString();
					if (key.Equals(""))
					{
						continue;
					}
					string value = e.Value.ToString();
					if (value.Equals(""))
					{
						continue;
					}
					extContent = extContent + ("," + key.Replace(",","/,") + "," + value.Replace(",","/,"));
				}
				trace = trace + extContent;
				extensions.Clear();
			}

			queue.Add (trace);
		}

		/// <summary>
		/// Adds a trace with the specified values
		/// </summary>
		/// <param name="values">Values of the trace.</param>
		[Obsolete("Use ActionTrace instead. Never intended to be public. Has to receive values in specific order.")]
		public void Trace (params string[] values)
		{
            if (strictMode) {
                Debug.LogWarning ("Tracker: Trace() method is Obsolete. Ignoring");
                return;
            }

			EnqueueTrace (values);
		}

		private void EnqueueTrace(params string[] values){
			string result = "";
			foreach (string value in values) {
				result += value.Replace(",","/,") + ",";
			}

			result = result.Substring (0, result.Length - 1);

			EnqueueTrace (result);
		}

		/// <summary>
		/// Adds a trace with verb, target and targeit
		/// </summary>
		/// <param name="values">Values of the trace.</param>
		public void ActionTrace (string verb, string target_type, string target_id)
		{
            if (verb == null) {
                if (strictMode)
                    throw(new TraceException ("Tracker: Trace verb can't be null."));
                else{
                    Debug.LogWarning ("Tracker: Trace verb can't be null, ignoring.");
                    return;
                }
            }

            if (target_type == null) {
                if (strictMode)
                    throw(new TraceException ("Tracker: Target type can't be null."));
                else{
                    Debug.LogWarning ("Tracker: Target type can't be null, ignoring.");
                    return;
                }
            }

            if (target_id == null) {
                if (strictMode)
                    throw(new TraceException ("Tracker: Target ID can't be null."));
                else{
                    Debug.LogWarning ("Tracker: Target ID can't be null, ignoring.");
                    return;
                }
            }

			EnqueueTrace (verb,target_type,target_id);
		}

		public enum Verb
		{
			Initialized,
			Progressed,
			Completed,
			Accessed,
			Skipped,
			Selected,
			Unlocked,
			Interacted,
			Used
		}

		public enum Extension
		{
			/* Special extensions, 
			those extensions are stored reparatedly in xAPI, e.g.:
			result: {
					score: {
						raw: <score_value: float>
					},
					success: <success_value: bool>,
					completion: <completion_value: bool>,
					response: <response_value: string>
					...
			}
			
				
			*/
			Score,
			Success,
			Response,
			Completion,

			/* Common extensions, these extensions are stored 
			in the result.extensions object (in the xAPI format), e.g.:

			result: {
					...
					extensions: {
						.../health: <value>,
						.../position: <value>,
						.../progress: <value>
					}
			}
			*/
			Health,
			Position,
			Progress
		}

        /// <summary>
        /// Sets if the following trace has been a success, including this value to the extensions.
        /// </summary>
        /// <param name="success">If set to <c>true</c> means it has been a success.</param>
		public void setSuccess(bool success)
		{
			setExtension(Extension.Success.ToString().ToLower(), success);
		}

        /// <summary>
        /// Sets the score of the following trace, including it to the extensions.
        /// </summary>
        /// <param name="score">Score, (Recomended between 0 and 1)</param>
		public void setScore(float score)
		{
            if(score < 0 || score > 1)
                Debug.LogWarning("Tracker: Score recommended between 0 and 1 (Current: " + score + ")");
            
            setVar(Extension.Score.ToString().ToLower(), score);
		}

        /// <summary>
        /// Sets the response. If the player chooses between alternatives, the response should be the selected alternative.
        /// </summary>
        /// <param name="response">Response.</param>
		public void setResponse(string response)
		{
			setExtension(Extension.Response.ToString().ToLower(), response);
		}

        /// <summary>
        /// Sets the completion of the following trace extensions. Completion specifies if something has been completed.
        /// </summary>
        /// <param name="completion">If set to <c>true</c> the trace action has been completed.</param>
		public void setCompletion(bool completion)
		{
			setExtension(Extension.Completion.ToString().ToLower(), completion);
		}

        /// <summary>
        /// Sets the progress of the action. 
        /// </summary>
        /// <param name="progress">Progress. (Recomended between 0 and 1)</param>
		public void setProgress(float progress)
		{
            if(progress < 0 || progress > 1)
                Debug.LogWarning("Tracker: Progress recommended between 0 and 1 (Current: " + progress + ")");
            
            setVar(Extension.Progress.ToString().ToLower(), progress);
		}

        /// <summary>
        /// Sets the coords where the trace takes place.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
		public void setPosition(float x, float y, float z)
		{
            if (x == null || y == null || z == null) {
                if (strictMode)
                    throw(new ExtensionException ("Tracker: x, y or z cant be null.", ExtensionException.ExtensionExceptionType.VALUE));
                else{
                    Debug.Log ("Tracker: x, y or z cant be null, ignoring.");
                    return;
                }
            }

			setExtension(Extension.Position.ToString().ToLower(), "{\"x\":" + x + ", \"y\": " + y
					+ ", \"z\": " + z + "}");
		}

        /// <summary>
        /// Sets the health of the player's character when the trace occurs. 
        /// </summary>
        /// <param name="health">Health.</param>
		public void setHealth(float health)
        {
            if (health == null) {
                if (strictMode)
                    throw(new ExtensionException ("Tracker: Health cant be null.", ExtensionException.ExtensionExceptionType.VALUE));
                else{
                    Debug.Log ("Tracker: Health cant be null, ignoring.");
                    return;
                }
            }

			setExtension(Extension.Health.ToString().ToLower(), health);
		}

        /// <summary>
        /// Adds a variable to the extensions.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="value">Value.</param>
		public void setVar(string id, string value)
		{
			setExtension(id, value);
		}

        /// <summary>
        /// Adds a variable to the extensions.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
		public void setVar(string key, float value){
			setExtension (key, value.ToString ("G", System.Globalization.CultureInfo.InvariantCulture));
		}

        /// <summary>
        /// Adds a variable to the extensions.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
		public void setVar(string key, double value){
			setExtension (key, value.ToString ("G", System.Globalization.CultureInfo.InvariantCulture));
		}


        private void setExtension(string key, float value){
            setExtension (key, value.ToString ("G", System.Globalization.CultureInfo.InvariantCulture));
        }

        private void setExtension(string key, double value){
            setExtension (key, value.ToString ("G", System.Globalization.CultureInfo.InvariantCulture));
        }

		private void setExtension(string key, System.Object value)
		{
            if (key == null || key == "") {
                if (strictMode)
                    throw(new ExtensionException ("Tracker: Extension key is null or empty. Ignored extension.", ExtensionException.ExtensionExceptionType.KEY));
                else {
                    Debug.LogWarning ("Tracker: Extension key is null or empty. Ignored extension.");
                    return;
                }
            }
            if (value == null) {
                if (strictMode)
                    throw(new ExtensionException ("Tracker: Extension value is null. Ignored extension.", ExtensionException.ExtensionExceptionType.VALUE));
                else {
                    Debug.LogWarning ("Tracker: Extension value is null. Ignored extension.");
                    return;
                }
            }

			if (extensions.ContainsKey(key))
				extensions[key] = value;
			else
				extensions.Add(key, value);
		}

        public class TraceException : Exception{
            public TraceException(string message) : base(message){
            }
        }

		public class ExtensionException : Exception{
			public enum ExtensionExceptionType { KEY, VALUE };

			public ExtensionExceptionType Type {
				get;
				private set;
			}

			public ExtensionException(string message, ExtensionExceptionType Type) : base(message){
				this.Type = Type;
			}
		}
	}
}