using System.Collections.Generic;
using UnityEngine;

public abstract class NetLayer : MonoBehaviour
{
    protected string _name;
    protected List<GameObject> _nodes = new List<GameObject>();
    protected float gridsize;
    protected float margin;
    public float width;

    public abstract void updateData(List<Texture2D> textureList, float scale, float zeroValue);
}