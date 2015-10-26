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
