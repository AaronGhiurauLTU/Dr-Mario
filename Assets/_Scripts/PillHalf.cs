using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


// class for half a pill, contains overridden fall method for checking both halves at once and has a variable for the other half's instance
public class PillHalf : Block
{
	// the instance of the other half of the pill
	public PillHalf otherHalf;

	// prefab of pill
	public static GameObject pillPrefab;

	// the current pill that is in control
	public static PillHalf currentPillLeftHalf;

	// current rotation of pill (0, 90, 180, or 270 degrees) and not necessarily the same as the transform but exists for ease of reading
	private int angle;

	// creates a new pill with random colors for each side
	public static void SpawnNewPill()
	{
		GameObject newPillObj = Object.Instantiate(pillPrefab, blockFolder.transform);

		// randomly select a color for each half, can be the same color for both
		List<Color> possibleColors = new()
			{
				Color.Red,
				Color.Yellow,
				Color.Blue,
			};
		// randomly select a color for the virus
		int colorIndex = Random.Range(0, possibleColors.Count);

		PillHalf rightHalf = new(newPillObj, possibleColors.ElementAt(colorIndex), 4, 15, false);

		colorIndex = Random.Range(0, possibleColors.Count);
		PillHalf leftHalf = new(newPillObj, possibleColors.ElementAt(colorIndex), 3, 15, true);

		// ensure both halves have the instance of each other
		leftHalf.otherHalf = rightHalf;
		rightHalf.otherHalf = leftHalf;

		// only add left half since the fall function accounts for the other half
		leftHalf.UpdateFallingSet();

		currentPillLeftHalf = leftHalf;
	}

	// split the pill, destroying the entire pill and replacing the half that should be kept with a normal block
	private void Split(bool destroyOther, HashSet<Block>[] blocksToClear = null)
	{
		// get the necessary information of the half being "kept" to create the new block
		GameObject newBlockObj = Object.Instantiate(blockPrefab, blockFolder.transform);
		Block keepingBlock = destroyOther ? this : otherHalf;

		// empty the board values
		SetBoardValue(x, y, null);
		SetBoardValue(otherHalf.x, otherHalf.y, null);

		// the part is the whole pill so it doesn't matter which side it is from
		Object.Destroy(part);
		part = null;
		otherHalf.part = null;

		Block newBlock = new(newBlockObj, keepingBlock.GetColor, keepingBlock.x, keepingBlock.y, blockOnTop: keepingBlock.blockOnTop);

		// ensure the block below has the proper instance of the new block
		if (keepingBlock.blockOnTop != null)
		{
			keepingBlock.blockOnTop.blockBelow = newBlock;
		}
		// set the block on top of the block below to null as the new block will be falling and will update the value properly
		if (keepingBlock.blockBelow != null)
		{
			keepingBlock.blockBelow.blockOnTop = null;
		}
		// add the new block to the falling set which should add the blocks on top as well
		newBlock.UpdateFallingSet(blocksToClear);
	}
	// overridden, split the pill if only one half is going to be cleared; otherwise, destroy the entire pill
	public override void Clear(HashSet<Block>[] blocksToClear)
	{
		// the other half of the pill may have already cleared, if so skip the rest as the pill should already be destroyed
		if (part == null)
			return;
		
		// true if the other half is going to cleared as well
		bool containsOtherHalf = SearchSetsForBlock(blocksToClear, otherHalf, false);

		// since the other half is going to be cleared, destroy the whole pill
		if (containsOtherHalf)
		{
			UpdateAboveAndBelowBlocks(blocksToClear);
			otherHalf.UpdateAboveAndBelowBlocks(blocksToClear);
			SetBoardValue(x, y, null);
			SetBoardValue(otherHalf.x, otherHalf.y, null);
			Object.Destroy(part);
			part = null;
			otherHalf.part = null;
		}
		else
		{
			// split the pill since only half the pill is getting cleared
			Split(false, blocksToClear);
		}
	}

	public override void UpdateFallingSet(HashSet<Block>[] blocksToClear = null)
	{
		bool thisContainsTop = SearchSetsForBlock(blocksToClear, blockOnTop, true);
		bool otherContainsTop = SearchSetsForBlock(blocksToClear, otherHalf.blockOnTop, true);
		Debug.Log($"{Time.time}: {x}, {y} color: {GetColor} type: {GetType()} length: {Block.fallingBlocks.Count}");
		fallingBlocks.Add(this);

		if (blockOnTop != null && !thisContainsTop)
		{
			Debug.Log(blockOnTop.x + " THIS" + blockOnTop.y + " " + blockOnTop.GetType() + " " + x + " " + y + " " + GetType());
			blockOnTop.UpdateFallingSet(blocksToClear);
		}

		if (otherHalf.blockOnTop != null && !otherContainsTop)
		{
			Debug.Log(otherHalf.blockOnTop.x + " OTHER" + otherHalf.blockOnTop.y + " " + otherHalf.blockOnTop.GetType() + " " + otherHalf.x + " " + otherHalf.y + " " + otherHalf.GetType());
			otherHalf.blockOnTop.UpdateFallingSet(blocksToClear);
		}
	}
	// overridden from block class, checks the other half of the pill when falling as well
	public override bool Fall(out bool doNotCheck)
	{
		int otherX = otherHalf.x;
		int otherY = otherHalf.y;

		// ensure both instances of y will 
		if (y > 0 && (gameBoard[x, y - 1] == null || gameBoard[x, y - 1] == otherHalf)
			&& otherY > 0 && (gameBoard[otherX, otherY - 1] == null || gameBoard[otherX, otherY - 1] == this))
		{
			// lower both halves
			SetBoardValue(x, y, null);
			SetBoardValue(otherX, otherY, null);

			y--;
			otherY--;
			otherHalf.y = otherY;

			SetBoardValue(x, y, this);
			SetBoardValue(otherX, otherY, otherHalf);

			// only need to lower the part once since it is the whole pill
			part.transform.position += Vector3.down;

			doNotCheck = false;
			return false;
		}
		else
		{
			// disable control of pill
			part.GetComponent<PillControl>().enabled = false;
			currentPillLeftHalf = null;
			
			// if half the pill is resting above the board, split the pill so only the half in the board remains
			if (y == 16)
			{
				doNotCheck = true;
				Split(false);
			}
			else if (otherHalf.y == 16)
			{
				Split(true);
				doNotCheck = true;
			}
			else
			{
				// update the current half and the block below's references
				if (y > 0 && gameBoard[x, y - 1] != null && gameBoard[x, y - 1] != otherHalf)
				{
					blockBelow = gameBoard[x, y - 1];
					blockBelow.blockOnTop = this;
				}
				else if (blockBelow != null)
				{
					blockBelow.blockOnTop = null;
					blockBelow = null;
				}

				// update the other half and the block below's references
				if (otherY > 0 && gameBoard[otherX, otherY - 1] != null && gameBoard[otherX, otherY - 1] != this)
				{
					otherHalf.blockBelow = gameBoard[otherX, otherY - 1];
					otherHalf.blockBelow.blockOnTop = otherHalf;
				}
				else if (otherHalf.blockBelow != null)
				{
					otherHalf.blockBelow.blockOnTop = null;
					otherHalf.blockBelow = null;
				}
				doNotCheck = false;
			}
			return true;
		}
	}
	// overridden, adds the matches of the other half only if they contain a different set of values
	public override void CheckMatchesToClear()
	{
		SameColorInARow(out HashSet<Block> thisHorizontalMatches, out HashSet<Block> thisVerticalMatches);
		otherHalf.SameColorInARow(out HashSet<Block> otherHorizontalMatches, 
			out HashSet<Block> otherVerticalMatches);

		// minimum size is 2, one horizontal and vertical set
		int currentSize = 2;

		// increment the size if the sets aren't equal, they can be equal if both sides of the pill are part of a single 4+ in a row match
		if (!thisHorizontalMatches.SetEquals(otherHorizontalMatches))
			currentSize++;

		if (!thisVerticalMatches.SetEquals(otherVerticalMatches))
			currentSize++;
		
		// create the array with the desired size
		HashSet<Block>[] allMatches = new HashSet<Block>[currentSize];
		
		allMatches[0] = thisHorizontalMatches;
		allMatches[1] = thisVerticalMatches;

		// the index of the next sets if they can be added
		int currentIndex = 2;

		// only add the sets if they are different, increment the index if it is added
		if (!thisHorizontalMatches.SetEquals(otherHorizontalMatches))
		{
			allMatches[currentIndex] = otherHorizontalMatches;
			currentIndex++;
		}

		if (!thisVerticalMatches.SetEquals(otherVerticalMatches))
		{
			allMatches[currentIndex] = otherVerticalMatches;
		}
		// send the sets to clear the ones large enough
		ClearBlocks(allMatches);
	}
	// ensure when moving in the specified direction (-1 or 1) that there is nothing in the way
	public void ShiftHorizontally(int direction)
	{
		if (x + direction >= 0 && x + direction < 8 && otherHalf.x + direction >= 0 && otherHalf.x + direction < 8
			&& (y == 16 || gameBoard[x + direction, y] == null || gameBoard[x + direction, y] == otherHalf)
			&& (otherHalf.y == 16 || gameBoard[otherHalf.x + direction, otherHalf.y] == null
				|| gameBoard[otherHalf.x + direction, otherHalf.y] == this))
		{
			// make the proper blocks null and add the direction to both halve's x value
			SetBoardValue(x, y, null);
			SetBoardValue(otherHalf.x, otherHalf.y, null);

			x += direction;
			otherHalf.x += direction;

			SetBoardValue(x, y, this);
			SetBoardValue(otherHalf.x, otherHalf.y, otherHalf);

			// part for pill half is the whole pill so only need to move it once
			part.transform.position += new Vector3(direction, 0, 0);
		}
	}
	// try rotating the pill
	public void Rotate()
	{
		// get the hypothetical values if the pill is rotated to check its final location
		int nextAngle = angle + 90;

		// ensure the angle stays within the desired range
		if (nextAngle == 360)
			nextAngle = 0;

		// the overall offset of the pill's position
		Vector3 positionOffset = Vector3.zero;

		int nextX = x;
		int nextY = y;
		int nextOtherX = otherHalf.x;
		int nextOtherY = otherHalf.y;

		/* to mimic the original game, the pill rotates within a 2x2 area throughout a full 360 degree rotation
		   so there is some adjustments to the positions to keep it consistent 
		   essentially it rotates clockwise but when going vertical to horizontal, the top will always be 
		   on the right of the pill */
		switch (nextAngle)
		{
			case 0:
				nextOtherX++;
				nextOtherY--;
				break;
			case 90:
				positionOffset += Vector3.up;
				nextY++;
				nextOtherX--;
				break;
			case 180:
				positionOffset += Vector3.down + Vector3.right;
				nextX++;
				nextY--;
				break;
			case 270:
				positionOffset += Vector3.left;
				nextX--;
				nextOtherY++;
				break;
		}
		/* only if the pill is currently vertical and trying to become horizontal, if the right side is out of the board or
		   inside another block, it will "push off" the wall, shift the whole pill to the left like the original game

		   essentially this if statement checks if the space its trying to rotate into is not empty and not currently the pill 
		   
		   the left half when horizontal will always be fine since there always is part of the pill in the bottom left
		   during rotation */
		if ((angle == 90 || angle == 270)
			&& (nextX > 7 || nextOtherX > 7 || (nextY < 16 && gameBoard[nextX, nextY] != null && gameBoard[nextX, nextY] != this 
				&& gameBoard[nextX, nextY] != otherHalf)
			|| (nextOtherY < 16 && gameBoard[nextOtherX, nextOtherY] != null && gameBoard[nextOtherX, nextOtherY] != this 
				&& gameBoard[nextOtherX, nextOtherY] != otherHalf)))
		{
			nextX--;
			nextOtherX--;
			positionOffset += Vector3.left;
		}
		/* only actually rotate the pill if the x values aren't to the left of the board (from pushing off another block)
		   and the spots its trying to rotate to are either empty or currently contain apart of the pill */
		if (nextX >= 0 && nextOtherX >= 0
			&& (nextY == 16 || gameBoard[nextX, nextY] == null || gameBoard[nextX, nextY] == this || gameBoard[nextX, nextY] == otherHalf)
			&& (nextOtherY == 16 || gameBoard[nextOtherX, nextOtherY] == null || gameBoard[nextOtherX, nextOtherY] == this
				|| gameBoard[nextOtherX, nextOtherY] == otherHalf))
		{
			// set the values of the current pill spots to null
			SetBoardValue(x, y, null);
			SetBoardValue(otherHalf.x, otherHalf.y, null);

			// update all values of the pill to the theoretical values since the pill can be rotated
			x = nextX;
			y = nextY;

			otherHalf.x = nextOtherX;
			otherHalf.y = nextOtherY;

			SetBoardValue(x, y, this);
			SetBoardValue(otherHalf.x, otherHalf.y, otherHalf);

			angle = nextAngle;

			part.transform.Rotate(new(90, 0, 0));
			part.transform.position += positionOffset;
		}
	}

	// constructor ensures the proper renderer is set with the desired color
	public PillHalf(GameObject part, Color color, int x, int y, bool leftSide)
		: base(part, color, x, y)
	{
		// the part is the whole pill while the renderer is just of one half
		meshRenderer = part.transform.Find("Pill" + (leftSide ? "Left" : "Right")).GetComponent<MeshRenderer>();
		SetColor(color);

		// all start with an angle of 0
		angle = 0;
	}
}
