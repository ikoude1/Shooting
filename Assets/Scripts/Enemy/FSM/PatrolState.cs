using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���˽���Ѳ��״̬
/// </summary>
public class PatrolState : EnemyBaseState
{
    public override void EnemyState(Enemy enemy)
    {
        enemy.animState = 0;
        //�������·��
        enemy.LoadPath(enemy.wayPointObj[ WayPointManager.Instance.usingIndex[enemy.nameIndex]] );

    }

    public override void OnUpdate(Enemy enemy)
    {
        //�ж� �����ǰidle�����Ѿ��������Ժ󣬲���ִ���ƶ� 
        if (!enemy.animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            enemy.animState = 1;//״̬ת��Ϊ1����ʾ�ƶ�
            enemy.MoveToTarget();
        }      
     

        //������˺͵�����ľ���
        float distance = Vector3.Distance(enemy.transform.position, enemy.wayPoints[enemy.index]);
        //�����С��ʱ���ʾ�Ѿ��ߵ��˵�����
        if (distance <= 0.5f)
        {
            enemy.animState = 0;
            enemy.animator.Play("idle");
            enemy.index++;//������1��������
            enemy.index = Mathf.Clamp(enemy.index, 0, enemy.wayPoints.Count - 1);
            //�����ٴ��жϵ��˺�Ѳ��·�������1��������ľ��룬��������С����ô��ǰ·���Ѿ����꣬�����õ������±꣬ʹ����������һ��
            if (Vector3.Distance(enemy.transform.position, enemy.wayPoints[enemy.wayPoints.Count - 1]) <= 0.5f)
            {
                enemy.index = 0;
            }
        }

        //�ж� ����Ѳ��ɨ�跶Χ�ڳ��ֵ��ˣ���ʱ���빥��״̬
        if (enemy.attackList.Count > 0)
        {
            enemy.TransitionToState(enemy.attackState);
        }

    }

   
}
