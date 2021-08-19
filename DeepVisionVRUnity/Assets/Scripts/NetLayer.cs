using System.Collections.Generic;
using UnityEngine;

public abstract class NetLayer : MonoBehaviour
{
    protected List<GameObject> items = new List<GameObject>();

    public abstract void UpdateData(List<Texture2D> textureList, float scale, bool isRGB,  float zeroValue=0f);

    public abstract void ApplyScale(float newScale);

    public abstract float GetWidth(bool local = false);
}