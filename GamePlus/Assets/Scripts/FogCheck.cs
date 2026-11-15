

using UnityEngine;

public class FogCheck : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"fog={RenderSettings.fog}  mode={RenderSettings.fogMode}  " +
                  $"start={RenderSettings.fogStartDistance}  end={RenderSettings.fogEndDistance}  " +
                  $"color={RenderSettings.fogColor}");
    }
}