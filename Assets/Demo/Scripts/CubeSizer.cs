using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputNew;
using UnityEngine.Serialization;

// This sizer does not use the UI event system,
// since it's not integrated with the Input System prototype at this point.
// UI is only used for graphics.

public class CubeSizer : MonoBehaviour
{
	FirstPersonControls m_PlayerInput;
	
	[FormerlySerializedAs("controlMap")]
	public ActionMap actionMap;
	public FirstPersonControls referencePlayerInput;
	
	[Space(10)]
	public GameObject menu;
	public Slider slider;
	
	public float size { get { return slider.value; } }
	
	public void OpenMenu()
	{
		enabled = true;
		menu.SetActive(true);
		m_PlayerInput = InputSystem.CreatePlayer<FirstPersonControls>(actionMap, referencePlayerInput);
		m_PlayerInput.Activate();
	}
	
	public void CloseMenu()
	{
		m_PlayerInput.Deactivate();
		menu.SetActive(false);
		enabled = false;
	}
	
	public void ToggleMenu()
	{
		if (enabled)
			CloseMenu();
		else
			OpenMenu();
	}
	
	void Update()
	{
		slider.value += m_PlayerInput.moveX.value * 0.05f;
		if (m_PlayerInput.menu.buttonDown)
			ToggleMenu();
	}
}