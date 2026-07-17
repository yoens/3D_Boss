
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    public void OnAttackHit()
    {
        Debug.Log("애니메이션 이벤트 호출됨!");
        player.CheckAttackHit();
    }
}
