using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class NetStorage : Storage
{
	public const string start = "start/";
	public const string track = "track/";
	private Net net;
	private string host;
	private string trackingCode;
	private string authorization;
	private NetStartListener netStartListener;
	private Dictionary<string,string> trackHeaders = new Dictionary<string, string> ();
	
	/// <summary>
	/// </summary>
	/// <param name="net">An object to interact with the network.</param>
	/// <param name="host">Host of the collector server.</param>
	/// <param name="trackingCode">Tracking code for the game.</param>
	/// <param name="authorization">Authorization to start the tracking.</param>
	public NetStorage (MonoBehaviour behaviour, string host, string trackingCode,
	                  string authorization)
	{
		this.net = new Net (behaviour);
		this.host = host;
		this.trackingCode = trackingCode;
		this.authorization = authorization;
	}
	
	public void SetTracker (Tracker tracker)
	{
		netStartListener = new NetStartListener (tracker, this);
		trackHeaders.Add ("Content-Type", "application/json");
	}

	public void Start (Tracker.StartListener startListener)
	{
		Dictionary<string,string> headers = new Dictionary<string, string> ();
		headers.Add ("Authorization", authorization);
		net.POST (host + start + trackingCode, null, headers, netStartListener);
	}

	public void Send (String data, Tracker.FlushListener flushListener)
	{
		net.POST (host + track, System.Text.Encoding.UTF8.GetBytes (data), trackHeaders, flushListener);
	}

	private void SetAuthToken (string authToken)
	{
		trackHeaders.Add ("Authorization", authToken);
	}
	
	public class NetStartListener : Tracker.StartListener
	{
		private NetStorage storage;
		
		public NetStartListener (Tracker tracker, NetStorage storage) : base(tracker)
		{
			this.storage = storage;
		}

		protected override void ProcessData (JSONNode dict)
		{
			storage.SetAuthToken ((string)dict ["authToken"]);
			base.ProcessData (dict);
		}
	}
}


