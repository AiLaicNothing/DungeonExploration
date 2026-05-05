using UnityEngine;

public class AnimatorBlocker : MonoBehaviour, IAmbushBlocker
{
    [SerializeField] private Animator animator;
    [SerializeField] private string blockTrigger = "Block";
    [SerializeField] private string unblockTrigger = "Unblock";

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void Block()
    {
        if (animator != null) animator.SetTrigger(blockTrigger);
    }

    public void Unblock()
    {
        if (animator != null) animator.SetTrigger(unblockTrigger);
    }
}
