using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillControl : MonoBehaviour
{
    public KeyCode left;
    public KeyCode right;
	public KeyCode rotate;
	// minimum time between moving left or right
	public float horizontalCoolDown = 0.2f;
	public float coolDownInitialMultiplier = 2.5f;
	private float timeSinceHorizontalMove = 0;
	private int timesMovedInARow = 0;
	private float coolDownMultiplier = 1;
	private KeyCode lastHorizontalKey;
	GameManager gameManager;

	void Start()
	{
		// find the instance of the game manager once for efficiency
		gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
	}
    void Update()
    {
		timeSinceHorizontalMove += Time.deltaTime;

		if (Input.GetKey(left) || Input.GetKey(right))
		{
			KeyCode currentKey = Input.GetKey(left) ? left : right;

			if (lastHorizontalKey != currentKey)
				timesMovedInARow = 0;

			if (timesMovedInARow == 0)
				coolDownMultiplier = 0f;
			else if (timesMovedInARow == 1)
				coolDownMultiplier = coolDownInitialMultiplier;
			else
				coolDownMultiplier = 1;

		    if (timeSinceHorizontalMove >= horizontalCoolDown * coolDownMultiplier) 
			{
				timesMovedInARow++;
				timeSinceHorizontalMove = 0;
				lastHorizontalKey = currentKey;
				gameManager.MoveCurrentPillHorizontally(lastHorizontalKey == left ? -1 : 1);				
			}
		}
		else
		{
			timesMovedInARow = 0;
		}

		if (Input.GetKeyDown(rotate))
		{
			gameManager.RotateCurrentPill();
		}
    }
}
