
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using static PlayerController;
/// <summary>
/// �Զ���ǹ�ͳ��ǹ���
/// </summary>
/*//fireRate ֵԽС������Խ��
 * arms_assault_rifle_01 fireRate-->0.15  sniperingFifleOnPostion=(0,-0.005,-0.13)
 * arms_assault_rifle_01 fireRate-->0.13  sniperingFifleOnPostion=(0,0.009,-0.13)
 */

/// <summary>
/// ������Ч�ڲ���
/// </summary>
[System.Serializable]
public class SoundClips
{
    public AudioClip shootSound;   //������Ч
    public AudioClip silencerShootSound;//������Ч��������
    public AudioClip reloadSoundAmmotLeft;//���ӵ���Ч
    public AudioClip reloadSoundOutOfAmmo;//���ӵ�����ǹ˨(һ����ϻ����)
    public AudioClip aimSound;//��׼��Ч
}



public class Weapon_AutomaticGun : Weapon
{
    public SoundClips soundClips;
    private Animator animator;
    private PlayerController playerController;
    private Camera mainCamera;
    public Camera gunCamera;

    public bool IS_AUTORIFLE;//�Ƿ����Զ�����
    public bool IS_SEMIGUN;//�Ƿ���Զ�����

    [Header("��������λ��")]
    [Tooltip("�����λ��")] public Transform ShootPoint; //���ߴ����λ��
    public Transform BulletShootPoint; //�ӵ���Ч�����λ��
    [Tooltip("�ӵ����׳���λ��")] public Transform CasingBulletSpawnPoint;

    [Header("�ӵ�Ԥ�������Ч")]
    public Transform bulletPrefab;//�ӵ�
    public Transform casingPrefab;//�ӵ��׿�


    [Header("ǹе����")]
    [Tooltip("�������")] private float range;
    [Tooltip("��������")] public float fireRate;
    private float originRate;//ԭʼ����
    private float SpreadFactor; //�����һ��ƫ����
    private float fireTimer;//��ʱ�� ������������
    private float bulletForce;//�ӵ��������
    [Tooltip("��ǰ������ÿ����ϻ�ӵ���")] public int bulletMag;
    [Tooltip("��ǰ�ӵ���")] public int currentBullets;
    [Tooltip("����")] public int bulletLeft;
    public bool isSilencer;//�Ƿ�װ��������
    private int shotgunFragment = 8;//1�δ�����ӵ���
    public float minDamage;
    public float maxDamage;


    [Header("��Ч")]
    public Light muzzleflashLight;//����ƹ�
    private float lightDuration;  //�ƹ����ʱ��
    public ParticleSystem muzzlePartic;//ǹ�ڻ���������Ч1
    public ParticleSystem sparkPartic;//ǹ�ڻ���������Ч2(������)
    public int minSparkEmission = 1;
    public int maxSparkEmission = 7;

    [Header("��Դ")]
    private AudioSource shootAudioSource;//�����Ч
    private AudioSource mainAudioSource;

    [Header("UI")]
    public Image[] crossQuarterImgs;  //׼��
    public float currentExpanedDegree;//��ǰ׼�Ŀ��϶�    
    private float crossExpanedDegree;//ÿ֡׼�Ŀ��϶�    
    private float maxCrossDegree;//��󿪺϶�
    public Text ammoTextUI;
    public Text shootModeTextUI;

    public PlayerController.MovementState state;
    public bool isReloading;//�ж��Ƿ���װ��
    private bool isAiming;//�ж��Ƿ�����׼

    private Vector3 sniperingFiflePosition;//ǹĬ�ϵĳ�ʼλ��
    public Vector3 sniperingFifleOnPosition;//��ʼ��׼��ģ��λ��


    [Header("��λ����")]
    [SerializeField][Tooltip("��װ�ӵ�����")] private KeyCode reloadInputName = KeyCode.R;
    [SerializeField][Tooltip("�鿴��������")] private KeyCode inspectInputName = KeyCode.I;
    [SerializeField][Tooltip("�Զ����Զ��л�����")] private KeyCode GunShootModelInputName = KeyCode.X;


    /*ʹ��ö������ȫ�Զ������Զ�ģʽ*/
    public ShootMode shootingMode;
    private bool GunShootInput; //����ȫ�Զ��Ͱ��Զ� ����ļ�λ���뷢���ı�
    private int modeNum; //ģʽ�л���һ���м������1��ȫ�Զ�ģʽ��2�����Զ�ģʽ��
    private string shootModeName;


    [Header("�ѻ�������")]
    [Tooltip("�ѻ�������")]public Material scopeRenderMaterial;
    [Tooltip("��û�н�����׼ʱ�ѻ�������ɫ")]public Color fadeColor;
    [Tooltip("����׼ʱ�ѻ�������ɫ")] public Color defaultColor;

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

        /*���ݲ�ͬǹе����Ϸ�տ�ʼʱ���в�ͬ���ģʽ����*/
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


        //�Զ�ǹе������뷽ʽ ������  GetMouseButton �� GetMouseButtonDown ���л�
        if (IS_AUTORIFLE)
        {
            //�л����ģʽ��ȫ�Զ��Ͱ��Զ���
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
            /*�������ģʽ��ת��  �����Ҫ�ô���ȥ��̬������*/
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
            //���Զ�ǹе������뷽ʽ��ΪGetMouseButtonDown
            GunShootInput = Input.GetMouseButtonDown(0);
        }




        state = playerController.state; //����ʵʱ��ȡ������ƶ�״̬(���ߣ����ܣ��¶�)
        if (state == MovementState.walking && Vector3.SqrMagnitude(playerController.moveDirction) > 0 && state != MovementState.running && state != MovementState.crouching)
        {   //�ƶ�ʱ��׼�Ŀ��϶�
            ExpandingCrossUpdate(crossExpanedDegree);
        }

        else if (state != MovementState.walking && state == MovementState.running && state != MovementState.crouching)
        {   //����ʱ��׼�Ŀ��϶�(2��)
            ExpandingCrossUpdate(crossExpanedDegree * 2);
        }
        else
        {
            //վ�������¶�ʱ��������׼�Ŀ��϶�
            ExpandingCrossUpdate(0);
        }


        if (GunShootInput && currentBullets > 0)
        {
            //����ǹ���1��ͬʱ���8�����ߣ�����ǹе����1������
            if (IS_SEMIGUN&&gameObject.name=="4")
            {
                shotgunFragment = 8;
            }
            else
            {
                shotgunFragment = 1;
            }
            //��ǹ���
            GunFire();
        }

        //��ʱ��
        if (fireTimer < fireRate)
        {
            fireTimer += Time.deltaTime;
        }

       //�������� �ܲ�����
        animator.SetBool("Run",playerController.isRun);
        animator.SetBool("Walk", playerController.isWalk);

    
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        //���ֻ��ӵ��Ķ�������������ǹ���������߼���
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


        //���ݵ�ǰ����ǹ�ӵ���װ�������жϽ���insert����
        //����˱�����2���ӵ�����ʵ��������ӵ��Ķ���������3�ε�����
        if ((
                info.IsName("reload_insert 1") ||
                info.IsName("reload_insert 2") ||
                info.IsName("reload_insert 3") ||
                info.IsName("reload_insert 4") ||
                info.IsName("reload_insert 5") ||
                info.IsName("reload_insert 6"
            )) && currentBullets == bulletMag)
        {
            //��ǰ����ǹ�ӵ���װ��ϣ�����ʱ���������ж�
            animator.Play("reload_close");
            isReloading = false;
        }


        //���»���������ǰ�ӵ�С�ڵ�ϻ��������������0��
        //�ж�����û�л�����ʱ�򣬲����в��Ż�������
        if (Input.GetKeyDown(reloadInputName) && currentBullets < bulletMag && bulletLeft > 0 && !isReloading)
        {
            DoReloadAnimation();
        }

        //����Ҽ�������׼
        if (Input.GetMouseButton(1) && !isReloading && !playerController.isRun)
        {
            isAiming = true;
            animator.SetBool("Aim",isAiming);
            //��׼��ʱ����Ҫ΢��һ��ǹ��ģ��λ��
            transform.localPosition = sniperingFifleOnPosition;
        }
        else
        {
            isAiming = false;
            animator.SetBool("Aim", isAiming);
            transform.localPosition = sniperingFiflePosition;
        }

        //�������׼�����׼��ͬ
        SpreadFactor = (isAiming) ? 0.01f : 0.1f;

        if (Input.GetKeyDown(inspectInputName))
        {
            animator.SetTrigger("Inspect");
        }

    }

    /// <summary>
    /// ���
    /// </summary>
    public override void GunFire()
    {   //1����������,
        //2�����ڱ���
        //3����ǰ���ڲ��� take_out ����
        //4����ǰû�ӵ���
        //5�����ڻ��ӵ�ʱ��
        //6����ǰ���ڲ��� inspect ����
        //�Ͳ����Է�����
        if (fireTimer < fireRate ||playerController.isRun || currentBullets <= 0 || animator.GetCurrentAnimatorStateInfo(0).IsName("take_out") ||isReloading || animator.GetCurrentAnimatorStateInfo(0).IsName("inspect")) return;

        StartCoroutine(MuzzleFlashLight());//����ƹ�
        muzzlePartic.Emit(1);//����1��ǹ�ڻ�������
        sparkPartic.Emit(Random.Range(minSparkEmission, maxSparkEmission));//����ǹ�ڻ�������                                                                           
        StartCoroutine(Shoot_Cross());  //����׼�Ĵ�С


        if (!isAiming)
        {
            //������ͨ���𶯻���ʹ�ö����ĵ��뵭��Ч����
            animator.CrossFadeInFixedTime("fire", 0.1f);
        }
        else
        {   //��׼״̬�£�������׼���𶯻�
            animator.Play("aim_fire", 0, 0);
        }

        for (int i = 0; i < shotgunFragment; i++)
        {
            RaycastHit hit;
            Vector3 shootDirection = ShootPoint.forward;//������ǰ�����
            shootDirection = shootDirection + ShootPoint.TransformDirection(new Vector3(Random.Range(-SpreadFactor, SpreadFactor), Random.Range(-SpreadFactor, SpreadFactor)));

            //���߼�� ���������߼��ķ�ʽ����Ļ�����������
            if (Physics.Raycast(ShootPoint.position, shootDirection, out hit, range))
            {

                Transform bullet;
                if (IS_AUTORIFLE|| (IS_SEMIGUN&& gameObject.name=="2") )
                {
                    //ʵ�������ӵ���β��Ч����β��Ч��������к͵�����Ч��
                    bullet = (Transform)Instantiate(bulletPrefab, BulletShootPoint.transform.position, BulletShootPoint.transform.rotation);

                }
                else
                {
                    //����ǹ���⴦���£����ӵ�����λ���趨�� hit.point 
                    bullet = Instantiate(bulletPrefab, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                }

                //���ӵ�һ����ǰ���ٶ���  (�������ߴ��ȥ��ƫ����)
                bullet.GetComponent<Rigidbody>().velocity = (bullet.transform.forward + shootDirection) * bulletForce;

                //���е���ʱ�����ж�
                if (hit.transform.gameObject.tag == "Enemy")
                {                  
                    hit.transform.gameObject.GetComponent<Enemy>().Health(Random.Range(minDamage, maxDamage));
                }

                Debug.Log(hit.transform.gameObject.name + "����");

            }
        }
   

        Instantiate(casingPrefab, CasingBulletSpawnPoint.transform.position, CasingBulletSpawnPoint.transform.rotation); //ʵ���׵���       
        //�����Ƿ�װ�����������л���ͬ�������Ч
        shootAudioSource.clip =  isSilencer?soundClips.silencerShootSound: soundClips.shootSound;
        shootAudioSource.Play();//���������Ч
        fireTimer = 0f;//���ü�ʱ��
        currentBullets--;//�ӵ�����
        UpdateAmmoUI();
    }

    /// <summary>
    /// ���ÿ���ʱ��ƹ�
    /// </summary>
    public IEnumerator MuzzleFlashLight()
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleflashLight.enabled = false;
    }



    /// <summary>
    /// ����ָ����С������������׼�Ŀ��϶�
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
    /// �ı�׼�ĵĿ��϶ȣ�����¼�µ�ǰ׼�ǿ��϶�
    /// </summary>
    public void ExpendCross(float add)
    {
        crossQuarterImgs[0].transform.localPosition += new Vector3(-add, 0, 0);//��׼��
        crossQuarterImgs[1].transform.localPosition += new Vector3(add, 0, 0);//��׼��
        crossQuarterImgs[2].transform.localPosition += new Vector3(0, add, 0);//��׼��
        crossQuarterImgs[3].transform.localPosition += new Vector3(0, -add, 0);//��׼��
        currentExpanedDegree += add;//���浱ǰ׼�Ŀ��϶�
        currentExpanedDegree = Mathf.Clamp(currentExpanedDegree, 0, maxCrossDegree);  //����׼�Ŀ��϶ȴ�С
    }

    /// <summary>
    /// Я�̣�����׼�ǿ��϶ȣ�1ִ֡��5��
    /// ���Я��ֻ�������ʱ˲������׼��
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
    /// �����ӵ�UI
    /// </summary>
    public void UpdateAmmoUI()
    {
        ammoTextUI.text = currentBullets + "/" + bulletLeft;
        shootModeTextUI.text = shootModeName;
    }

    /// <summary>
    /// ���Ų�ͬ��װ������
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
            //����ǹ���ӵ���������
            animator.SetTrigger("shotgun_reload");
        }
       
    }

    /// <summary>
    /// ��װ��ҩ�߼����ڶ��������
    /// </summary>
    public override void Reload()
    {
        if (bulletLeft <= 0) return;
        //������Ҫ�����ӵ�
        int bulletToLoad = bulletMag - currentBullets;
        //���㱸���۳����ӵ���
        int bulletToReduce = bulletLeft >= bulletToLoad ? bulletToLoad : bulletLeft;
        bulletLeft -= bulletToReduce;//��������
        currentBullets += bulletToReduce;//��ǰ�ӵ�����
        UpdateAmmoUI();
    }

    /// <summary>
    /// ����ǹ����ҩ�߼�
    /// ReloadAmmoState  �ű������
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
    /// ������׼������׼�ģ��������Ұ���
    /// </summary>
    public override void AimIn()
    {
        float currentVelocity = 0f;
        for (int i = 0; i < crossQuarterImgs.Length; i++)
        {
            crossQuarterImgs[i].gameObject.SetActive(false);
        }
        //�ѻ�ǹ��׼ʱ�򣬸ı� gunCamera ����Ұ����׼������ɫ
        if (IS_SEMIGUN && (gameObject.name=="4") )
        {
            scopeRenderMaterial.color = defaultColor;
            gunCamera.fieldOfView = 15;
        }

        //��׼��ʱ���������Ұ���
        mainCamera.fieldOfView = Mathf.SmoothDamp(30, 60, ref currentVelocity, 0.1f);
        mainAudioSource.clip = soundClips.aimSound;
        mainAudioSource.Play();
    }

    /// <summary>
    /// animation event
    /// �˳���׼����ʾ׼�ģ��������Ұ�ָ�
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


        //��׼��ʱ���������Ұ�ָ�
        mainCamera.fieldOfView = Mathf.SmoothDamp(60, 30, ref currentVelocity, 0.1f);
        mainAudioSource.clip = soundClips.aimSound;
        mainAudioSource.Play();
    }

    public enum ShootMode {
        AutoRifle,
        SemiGun
    };
}
