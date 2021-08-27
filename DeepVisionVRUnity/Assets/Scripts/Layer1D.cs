using System.Collections.Generic;
using UnityEngine;

public class Layer1D : NetLayer
{

    float gridsize;
    float margin;
    int size;


    public override float GetWidth(bool local = false)
    {
        return (float)size * gridsize - margin;
    }


    public void Prepare(GameObject layer1DParticleSystemPrefab, int _size)
    {
        size = _size;
        gridsize = 0.05f;
        margin = 0.2f * gridsize;
        Vector3 centerPos = new Vector3(0.5f * GetWidth(), 0f, 0f);

        GameObject particleSystemInstance = (GameObject)Instantiate(layer1DParticleSystemPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), transform);
        particleSystemInstance.name = "layer1DParticleSystem";
        particleSystemInstance.transform.localPosition = -centerPos;
        ParticleSystem particleSystem = particleSystemInstance.GetComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.maxParticles = size;
        items.Add(particleSystemInstance);
    }


    public override void UpdateData(List<ActivationImage> activationImageList, float scale)
    {
        var particleSystem = items[0].GetComponent<ParticleSystem>();
        var pixels = ((Texture2D)activationImageList[0].tex).GetPixelData<Color32>(0);

        particleSystem.Clear();
        for (int i = 0; i < size; i++)
        {
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.startSize = scale * (gridsize - margin);
            emitParams.position = new Vector3(scale * gridsize * i + 0.5f * scale * (gridsize - margin), 0.5f * scale * (gridsize - margin), 0f);
            //emitParams.startColor = new Color32(pixels[i][1], pixels[i][2], pixels[i][3], pixels[i][0]);
            byte colorComponent = (byte)(180f / 255f * pixels[i][1]);
            //byte colorComponent = pixels[i][1];
            emitParams.startColor = new Color32(pixels[i][1], colorComponent, colorComponent, pixels[i][0]);
            particleSystem.Emit(emitParams, 1);
        }
    }


    public override void ApplyScale(float newScale)
    {
        float oldScale = transform.localScale.x;
        transform.localScale = new Vector3(newScale * oldScale, newScale * oldScale, newScale * oldScale);
    }
}