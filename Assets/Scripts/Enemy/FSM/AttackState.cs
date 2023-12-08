using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���˽��빥��״̬
/// </summary>
public class AttackState : EnemyBaseState
{
    public override void EnemyState(Enemy enemy)
    {
        enemy.animState = 2;
        enemy.targetPoint = enemy.attackList[0];
    }

    public override void OnUpdate(Enemy enemy)
    {
        //��ǰ����û��Ŀ�꣬��ʱ�����л�ΪѲ��״̬
        if (enemy.attackList.Count == 0)
        {
            enemy.TransitionToState(enemy.patrolState);
        }

        //��ǰ������Ŀ�꣬�����ܻ����ڶ��Ŀ��ʱ��Ҫ�Ҿ���������Ǹ�
        if (enemy.attackList.Count > 1)
        {
            for (int i = 0; i < enemy.attackList.Count; i++)
            {
                //�жϣ����˺͹����б���Ķ��Ŀ������   ����  ���˺͵�1��Ŀ������  ҪС
                //˵����i��Ŀ��ľ�������˸�Զ�����ٴθ��µ���Ŀ��
                if (Mathf.Abs(enemy.transform.position.x - enemy.attackList[i].position.x) <
                    Mathf.Abs(enemy.transform.position.x - enemy.targetPoint.position.x))
                {
                    enemy.targetPoint = enemy.attackList[i];
                }
            }
        }

        //������ֻ��1������Ŀ��ʱ����ֻ��list��ĵ�һ��
        if (enemy.attackList.Count == 1)
        {
            enemy.targetPoint = enemy.attackList[0];
        }



        //������ֻ��1������Ŀ��ʱ����ֻ��list��ĵ�һ��
        if (enemy.attackList.Count == 1)
        {
            enemy.targetPoint = enemy.attackList[0];
        }


        //���˹������
        if (enemy.targetPoint.tag == "Player")
        {
            //����Ҫ����ҽ��й���
            enemy.AttackAction();
        }

        enemy.MoveToTarget();
    }


}
