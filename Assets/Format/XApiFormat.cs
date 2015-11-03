using UnityEngine;
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

    private List<JSONNode> statements = new List<JSONNode>();

    private string objectId;
    private JSONNode actor;

    public void StartData(JSONNode data)
    {
        actor = data["actor"];
        objectId = data["objectId"].ToString();
        if (!objectId.EndsWith("/"))
        {
            objectId += "/";
        }
    }

    public string Serialize(List<string> traces)
    {
        statements.Clear();

        foreach (string trace in traces)
        {
            statements.Add(CreateStatement(trace));
        }

        string result = "[";
        foreach (JSONNode statement in statements)
        {
            result += statement.ToString() + ",";
        }
        return result.Substring(0, result.Length - 1).Replace("[\n\t]", "").Replace(": ", ":") + "]";
    }

    private JSONNode CreateStatement(string trace)
    {
        JSONNode statement = JSONNode.Parse("{}");

        statement.Add("actor", actor);

        string[] parts = trace.Split(',');

        statement.Add("verb", CreateVerb(parts[0]));

        statement.Add("object", CreateActivity(parts));

		if (parts.Length > 2)
		{
			JSONNode extensions = JSONNode.Parse("{}");
			JSONNode extensionsChild = JSONNode.Parse("{}");
			extensionsChild.Add(EXT_PREFIX + "value", parts[2]);

			extensions.Add("extensions", extensionsChild);

			statement.Add("result", extensions);
		}

		return statement;
    }

    private JSONNode CreateVerb(string ev)
    {

        string id;
        if (CHOICE.Equals(ev)) {
            id = "choose";
        } else if (SCREEN.Equals(ev)) {
            id = "viewed";
        } else if (ZONE.Equals(ev)) {
            id = "entered";
        } else if (VAR.Equals(ev)) {
            id = "updated";
        } else {
            id = ev;
        }

        JSONNode verb = JSONNode.Parse("{ id : }");
        verb["id"] = VERB_PREFIX + id;

        return verb;
    }

    private JSONNode CreateActivity(string[] parts)
    {
        string ev = parts[0];
        string id;
        if (CHOICE.Equals(ev)) {
            id = "choice";
        } else if (SCREEN.Equals(ev)) {
            id = "screen";
        } else if (ZONE.Equals(ev)) {
            id = "zone";
        } else if (VAR.Equals(ev)) {
            id = "variable";
        } else {
            id = ev;
        }
        return JSONNode.Parse("{ id : " + objectId + id + "/" + parts[1] + "}");
    }
}


