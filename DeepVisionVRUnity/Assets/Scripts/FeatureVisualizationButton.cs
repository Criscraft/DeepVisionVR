using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FeatureVisualizationButton : MonoBehaviour
{
	private DLManager dlManager;
	[SerializeField]
	private ImageGetterButton imageGetterButton;
	[SerializeField]
	private XRBaseInteractor rightInteractor;
	[SerializeField]
	private XRBaseInteractor leftInteractor;



	public void Prepare(XRBaseInteractor _rightInteractor, XRBaseInteractor _leftInteractor, DLManager _dlManager)
	{
		rightInteractor = _rightInteractor;
		leftInteractor = _leftInteractor;
		dlManager = _dlManager;
	}


	public void TaskOnClick()
	{
		Debug.Log("Click!");
		// Potential issue if both hands have an interaction gun
		XRBaseInteractable selectTarget = null;
		if (rightInteractor != null) selectTarget = rightInteractor.selectTarget;
		else if (leftInteractor != null) selectTarget = leftInteractor.selectTarget;

		if (selectTarget == null)
		{
			return;
		}

		FeatureVisualizer featureVisualizer = selectTarget.gameObject.GetComponent<FeatureVisualizer>();
		if (featureVisualizer != null)
		{
			dlManager.RequestLayerFeatureVisualization(imageGetterButton.ActivationImageUsed.layerID);
			dlManager.SetLoading(imageGetterButton.ActivationImageUsed.layerID);
		}
		
	}
}
