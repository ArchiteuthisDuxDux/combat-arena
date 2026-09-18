using UnityEngine;

public class FighterAnimationEvents : MonoBehaviour
{
    public bool ShieldTransition;
    public bool ShieldRaised;
    public bool IsShieldBusy;

    public bool SwordActive;
    public bool IsSwordBusy;

    [Header("Combat objects")]
    [SerializeField] private GameObject blade;
    [SerializeField] private Collider bladeCollider;
    [SerializeField] private GameObject shield;

    private void Start()
    {
        blade.tag = "SwordInactive";
        shield.tag = "ShieldInactive";

        ShieldRaised = false;
        IsShieldBusy = false;
        SwordActive = false;
        IsSwordBusy = false;

        if (bladeCollider != null)
            bladeCollider.enabled = false;
    }

    public void Anim_OnShieldRaiseStarted()
    {
        ShieldTransition = true;
        IsShieldBusy = true;
    }

    public void Anim_OnShieldRaised()
    {
        ShieldTransition = false;
        ShieldRaised = true;
        shield.tag = "ShieldActive";
    }

    public void Anim_OnShieldLowerStarted()
    {
        ShieldTransition = true;
        ShieldRaised = false;
        shield.tag = "ShieldInactive";
    }

    public void Anim_OnShieldLowerFinished()
    {
        //Debug.Log("Shield lower finished");
        ShieldTransition = false;
        IsShieldBusy = false;
    }

    //

    public void Anim_OnAttackStarted()
    {
        IsSwordBusy = true;
    }

    public void Anim_OnSwordActive()
    {
        SwordActive = true;
        blade.tag = "SwordActive";
        if (bladeCollider != null)
            bladeCollider.enabled = true;
    }

    public void Anim_OnSwordInactive()
    {
        SwordActive = false;
        blade.tag = "SwordInactive";
        if (bladeCollider != null)
            bladeCollider.enabled = false;
    }

    public void Anim_OnAttackFinished()
    {
        //Debug.Log("Attack animation finished");
        IsSwordBusy = false;
    }
}