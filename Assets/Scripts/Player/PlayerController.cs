using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// ��ɫ������
/// </summary>
public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    public Vector3 moveDirction;//���������ƶ�����
    private AudioSource audioSource;

    [Header("�����ֵ")]
    public float Speed;
    [Tooltip("�����ٶ�")]public float walkSpeed;
    [Tooltip("�����ٶ�")] public float runSpeed;
    [Tooltip("�¶������ٶ�")] public float crouchSpeed;
    [Tooltip("�������ֵ")]public float playerHealth;

    [Tooltip("��Ծ����")]public float jumpForce;
    [Tooltip("�������")] public float fallForce;
    [Tooltip("�¶�ʱ�����Ҹ߶�")]public float crouchHeight;
    [Tooltip("����վ��ʱ����Ҹ߶�")] public float standHeight;

    [Header("��λ����")]
    [Tooltip("���ܰ���")]public KeyCode runInputName = KeyCode.LeftShift;
    [Tooltip("��Ծ����")] public KeyCode jumpInputName = KeyCode.Space;
    [Tooltip("�¶װ���")] public KeyCode crouchInputName = KeyCode.LeftControl;


    [Header("��������ж�")]
    public MovementState state;
    private CollisionFlags collisonFlags;
    public bool isWalk;//�ж�����Ƿ�����
    public bool isRun;//�ж�����Ƿ��ڱ���
    public bool isJump;//�ж�����Ƿ���Ծ
    public bool isGround;//�ж�����Ƿ��ڵ�����
    public bool isCanCrouch;//�ж�����Ƿ�����¶�
    public bool isCrouching;//�ж�����Ƿ����¶�
    public bool playerisDead;//�ж�����Ƿ�����
    private bool isDamage;//�ж�����Ƿ��յ��˺�

    public LayerMask crouchLayerMask;
    public Text playerHealthUI;
    public Image hurtImage;//���Ѫ��
    //Color.red;
    private Color flashColor = new Color(1f,0f,0f,1f);
    private Color clearColor = Color.clear;


    [Header("��Ч")]
    [Tooltip("������Ч")]public AudioClip walkingSound;
    [Tooltip("������Ч")]public AudioClip runnningSound;

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
        audioSource.loop = true;//��Чѭ����
        playerHealthUI.text = "HP��" + playerHealth;

    }

    // Update is called once per frame
    void Update()
    {
        /*����ܵ��˺�����Ļ������ɫ����*/
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
            //���������ֹͣȫ���ƶ���Ϊ
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
    /// �ƶ�
    /// </summary>
    public void Moveing()
    {
        /*������������ƶ���ͣ�Ļ���������GetAxisRaw��
         ����Ҫ��ͣ������GetAxis*/
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        //�����ڵ����ϣ����Ұ��±��ܼ�,�Ƕ��¼�����ʱ����
        isRun=Input.GetKey(runInputName);
        isWalk = (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0) ? true : false;
        if (isRun && isGround && isCanCrouch && !isCrouching)
        {
            state = MovementState.running;
            Speed = runSpeed;
        }
        else if (isGround)  //��������
        {
            state = MovementState.walking;
            Speed = walkSpeed;
            if (isCrouching )//�¶�����
            {
                state = MovementState.crouching;
                Speed = crouchSpeed;
            }
        }
        //ͬʱ�����¶׺ͱ��ܼ���ʱ�򣬣����ﻹ���¶��ٶ�
        if (isRun && isCrouching)
        {
            state = MovementState.crouching;
            Speed = crouchSpeed;
        }
           
        

        //���������ƶ����򣨽��ƶ��ٶȽ��й淶��������ֹб�����ٶȱ��
        moveDirction = (transform.right * h + transform.forward * v).normalized;
        characterController.Move(moveDirction * Speed * Time.deltaTime);//�����ƶ�
    }

    /// <summary>
    /// ��Ծ
    /// </summary>
    public void Jump() {

        if (!isCanCrouch) return;
        isJump = Input.GetKeyDown(jumpInputName);
        //�ж���Ұ�����Ծ�������Ҵ�ʱ�ڵ����ϣ����ܽ�����Ծ
        if (isJump && isGround)
        {
            isGround = false;
            jumpForce = 5f;//������Ծ����
        }
        //20231011��ӣ������ǰû�а��¿ո����ڼ������ϣ���ôisGround�ж�Ϊfalse��jumpForce��һ��������ֵ        
        else if (!isJump && isGround)
        {
            isGround = false;
           // jumpForce = -2f;
        }

        //��ʱ������Ծ���������ˣ����ڵ���ʱ
        if (!isGround)
        {
            jumpForce = jumpForce - fallForce * Time.deltaTime;//ÿ�뽫��Ծ�������ۼ��������������
            Vector3 jump = new Vector3(0, jumpForce * Time.deltaTime, 0);//����Ծ�����ó�V3����
            collisonFlags = characterController.Move(jump);//���ý�ɫ���������ƶ���������ֻ�������������ƶ��γ���Ծ
            //  Debug.Log("collisonFlags:" + collisonFlags) ;
            // Debug.Log("characterController.isGrounded:" + characterController.isGrounded);

            /*�ж�����ڵ�����
           CollisionFlags:characterController ���õ���ײλ�ñ�ʶ��
           CollisionFlags.Below-->�ڵ�����
           */
            if (collisonFlags == CollisionFlags.Below)
            {
                isGround = true;
                jumpForce = -2f;
            }

            ///*�������ǲ����жϣ������ǰ����ʲô���������ͱ�ʾ���ڵ�����*/
            //if (isGround && collisonFlags == CollisionFlags.None)
            //{
            //    isGround = false;
            //}
        }
      
    

    }

    /// <summary>
    /// �ж������Ƿ�����¶�
    /// isCanCrouch==true  -->˵����������¶ף���ʱ������վ��
    /// isCanCrouch==false  -->˵�����ﲻ�����¶ף���ʱ�������¶�ͷ������ײ
    /// </summary>
    public void CanCrouch()
    {
        Vector3 sphereLocation = transform.position + Vector3.up * standHeight; //��ȡ����ͷ���ĸ߶�V3λ��    
        //����ͷ�����Ƿ������壬���ж��Ƿ���Խ����¶�
        isCanCrouch = (Physics.OverlapSphere(sphereLocation, characterController.radius, crouchLayerMask).Length) == 0;

       
    }



    /// <summary>
    /// �¶�
    /// </summary>
    public void Crouch(bool newCrouching)
    {
        if (!isCanCrouch) return;//�����¶ף��������״̬ʱ�����ܽ���������
        isCrouching = newCrouching;
        characterController.height = isCrouching ? crouchHeight : standHeight;//�����¶�״̬�����¶�ʱ��߶Ⱥ�վ���ĸ߶�
        characterController.center = characterController.height / 2.0f * Vector3.up;//����ɫ������������λ��Y����ͷ�����¼���1��ĸ߶�
    }

    /// <summary>
    /// �����ƶ���Ч
    /// </summary>
    public void PlayerFootSoundSet() {
        
        //�ڵ��沢���ƶ��ķ�������������0��˵���������ƶ�
        if (isGround && moveDirction.sqrMagnitude>0)
        {
            audioSource.clip = isRun ? runnningSound : walkingSound;
            if (!audioSource.isPlaying)
            {
                //�������߻��߱�����Ч
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
        //�¶�ʱ������������Ч
        if (isCrouching)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    /// <summary>
    /// ʰȡ����
    /// </summary>
    public void PickUpWeapon(int itemID,GameObject weapon) {
        /*��������������������ӣ������򲹳䱸��*/
        if (inventory.weapons.Contains(weapon))
        {
            weapon.GetComponent<Weapon_AutomaticGun>().bulletLeft = weapon.GetComponent<Weapon_AutomaticGun>().bulletMag * 5;
            weapon.GetComponent<Weapon_AutomaticGun>().UpdateAmmoUI();
            Debug.Log("�������Ѵ��ڴ�ǹе�����䱸��");
            return;
        }
        else
        {
            inventory.AddWeapon(weapon);
        }
    }

    /// <summary>
    /// �������ֵ
    /// </summary>
    /// <param name="damage">���յ��˺�ֵ</param>
    public void PlayerHealth(float damage)
    {
        playerHealth -= damage;
        isDamage = true;
        playerHealthUI.text = "����ֵ��" + playerHealth;
        if (playerHealth <= 0)
        {
            playerisDead = true;
            playerHealthUI.text = "�������";
            Time.timeScale = 0;//��Ϸ��ͣ
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
