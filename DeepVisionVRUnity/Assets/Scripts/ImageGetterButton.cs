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
		//image.material = Instantiate(image.material);
	}


	public void LoadImage(int _imageIdx, string _className, Texture _tex, Material material = null)
    {
		imageIdx = _imageIdx;
		className = _className;
		if (textMeshPro != null) textMeshPro.text = className;
		//image.material.SetTexture("_MainTex", _tex);
		if (material != null) image.material = material;
		image.texture = (Texture2D)_tex;
	}


	public void TaskOnClick()
	{
		// Potential issue if both hands have an interaction gun
		XRBaseInteractable selectTarget = null;
		if (rightInteractor != null) selectTarget = rightInteractor.selectTarget;
		else if (leftInteractor != null) selectTarget = leftInteractor.selectTarget;
		
		if (canReturnImage) ReturnImageToInteractor(selectTarget);
		if (canAcceptImage) AcceptImage(selectTarget);
	}


	private void ReturnImageToInteractor(XRBaseInteractable selectTarget)
	{
		if (selectTarget == null)
		{
			return;
		}

		InteractionGun interactionGun = selectTarget.gameObject.GetComponent<InteractionGun>();
		if (interactionGun != null)
		{
			//Texture2D tex = TextureToTexture2D(image.texture);
			interactionGun.LoadImage(imageIdx, className, image.texture);
		}
	}


	public (int imageId, string className, Texture tex) GetImage()
	{
		return (imageIdx, className, image.texture);
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
}
