using System;
using System.IO;

public class LocalStorage : Storage
{
		
	private string tracesFile;
		
	public LocalStorage (string tracesFile)
	{
		this.tracesFile = tracesFile;
	}
				
	public void SetTracker (Tracker tracker)
	{
	}
		
	public void Start (Tracker.StartListener startListener)
	{
		try {
			File.AppendAllText (tracesFile, "--new session\n");
			startListener.Result ("");
		} catch (Exception e) {
			startListener.Error (e.Message);
		}
	}

	public void Send (String data, Tracker.FlushListener flushListener)
	{
		try {
			File.AppendAllText (tracesFile, data);
			flushListener.Result ("");
		} catch (Exception e) {
			flushListener.Error (e.Message);
		}
	}

}


