
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private StateMachine stateMachine;

    private IdleState idleState;
    private ChaseState chaseState;
    private AttackState attackState;
    private DeadState deadState;

    [SerializeField] private Transform player;

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float detectRange = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int hp = 100;

    public Transform Player => player;
    public float MoveSpeed => moveSpeed;
    public float RotaTionSpeed => rotationSpeed;
    public float DetectRange => detectRange;
    public float AttackRange => attackRange;
    public int Hp => hp;

    public IdleState IdleState => idleState;
    public ChaseState ChaseState => chaseState;
    public AttackState AttackState => attackState;
    public DeadState DeadState => deadState;

    private void Awake()
    {
        stateMachine = new StateMachine();

        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);
    }

    private void Start()
    {
        stateMachine.ChangeState(idleState);
    }

    private void Update()
    {
        if(hp <=0)
        {
            stateMachine.ChangeState(deadState);
            return;
        }

        stateMachine.Update();
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    public void MoveToPlayer()
    {

    }

    public void LookAtPlayer()
    {

    }

    public void Attack()
    {

    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
    }
}
