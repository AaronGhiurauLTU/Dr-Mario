using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
	public Slider levelSelectSlider;
	public TextMeshProUGUI levelLabel;
	public Button lowButton, medButton, hiButton;

	// colors used for the speed buttons
	public Material red, blue, yellow;
	public GameObject gameManagerObj;
	
	// the amount the R, G, and B values of the currently selected speed button gets darkened by to indicate that it was selected
	public float darkenBy = 0.15f;
	private GameManager gameManager;

	// default speed is medium
	private string speed = "MED";

	void Start()
	{
		// get the game manager component
		gameManager = gameManagerObj.GetComponent<GameManager>();

		// make the speed select buttons the same color as the materials with medium being darker since it is the default selection
		lowButton.GetComponent<Image>().color = blue.color;
		medButton.GetComponent<Image>().color = darkenColor(yellow.color);
		hiButton.GetComponent<Image>().color = red.color;
	}
	private Color darkenColor(Color color)
	{
		return new Color(color.r - darkenBy, color.g - darkenBy, color.b - darkenBy);
	}
	// display the current level that the slider is at
	public void OnValueChanged()
	{
		levelLabel.text = $"Level {levelSelectSlider.value}";
	}
	// indicate what speed button was clicked and store the selected speed
	public void OnSpeedClicked(string speed)
	{
		this.speed = speed;

		// reset the color of every button
		lowButton.GetComponent<Image>().color = blue.color;
		medButton.GetComponent<Image>().color = yellow.color;
		hiButton.GetComponent<Image>().color = red.color;

		// darken the selected button
		switch (speed)
		{
			case "LOW":
				lowButton.GetComponent<Image>().color = darkenColor(blue.color);
				break;
			case "MED":
				medButton.GetComponent<Image>().color = darkenColor(yellow.color);
				break;
			case "HI":
				hiButton.GetComponent<Image>().color = darkenColor(red.color);
				break;
		}
	}
	// start a new game when clicked
	public void OnStartClicked()
	{
		transform.GetComponent<Canvas>().enabled = false;
		gameManager.ResetGame((int)levelSelectSlider.value, speed);
	}
	// display menu again, other half is in Game Manager.cs
	public void OnBackToMenuClicked()
	{
		transform.GetComponent<Canvas>().enabled = true;
	}
}
