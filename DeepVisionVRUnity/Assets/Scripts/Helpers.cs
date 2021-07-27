using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers
{
    static public Bounds RecursiveMeshBB(GameObject go)
    {
        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();

        if (mfs.Length > 0)
        {
            Bounds b = mfs[0].mesh.bounds;
            for (int i = 1; i < mfs.Length; i++)
            {
                b.Encapsulate(mfs[i].mesh.bounds);
            }
            return b;
        }
        else
            return new Bounds();
    }
}


