
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    private StateMachine stateMachine;

    private IdleState idleState;
    private ChaseState chaseState;
    private AttackState attackState;
    private DeadState deadState;
    private HitState hitState;

    [SerializeField] private Transform player;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float detectRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int hp = 100;
    [SerializeField] private float hitDuration = 0.4f;
    [SerializeField] private int[] comboDamages = { 5, 5, 15 };
    [SerializeField] private float attackCooldown = 1f;

    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float DetectRange => detectRange;
    public float AttackRange => attackRange;
    public float HitDuration => hitDuration;
    public Animator Animator => animator;
    public int Hp => hp;
    private float lastAttackTime = -999f; 

    public IdleState IdleState => idleState;
    public ChaseState ChaseState => chaseState;
    public AttackState AttackState => attackState;
    public HitState HitState => hitState;
    public DeadState DeadState => deadState;

    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;

    private Animator animator;

    private void Awake()
    {
        stateMachine = new StateMachine();
        animator = GetComponentInChildren<Animator>();
        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        hitState = new HitState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.ChangeState(idleState);
    }

    private void Update()
    {
        
        stateMachine.Update();
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    public void MoveToPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;
        
    }

    public void LookAtPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void Attack()
    {
        if (GetDistanceToPlayer() > attackRange) return;

        if(player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (stateMachine.CurrentState == deadState)
        {
            return;
        }
        Debug.Log($"{name} 피격! 남은 HP : {hp}");
        hp -= damage;

        if (hp <= 0)
        {
            stateMachine.ChangeState(deadState);
            return;
        }

        stateMachine.ChangeState(hitState);
    }
    public void ComboAttack(int index)
    {
        if (GetDistanceToPlayer() > attackRange)
        {
            return;
        }

        int arrayIndex = index - 1;

        if (arrayIndex < 0 || arrayIndex >= comboDamages.Length)
        {
            Debug.LogWarning($"잘못된 콤보 인덱스: {index}");
            return;
        }

        int damage = comboDamages[arrayIndex];

        if (player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }
    }

    public void FinishAttack()
    {
        attackState.FinishAttack();
    }
    public void ResetAttackCooldown()
    {
        lastAttackTime = Time.time;
    }
}
