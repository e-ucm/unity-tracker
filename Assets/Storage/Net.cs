using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Net
{
	private MonoBehaviour behaviour;

	public Net(MonoBehaviour behaviour){
		this.behaviour = behaviour;
	}
		
	public WWW GET (string url, IRequestListener requestListener)
	{
			
		WWW www = new WWW (url);
		behaviour.StartCoroutine (WaitForRequest (www, requestListener));
		return www;
	}
		
	public WWW POST (string url, byte[] data, Dictionary<string,string> headers, IRequestListener requestListener)
	{
		// Force post
		if (data == null) {
			data = Encoding.UTF8.GetBytes(" ");
		}
		WWW www = new WWW (url, data, headers);
			
		behaviour.StartCoroutine (WaitForRequest (www, requestListener));
		return www;
	}
		
	private IEnumerator WaitForRequest (WWW www, IRequestListener requestListener)
	{
		yield return www;
		// check for errors
		if (www.error == null) {
			requestListener.Result(www.text);
		} else {
			Debug.LogError(www.error);
			requestListener.Error(www.error);
		}
	}

	public interface IRequestListener {

		void Result(string data);

		void Error(string error);
	}
}


