using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillControl : MonoBehaviour
{
    public KeyCode left;
    public KeyCode right;

	GameManager gameManager;

	void Start()
	{
		// find the instance of the game manager once for efficiency
		gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
	}
    void Update()
    {
		// don't allow moving the pill left or right when it first spawns in at y = 16
		if (transform.position.y > 15)
			return;

        if (Input.GetKeyDown(left))
        {
			gameManager.MoveCurrentPillHorizontally(-1);
        } 
        else if (Input.GetKeyDown(right))
        {
			gameManager.MoveCurrentPillHorizontally(1);
        }
    }
}
