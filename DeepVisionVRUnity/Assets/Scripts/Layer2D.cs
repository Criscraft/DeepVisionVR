using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Layer2D : NetLayer
{
    public RectTransform featureMaps;
    public GameObject channel2DPrefab;
    public Transform horizontalShift;
    public Material material;

    public void Prepare(Vector3Int size, Camera mainCamera)
    {
        transform.GetComponent<Canvas>().worldCamera = mainCamera;
        for (int i = 0; i < size[0]; i++)
        {
            Transform newChannel2DInstance = ((GameObject)Instantiate(channel2DPrefab)).transform;
            newChannel2DInstance.name = "channel2D " + string.Format("{0}", i);
            newChannel2DInstance.SetParent(featureMaps);
            newChannel2DInstance.localPosition = Vector3.zero;
            newChannel2DInstance.localRotation = Quaternion.identity;
            newChannel2DInstance.localScale = Vector3.one;
            var image = newChannel2DInstance.GetComponent<RawImage>();
            image.material = Instantiate(material);
            items.Add(newChannel2DInstance.gameObject);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
    }


    public override float GetWidth(bool local=false)
    {
        Vector3[] fourCornersArray = new Vector3[4];
        featureMaps.GetLocalCorners(fourCornersArray);
        float width = Mathf.Abs(fourCornersArray[0].x - fourCornersArray[3].x) * featureMaps.localScale.x;
        if (!local) width *= transform.localScale.x;
        return width;
    }

    public override void UpdateData(List<Texture2D>textureList, float scale, float zeroValue)
    {
        RawImage image;

        for (int i = 0; i < items.Count; i++)
        {
            image = items[i].GetComponent<RawImage>();
            image.texture = textureList[i];
            image.material.SetFloat("_TransitionValue", zeroValue / 255f);
            image.material.SetTexture("_MainTex", textureList[i]);
            //image.SetNativeSize(); // pixel perfect
        }
    }

    public override void ApplyScale(float newScale)
    {
        float scale = featureMaps.localScale.x * newScale;
        featureMaps.localScale = new Vector3(scale, scale, scale);
        LayoutRebuilder.ForceRebuildLayoutImmediate(featureMaps);
        Center();
    }

    public void Center()
    {
        horizontalShift.localPosition = new Vector3(- 0.5f * GetWidth(true), 0f, 0f);
    }
}