using System.Collections.Generic;
using UnityEngine;

public class Layer2D : NetLayer
{
    private Vector3Int _size;

    public void Prepare(GameObject node2DPrefab, Vector2 pos, string name, Vector3Int size)
    {
        _pos = pos;
        _name = name;
        _size = size;
        gridsize = 0.2f;
        margin = 0.2f * gridsize;
        int n_grid_x = (int)Mathf.Ceil(Mathf.Sqrt(_size[0]));
        width = (float)n_grid_x * gridsize - margin;
        Vector3 center_pos = new Vector3(0.5f * width, 0f, 0f);

        for (int i = 0; i < _size[0]; i++)
        {
            int grid_y = i / n_grid_x;
            int grid_x = i % n_grid_x;
            
            GameObject newnetPlaneInstance = (GameObject)Instantiate(node2DPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f), transform);
            newnetPlaneInstance.name = "netPlane " + string.Format("{0}", i);
            newnetPlaneInstance.transform.localPosition = new Vector3(gridsize * grid_x + 0.5f * (gridsize - margin), gridsize * grid_y + 0.5f * (gridsize - margin), 0f) - center_pos;
            newnetPlaneInstance.transform.localScale = new Vector3(gridsize - margin, gridsize - margin, gridsize - margin);
            _nodes.Add(newnetPlaneInstance);
        }
    }

    public override void updateData(List<Texture2D>textureList, float scale, float zeroValue)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            Renderer m_Renderer = _nodes[i].GetComponent<Renderer>();
            m_Renderer.material.SetTexture("_MainTex", textureList[i]);
            m_Renderer.material.SetFloat("_TransitionValue", zeroValue / 255f);
        }
    }
}