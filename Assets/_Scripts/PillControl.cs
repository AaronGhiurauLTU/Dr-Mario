using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillControl : MonoBehaviour
{
    public KeyCode left;
    public KeyCode right;
	public KeyCode rotate;
	public KeyCode down;

	// minimum time between moving left or right
	public float horizontalCoolDown = 0.2f;

	// the cool down for shifting a pill horizontally a second time while holding it down to avoid moving multiple times per key tap
	public float horizontalCoolDownInitial = 0.3f;

	// the timer for when left or right was last pressed
	private float timeSinceHorizontalMove = 0;

	// the amount of times it was shifted left or right from holding down the key
	private int timesMovedInARow = 0;

	// current instance of the game manager
	private GameManager gameManager;

	void Start()
	{
		// find the instance of the game manager once for efficiency
		gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
	}
    void Update()
    {
		// update timer
		timeSinceHorizontalMove += Time.deltaTime;

		// only move pill if one key is pressed at a time
		if (Input.GetKey(left) ^ Input.GetKey(right))
		{
			KeyCode currentKey = Input.GetKey(left) ? left : right;

			float currentCoolDown;


			// set the cool down to 0 for the first press to allow quickly tapping the left/right
			if (timesMovedInARow == 0)
			{
				currentCoolDown = 0f;
			}
			else if (timesMovedInARow == 1)
			{
				/* set the cool down to the value set in the inspector so movement starts slow and doesn't accidentally move twice
				   per key press. This is the cool down for the second horizontal shift in a row */
				currentCoolDown = horizontalCoolDownInitial;
			}
			else
			{
				// make the normal for all following movements while its pressed down in the same direction
				currentCoolDown = horizontalCoolDown;
			}

			// call game manager to check if the pill can move if enough time passed based on the current multiplier
		    if (timeSinceHorizontalMove >= currentCoolDown) 
			{
				timesMovedInARow++;
				timeSinceHorizontalMove = 0;
				gameManager.MoveCurrentPillHorizontally(currentKey == left ? -1 : 1);				
			}
		}
		else
		{
			// reset if neither keys are pressed
			timesMovedInARow = 0;
		}

		if (Input.GetKey(down))
		{
			gameManager.DownHeld();
		}

		if (Input.GetKeyDown(rotate))
		{
			gameManager.RotateCurrentPill();
		}
    }
}