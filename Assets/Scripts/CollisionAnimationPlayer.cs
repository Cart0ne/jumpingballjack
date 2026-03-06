using UnityEngine;

public class CollisionAnimationPlayer : MonoBehaviour
{
    private Animator animator;

    [SerializeField] public GameObject visualWithAnimator;

    void Start()
    {
        if (visualWithAnimator != null)
            animator = visualWithAnimator.GetComponent<Animator>();
        else
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("CollisionAnimationPlayer: visualWithAnimator non assegnato!");
#endif
        }
    }

    public void PlayLandAnimation()
    {
        if (animator == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("CollisionAnimationPlayer: animator è NULL!");
#endif
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

#endif
        // Se hai un Trigger nel tuo Animator, meglio usarlo:
        // animator.SetTrigger("land");
        // Altrimenti:
        animator.Play("land", 0, 0f);
    }
}

