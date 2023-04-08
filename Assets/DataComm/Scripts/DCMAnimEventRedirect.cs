using UnityEngine;

public sealed class DCMAnimEventRedirect : MonoBehaviour
{
    /************************************************************************************************************************/

    [SerializeField] private DCMThirdPersonController _Character;
    
    /************************************************************************************************************************/
    
    private void OnFootstep(AnimationEvent animationEvent)
    {
        _Character.OnFootstep(animationEvent);
    }

    private void OnLand(AnimationEvent animationEvent)
    {        
        _Character.OnLand(animationEvent);
    }
    /************************************************************************************************************************/

}