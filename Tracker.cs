/// <summary>
/// Gleaner Tracker Unity implementation.
/// </summary>
using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Tracker : MonoBehaviour
{
	public interface ITraceFormatter
	{
		string Serialize (List<string> traces);
	}
	
	public const string start = "start/";
	public const string track = "track/";
	private Net net;
	private ITraceFormatter traceFormatter;
	private bool sending;
	private bool connected;
	private bool connecting;
	private bool flushRequested;
	private List<string> queue = new List<string> ();
	private List<string> sent = new List<string> ();
	private float nextFlush;
	public float flushInterval = -1;
	public string host;
	public string trackingCode;
	public string authorization;
	public string traceFormat = "json";
	public Boolean debug = false;
	private Dictionary<string, string> trackHeaders = new Dictionary<string, string> ();
	private StartListener startListener;
	private FlushListener flushListener;

	public Tracker ()
	{
		this.nextFlush = flushInterval;
		startListener = new StartListener (this);
		flushListener = new FlushListener (this);
		trackHeaders.Add ("Content-Type", "application/json");
		switch (traceFormat) {
		case "json":
			traceFormatter = new SimpleJsonFormat ();
			break;
		default:
			traceFormatter = new DefaultTraceFromat ();
			break;
		}
	}

	private void SetAuthToken (string authToken)
	{
		if (authToken != null) {
			trackHeaders.Add ("Authorization", authToken);
			connected = true;
		}
		connecting = false;
	}

	public void Start ()
	{			
		this.net = new Net (this);
		this.Connect ();
	}

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
			
		if (flushRequested) {
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
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers.Add ("Authorization", authorization);
			if (debug) {
				Debug.Log ("Connecting to " + host);
			}
			net.POST (host + start + trackingCode, null, headers, startListener);
		}
	}

	private void Flush ()
	{
		if (!connected) {
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
			string data = traceFormatter.Serialize (sent);
			if (debug) {
				Debug.Log (data);
			}
			net.POST (host + track, System.Text.Encoding.UTF8.GetBytes (data), trackHeaders, flushListener);
		}
	}

	private void Sent (bool error)
	{
		if (!error) {
			if (debug) {
				Debug.LogError ("Traces received by the server.");
			}
			sent.Clear ();
		} else if (debug) {
			Debug.LogError ("Traces dispatch failed");
		}
		sending = false;
	}

	public class StartListener : Net.IRequestListener
	{

		private Tracker tracker;

		public StartListener (Tracker tracker)
		{
			this.tracker = tracker;
		}

		public void Result (string data)
		{
			if (tracker.debug) {
				Debug.Log ("Start successfull");
			}
			var dict = MiniJSON.Json.Deserialize (data) as Dictionary<string,object>;
			tracker.SetAuthToken ((string)dict ["authToken"]);
		}
		
		public void Error (string error)
		{
			if (tracker.debug) {
				Debug.Log ("Error " + error);
			}
			tracker.SetAuthToken (null);
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
	public void Trace (string trace)
	{
		if (debug) {
			Debug.Log ("'" + trace + "' added to the queue.");
		}
		queue.Add (trace);
	}

	/// <summary>
	/// Adds a trace with the specified values
	/// </summary>
	/// <param name="values">Values of the trace.</param>
	public void Trace (params string[] values)
	{
		string result = "";
		foreach (string value in values) {
			result += value + ",";
		}
		Trace (result);
	}

	/// <summary>
	/// Player entered in a screen.
	/// </summary>
	/// <param name="screenId">Screen identifier.</param>
	public void Screen (string screenId)
	{
		Trace ("screen", screenId);
	}

	/// <summary>
	/// Player entered in a zone inside the game world.
	/// </summary>
	/// <param name="zoneid">Zone identifier.</param>
	public void Zone (string zoneId)
	{
		Trace ("zone", zoneId);
	}

	/// <summary>
	/// Player selected an option in a presented choice
	/// </summary>
	/// <param name="choiceId">Choice identifier.</param>
	/// <param name="optionId">Option identifier.</param>
	public void Choice (string choiceId, string optionId)
	{
		Trace ("choice", choiceId, optionId);
	}

	/// <summary>
	/// A meaningful variable was updated in the game.
	/// </summary>
	/// <param name="varName">Variable name.</param>
	/// <param name="value">New value for the variable.</param>
	public void Var (string varName, System.Object value)
	{
		Trace ("var", varName, value.ToString ());
	}
}


