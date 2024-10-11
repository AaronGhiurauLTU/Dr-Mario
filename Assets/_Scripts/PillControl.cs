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

	// the cool down multiplier for shifting a pill horizontally a second time while holding it down
	public float horizontalCoolDownInitialMultiplier = 2.5f;

	// the timer for when left or right was last pressed
	private float timeSinceHorizontalMove = 0;

	// the amount of times it was shifted left or right from holding down the key
	private int timesMovedInARow = 0;

	// the current multiplier for the horizontal cool down
	private float horizontalCoolDownMultiplier = 1;

	// the last horizontal key presses (either left or right)
	private KeyCode lastHorizontalKey;

	// current instance of the game manager
	GameManager gameManager;

	void Start()
	{
		// find the instance of the game manager once for efficiency
		gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
	}
    void Update()
    {
		// update timer
		timeSinceHorizontalMove += Time.deltaTime;

		if (Input.GetKey(left) || Input.GetKey(right))
		{
			KeyCode currentKey = Input.GetKey(left) ? left : right;

			// reset times moved in a row if the direction was changed
			if (lastHorizontalKey != currentKey)
				timesMovedInARow = 0;

			// set the multiplier to 0 for the first press to allow quickly tapping the left/right
			if (timesMovedInARow == 0)
			{
				horizontalCoolDownMultiplier = 0f;
			}
			else if (timesMovedInARow == 1)
			{
				/* set the multiplier to the value set in the inspector so movement starts slow and doesn't accidentally move twice
				   per key press. This is the cool down multiplier for the second horizontal shift in a row */
				horizontalCoolDownMultiplier = horizontalCoolDownInitialMultiplier;
			}
			else
			{
				// make the multiplier 1 to act normal for all following movements while its pressed down in the same direction
				horizontalCoolDownMultiplier = 1;
			}

			// call game manager to check if the pill can move if enough time passed based on the current multiplier
		    if (timeSinceHorizontalMove >= horizontalCoolDown * horizontalCoolDownMultiplier) 
			{
				timesMovedInARow++;
				timeSinceHorizontalMove = 0;
				lastHorizontalKey = currentKey;
				gameManager.MoveCurrentPillHorizontally(lastHorizontalKey == left ? -1 : 1);				
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
