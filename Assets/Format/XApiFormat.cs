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
﻿using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;

public class XApiFormat : Tracker.ITraceFormatter
{
	string SCREEN = "screen";
	string ZONE = "zone";
	string CHOICE = "choice";
	string VAR = "var";
	public static string VOCAB_PREFIX = "http://purl.org/xapi/games/";
	public static string VERB_PREFIX = VOCAB_PREFIX + "verbs/";
	public static string EXT_PREFIX = VOCAB_PREFIX + "ext/";
	private List<JSONNode> statements = new List<JSONNode> ();
	private string objectId;
	private JSONNode actor;

	public void StartData (JSONNode data)
	{
		actor = data ["actor"];
		objectId = data ["objectId"].ToString ();
		if (!objectId.EndsWith ("/")) {
			objectId += "/";
		}
	}

	public string Serialize (List<string> traces)
	{
		statements.Clear ();

		foreach (string trace in traces) {
			statements.Add (CreateStatement (trace));
		}

		string result = "[";
		foreach (JSONNode statement in statements) {
			result += statement.ToString () + ",";
		}
		return result.Substring (0, result.Length - 1).Replace ("[\n\t]", "").Replace (": ", ":") + "]";
	}

	private JSONNode CreateStatement (string trace)
	{
		string[] parts = trace.Split (',');
		string timestamp = new System.DateTime (1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds (long.Parse (parts [0])).ToString ("yyyy-MM-ddTHH:mm:ss.fffZ");

		JSONNode statement = JSONNode.Parse ("{\"timestamp\": \"" + timestamp + "\"}");

		if (actor != null) {
			statement.Add ("actor", actor);		      
		}
		statement.Add ("verb", CreateVerb (parts [1]));


		statement.Add ("object", CreateActivity (parts));

		if (parts.Length > 3) {
			JSONNode extensions = JSONNode.Parse ("{}");
			JSONNode extensionsChild = JSONNode.Parse ("{}");
			extensionsChild.Add (EXT_PREFIX + "value", parts [3]);

			extensions.Add ("extensions", extensionsChild);

			statement.Add ("result", extensions);
		}

		return statement;
	}

	private JSONNode CreateVerb (string ev)
	{

		string id;
		if (CHOICE.Equals (ev)) {
			id = "choose";
		} else if (SCREEN.Equals (ev)) {
			id = "viewed";
		} else if (ZONE.Equals (ev)) {
			id = "entered";
		} else if (VAR.Equals (ev)) {
			id = "updated";
		} else {
			id = ev;
		}

		JSONNode verb = JSONNode.Parse ("{ id : }");
		verb ["id"] = VERB_PREFIX + id;

		return verb;
	}

	private JSONNode CreateActivity (string[] parts)
	{
		string ev = parts [1];
		string id;
		if (CHOICE.Equals (ev)) {
			id = "choice";
		} else if (SCREEN.Equals (ev)) {
			id = "screen";
		} else if (ZONE.Equals (ev)) {
			id = "zone";
		} else if (VAR.Equals (ev)) {
			id = "variable";
		} else {
			id = ev;
		}
		return JSONNode.Parse ("{ id : " + objectId + id + "/" + parts [2] + "}");
	}
}


