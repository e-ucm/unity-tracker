using System;
using System.Collections.Generic;
using SimpleJSON;

public class DefaultTraceFromat : Tracker.ITraceFormatter
{
	public string Serialize (List<string> traces)
	{
		string result = "";
		foreach (string trace in traces) {
			result += trace + "\n";
		}
		return result;
	}

    public void StartData(JSONNode data)
    {

    }
}