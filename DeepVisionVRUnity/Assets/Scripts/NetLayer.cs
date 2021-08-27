using System.Collections.Generic;
using UnityEngine;

public abstract class NetLayer : MonoBehaviour
{
    protected List<GameObject> items = new List<GameObject>();

    public abstract void UpdateData(List<ActivationImage> activationImageList, float scale);

    public abstract void ApplyScale(float newScale);

    public abstract float GetWidth(bool local = false);
}