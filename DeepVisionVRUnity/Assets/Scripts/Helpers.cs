using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Helpers
{
    /*
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
    */

    public static Vector2 SizeToMatchAspectRatioInCanvas(Texture tex, RectTransform canvasRectTransform)
    {
        Vector3[] fourCornersArray = new Vector3[4];
        canvasRectTransform.GetLocalCorners(fourCornersArray);
        float canvasWidth = Mathf.Abs(fourCornersArray[0].x - fourCornersArray[3].x);
        float canvasHeight = Mathf.Abs(fourCornersArray[0].y - fourCornersArray[1].y);
        float aspectRatioCanvas = canvasWidth / canvasHeight;

        float aspectRatioTex = tex.width / (float)tex.height;


        float w = 0, h = 0;
        if (aspectRatioTex < aspectRatioCanvas)
        {
            // texture is thinner than constraints
            h = canvasHeight;
            w = h * aspectRatioTex;
        }
        else
        {
            // texture is broader than constraints
            w = canvasWidth;
            h = w / aspectRatioTex;
        }

        w = w / canvasWidth;
        h = h / canvasHeight;

        return new Vector2(w, h);
    }


    public static Vector2 SizeToMatchAspectRatioInSquare(Texture tex)
    {
        float aspectRatioTex = tex.width / (float)tex.height;

        float w = 0f, h = 0f;
        if (aspectRatioTex < 1f)
        {
            // texture is thinner than square
            h = 1f; 
            w = aspectRatioTex;
            
        }
        else
        {
            // texture is broader than constraints
            w = 1f; 
            h = 1f / aspectRatioTex;
        }

        return new Vector2(w, h);
    }
}


