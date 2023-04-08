using UnityEngine;

public sealed class DCMRootMotionRedirect : MonoBehaviour
{
    /************************************************************************************************************************/

    [SerializeField] private DCMThirdPersonController _Character;
    public bool isCallOnAnimatorMove = true;



    private void OnAnimatorMove()
    {
        if (_Character != null && isCallOnAnimatorMove)
            _Character.OnAnimatorMove();
    }
    /************************************************************************************************************************/
    
    private void OnFootstep(AnimationEvent animationEvent)
    {
        print("OnFootstep");
        _Character.OnFootstep(animationEvent);
    }

    private void OnLand(AnimationEvent animationEvent)
    {        
        _Character.OnLand(animationEvent);
    }
}