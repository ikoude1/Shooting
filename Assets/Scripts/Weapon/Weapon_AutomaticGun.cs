
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using static PlayerController;
/// <summary>
/// 自动步枪和冲锋枪射击
/// </summary>
/*//fireRate 值越小，射速越快
 * arms_assault_rifle_01 fireRate-->0.15  sniperingFifleOnPostion=(0,-0.005,-0.13)
 * arms_assault_rifle_01 fireRate-->0.13  sniperingFifleOnPostion=(0,0.009,-0.13)
 */

/// <summary>
/// 武器音效内部类
/// </summary>
[System.Serializable]
public class SoundClips
{
    public AudioClip shootSound;   //开火音效
    public AudioClip silencerShootSound;//开火音效带消音器
    public AudioClip reloadSoundAmmotLeft;//换子弹音效
    public AudioClip reloadSoundOutOfAmmo;//换子弹并拉枪栓(一个弹匣打完)
    public AudioClip aimSound;//瞄准音效
}



public class Weapon_AutomaticGun : Weapon
{
    public SoundClips soundClips;
    private Animator animator;
    private PlayerController playerController;
    private Camera mainCamera;
    public Camera gunCamera;

    public bool IS_AUTORIFLE;//是否是自动武器
    public bool IS_SEMIGUN;//是否半自动武器

    [Header("武器部件位置")]
    [Tooltip("射击的位置")] public Transform ShootPoint; //射线打出的位置
    public Transform BulletShootPoint; //子弹特效打出的位置
    [Tooltip("子弹壳抛出的位置")] public Transform CasingBulletSpawnPoint;

    [Header("子弹预制体和特效")]
    public Transform bulletPrefab;//子弹
    public Transform casingPrefab;//子弹抛壳


    [Header("枪械属性")]
    [Tooltip("武器射程")] private float range;
    [Tooltip("武器射速")] public float fireRate;
    private float originRate;//原始射速
    private float SpreadFactor; //射击的一点偏移量
    private float fireTimer;//计时器 控制武器射速
    private float bulletForce;//子弹发射的力
    [Tooltip("当前武器的每个弹匣子弹数")] public int bulletMag;
    [Tooltip("当前子弹数")] public int currentBullets;
    [Tooltip("备弹")] public int bulletLeft;
    public bool isSilencer;//是否装备消音器
    private int shotgunFragment = 8;//1次打出的子弹数
    public float minDamage;
    public float maxDamage;


    [Header("特效")]
    public Light muzzleflashLight;//开火灯光
    private float lightDuration;  //灯光持续时间
    public ParticleSystem muzzlePartic;//枪口火焰粒子特效1
    public ParticleSystem sparkPartic;//枪口火焰粒子特效2(火星子)
    public int minSparkEmission = 1;
    public int maxSparkEmission = 7;

    [Header("音源")]
    private AudioSource shootAudioSource;//射击音效
    private AudioSource mainAudioSource;

    [Header("UI")]
    public Image[] crossQuarterImgs;  //准心
    public float currentExpanedDegree;//当前准心开合度    
    private float crossExpanedDegree;//每帧准心开合度    
    private float maxCrossDegree;//最大开合度
    public Text ammoTextUI;
    public Text shootModeTextUI;

    public PlayerController.MovementState state;
    public bool isReloading;//判断是否在装弹
    private bool isAiming;//判断是否在瞄准

    private Vector3 sniperingFiflePosition;//枪默认的初始位置
    public Vector3 sniperingFifleOnPosition;//开始瞄准的模型位置


    [Header("键位设置")]
    [SerializeField][Tooltip("填装子弹按键")] private KeyCode reloadInputName = KeyCode.R;
    [SerializeField][Tooltip("查看武器按键")] private KeyCode inspectInputName = KeyCode.I;
    [SerializeField][Tooltip("自动半自动切换按键")] private KeyCode GunShootModelInputName = KeyCode.X;


    /*使用枚举区分全自动及半自动模式*/
    public ShootMode shootingMode;
    private bool GunShootInput; //根据全自动和半自动 射击的键位输入发生改变
    private int modeNum; //模式切换的一个中间参数（1：全自动模式，2：半自动模式）
    private string shootModeName;


    [Header("狙击镜设置")]
    [Tooltip("狙击镜材质")]public Material scopeRenderMaterial;
    [Tooltip("当没有进行瞄准时狙击镜的颜色")]public Color fadeColor;
    [Tooltip("当瞄准时狙击镜的颜色")] public Color defaultColor;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();
        mainAudioSource = GetComponent<AudioSource>();
        shootAudioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        sniperingFiflePosition = transform.localPosition;
        muzzleflashLight.enabled = false;
        lightDuration = 0.02f;
        crossExpanedDegree = 50f;
        maxCrossDegree = 300f;
        range = 300f;
        bulletForce = 100f;
        //bulletMag = 30;
        bulletLeft = bulletMag * 5;
        currentBullets = bulletMag;
        originRate = fireRate;
        UpdateAmmoUI();

        /*根据不同枪械，游戏刚开始时进行不同射击模式设置*/
        if (IS_AUTORIFLE)
        {
            modeNum = 1;
            shootModeName = "fully automatic";
            shootingMode = ShootMode.AutoRifle;
            UpdateAmmoUI();
        }
        if (IS_SEMIGUN)
        {
            modeNum = 0;
            shootModeName = "semi-automatic";
            shootingMode = ShootMode.SemiGun;
            UpdateAmmoUI();
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.playerisDead)
        {
            mainAudioSource.Pause();
            shootAudioSource.Pause();
        }


        //自动枪械鼠标输入方式 可以在  GetMouseButton 和 GetMouseButtonDown 里切换
        if (IS_AUTORIFLE)
        {
            //切换射击模式（全自动和半自动）
            if (Input.GetKeyDown(GunShootModelInputName) && modeNum != 1)
            {
                modeNum = 1;
                shootModeName = "fully automatic";
                shootingMode = ShootMode.AutoRifle;
                UpdateAmmoUI();
            }
            else if (Input.GetKeyDown(GunShootModelInputName) && modeNum != 0)
            {
                modeNum = 0;
                shootModeName = "semi-automatic";
                shootingMode = ShootMode.SemiGun;
                UpdateAmmoUI();
            }
            /*控制射击模式的转换  后面就要用代码去动态控制了*/
            switch (shootingMode)
            {
                case ShootMode.AutoRifle:
                    GunShootInput = Input.GetMouseButton(0);
                    fireRate = originRate;
                    break;
                case ShootMode.SemiGun:
                    GunShootInput = Input.GetMouseButtonDown(0);
                    fireRate = 0.2f;
                    break;
            }
        }
        else
        {
            //半自动枪械鼠标输入方式改为GetMouseButtonDown
            GunShootInput = Input.GetMouseButtonDown(0);
        }




        state = playerController.state; //这里实时获取人物的移动状态(行走，奔跑，下蹲)
        if (state == MovementState.walking && Vector3.SqrMagnitude(playerController.moveDirction) > 0 && state != MovementState.running && state != MovementState.crouching)
        {   //移动时的准心开合度
            ExpandingCrossUpdate(crossExpanedDegree);
        }

        else if (state != MovementState.walking && state == MovementState.running && state != MovementState.crouching)
        {   //奔跑时的准心开合度(2倍)
            ExpandingCrossUpdate(crossExpanedDegree * 2);
        }
        else
        {
            //站立或者下蹲时，不调整准心开合度
            ExpandingCrossUpdate(0);
        }


        if (GunShootInput && currentBullets > 0)
        {
            //霰弹枪射击1此同时打出8次射线，其余枪械正常1次射线
            if (IS_SEMIGUN&&gameObject.name=="4")
            {
                shotgunFragment = 8;
            }
            else
            {
                shotgunFragment = 1;
            }
            //开枪射击
            GunFire();
        }

        //计时器
        if (fireTimer < fireRate)
        {
            fireTimer += Time.deltaTime;
        }

       //播放行走 跑步动画
        animator.SetBool("Run",playerController.isRun);
        animator.SetBool("Walk", playerController.isWalk);

    
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        //两种换子弹的动画（包括霰弹枪换弹动画逻辑）
        if ( 
            info.IsName("reload_ammo_left") || 
            info.IsName("reload_out_of_ammo") ||
            info.IsName("reload_open") ||
            info.IsName("reload_close") ||
            info.IsName("reload_insert 1") ||
            info.IsName("reload_insert 2") ||
            info.IsName("reload_insert 3") ||
            info.IsName("reload_insert 4") ||
            info.IsName("reload_insert 5") ||
            info.IsName("reload_insert 6")
            )
        {
            isReloading = true;
        }
        else
        {
            isReloading = false;
        }


        //根据当前霰弹枪子弹填装的数量判断结束insert动画
        //解决了本来放2个子弹，但实际上添加子弹的动画播放了3次的问题
        if ((
                info.IsName("reload_insert 1") ||
                info.IsName("reload_insert 2") ||
                info.IsName("reload_insert 3") ||
                info.IsName("reload_insert 4") ||
                info.IsName("reload_insert 5") ||
                info.IsName("reload_insert 6"
            )) && currentBullets == bulletMag)
        {
            //当前霰弹枪子弹填装完毕，，随时结束换弹判断
            animator.Play("reload_close");
            isReloading = false;
        }


        //按下换弹键，当前子弹小于弹匣数，备弹量大于0，
        //判断现在没有换弹的时候，才运行播放换弹动画
        if (Input.GetKeyDown(reloadInputName) && currentBullets < bulletMag && bulletLeft > 0 && !isReloading)
        {
            DoReloadAnimation();
        }

        //鼠标右键进入瞄准
        if (Input.GetMouseButton(1) && !isReloading && !playerController.isRun)
        {
            isAiming = true;
            animator.SetBool("Aim",isAiming);
            //瞄准的时候需要微调一下枪的模型位置
            transform.localPosition = sniperingFifleOnPosition;
        }
        else
        {
            isAiming = false;
            animator.SetBool("Aim", isAiming);
            transform.localPosition = sniperingFiflePosition;
        }

        //腰射和瞄准射击精准不同
        SpreadFactor = (isAiming) ? 0.01f : 0.1f;

        if (Input.GetKeyDown(inspectInputName))
        {
            animator.SetTrigger("Inspect");
        }

    }

    /// <summary>
    /// 射击
    /// </summary>
    public override void GunFire()
    {   //1、控制射速,
        //2、正在奔跑
        //3、当前正在播放 take_out 动画
        //4、当前没子弹了
        //5、正在换子弹时，
        //6、当前正在播放 inspect 动画
        //就不可以发射了
        if (fireTimer < fireRate ||playerController.isRun || currentBullets <= 0 || animator.GetCurrentAnimatorStateInfo(0).IsName("take_out") ||isReloading || animator.GetCurrentAnimatorStateInfo(0).IsName("inspect")) return;

        StartCoroutine(MuzzleFlashLight());//开火灯光
        muzzlePartic.Emit(1);//发射1个枪口火焰粒子
        sparkPartic.Emit(Random.Range(minSparkEmission, maxSparkEmission));//发射枪口火星粒子                                                                           
        StartCoroutine(Shoot_Cross());  //增大准心大小


        if (!isAiming)
        {
            //播放普通开火动画（使用动画的淡入淡出效果）
            animator.CrossFadeInFixedTime("fire", 0.1f);
        }
        else
        {   //瞄准状态下，播放瞄准开火动画
            animator.Play("aim_fire", 0, 0);
        }

        for (int i = 0; i < shotgunFragment; i++)
        {
            RaycastHit hit;
            Vector3 shootDirection = ShootPoint.forward;//射线向前方射击
            shootDirection = shootDirection + ShootPoint.TransformDirection(new Vector3(Random.Range(-SpreadFactor, SpreadFactor), Random.Range(-SpreadFactor, SpreadFactor)));

            //射线检测 （这里射线检测的方式从屏幕正中心射出）
            if (Physics.Raycast(ShootPoint.position, shootDirection, out hit, range))
            {

                Transform bullet;
                if (IS_AUTORIFLE|| (IS_SEMIGUN&& gameObject.name=="2") )
                {
                    //实例化出子弹拖尾特效（拖尾特效里包含击中和弹孔特效）
                    bullet = (Transform)Instantiate(bulletPrefab, BulletShootPoint.transform.position, BulletShootPoint.transform.rotation);

                }
                else
                {
                    //霰弹枪特殊处理下，将子弹限制位置设定到 hit.point 
                    bullet = Instantiate(bulletPrefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                }

                //给子弹一个向前的速度力  (加上射线打出去的偏移量)
                bullet.GetComponent<Rigidbody>().velocity = (bullet.transform.forward + shootDirection) * bulletForce;

                //击中敌人时做的判断
                if (hit.transform.gameObject.tag == "Enemy")
                {                  
                    hit.transform.gameObject.GetComponent<Enemy>().Health(Random.Range(minDamage, maxDamage));
                }

                Debug.Log(hit.transform.gameObject.name + "打到了");

            }
        }
   

        Instantiate(casingPrefab, CasingBulletSpawnPoint.transform.position, CasingBulletSpawnPoint.transform.rotation); //实例抛弹壳       
        //根据是否装备消音器，切换不同的射击音效
        shootAudioSource.clip =  isSilencer?soundClips.silencerShootSound: soundClips.shootSound;
        shootAudioSource.Play();//播放射击音效
        fireTimer = 0f;//重置计时器
        currentBullets--;//子弹减少
        UpdateAmmoUI();
    }

    /// <summary>
    /// 设置开火时候灯光
    /// </summary>
    public IEnumerator MuzzleFlashLight()
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }



    /// <summary>
    /// 根据指定大小，来增大或减少准心开合度
    /// </summary>
    public override void ExpandingCrossUpdate(float expanDegree)
    {
        if (currentExpanedDegree < expanDegree - 5)
        {
            ExpendCross(150 * Time.deltaTime);
        }
        else if (currentExpanedDegree > expanDegree + 5)
        {
            ExpendCross(-300 * Time.deltaTime);
        }
    }

    /// <summary>
    /// 改变准心的开合度，并记录下当前准星开合度
    /// </summary>
    public void ExpendCross(float add)
    {
        crossQuarterImgs[0].transform.localPosition += new Vector3(-add, 0, 0);//左准心
        crossQuarterImgs[1].transform.localPosition += new Vector3(add, 0, 0);//右准心
        crossQuarterImgs[2].transform.localPosition += new Vector3(0, add, 0);//上准心
        crossQuarterImgs[3].transform.localPosition += new Vector3(0, -add, 0);//下准心
        currentExpanedDegree += add;//保存当前准心开合度
        currentExpanedDegree = Mathf.Clamp(currentExpanedDegree, 0, maxCrossDegree);  //限制准心开合度大小
    }

    /// <summary>
    /// 携程，调用准星开合度，1帧执行5次
    /// 这个携程只负责射击时瞬间增大准心
    /// </summary>
    /// <returns></returns>
    public IEnumerator Shoot_Cross()
    {
        yield return null;
        for (int i = 0; i < 5; i++)
        {
            ExpendCross(Time.deltaTime * 500);
        }
    }

    /// <summary>
    /// 更新子弹UI
    /// </summary>
    public void UpdateAmmoUI()
    {
        ammoTextUI.text = currentBullets + "/" + bulletLeft;
        shootModeTextUI.text = shootModeName;
    }

    /// <summary>
    /// 播放不同的装弹动画
    /// </summary>
    public override void DoReloadAnimation()
    {
        if ( !(IS_SEMIGUN&&(gameObject.name=="3" || gameObject.name == "4")) )
        {
            if (currentBullets > 0 && bulletLeft > 0)
            {
                animator.Play("reload_ammo_left", 0, 0);
                Reload();
                mainAudioSource.clip = soundClips.reloadSoundAmmotLeft;
                mainAudioSource.Play();
            }
            if (currentBullets == 0 && bulletLeft > 0)
            {
                animator.Play("reload_out_of_ammo", 0, 0);
                Reload();
                mainAudioSource.clip = soundClips.reloadSoundOutOfAmmo;
                mainAudioSource.Play();
            }
        }
        else
        {
            if (currentBullets == bulletMag) return;           
            //霰弹枪换子弹动画触发
            animator.SetTrigger("shotgun_reload");
        }
       
    }

    /// <summary>
    /// 填装弹药逻辑，在动画里调用
    /// </summary>
    public override void Reload()
    {
        if (bulletLeft <= 0) return;
        //计算需要填充的子弹
        int bulletToLoad = bulletMag - currentBullets;
        //计算备弹扣除的子弹数
        int bulletToReduce = bulletLeft >= bulletToLoad ? bulletToLoad : bulletLeft;
        bulletLeft -= bulletToReduce;//备弹减少
        currentBullets += bulletToReduce;//当前子弹增加
        UpdateAmmoUI();
    }

    /// <summary>
    /// 霰弹枪换弹药逻辑
    /// ReloadAmmoState  脚本里调用
    /// </summary>
    public void ShotGunReload()
    {
        if (currentBullets < bulletMag)
        {
            currentBullets++;
            bulletLeft--;
            UpdateAmmoUI();
        }
        else
        {
            animator.Play("reload_close");
            return;
        }
        if (bulletLeft <= 0) return;
    }

    /// <summary>
    /// animation event
    /// 进入瞄准，隐藏准心，摄像机视野变近
    /// </summary>
    public override void AimIn()
    {
        float currentVelocity = 0f;
        for (int i = 0; i < crossQuarterImgs.Length; i++)
        {
            crossQuarterImgs[i].gameObject.SetActive(false);
        }
        //狙击枪瞄准时候，改变 gunCamera 的视野和瞄准镜的颜色
        if (IS_SEMIGUN && (gameObject.name=="4") )
        {
            scopeRenderMaterial.color = defaultColor;
            gunCamera.fieldOfView = 15;
        }

        //瞄准的时候摄像机视野变近
        mainCamera.fieldOfView = Mathf.SmoothDamp(30, 60, ref currentVelocity, 0.1f);
        mainAudioSource.clip = soundClips.aimSound;
        mainAudioSource.Play();
    }

    /// <summary>
    /// animation event
    /// 退出瞄准，显示准心，摄像机视野恢复
    /// </summary>
    public override void AimOut()
    {
        float currentVelocity = 0f;
        for (int i = 0; i < crossQuarterImgs.Length; i++)
        {
            crossQuarterImgs[i].gameObject.SetActive(true);
        }

        if (IS_SEMIGUN && (gameObject.name == "4"))
        {
            scopeRenderMaterial.color = fadeColor;
            gunCamera.fieldOfView = 35;
        }


        //瞄准的时候摄像机视野恢复
        mainCamera.fieldOfView = Mathf.SmoothDamp(60, 30, ref currentVelocity, 0.1f);
        mainAudioSource.clip = soundClips.aimSound;
        mainAudioSource.Play();
    }

    public enum ShootMode {
        AutoRifle,
        SemiGun
    };
}
