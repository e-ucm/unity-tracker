using System;

public interface Storage
{
	void SetTracker (Tracker tracker);
	
	/// <summary>
	/// The tracker wants to start sending traces
	///</summary>
	void Start (Net.IRequestListener startListener);
	
	///<summary>
	/// The tracker wants to send the given data
	///</summary>
	void Send (String data, Net.IRequestListener flushListener);

	bool IsAvailable();
}