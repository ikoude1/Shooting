using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


/// <summary>
/// ������
/// ʵ��״̬�л��������ص���Ѳ��·��
/// </summary>
public class Enemy : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    private AudioSource audioSource;

    [Tooltip("����Ѫ��")]public float enemyHealth;
    [Tooltip("����Ѫ��")] public Slider slider;
    [Tooltip("�����ܵ��˺�������UI")] public Text getDamageText;
    [Tooltip("����������Ч")] public GameObject deadEffect;


    public GameObject[] wayPointObj;//��ŵ��˲�ͬ·��
    public List<Vector3> wayPoints = new List<Vector3>();//���Ѳ��·�ߵ�ÿ��Ѳ�ߵ�
    public int index; //�±�ֵ
    [Tooltip("�����±꣨�����������·�ߣ�")]public int nameIndex;
    public int animState;//����״̬��ʶ��0��idle����1��run����2��attack
    public Transform targetPoint;//Ŀ��λ��

    public EnemyBaseState currentState;//�洢���˵�ǰ��״̬
    public PatrolState patrolState ;//�������Ѳ��״̬����������
    public AttackState attackState ;//������˹���״̬����������
    Vector3 targetPostion;

    //���˵Ĺ���Ŀ�꣬�������е��ˣ���ң����б�洢
    public List<Transform> attackList= new List<Transform>();
    [Tooltip("���������ʱ��Խ������Ƶ��Խ��")] public float attackRate;
    private float nextAttack = 0;//�´ι���ʱ��
    [Tooltip("��ͨ��������")] public float attackRange;
    private bool isDead;//�ж��Ƿ�����

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
        //��Ϸһ��ʼ��ʱ����˽���Ѳ��״̬
        TransitionToState(patrolState);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;   
        currentState.OnUpdate(this);//�����ƶ�������һֱִ�еģ�������this���ݽ�ȥ��
        animator.SetInteger("state", animState);
    }

    /// <summary>
    /// �������ŵ������ƶ�
    /// </summary>
    public void MoveToTarget()
    {
        if (attackList.Count==0)
        {
            //����û�й���Ŀ�꣬��Ѳ�ߵ�
            targetPostion = Vector3.MoveTowards(transform.position, wayPoints[index], agent.speed * Time.deltaTime);
        }
        else
        {
            //����ɨ�赽��ң�����ҷ�����ȥ
            targetPostion=Vector3.MoveTowards(transform.position, attackList[0].transform.position,agent.speed*Time.deltaTime);
        }

        agent.destination = targetPostion;
    }


    /// <summary>
    /// ����·��
    /// </summary>
    public void LoadPath(GameObject go)
    {
        //����·��֮ǰ���list
        wayPoints.Clear();
        //����·��Ԥ���������е�����λ����Ϣ�����ӵ�list��
        foreach (Transform T in go.transform)
        {
            wayPoints.Add(T.position);
        }
    }

    /// <summary>
    /// �л�����״̬
    /// </summary>
    public void TransitionToState(EnemyBaseState state) {
        currentState = state;
        currentState.EnemyState(this);
    }

    /// <summary>
    /// �����յ��˺����۳�Ѫ��
    /// </summary>
    /// <param name="damage">�˺�ֵ</param>
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
            Destroy(Instantiate(deadEffect, transform.position, Quaternion.identity), 3f);//����������ը��Ч����3��
        }
    }

    /// <summary>
    /// ���˹������
    /// ��ͨ����
    /// </summary>
    public void AttackAction()
    {
        //�����˺���Ҿ���ܽ���ʱ�򣬴�����������
        if (Vector3.Distance(transform.position, targetPoint.position) < attackRange)
        {
            if (Time.time>nextAttack)
            {         
                //��������
                animator.SetTrigger("attack");               
                //�����´ι���ʱ��
                nextAttack = Time.time + attackRate;              
            }
        }
    }

    /// <summary>
    /// attack ������
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
        //�����б���Ҫ�޳��ӵ����ӵ��������
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
