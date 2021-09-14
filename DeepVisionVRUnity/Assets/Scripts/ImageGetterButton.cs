using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class ImageGetterButton : MonoBehaviour
{
	//public Button button;
	[SerializeField]
	private XRBaseInteractor rightInteractor;
	[SerializeField]
	private XRBaseInteractor leftInteractor;
	[SerializeField]
	private RawImage image;
	[SerializeField]
	private TextMeshProUGUI textMeshPro;
	[SerializeField]
	private bool canReturnImage = true;
	[SerializeField]
	private bool canAcceptImage = false;

	private ActivationImage activationImageUsed;
	public ActivationImage ActivationImageUsed
	{
		get
		{
			return this.activationImageUsed;
		}
		set
		{
			this.activationImageUsed = value;
			if (textMeshPro != null) textMeshPro.text = value.className;
			image.texture = (Texture2D)value.tex;
		}
	}

	public Material MaterialUsed
	{
		get
		{
			return this.image.material;
		}
		set
		{
			this.image.material = value;
		}
	}

	/*void Start()
	{
		Button btn = button.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);
	}*/

	public void Prepare(XRBaseInteractor _rightInteractor, XRBaseInteractor _leftInteractor)
	{
		rightInteractor = _rightInteractor;
		leftInteractor = _leftInteractor;
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
			interactionGun.ActivationImageUsed = ActivationImageUsed;
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
			ActivationImageUsed = interactionGun.ActivationImageUsed;
		}
	}
}
