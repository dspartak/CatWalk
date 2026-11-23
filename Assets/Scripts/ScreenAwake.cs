using UnityEngine;

public class ScreenAwake : MonoBehaviour
{
    void Start() => Screen.sleepTimeout = SleepTimeout.NeverSleep;
    void OnDestroy() => Screen.sleepTimeout = SleepTimeout.SystemSetting;
}
