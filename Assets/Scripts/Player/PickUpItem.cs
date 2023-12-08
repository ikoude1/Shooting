using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����ʰȡ
/// </summary>
public class PickUpItem : MonoBehaviour
{
    [Tooltip("������ת���ٶ�")]private float rotateSpeed;
    [Tooltip("�������")] public int itemID;
    private GameObject weaponModel;

    // Start is called before the first frame update
    void Start()
    {
        rotateSpeed = 100f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles += new Vector3(0,rotateSpeed*Time.deltaTime,0);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerController player = other.GetComponent<PlayerController>();
            //���һ�ȡ Inventory �����µĸ�����������
            weaponModel = GameObject.Find("Player/Assault_Rifle_Arm/Inventory/").gameObject.transform.GetChild(itemID).gameObject;
            player.PickUpWeapon(itemID, weaponModel);
            Destroy(gameObject);
        }
    }


}
