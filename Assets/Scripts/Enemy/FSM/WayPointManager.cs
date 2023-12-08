using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ÿ�����˷��䲻ͬ·��
/// </summary>
public class WayPointManager : MonoBehaviour
{

    private static WayPointManager _instance;

    /*���Է�װ*/
    public static WayPointManager Instance
    {
        get
        {
            return _instance;
        }
    }
    //��2��list������ɲ�ͬ·�߸���������ˣ�����ֹ���˳�����ͬһ��·�ߵ����
    //�൱�ڸ�ÿ�����˷��䲻ͬ��·��ID
    public List<int> usingIndex = new List<int>();//ÿ�����˷����õ���·��ID
    public List<int> rawIndex = new List<int>();//������list����0��1��2���ң����·���

    private void Awake()
    {
        _instance = this;
        //����·��ID
        int tempCount=rawIndex.Count;
        for (int i = 0; i < tempCount; i++) {
            //0-99  ����ҿ�[0,100)
            int tempIndex = Random.Range(0, rawIndex.Count);  //���3��·�ߵ�λ��
            usingIndex.Add(rawIndex[tempIndex]);//����·��
            rawIndex.RemoveAt(tempIndex);//����·��֮��ɾ����ţ���ֹ�ظ���
        }
    }


}
