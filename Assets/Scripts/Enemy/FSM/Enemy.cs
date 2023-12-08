using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


/// <summary>
/// 敌人类
/// 实现状态切换，，加载敌人巡逻路线
/// </summary>
public class Enemy : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    private AudioSource audioSource;

    [Tooltip("敌人血量")]public float enemyHealth;
    [Tooltip("敌人血条")] public Slider slider;
    [Tooltip("敌人受到伤害的文字UI")] public Text getDamageText;
    [Tooltip("敌人死亡特效")] public GameObject deadEffect;


    public GameObject[] wayPointObj;//存放敌人不同路线
    public List<Vector3> wayPoints = new List<Vector3>();//存放巡逻路线的每个巡逻点
    public int index; //下标值
    [Tooltip("敌人下标（用来分配随机路线）")]public int nameIndex;
    public int animState;//动画状态标识，0：idle，，1：run，，2：attack
    public Transform targetPoint;//目标位置

    public EnemyBaseState currentState;//存储敌人当前的状态
    public PatrolState patrolState ;//定义敌人巡逻状态，声明对象
    public AttackState attackState ;//定义敌人攻击状态，声明对象
    Vector3 targetPostion;

    //敌人的攻击目标，场景中有敌人（玩家）用列表存储
    public List<Transform> attackList= new List<Transform>();
    [Tooltip("攻击间隔，时间越长攻击频率越慢")] public float attackRate;
    private float nextAttack = 0;//下次攻击时间
    [Tooltip("普通攻击距离")] public float attackRange;
    private bool isDead;//判断是否死亡

    public GameObject attackParticle01;
    public Transform attackParticle01Postion;
    public AudioClip attackSound;

    // Start is called before the first frame update

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        patrolState = transform.gameObject.AddComponent<PatrolState>();
        attackState = transform.gameObject.AddComponent<AttackState>();
    }

    void Start()
    {      
        isDead = false;
        slider.minValue = 0;
        slider.maxValue = enemyHealth;
        slider.value = enemyHealth;
        index = 0;
        //游戏一开始的时候敌人进入巡逻状态
        TransitionToState(patrolState);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;   
        currentState.OnUpdate(this);//敌人移动方法是一直执行的（将本身this传递进去）
        animator.SetInteger("state", animState);
    }

    /// <summary>
    /// 敌人向着导航点移动
    /// </summary>
    public void MoveToTarget()
    {
        if (attackList.Count==0)
        {
            //敌人没有攻击目标，走巡逻点
            targetPostion = Vector3.MoveTowards(transform.position, wayPoints[index], agent.speed * Time.deltaTime);
        }
        else
        {
            //敌人扫描到玩家，向玩家方向走去
            targetPostion=Vector3.MoveTowards(transform.position, attackList[0].transform.position,agent.speed*Time.deltaTime);
        }

        agent.destination = targetPostion;
    }


    /// <summary>
    /// 加载路线
    /// </summary>
    public void LoadPath(GameObject go)
    {
        //加载路线之前清空list
        wayPoints.Clear();
        //遍历路线预制体里所有导航点位置信息，并加到list里
        foreach (Transform T in go.transform)
        {
            wayPoints.Add(T.position);
        }
    }

    /// <summary>
    /// 切换敌人状态
    /// </summary>
    public void TransitionToState(EnemyBaseState state) {
        currentState = state;
        currentState.EnemyState(this);
    }

    /// <summary>
    /// 敌人收到伤害，扣除血量
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void Health(float damage)
    {
        if (isDead) return;
        getDamageText.text = Mathf.Round(damage).ToString();
        enemyHealth -= damage;
        slider.value = enemyHealth;
        if (slider.value <= 0)
        {
            isDead = true;
            animator.SetTrigger("dying");
            slider.gameObject.SetActive(false);
            Destroy(Instantiate(deadEffect, transform.position, Quaternion.identity), 3f);//敌人死亡爆炸特效持续3秒
        }
    }

    /// <summary>
    /// 敌人攻击玩家
    /// 普通攻击
    /// </summary>
    public void AttackAction()
    {
        //当敌人和玩家距离很近的时候，触发攻击动画
        if (Vector3.Distance(transform.position, targetPoint.position) < attackRange)
        {
            if (Time.time>nextAttack)
            {         
                //触发攻击
                animator.SetTrigger("attack");               
                //更新下次攻击时间
                nextAttack = Time.time + attackRate;              
            }
        }
    }

    /// <summary>
    /// attack 动画的
    ///  Animation Event
    /// </summary>
    public void PlayAtackSound() {
        audioSource.clip = attackSound;
        audioSource.Play();
    }






    /// <summary>
    /// Animation Event
    /// </summary>
    public void PlayMustatAttackEff()
    {
        if (gameObject.name == "Mutant")
        {
            GameObject attackPar01 = Instantiate(attackParticle01, attackParticle01Postion.position, attackParticle01Postion.rotation);
            audioSource.clip = attackSound;
            audioSource.Play();
            Destroy(attackPar01, 3f);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        //攻击列表里要剔除子弹，子弹不能添加
        if (!attackList.Contains(other.transform) &&!isDead && !other.CompareTag("Bullect"))
        {
            attackList.Add(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        attackList.Remove(other.transform);
    }

}
