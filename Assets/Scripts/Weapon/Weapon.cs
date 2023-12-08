using System.Collections;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public abstract void GunFire();


    public abstract void ExpandingCrossUpdate(float expanDegree);

    public abstract void DoReloadAnimation();

    public abstract void Reload();

    public abstract void AimIn();
    public abstract void AimOut();


}

