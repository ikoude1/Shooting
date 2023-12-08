using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// �������ת
/// ���������ת�������������ƶ�
/// �����������ת�������������ƶ�
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Tooltip("��Ұ������")] public float mouseSenstivity = 400f;
    private Transform playerBody;//��ҵ�λ��
    private float yRotation = 0f; //�����������ת����ֵ
    private CharacterController characterController;

    [Tooltip("��ǰ������ĳ�ʼ�߶�")] public float height = 1.8f;
    private float interpolationSpeed = 12f;//�߶ȱ仯��ƽ��ֵ




    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerBody = transform.GetComponentInParent<PlayerController>().transform;
        characterController = GetComponentInParent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        //���ﱣ֤����Ľ�ɫ������ʱ��ȡ���˵�
        if (characterController == null) return;

        float mouseX=Input.GetAxis("Mouse X") *mouseSenstivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSenstivity * Time.deltaTime;

        yRotation -= mouseY;//��������ת����ֵ�����ۼ�
        yRotation = Mathf.Clamp(yRotation,-60f,60f);
        transform.localRotation = Quaternion.Euler(yRotation,0f,0f);//�����������ת
        playerBody.Rotate(Vector3.up*mouseX);//���������ת

        /*�������¶׺�վ����ʱ������߶ȱ仯��������߶�ҲҪ�����仯*/
        float heightTarget=characterController.height * 0.9f;
        height = Mathf.Lerp(height,heightTarget,interpolationSpeed*Time.deltaTime);
        //�����¶�վ��ʱ���������߶�
        transform.localPosition = Vector3.up * height;

    }
}
