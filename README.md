# Unity tracker
## Installation
1. Clone the repository in your **Assets** folder
1. Add the [Tracker MonoBehaviour](https://github.com/e-ucm/unity-tracker/blob/master/Assets/Tracker.cs) to an empty in your first scene (the component will be kept across scenes)
1. Configure the component parameters
  1. **Flush interval**: time between each flush of the tracker to the server. If this is set to `-1`, it will be necessary to call `T().RequestFlush()` to send traces to the server.
  1. **Storage type**: can be `net`, to send traces to a server, o `local`, to store them locally.
  1. **Trace format**: the format of the traces. Can be `csv`, `json` or `xapi`.
  1. **Host**: If storage type is set to `net`, this should have the host for the analysis server
  1. **Tracking code**: If storage type is set to `net`, this is the [tracking code identifying the game](https://github.com/e-ucm/rage-analytics/wiki/Tracking-code)
  1. **Debug**: Enable to see tracker messages in the Unity console.
1. Send traces

## MonoBehaviour Example

```c#
using UnityEngine;
using System.Collections;

public class TraceGeneratorsScript : MonoBehaviour {

	void Start () {
		Tracker tracker = Tracker.T();
		tracker.Screen ("start");
		tracker.Var ("score", 0);
	}

	void Update () {	
	}
}
```

## Detailed Feature List
1. Configurable flush intervals (via `T().SetFlushInterval()`; use `-1` to entirely avoid auto-flush). If flushing fails, for example due to transient network problems, the tracker will periodically attempt to re-send the data. 
1. Different storage types: 
	1. `net`: sends data to a trace-server, such as the [rage-analytics Backend](https://github.com/e-ucm/rage-analytics-backend). If set, a hostname should be specified via the `host` property.
	2. `local`, to store them locally for later retrieval. Un-sent traces are always persisted locally before being sent through the net, to support intermittent internet access.
1. Different trace formats:
	2. `csv`: allow processing in MS Excel or other spreadsheets. Also supported by many analytics environments.
	3. `json`: especially intended for programmatic analysis, for instance using python or java/javascript or
	4. `xapi`: an upcoming standard for student activity. Note that, if the tracker's storage type is `net` it is required to use the `xapi` trace format since the [rage-analytics Backend](https://github.com/e-ucm/rage-analytics-backend) expects xAPI Statements. The [xAPI tracking model] (https://github.com/e-ucm/xapi-seriousgames) that the backend expects is composed of [Completables](https://github.com/e-ucm/xapi-seriousgames/blob/master/README.md#1341-completable), [Reachables](https://github.com/e-ucm/xapi-seriousgames/blob/master/README.md#1341-reachable), [Variables](https://github.com/e-ucm/xapi-seriousgames/blob/master/README.md#1342-variables) and [Alternatives](https://github.com/e-ucm/xapi-seriousgames/blob/master/README.md#1343-alternatives). 
1. Tracker messages can be displayed in the Unity console by setting the `Debug` property
1. Uses Unity's in-built facilities to handle connections, files and timing.
 
### Tracker and Collector Flow
If the storage type is `net`, the tracker will try to connect to a `Collector` [endpoint](https://github.com/e-ucm/rage-analytics-backend/wiki/Collector), exposed by the [rage-analytics Backend](https://github.com/e-ucm/rage-analytics-backend). 

More information about the tracker can be found in the [official documentation of rage-analytics] (https://github.com/e-ucm/rage-analytics/wiki/Tracker).

