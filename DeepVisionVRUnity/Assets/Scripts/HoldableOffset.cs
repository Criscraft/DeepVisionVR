using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldableOffset : MonoBehaviour
{
    public Transform handOffset;
    public Transform grabOffset;
    void Start()
    {
        transform.localPosition = handOffset.localPosition + grabOffset.localPosition;
        transform.localRotation = handOffset.localRotation * grabOffset.localRotation;
    }
}
