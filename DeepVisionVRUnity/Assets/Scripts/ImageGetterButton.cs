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
	private Texture2D tex;

	/*void Start()
	{
		Button btn = button.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
	}*/


	public void Prepare(XRBaseInteractor _rightInteractor, XRBaseInteractor _leftInteractor, int _imageIdx, int _label, string _className, Texture2D _tex)
    {
		rightInteractor = _rightInteractor;
		leftInteractor = _leftInteractor;
		imageIdx = _imageIdx;
		label = _label;
		className = _className;
		tex = _tex;
		Sprite mySprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
		GetComponent<Button>().image.sprite = mySprite;
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
			interactionGun.LoadImage(imageIdx, tex, label, className);
		}
	}
}
