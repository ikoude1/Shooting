using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 角色控制器
/// </summary>
public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    public Vector3 moveDirction;//设置人物移动方向
    private AudioSource audioSource;

    [Header("玩家数值")]
    public float Speed;
    [Tooltip("行走速度")]public float walkSpeed;
    [Tooltip("奔跑速度")] public float runSpeed;
    [Tooltip("下蹲行走速度")] public float crouchSpeed;
    [Tooltip("玩家生命值")]public float playerHealth;

    [Tooltip("跳跃的力")]public float jumpForce;
    [Tooltip("下落的力")] public float fallForce;
    [Tooltip("下蹲时候的玩家高度")]public float crouchHeight;
    [Tooltip("正常站立时的玩家高度")] public float standHeight;

    [Header("键位设置")]
    [Tooltip("奔跑按键")]public KeyCode runInputName = KeyCode.LeftShift;
    [Tooltip("跳跃按键")] public KeyCode jumpInputName = KeyCode.Space;
    [Tooltip("下蹲按键")] public KeyCode crouchInputName = KeyCode.LeftControl;


    [Header("玩家属性判断")]
    public MovementState state;
    private CollisionFlags collisonFlags;
    public bool isWalk;//判断玩家是否行走
    public bool isRun;//判断玩家是否在奔跑
    public bool isJump;//判断玩家是否跳跃
    public bool isGround;//判断玩家是否在地面上
    public bool isCanCrouch;//判断玩家是否可以下蹲
    public bool isCrouching;//判断玩家是否在下蹲
    public bool playerisDead;//判断玩家是否死亡
    private bool isDamage;//判断玩家是否收到伤害

    public LayerMask crouchLayerMask;
    public Text playerHealthUI;
    public Image hurtImage;//玩家血雾
    //Color.red;
    private Color flashColor = new Color(1f,0f,0f,1f);
    private Color clearColor = Color.clear;


    [Header("音效")]
    [Tooltip("行走音效")]public AudioClip walkingSound;
    [Tooltip("奔跑音效")]public AudioClip runnningSound;

    private Inventory inventory;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        inventory=GetComponentInChildren<Inventory>();
        walkSpeed = 4f;
        runSpeed = 6f;
        crouchSpeed = 2f;
        jumpForce = 0f;
        fallForce = 10f;
        crouchHeight = 1f;
        playerHealth = 100f;
        standHeight = characterController.height;
        audioSource.clip = walkingSound;
        audioSource.loop = true;//音效循环打开
        playerHealthUI.text = "HP：" + playerHealth;

    }

    // Update is called once per frame
    void Update()
    {
        /*玩家受到伤害后，屏幕产生红色渐变*/
        if (isDamage)
        {
            hurtImage.color = flashColor;
        }
        else
        {
            hurtImage.color = Color.Lerp(hurtImage.color, clearColor, Time.deltaTime * 5);
        }
        isDamage = false;
        if (playerisDead)
        {
            audioSource.Pause();
            //玩家死亡，停止全部移动行为
            return;
        }

        CanCrouch();
        if (Input.GetKey(crouchInputName))
        {
            Crouch(true);
        }
        else
        {
            Crouch(false);
        }

        Jump();
        PlayerFootSoundSet();
        Moveing();
    }
    /// <summary>
    /// 移动
    /// </summary>
    public void Moveing()
    {
        /*这里如果想让移动急停的话，，就用GetAxisRaw，
         不需要急停可以用GetAxis*/
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        //人物在地面上，并且按下奔跑键,非蹲下键，此时奔跑
        isRun=Input.GetKey(runInputName);
        isWalk = (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0) ? true : false;
        if (isRun && isGround && isCanCrouch && !isCrouching)
        {
            state = MovementState.running;
            Speed = runSpeed;
        }
        else if (isGround)  //正常行走
        {
            state = MovementState.walking;
            Speed = walkSpeed;
            if (isCrouching )//下蹲行走
            {
                state = MovementState.crouching;
                Speed = crouchSpeed;
            }
        }
        //同时按下下蹲和奔跑键的时候，，人物还是下蹲速度
        if (isRun && isCrouching)
        {
            state = MovementState.crouching;
            Speed = crouchSpeed;
        }
           
        

        //设置人物移动方向（将移动速度进行规范化，，防止斜向走速度变大）
        moveDirction = (transform.right * h + transform.forward * v).normalized;
        characterController.Move(moveDirction * Speed * Time.deltaTime);//人物移动
    }

    /// <summary>
    /// 跳跃
    /// </summary>
    public void Jump() {

        if (!isCanCrouch) return;
        isJump = Input.GetKeyDown(jumpInputName);
        //判断玩家按下跳跃键，并且此时在地面上，才能进行跳跃
        if (isJump && isGround)
        {
            isGround = false;
            jumpForce = 5f;//设置跳跃力度
        }
        //20231011添加，如果当前没有按下空格并且在检测地面上，那么isGround判断为false，jumpForce给一个负数的值        
        else if (!isJump && isGround)
        {
            isGround = false;
           // jumpForce = -2f;
        }

        //此时按下跳跃键跳起来了，不在地面时
        if (!isGround)
        {
            jumpForce = jumpForce - fallForce * Time.deltaTime;//每秒将跳跃力进行累减，视其进行下落
            Vector3 jump = new Vector3(0, jumpForce * Time.deltaTime, 0);//将跳跃力设置成V3坐标
            collisonFlags = characterController.Move(jump);//调用角色控制器的移动方法，，只不过这里向上移动形成跳跃
            //  Debug.Log("collisonFlags:" + collisonFlags) ;
            // Debug.Log("characterController.isGrounded:" + characterController.isGrounded);

            /*判断玩家在地面上
           CollisionFlags:characterController 内置的碰撞位置标识号
           CollisionFlags.Below-->在地面上
           */
            if (collisonFlags == CollisionFlags.Below)
            {
                isGround = true;
                jumpForce = -2f;
            }

            ///*这里算是补充判断，如果当前人物什么都被碰到就表示不在地面上*/
            //if (isGround && collisonFlags == CollisionFlags.None)
            //{
            //    isGround = false;
            //}
        }
      
    

    }

    /// <summary>
    /// 判断人物是否可以下蹲
    /// isCanCrouch==true  -->说明人物可以下蹲，此时人物在站立
    /// isCanCrouch==false  -->说明人物不可以下蹲，此时人物在下蹲头顶有碰撞
    /// </summary>
    public void CanCrouch()
    {
        Vector3 sphereLocation = transform.position + Vector3.up * standHeight; //获取人物头顶的高度V3位置    
        //根据头顶上是否有物体，来判断是否可以进行下蹲
        isCanCrouch = (Physics.OverlapSphere(sphereLocation, characterController.radius, crouchLayerMask).Length) == 0;

       
    }



    /// <summary>
    /// 下蹲
    /// </summary>
    public void Crouch(bool newCrouching)
    {
        if (!isCanCrouch) return;//不可下蹲（在隧道）状态时，不能进行起立，
        isCrouching = newCrouching;
        characterController.height = isCrouching ? crouchHeight : standHeight;//根据下蹲状态设置下蹲时候高度和站立的高度
        characterController.center = characterController.height / 2.0f * Vector3.up;//将角色控制器的中心位置Y，从头顶往下减少1半的高度
    }

    /// <summary>
    /// 人物移动音效
    /// </summary>
    public void PlayerFootSoundSet() {
        
        //在地面并且移动的方向向量化大于0，说明人物在移动
        if (isGround && moveDirction.sqrMagnitude>0)
        {
            audioSource.clip = isRun ? runnningSound : walkingSound;
            if (!audioSource.isPlaying)
            {
                //播放行走或者奔跑音效
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {               
                audioSource.Pause();
            }
        }
        //下蹲时不播放行走音效
        if (isCrouching)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    /// <summary>
    /// 拾取武器
    /// </summary>
    public void PickUpWeapon(int itemID,GameObject weapon) {
        /*捡到武器后，在武器库里添加，，否则补充备弹*/
        if (inventory.weapons.Contains(weapon))
        {
            weapon.GetComponent<Weapon_AutomaticGun>().bulletLeft = weapon.GetComponent<Weapon_AutomaticGun>().bulletMag * 5;
            weapon.GetComponent<Weapon_AutomaticGun>().UpdateAmmoUI();
            Debug.Log("集合里已存在此枪械，补充备弹");
            return;
        }
        else
        {
            inventory.AddWeapon(weapon);
        }
    }

    /// <summary>
    /// 玩家生命值
    /// </summary>
    /// <param name="damage">接收到伤害值</param>
    public void PlayerHealth(float damage)
    {
        playerHealth -= damage;
        isDamage = true;
        playerHealthUI.text = "生命值：" + playerHealth;
        if (playerHealth <= 0)
        {
            playerisDead = true;
            playerHealthUI.text = "玩家死亡";
            Time.timeScale = 0;//游戏暂停
        }
    }


    public enum MovementState
    {
        walking,
        running,
        crouching,
        idle
    }
}
