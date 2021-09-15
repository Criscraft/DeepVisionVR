using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class Dataset : MonoBehaviour
{
    [SerializeField]
    private GameObject imageGetterButtonPrefab;
    [SerializeField]
    private Transform datasetContent;
    private DLWebClient dlClient;
    private int datasetID = -1;
    private int lenDataset = 0;
    private JArray classNames;

    public void RequestDatasetImages()
    {
        dlClient.RequestDatasetImages(AcceptDatasetImages, datasetID);
    }


    public IEnumerator AcceptDatasetImages(JObject jObject)
    {
        //nClasses = (int)jObject["n_classes"];
        lenDataset = (int)jObject["len"];
        classNames = (JArray)jObject["class_names"];
        JArray labelIDs = (JArray)jObject["label_ids"];
        for (int i = 0; i < lenDataset; i++)
        {
            CreateDatasetImage(DLWebClient.StringToTex((string)jObject["tensors"][i]), (string)classNames[(int)labelIDs[i]], i);
        }
        yield return null;
    }


    private void CreateDatasetImage(Texture2D tex, string className, int imgIndex)
    {
        ActivationImage activationImage = new ActivationImage();
        activationImage.datasetID = datasetID;
        activationImage.imageID = imgIndex;
        activationImage.isRGB = true;
        activationImage.mode = ActivationImage.Mode.DatasetImage;
        activationImage.className = className;
        activationImage.tex = tex;
        activationImage.nDim = 2;

        GameObject newImageGetterButton = (GameObject)Instantiate(imageGetterButtonPrefab, Vector3.zero, Quaternion.identity);
        newImageGetterButton.name = "ImageGetterButton " + string.Format("{0}", imgIndex);
        newImageGetterButton.transform.SetParent(datasetContent, false);
        var imageGetterButtonScript = newImageGetterButton.GetComponent<ImageGetterButton>();
        imageGetterButtonScript.ActivationImageUsed = activationImage;
    }


    public void Prepare(DLWebClient _dlClient, int _datasetID)
    {
        dlClient = _dlClient;
        datasetID = _datasetID;
    }


    public void BuildDataset()
    {
        Debug.Log("Begin RequestDatasetImages");
        RequestDatasetImages();
    }
}
