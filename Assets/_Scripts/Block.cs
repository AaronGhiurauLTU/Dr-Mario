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
	public static int score;

	// the game object that new blocks are placed in
	public static GameObject blockFolder;
	public static GameObject blockPrefab;

	// the current instance of the game manager script
	public static GameManager gameManager;

	// a set of all falling blocks, hashset is simply a set of unique values
	public static HashSet<Block> fallingBlocks;

	// the possible colors a block can be
	public enum Color
	{
		Red,
		Yellow,
		Blue,
		Grey
	}
	// the block that is below this instance, is null if there is nothing below
	public Block blockBelow;

	// the block that is on top of this instance, is null if there is nothing above
	public Block blockOnTop;

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
	   this method gets overridden in the pill half, the array is used as it likely will be larger */
	public virtual void CheckMatchesToClear()
	{
		SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches);

		HashSet<Block>[] allMatches = new HashSet<Block>[]
		{
				horizontalMatches,
				verticalMatches
		};

		ClearBlocks(allMatches);
	}
	// call every blocks clear method in the specified sets if they are long enough
	public static void ClearBlocks(HashSet<Block>[] blocksToClear)
	{
		foreach (HashSet<Block> set in blocksToClear)
		{
			// skip set if it is too small
			if (set.Count < 4)
				continue;

			foreach (Block block in set)
			{
				block.Clear(blocksToClear);
			}
		}
	}
	// destroy the current block, parameter is simply for helper functions, particularly in overridden versions of this method
	public virtual void Clear(HashSet<Block>[] blocksToClear)
	{
		if (part == null)
			return;

		UpdateAboveAndBelowBlocks(blocksToClear);
		SetBoardValue(x, y, null);
		Object.Destroy(part);
		part = null;
	}
	// when clearing a block, ensure the block above and below get properly updated
	public void UpdateAboveAndBelowBlocks(HashSet<Block>[] blocksToClear)
	{
		bool containsTop = SearchSetsForBlock(blocksToClear, blockOnTop, true);
		if (blockOnTop != null)
		{
			// don't make the block above fall if it will be cleared
			if (!containsTop)
				blockOnTop.UpdateFallingSet(blocksToClear);

			blockOnTop.blockBelow = null;
			blockOnTop = null;
		}

		if (blockBelow != null)
		{
			blockBelow.blockOnTop = null;
			blockBelow = null;
		}
	}
	/* search the sets of blocks that were matched for the target and possible the other half if the target was a pill
	   returns true if the target or other pill half (if it exists) was found in a long enough sets */
	public bool SearchSetsForBlock(HashSet<Block>[] blocksToClear, Block target, bool checkOtherPillHalf)
	{
		// if the sets or target is null, simply return false
		if (blocksToClear == null || target == null)
			return false;

		bool blockFound = false;

		// the other half of the pill if the target is a pill half
		Block otherTarget = null;

		// set otherTarget to the other half if it should check and the target is the right type
		if (checkOtherPillHalf && target.GetType() == typeof(PillHalf))
		{
			PillHalf thisHalf = (PillHalf)target;
			otherTarget = thisHalf.otherHalf;
		}
		
		// iterate through every set
		foreach (HashSet<Block> set in blocksToClear)
		{
			// if the set is large enough and has the value, set blockFound to true
			if (set.Count >= 4 && (set.Contains(target) || (otherTarget != null && set.Contains(otherTarget))))
			{
				blockFound = true;
				break;
			}	
		}

		return blockFound;
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
	// this returns the set of all blocks with the same color in the direction until a different color, empty space, or edge of game board is reached
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
	// sort the falling blocks in ascending order by y level
	public static void SortFallingBlocks()
	{
		fallingBlocks = fallingBlocks.OrderBy(o => o.y).ToHashSet();
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
	// overridable for pill, falls until it reaches the bottom or a block is directly below it and returns true if it stopped falling
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

			return false;
		}
		else
		{
			// stop falling since there is no room to fall

			// update the blocks below and blocks on top
			if (y > 0)
			{
				blockBelow = gameBoard[x, y - 1];
				blockBelow.blockOnTop = this;
			}
			else if (blockBelow != null)
			{
				// the block reached the bottom
				blockBelow.blockOnTop = null;
				blockBelow = null;
			}

			return true;
		}
	}
	// recursively add all blocks resting on top of this block to the falling set
	public virtual void UpdateFallingSet(HashSet<Block>[] blocksToClear = null)
	{
		bool containsTop = SearchSetsForBlock(blocksToClear, blockOnTop, true);
		
		Debug.Log($"{Time.time}: {x}, {y} color: {GetColor} type: {GetType()} length: {Block.fallingBlocks.Count}");
		fallingBlocks.Add(this);

		if (blockOnTop != null && !containsTop)
		{
			Debug.Log(blockOnTop.x + " " + blockOnTop.y + " " + blockOnTop.GetType() + " " + x + " " + y + " " + GetType());
			blockOnTop.UpdateFallingSet(blocksToClear);
		}
	}
	// constructor
	public Block(GameObject part, Color color, int x, int y, Block blockOnTop = null)
	{
		this.part = part;
		this.x = x;
		this.y = y;

		// when a pill splits, the block needs to get the proper block on top
		this.blockOnTop = blockOnTop;
		this.blockBelow = null;

		gameBoard[x, y] = this;
		part.transform.position = new(x, y, 0);
		meshRenderer = part.GetComponent<MeshRenderer>();
		SetColor(color);
	}
}

