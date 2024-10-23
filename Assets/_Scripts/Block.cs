using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// stores information about the position and color while providing methods to check for matching adjacent blocks
public class Block
{
	public static readonly int boardSizeX = 8;
	public static readonly int boardSizeY = 16;

	// stores the blocks at each position in this 2D array, shared across all instances of this class
	public static Block[,] gameBoard = new Block[boardSizeX, boardSizeY];
	
	// current score of player
	public static int score;

	// this set contains all blocks that have fallen during the last gravity interval
	public static HashSet<Block> stillFalling = new();

	// the game object that new blocks are placed in
	public static GameObject blockFolder;

	// the block prefab
	public static GameObject blockPrefab;

	// the current instance of the game manager script
	public static GameManager gameManager;

	// the possible colors that a garbage block or pill half can be
	public static readonly List<Color> blockColorList = new() {
		Color.Red,
		Color.Yellow,
		Color.Blue,
	};

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
	// get a random color without removing the selected color from the list (drawing with replacement)
	public static Color GetRandomColor(List<Color> colorList)
	{
		int index = Random.Range(0, colorList.Count);
		Color selectedColor = colorList.ElementAt(index);

		return selectedColor;
	}
	// get a random color and remove all instances of the selected color (drawing without replacement)
	public static Color GetRandomColor(ref List<Color> colorList)
	{
		Color selectedColor = GetRandomColor(colorList);

		/* remove every instance as the virus list has duplicated to have weights for certain colors
		 * code from https://www.techiedelight.com/remove-all-occurrences-of-an-item-from-a-list-in-csharp/ */
		colorList.RemoveAll(item => item == selectedColor);

		return selectedColor;
	}
	// spawn the specified amount of garbage blocks at one time
	public static void SpawnGarbageBlocks(int garbageCount)
	{
		// the available x positions for the garbage blocks
		List<int> availableHorizontalPositions = new() { 0, 1, 2, 3, 4, 5, 6, 7 };

		// each iteration of the loop spawns a block
		for (int i = 0; i < garbageCount; i++)
		{
			// randomly select an x position and retry if the spot was not empty
			int xValue;
			do
			{
				int index = Random.Range(0, availableHorizontalPositions.Count);

				xValue = availableHorizontalPositions.ElementAt(index);

				availableHorizontalPositions.Remove(xValue);
			} 
			while (gameBoard[xValue, boardSizeY - 1] != null && availableHorizontalPositions.Count > 0);

			// if it exited the loop with an xValue that does not point to an empty spot, don't attempt to spawn any more garbage
			if (gameBoard[xValue, boardSizeY - 1] != null)
				return;

			// create a new block
			GameObject newBlockObj = Object.Instantiate(blockPrefab, blockFolder.transform);

			Block newBlock = new(newBlockObj, GetRandomColor(blockColorList), xValue, boardSizeY - 1);
			stillFalling.Add(newBlock);
		}
	}
	// ensure the block is within the board before trying to set the value of the 2D array
	public static void SetBoardValue(int x, int y, Block value)
	{
		if (x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY)
			gameBoard[x, y] = value;
	}
	// returns the value at the position in the board and null if it is outside the board
	public Block GetBoardValue(int x, int y, out bool outOfBounds)
	{
		outOfBounds = !IsWithinBounds(x, y);
		// the overridden within bounds function allows y = boardSizeY so double check the y value to return the board value
		if (!outOfBounds && y < boardSizeY)
			return gameBoard[x, y];
		
		return null;
	}
	// returns true if the specified values are within the bounds of the board, overridden for pill to allow going halfway over the board
	public virtual bool IsWithinBounds(int x, int y)
	{
		return x >= 0 && x < boardSizeX && y >= 0 && y < boardSizeY;
	}
	/* get the horizontal and vertical set of matches and forward them to the clear blocks method to determine which ones are large enough
	 * this method gets overridden in the pill half, returns all matches that were large enough to be cleared */
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
	public virtual void Clear(HashSet<Block> blocksToClear, out int garbageCount)
	{
		garbageCount = 0;
		DestroyBlock();
	}
	// move the block to the new position, overridden by pill class to handle other half, returns true if successfully moved
	protected virtual bool TryMove(int newX, int newY)
	{
		if (GetBoardValue(newX, newY, out bool outOfBounds) == null && !outOfBounds)
		{
			SetBoardValue(x, y, null);

			x = newX;
			y = newY;
			
			SetBoardValue(x , y, this);
			part.transform.position = new(x, y, 0);

			return true;
		}
		return false;
	}
	// destroy the block, ensuring it is not in the game board
	protected void DestroyBlock()
	{
		SetBoardValue(x, y, null);

		if (part != null)
			Object.Destroy(part);

		part = null;
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

		// if the current block is grey, there may be different colored blocks in this set so clean the list and keep the largest match
		CleanMatchSet(ref horizontalMatches);

		checkingDirection = Vector2.down;
		verticalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));

		checkingDirection = Vector2.up;
		verticalMatches.UnionWith(CheckColorsInDirection(checkingDirection, color));

		CleanMatchSet(ref verticalMatches);
	}
	/* this returns the set of all blocks with the same color in the direction until a different color, empty space, 
	 * or edge of game board is reached, this is a helper function for SameColorInARow
	 * the out variable is used for when the original block is grey to decide what to do with with what color was matched */
	private HashSet<Block> CheckColorsInDirection(Vector2 direction, Color checkingColor)
	{
		// the coordinate of the current block being checked
		Vector2 currentCoordinate = new Vector2(x, y) + direction;

		// the set that will be returned
		HashSet<Block> matches = new();

		// ensure the coordinates are integers for the array
		int currentX = (int)currentCoordinate.x;
		int currentY = (int)currentCoordinate.y;

		// loop until the edge of the game board is reached
		while (IsWithinBounds(currentX, currentY) && currentY < boardSizeY)
		{
			Block checkingBlock = gameBoard[currentX, currentY];

			// if a block is there and it has the same color as the root or is grey (custom mechanic), add this part to the return set
			if (checkingBlock != null && (checkingBlock.GetColor == checkingColor || checkingBlock.GetColor == Color.Grey 
				|| checkingColor == Color.Grey))
			{
				/* if the original checking color was grey and the checking block is not grey, make the checking color
				 * the checking block color so it only adds blocks of the same color to the matching set
				 * and do not change the currently checking color back to grey as it could be another color at the moment */
				if (checkingColor == Color.Grey && checkingBlock.GetColor != Color.Grey)
					checkingColor = checkingBlock.GetColor;

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
	/* if the starting block was grey, there may have been different colors that do not match 
	 * so remove the ones that would create the smallest match */
	private void CleanMatchSet(ref HashSet<Block> matches)
	{
		// no point of cleaning a set that is too small
		if (matches.Count < 2)
			return;

		// the key is the color and the value is the number of occurrences of this color in the matches
		Dictionary<Color, int> colorCounts = new();

		// count the occurrences of every color
		foreach (Block block in matches)
		{
			if (!colorCounts.ContainsKey(block.GetColor))
				colorCounts.Add(block.GetColor, 0);
			
			colorCounts[block.GetColor]++;
		}

		// theres no need to clean the set if there is only one color or there are 2 colors where one is grey
		if (colorCounts.Count == 1 || (colorCounts.Count == 2 && colorCounts.ContainsKey(Color.Grey)))
			return;

		// grey is just an initial value, guaranteed to change due to statement above
		Color keepingColor = Color.Grey;
		int keepingColorCount = 0;

		// keep the color that occurs the most often and isn't grey as grey is kept anyways
		foreach (KeyValuePair<Color, int> colorCountPair in colorCounts)
		{
			if (colorCountPair.Value > keepingColorCount && colorCountPair.Key != Color.Grey)
			{
				keepingColor = colorCountPair.Key;
				keepingColorCount = colorCountPair.Value;
			}
		}

		// determine the direction that the matching set is along
		Vector2 block0position = new(matches.ElementAt(0).x, matches.ElementAt(0).y);
		Vector2 block1position = new(matches.ElementAt(1).x, matches.ElementAt(1).y);
		Vector2 difference = (block1position - block0position).normalized;

		// true if the match set goes along the x axis
		bool xAxis = difference.x != 0;

		List<Block> orderedBlocks = new(matches);

		// order the list by the axis that the matching list is on. Code from: https://stackoverflow.com/a/3309230
		orderedBlocks = orderedBlocks.OrderBy(o => xAxis ? o.x : o.y).ToList();

		// -1 is just the initial value
		int thisBlockIndex = -1;

		// all parts to the left of this in the matching set should be removed
		int rightmostLeftNonMatchingIndex = -1;

		// all parts to the right of this in the matching set should be removed
		int leftmostRightNonMatchingIndex = -1;

		// locate all the indices
		for (int i = 0; i < orderedBlocks.Count; i++)
		{
			Block block = orderedBlocks.ElementAt(i);

			if (block == this)
				thisBlockIndex = i;

			// if the thisBlock index isn't set then it is currently to the left of it
			if (thisBlockIndex == -1 && block.GetColor != keepingColor && block.GetColor != Color.Grey)
			{
				rightmostLeftNonMatchingIndex = i;
			}
			else if (thisBlockIndex != -1 && i > thisBlockIndex && leftmostRightNonMatchingIndex == -1 
				&& block.GetColor != keepingColor && block.GetColor != Color.Grey)
			{
				// the very first one found is the leftmost instance due to the list being sorted so no need to continue checking
				leftmostRightNonMatchingIndex = i;
				break;
			}
		}

		// only remove the ranges if the indices were changed
		if (leftmostRightNonMatchingIndex > -1)
			orderedBlocks.RemoveRange(leftmostRightNonMatchingIndex, orderedBlocks.Count - leftmostRightNonMatchingIndex);

		if (rightmostLeftNonMatchingIndex > -1)
			orderedBlocks.RemoveRange(0, rightmostLeftNonMatchingIndex + 1);

		matches = new(orderedBlocks);
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
	 * the out variable is false in most cases, but is true when the block should not be checked for matches (pills splitting) */
	public virtual bool TryToFall(out bool doNotCheck)
	{
		doNotCheck = false;

		// check if it has room to fall down
		if (TryMove(x, y - 1))
		{
			return true;
		}

		return false;
	}
	// constructor, initialize the proper variables and set the color of the block
	public Block(GameObject part, Color color, int x, int y)
	{
		this.part = part;
		this.x = x;
		this.y = y;

		SetBoardValue(x, y, this);

		part.transform.position = new(x, y, 0);
		meshRenderer = part.GetComponent<MeshRenderer>();
		SetColor(color);
	}
}