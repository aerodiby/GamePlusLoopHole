using UnityEngine;

public class HandsVisibilityState : StateMachineBehaviour
{
    [Tooltip("Tangan kelihatan saat state ini aktif?")]
    public bool visible = true;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo info, int layerIndex)
    {
        var c = animator.GetComponent<GBAHandsController>();
        if (c != null) c.SetHandsVisible(visible);
    }
}
