using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
	public Slider levelSelectSlider;
	public TextMeshProUGUI levelLabel;
	public GameObject gameManagerObj;
	private GameManager gameManager;

	void Start()
	{
		// get the game manager component
		gameManager = gameManagerObj.GetComponent<GameManager>();
	}
	// display the current level that the slider is at
	public void OnValueChanged()
	{
		levelLabel.text = $"Level {levelSelectSlider.value}";
	}
	// start a new game when clicked
	public void OnStartClicked()
	{
		transform.GetComponent<Canvas>().enabled = false;
		gameManager.ResetGame((int)levelSelectSlider.value);
	}
	// display menu again, other half is in Game Manager.cs
	public void OnBackToMenuClicked()
	{
		transform.GetComponent<Canvas>().enabled = true;
	}
}
