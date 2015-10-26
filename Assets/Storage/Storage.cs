using System;

public interface Storage
{
	void SetTracker (Tracker tracker);
	
	/// <summary>
	/// The tracker wants to start sending traces
	///</summary>
	void Start (Tracker.StartListener startListener);
	
	///<summary>
	/// The tracker wants to send the given data
	///</summary>
	void Send (String data, Tracker.FlushListener flushListener);

}