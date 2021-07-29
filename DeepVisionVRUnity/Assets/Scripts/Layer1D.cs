using System.Collections.Generic;
using UnityEngine;

public class Layer1D : NetLayer
{
    private int _size;

    public void Prepare(GameObject layer1DParticleSystemPrefab, string name, int size)
    {
        _name = name;
        _size = size;
        gridsize = 0.05f;
        margin = 0.2f * gridsize;
        width = _size * gridsize - margin;
        Vector3 centerPos = new Vector3(0.5f * width, 0f, 0f);

        GameObject particleSystemInstance = (GameObject)Instantiate(layer1DParticleSystemPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), transform);
        particleSystemInstance.name = "layer1DParticleSystem";
        particleSystemInstance.transform.localPosition = -centerPos;
        ParticleSystem particleSystem = particleSystemInstance.GetComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.maxParticles = _size;
        _nodes.Add(particleSystemInstance);
    }

    public override void updateData(List<Texture2D> textureList, float scale, float zeroValue)
    {
        var particleSystem = _nodes[0].GetComponent<ParticleSystem>();
        var pixels = textureList[0].GetPixelData<Color32>(0);

        particleSystem.Clear();
        for (int i = 0; i < _size; i++)
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
}