using UnityEngine;

public class GBAHandsController : MonoBehaviour
{
    [Tooltip("Semua Renderer bagian tangan")]
    public Renderer[] hands;

    public void SetHandsVisible(bool v)
    {
        for (int i = 0; i < hands.Length; i++)
            if (hands[i] != null) hands[i].enabled = v;
    }
}
