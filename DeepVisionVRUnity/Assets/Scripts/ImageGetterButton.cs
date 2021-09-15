using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class ImageGetterButton : MonoBehaviour
{
	//public Button button;
	[SerializeField]
	private RawImage image;
	[SerializeField]
	private TextMeshProUGUI textMeshPro;
	[SerializeField]
	private bool canReturnImage = true;
	[SerializeField]
	private bool canAcceptDatasetImage = false;
	[SerializeField]
	private bool canAcceptActivationImage = false;
	[SerializeField]
	private bool canAcceptFeatureVisualizationImage = false;
	[SerializeField]
	private bool canAcceptNoiseImage = false;


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
				Vector2 wh = Helpers.SizeToMatchAspectRatioInSquare(value.tex);
				image.rectTransform.localScale = new Vector3(wh.x, wh.y, 1f);
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

	public void TaskOnClick()
	{
		XRBaseInteractable selectTarget = InteractableController.instance.Interactor.selectTarget;
		if (selectTarget != null) TaskOnClickWithInteractor(selectTarget);
	}


	private void TaskOnClickWithInteractor(XRBaseInteractable selectTarget)
	{
		ImageSelector interactionGun = selectTarget.gameObject.GetComponent<ImageSelector>();
		if (interactionGun != null)
		{
			if (canReturnImage) interactionGun.ActivationImageUsed = ActivationImageUsed;
			ActivationImage.Mode mode = interactionGun.ActivationImageUsed.mode;
			if ((mode == ActivationImage.Mode.DatasetImage && canAcceptDatasetImage) ||
				(mode == ActivationImage.Mode.Activation && canAcceptActivationImage) ||
				(mode == ActivationImage.Mode.FeatureVisualization && canAcceptFeatureVisualizationImage) ||
				(mode == ActivationImage.Mode.NoiseImage && canAcceptNoiseImage))
			{
				ActivationImageUsed = interactionGun.ActivationImageUsed;
			}
		}
	}
}
