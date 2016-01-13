using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class LocalStorage : Storage
{
	private const string Separator = "--session--,";
	private string tracesPathPrefix;
	private string tracesFile;
		
	public LocalStorage (string tracesPathPrefix)
	{
		this.tracesPathPrefix = tracesPathPrefix;
	}
				
	public void SetTracker (Tracker tracker)
	{
	}
		
	public void Start (Net.IRequestListener startListener)
	{
		string now = System.DateTime.Now.ToString ().Replace('/', '_').Replace(':', '_');
		tracesFile = tracesPathPrefix + ".csv";	
		Write("\n" + Separator + now + "\n", startListener);
	}

	public void Send (String data, Net.IRequestListener flushListener)
	{
		Write (data, flushListener);
	}

	private void Write (String data, Net.IRequestListener requestListener)
	{

#if UNITY_WEBGL
		requestListener.Error ("Impossible to use LocalStorage in WebGL version");
#elif UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
		try {
			File.AppendAllText (tracesFile, data);
			requestListener.Result ("");
		} catch (Exception e) {
			requestListener.Error (e.Message);
		}	
#endif
	}

	public bool IsAvailable ()
	{
#if UNITY_WEBGL
     return false;
#else
		return true;
#endif
	}

	public void RemoveBackupFile ()
	{
		if (File.Exists (tracesFile))
		{
			File.Delete (tracesFile);
		}
	}

	public List<string> RecoverData ()
	{
		List<String> tracesList = new List<String>();
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
		string file = tracesFile;
		string data = File.ReadAllText (file);
		String[] traces = data.Split (new Char[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (String s in traces)
		{
			if (!String.IsNullOrEmpty (s) && s.Substring (0, Separator.Length) != Separator)
			{
				tracesList.Add (s);
			}
		}
#endif
		return tracesList;
	}
}


