using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FeatureVisualizationButton : MonoBehaviour
{
	private DLManager dlManager;
	[SerializeField]
	private ImageGetterButton imageGetterButton;



	public void Prepare(DLManager _dlManager)
	{
		dlManager = _dlManager;
	}


	public void TaskOnClick()
	{
		XRBaseInteractable selectTarget = InteractableController.instance.Interactor.selectTarget;
		if (selectTarget != null) TaskOnClickWithInteractor(selectTarget);
	}


	private void TaskOnClickWithInteractor(XRBaseInteractable selectTarget)
	{
		FeatureVisualizer featureVisualizer = selectTarget.gameObject.GetComponent<FeatureVisualizer>();
		if (featureVisualizer != null)
		{
			dlManager.RequestLayerFeatureVisualization(imageGetterButton.ActivationImageUsed.layerID);
			dlManager.SetLoading(imageGetterButton.ActivationImageUsed.layerID);
		}
	}
}
