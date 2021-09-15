using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FeatureVisualizationButton : MonoBehaviour
{
	private DLNetwork dlNetwork;
	[SerializeField]
	private ImageGetterButton imageGetterButton;



	public void Prepare(DLNetwork _dlNetwork)
	{
		dlNetwork = _dlNetwork;
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
			dlNetwork.RequestLayerFeatureVisualization(imageGetterButton.ActivationImageUsed.layerID);
			dlNetwork.SetLoading(imageGetterButton.ActivationImageUsed.layerID);
		}
	}
}
