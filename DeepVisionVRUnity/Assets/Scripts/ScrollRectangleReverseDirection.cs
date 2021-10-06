using UnityEngine;
using UnityEngine.UI;

public class ScrollRectangleReverseDirection : MonoBehaviour
{
    [SerializeField]
    RectTransform content;
    [SerializeField]
    Scrollbar verticalScollBar;

    public void OnScrollbarChanged(float value)
    {
        value = Mathf.Clamp(value, 0, 1);
        verticalScollBar.value = value;
        float height = content.sizeDelta.y;
        content.localPosition = new Vector3(content.localPosition.x, -(int)(height * value), content.localPosition.z);
    }

    
}
