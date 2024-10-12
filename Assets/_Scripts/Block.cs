using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


// stores information about the position and color while providing methods to check for matching adjacent blocks
public class Block
{
	// stores the blocks at each position in this 2D array, shared across all instances of this class
	public static Block[,] gameBoard;
	
	// current score of player
	protected static int score;

	// this set contains all blocks that are able to fall at some point (everything but viruses)
	public static HashSet<Block> blocksThatMayFall;

	// this set contains all blocks that have fallen during the last gravity interval
	public static HashSet<Block> stillFalling;

	// the game object that new blocks are placed in
	public static GameObject blockFolder;

	// the block prefab
	public static GameObject blockPrefab;

	// the current instance of the game manager script
	public static GameManager gameManager;

	// the possible colors a block can be
	public enum Color
	{
		Red,
		Yellow,
		Blue,
		Grey
	}
	// the coordinates of the block
	public int x, y;

	// the current color of the block
	protected Color color;

	// the game object of this block
	protected GameObject part;

	// the mesh renderer component that takes the material to show the color
	protected MeshRenderer meshRenderer;

	// only allow publicly getting the color but not setting it as blocks in general will not need to change color
	public Color GetColor
	{
		get { return color; }
	}
	// because the top of a pill is allowed to be over the game board, only set the value if the y value is within the board
	public void SetBoardValue(int x, int y, Block value)
	{
		if (y < 16)
			gameBoard[x, y] = value;
	}
	/* get the horizontal and vertical set of matches and forward them to the clear blocks method to determine which ones are large enough
	   this method gets overridden in the pill half, returns all matches that were large enough to be cleared */
	public virtual HashSet<Block> CheckMatchesToClear()
	{
		SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches);

		HashSet<Block> matches = new();

		// add the sets only if they are large enough
		if (horizontalMatches.Count >= 4)
			matches.UnionWith(horizontalMatches);
		if (verticalMatches.Count >= 4)
			matches.UnionWith(verticalMatches);

		return matches;
	}
	// destroy the current block, parameter is simply for helper functions, particularly in overridden versions of this method
	public virtual void Clear(HashSet<Block> blocksToClear)
	{
		DestroyBlock();
	}
	// destroy the block, ensuring it is not in the game board and in the blocks that may fall set
	protected void DestroyBlock()
	{
		SetBoardValue(x, y, null);

		if (part != null)
			Object.Destroy(part);

		part = null;
		blocksThatMayFall.Remove(this);
	}
	// the out variables are sets of all the positions of the same color in a row horizontally or vertically
	public void SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches)
	{
		// start both sets with the current part that this function was called from (we'll call it the root block) so the count represents how many in a row
		horizontalMatches = new() { this };
		verticalMatches = new() { this };

		// the direction to check for matches
		Vector2 checkingDirection = Vector2.left;

		// append the result of checking the color in that certain direction
		horizontalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));

		checkingDirection = Vector2.right;
		horizontalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));

		checkingDirection = Vector2.down;
		verticalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));

		checkingDirection = Vector2.up;
		verticalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));
	}
	/* this returns the set of all blocks with the same color in the direction until a different color, empty space, 
	   or edge of game board is reached, this is a helper function for SameColorInARow */
	private HashSet<Block> CheckColorsInDirection(Vector2 direction, Color color)
	{
		// the coordinate of the current block being checked
		Vector2 currentCoordinate = new Vector2(x, y) + direction;

		// the set that will be returned
		HashSet<Block> matches = new();

		// ensure the coordinates are integers for the array
		int currentX = (int)currentCoordinate.x;
		int currentY = (int)currentCoordinate.y;

		// loop until the edge of the game board is reached
		while (currentX >= 0 && currentX < 8 && currentY >= 0 && currentY < 16)
		{
			Block checkingBlock = gameBoard[currentX, currentY];

			// if a block is there and it has the same color as the root, add this part to the return set
			if (checkingBlock != null && checkingBlock.GetColor == color)
			{
				matches.Add(checkingBlock);
			}
			else
			{
				// since the next position didn't contain a matching block, break out of the loop since there is no need to check further
				break;
			}
			// check the next position in that direction
			currentCoordinate += direction;
			currentX = (int)currentCoordinate.x;
			currentY = (int)currentCoordinate.y;
		}

		return matches;
	}
	// sets the color of the mesh renderer if it exists
	protected void SetColor(Color color)
	{
		// skip setting anything if a mesh renderer isn't set (used for derived classes)
		if (meshRenderer == null)
			return;

		switch (color)
		{
			case Color.Red:
				meshRenderer.material = gameManager.red;
				break;
			case Color.Yellow:
				meshRenderer.material = gameManager.yellow;
				break;
			case Color.Blue:
				meshRenderer.material = gameManager.blue;
				break;
			case Color.Grey:
				meshRenderer.material = gameManager.grey;
				break;
		}

		this.color = color;
	}
	/* overridable, falls until it reaches the bottom or a block is directly below it and returns true if it fell
	   the out variable is false in most cases, but is true when the block should not be checked for matches (pills splitting) */
	public virtual bool Fall(out bool doNotCheck)
	{
		doNotCheck = false;
		// check if it has room to fall down
		if (y > 0 && gameBoard[x, y - 1] == null)
		{
			// lower block by one
			gameBoard[x, y] = null;

			y--;

			gameBoard[x, y] = this;

			part.transform.position += Vector3.down;

			return true;
		}
		else
		{
			return false;
		}
	}
	// constructor, initialize the proper variables and set the color of the block
	public Block(GameObject part, Color color, int x, int y)
	{
		this.part = part;
		this.x = x;
		this.y = y;

		gameBoard[x, y] = this;
		part.transform.position = new(x, y, 0);
		meshRenderer = part.GetComponent<MeshRenderer>();
		SetColor(color);

		blocksThatMayFall.Add(this);
	}
}

