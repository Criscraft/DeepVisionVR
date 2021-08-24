using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class ImageGetterButton : MonoBehaviour
{
	private int imageIdx = -1;
	private int label;
	private string className;
	//public Button button;
	private XRBaseInteractor rightInteractor;
	private XRBaseInteractor leftInteractor;
	[SerializeField]
	private RawImage image;

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


	public void LoadImage(int _imageIdx, int _label, string _className, Texture2D _tex)
    {
		imageIdx = _imageIdx;
		label = _label;
		className = _className;
		image.material.SetTexture("_MainTex", _tex);
	}


	public void TaskOnClick()
	{
		XRBaseInteractable selectTarget;
		if (rightInteractor != null)
		{
			selectTarget = rightInteractor.selectTarget;
			TransferImage(selectTarget);
		}

		if (leftInteractor != null)
		{
			selectTarget = leftInteractor.selectTarget;
			TransferImage(selectTarget);
		}
	}


	private void TransferImage(XRBaseInteractable selectTarget)
    {
		if (selectTarget == null)
        {
			return;
        }

			InteractionGun interactionGun = selectTarget.gameObject.GetComponent<InteractionGun>();
		if (interactionGun != null)
		{
			interactionGun.LoadImage(imageIdx, (Texture2D)image.texture, label, className);
		}
	}
}
