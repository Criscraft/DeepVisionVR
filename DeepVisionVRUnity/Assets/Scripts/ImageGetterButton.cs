using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class ImageGetterButton : MonoBehaviour
{
	private int imageIdx = -1;
	private string className;
	//public Button button;
	private XRBaseInteractor rightInteractor;
	private XRBaseInteractor leftInteractor;
	[SerializeField]
	private RawImage image;
	[SerializeField]
	private TextMeshProUGUI textMeshPro;
	[SerializeField]
	private bool canReturnImage = true;
	[SerializeField]
	private bool canAcceptImage = false;

	/*void Start()
	{
		Button btn = button.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
	}*/

	public void Prepare(XRBaseInteractor _rightInteractor, XRBaseInteractor _leftInteractor)
	{
		rightInteractor = _rightInteractor;
		leftInteractor = _leftInteractor;
		image.material = Instantiate(image.material); // Prevent that all images use the same material (and image)
	}


	public void LoadImage(int _imageIdx, string _className, Texture _tex)
    {
		imageIdx = _imageIdx;
		className = _className;
		textMeshPro.text = className;
		image.material.SetTexture("_MainTex", _tex);
		//image.texture = _tex; // has no effect
	}


	public void TaskOnClick()
	{
		// Potential issue if both hands have an interaction gun
		XRBaseInteractable selectTarget = null;
		if (rightInteractor != null) selectTarget = rightInteractor.selectTarget;
		else if (leftInteractor != null) selectTarget = leftInteractor.selectTarget;
		
		if (canReturnImage) ReturnImage(selectTarget);
		if (canAcceptImage) AcceptImage(selectTarget);
	}


	private void ReturnImage(XRBaseInteractable selectTarget)
    {
		if (selectTarget == null)
        {
			return;
        }

			InteractionGun interactionGun = selectTarget.gameObject.GetComponent<InteractionGun>();
		if (interactionGun != null)
		{
			//Texture2D tex = TextureToTexture2D(image.texture);
			interactionGun.LoadImage(imageIdx, image.material.GetTexture("_MainTex"), className);
		}
	}


	private void AcceptImage(XRBaseInteractable selectTarget)
    {
		if (selectTarget == null)
        {
			return;
        }

			InteractionGun interactionGun = selectTarget.gameObject.GetComponent<InteractionGun>();
		if (interactionGun != null)
		{
			(int _imageId, string _className, Texture _tex) = interactionGun.GetImage();
			LoadImage(_imageId, _className, _tex);
		}
	}


/*
	private Texture2D TextureToTexture2D(Texture texture) 
	{
		Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
		RenderTexture.active = rTex;
		texture2D.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
		texture2D.Apply();
		return texture2D;
	}
*/
}
