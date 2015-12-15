using System;
using System.IO;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using System.Collections;

public class SemiStorage : Storage
{
	private string tracesFile;

	public const string start = "start/";
	public const string track = "track/";
	private Net net;
	private string host;
	private string trackingCode;
	private string authorization;
	private SemiConnectedStartListener netStartListener;
	private SemiConnectedFlushListener semiFlushListener;
	private Dictionary<string, string> trackHeaders = new Dictionary<string, string>();
	private MonoBehaviour behaviour;
	private Boolean hostLoaded = false;

	public SemiStorage(MonoBehaviour behaviour,  string tracesPathPrefix, string host, string trackingCode)
	{
		this.net = new Net(behaviour);
		this.host = host;
		this.trackingCode = trackingCode;
		this.authorization = "a:";
		this.behaviour = behaviour;
		tracesFile = tracesPathPrefix + "pending.txt";

		string filePath = Application.dataPath + "/Assets/track.txt";

		if (Application.platform != RuntimePlatform.WebGLPlayer)
		{
			filePath = "file:///" + filePath;
		}

		WWW www = new WWW(filePath);

		behaviour.StartCoroutine(WaitForRequest(www));
	}

	// Read host and TrackingCode
	private IEnumerator WaitForRequest(WWW www)
	{
		yield return www;
		// check for errors
		if (www.error == null)
		{
			string line = www.text;
			string[] readLine = line.Split(';');
			if (readLine.Length == 2)
			{
				host = readLine[0];
				trackingCode = readLine[1];
			}
		}
		hostLoaded = true;
	}

	public void SetTracker(Tracker tracker)
	{
		netStartListener = new SemiConnectedStartListener(tracker, this);
		semiFlushListener = new SemiConnectedFlushListener(this);
        netStartListener.SetTraceFormatter(tracker.GetTraceFormatter());
		trackHeaders.Add("Content-Type", "application/json");
	}

	public void Start(Net.IRequestListener startListener)
	{
		if (hostLoaded)
		{
			Write('\n' + "new session" + '\n' + host + '\n' + trackingCode + '\n', startListener);
		}
		else
		{
			Write("", new DummyStartListener(startListener));
		}
	}

	private void Write(List<String> sent, Net.IRequestListener requestListener)
	{
		if (!File.Exists(tracesFile))
		{
			File.AppendAllText(tracesFile, '\n' + "new session" + '\n' + host + '\n' + trackingCode + '\n');
		}
		string data = "";
		foreach (string s in sent)
		{
			data += s + ';';
		}
		Write(data.Substring(0, data.Length - 2), requestListener);
	}

	private void Write(string data, Net.IRequestListener requestListener)
	{
		try
		{
			File.AppendAllText(tracesFile, data);
			requestListener.Result("");
		}
		catch (Exception e)
		{
			requestListener.Error(e.Message);
		}
	}

	public void Send(List<string> sent, Tracker.ITraceFormatter traceFormatter, Net.IRequestListener flushListener)
	{
		semiFlushListener.prepareErrorOption(flushListener, sent);
		string filePath = tracesFile;

		if (Application.platform != RuntimePlatform.WebGLPlayer)
		{
			filePath = "file:///" + filePath;
		}

		WWW www = new WWW(filePath);

		behaviour.StartCoroutine(WaitForRequest(www, sent, traceFormatter, flushListener));
	}

	private IEnumerator WaitForRequest(WWW www, List<string> sent, Tracker.ITraceFormatter traceFormatter, Net.IRequestListener flushListener)
	{
		yield return www;
		if (www.error == null)
		{
			string dataString = www.text;
			String[] sessions = dataString.Split(new string[] { "\nnew session\n" }, StringSplitOptions.RemoveEmptyEntries);
			
			foreach( string str in sessions)
			{
				string[] sTraces = str.Split('\n');
				
				string h = sTraces[0];
				string t = sTraces[1];

				List<string> all = new List<String>();
				if (sTraces.Length > 2)
				{
					foreach (String s in sTraces[2].Split(';'))
					{
						if(s!="")
							all.Add(s);
					}
				}
				all.AddRange(sent);
				Dictionary<string, string> headers = new Dictionary<string, string>();
				headers.Add("Authorization", authorization);
				net.POST(h + start + t, null, headers, new SendDataCallback(net, traceFormatter, h+track, all, trackHeaders, netStartListener, semiFlushListener));
				File.Delete(tracesFile);
			} 
		} else if (sent.Count > 0) {
			net.POST(host + track , System.Text.Encoding.UTF8.GetBytes(traceFormatter.Serialize(sent)), trackHeaders, semiFlushListener);
		}        
		
	}

	private void SetAuthToken(string authToken)
	{
		trackHeaders.Add("Authorization", authToken);
	}

	public class SemiConnectedStartListener : Tracker.StartListener
	{
		private SemiStorage storage;

		public SemiConnectedStartListener(Tracker tracker, SemiStorage storage) : base(tracker)
		{
			this.storage = storage;
		}

		protected override void ProcessData(JSONNode dict)
		{
			storage.SetAuthToken(dict["authToken"]);
			base.ProcessData(dict);
		}
	}

	public class SendDataCallback : Net.IRequestListener
	{
		private Net.IRequestListener startListener;
		private List<string> traces;
		private string url;
		private Dictionary<String, String> trackHeaders;
		private Net.IRequestListener semiFlushListener;
		Tracker.ITraceFormatter traceFormatter;
        private Net net;

		public SendDataCallback(Net net, Tracker.ITraceFormatter traceFormatter, string url, List<string> traces, 
			Dictionary<String, String> trackHeaders, Net.IRequestListener startListener, Net.IRequestListener flushListener)
		{
            this.traces = traces;
			this.url = url;
			this.trackHeaders = trackHeaders;
			this.semiFlushListener = flushListener;
			this.startListener = startListener;
			this.traceFormatter = traceFormatter;
            this.net = net;
		}

		public void Result(string result)
		{
			startListener.Result(result);
			if (traces.Count>0)
			{ 
				net.POST(url, System.Text.Encoding.UTF8.GetBytes(traceFormatter.Serialize(traces)), trackHeaders, semiFlushListener);
			}
		}

		public void Error(string error)
		{
			Debug.Log("Error on Start Data" + error);
		}
	}

	public class DummyStartListener : Net.IRequestListener
	{
		private Net.IRequestListener startListener;

		public DummyStartListener(Net.IRequestListener startListener)
		{
			this.startListener = startListener;
		}

		public void Result(string data)
		{
			startListener.Error("Waiting host");
		}

		public void Error(string error)
		{
			startListener.Error("Waiting host");
		}
	}

	public class SemiConnectedFlushListener : Net.IRequestListener
	{
		private Net.IRequestListener flushListener;
		private SemiStorage storage;
		private List<string> sent;

		public SemiConnectedFlushListener(SemiStorage storage)
		{
			this.storage = storage;
		}

		public void prepareErrorOption(Net.IRequestListener flushListener, List<string> sent)
		{
			this.flushListener = flushListener;
			this.sent = sent;
		}
		public void Result(string data)
		{
			flushListener.Result(data);
        }

		public void Error(string error)
		{
			storage.Write(sent, flushListener);
		}
	}

}

