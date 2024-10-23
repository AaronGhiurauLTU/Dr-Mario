using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// class for half a pill, contains overridden fall method for checking both halves at once and has a variable for the other half's instance
public class PillHalf : Block
{
	// the instance of the other half of the pill
	public PillHalf otherHalf;

	// prefab of pill
	public static GameObject pillPrefab;

	// the current pill that is in control
	public static PillHalf currentPillLeftHalf = null;

	// the next pill queued to be given to the player
	public static PillHalf nextPillLeftHalf = null;

	// used to ensure each pill falls only once per interval as a single call to one of the halves makes the whole pill fall
	public bool fallen;

	// current rotation of pill (0, 90, 180, or 270 degrees) and not necessarily the same as the transform but exists for ease of reading
	private int angle;

	// is true if it is the left half of the pill when instantiating; false if its the right side
	private readonly bool isLeftHalf;

	// the amount of times the pill was determined to be resting on something while falling
	private int timesRestedOnSomething;

	// creates a new pill with random colors for each side
	public static void SpawnNewPill()
	{
		// set the queued pill to the current one
		currentPillLeftHalf = nextPillLeftHalf;

		// create the next pill to be queued
		GameObject newPillObj = Object.Instantiate(pillPrefab, blockFolder.transform);

		// randomly select a color for each half, can be the same color for both, and place the pill outside the board to show it to the player
		PillHalf rightHalf = new(newPillObj, GetRandomColor(blockColorList), 11, 15, false);

		PillHalf leftHalf = new(newPillObj, GetRandomColor(blockColorList), 10, 15, true);

		// ensure both halves have the instance of each other
		leftHalf.otherHalf = rightHalf;
		rightHalf.otherHalf = leftHalf;

		nextPillLeftHalf = leftHalf;
		
		if (currentPillLeftHalf == null)
		{
			// initially, there is no pill queued so recall this method to move the newly made queued pill to the current pill and queue a new pill
			SpawnNewPill();
		}
		else
		{
			// move the queued pill to the game board
			if (currentPillLeftHalf.TryMove(3, boardSizeY - 1))
			{
				// enable control of the current pill
				currentPillLeftHalf.part.GetComponent<PillControl>().enabled = true;

				// ensure the pill is in the falling set
				stillFalling.Add(currentPillLeftHalf);
			}
			else
			{
				gameManager.EndGame(true);
			}
		}
	}
	// split the pill, destroying the entire pill and replacing the half that should be kept with a normal block
	private void Split(bool destroyOther)
	{
		// get the necessary information of the half being "kept" to create the new block
		GameObject newBlockObj = Object.Instantiate(blockPrefab, blockFolder.transform);
		Block keepingBlock = destroyOther ? this : otherHalf;

		// destroy the pill since the remaining half is simulated with a separate block
		DestroyBlock();
		otherHalf.DestroyBlock();

		Block newBlock = new(newBlockObj, keepingBlock.GetColor, keepingBlock.x, keepingBlock.y);
		
		// ensure the block is still in the list
		stillFalling.Add(newBlock);
	}
	// ensure when moving in the specified direction (-1 or 1) that there is nothing in the way
	public void ShiftHorizontally(int direction)
	{
		if (IsAvailable(x + direction, y, otherHalf.x + direction, y))
		{
			TryMove(x + direction, y);
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
		 * so there is some adjustments to the positions to keep it consistent 
		 * essentially it rotates clockwise but when going vertical to horizontal, the top will always be 
		 * on the right of the pill */
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
		 * inside another block, it will "push off" the wall, shift the whole pill to the left like the original game
		 *
		 * essentially this if statement checks if the space its trying to rotate into is not empty and not currently the pill 
		 * 
		 * the left half when horizontal will always be fine since there always is part of the pill in the bottom left
		 * during rotation */
		if ((angle == 90 || angle == 270) && !IsAvailable(nextX, nextY, nextOtherX, nextOtherY))
		{
			nextX--;
			nextOtherX--;
			positionOffset += Vector3.left;
		}
		/* only actually rotate the pill if the x values aren't to the left of the board (from pushing off another block)
		 * and the spots its trying to rotate to are either empty or currently contain apart of the pill */
		if (IsAvailable(nextX, nextY, nextOtherX, nextOtherY))
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
	// returns true if the specified values are within the bounds of the board or one above the board since half the pill is allowed off the board
	public override bool IsWithinBounds(int x, int y)
	{
		return x >= 0 && x < boardSizeX && y >= 0 && y <= boardSizeY;
	}
	// checks if the 2 specified positions are available for this pill
	public bool IsAvailable(int thisX, int thisY, int otherX, int otherY)
	{
		// OOB = Out of Bounds
		Block thisTarget = GetBoardValue(thisX, thisY, out bool thisOOB);
		Block otherTarget = GetBoardValue(otherX, otherY, out bool otherOOB);

		return !thisOOB && !otherOOB && (thisTarget == null || thisTarget == otherHalf || thisTarget == this) 
			&& (otherTarget == null || otherTarget == this || otherTarget == otherHalf);
	}
	// checks the positions of both halves where it only takes the position of one half and calculates the position of the other
	protected override bool TryMove(int newX, int newY)
	{
		// get the difference between the halves of pills' positions
		int otherNewX = newX + (otherHalf.x - x);
		int otherNewY = newY + (otherHalf.y - y);

		// only move the pill half if both spots are available
		if (IsAvailable(newX, newY, otherNewX, otherNewY))
		{
			SetBoardValue(x, y, null);
			SetBoardValue(otherHalf.x, otherHalf.y, null);
			
			otherHalf.x = otherNewX;
			otherHalf.y = otherNewY;
			x = newX;
			y = newY;

			SetBoardValue(x , y, this);
			SetBoardValue(otherHalf.x, otherHalf.y, otherHalf);
			
			// position of part is based on left half's position
			if (isLeftHalf)
				part.transform.position = new(x, y, 0);
			else
				part.transform.position = new(otherHalf.x, otherHalf.y, 0);

			return true;
		}
		return false;
	}
	// overridden, split the pill if only one half is going to be cleared; otherwise, destroy the entire pill
	public override void Clear(HashSet<Block> blocksToClear, out int garbageCount)
	{
		garbageCount = 0;
		// the other half of the pill may have already cleared, if so skip the rest as the pill should already be destroyed
		if (part == null)
			return;
		
		// true if the other half is going to cleared as well
		bool containsOtherHalf = blocksToClear.Contains(otherHalf);

		// since the other half is going to be cleared, destroy the whole pill
		if (containsOtherHalf)
		{
			DestroyBlock();
			otherHalf.DestroyBlock();
		}
		else
		{
			// split the pill since only half the pill is getting cleared
			Split(false);
		}
	}
	// overridden from block class, checks the other half of the pill when falling as well
	public override bool TryToFall(out bool doNotCheck)
	{
		doNotCheck = false;
		
		// ensure each pill falls only once as the TryToMove method handles both halves, only matters if it isn't part of the current pill
		if (this != currentPillLeftHalf && otherHalf != currentPillLeftHalf)
		{
			if (!fallen)
			{
				fallen = true;
				otherHalf.fallen = true;
			}
			else
			{
				fallen = false;
				otherHalf.fallen = false;
				return false;
			}
		}

		// ensure both instances of y will be within the board and have no blocks underneath that aren't the other half
		if (TryMove(x, y - 1))
		{
			timesRestedOnSomething = 0;

			return true;
		}

		timesRestedOnSomething++;

		// if the current pill is in control, allow some leeway for moving the pill after landing by allowing an extra time interval before stopping
		if (timesRestedOnSomething < 2 && currentPillLeftHalf == this)
		{
			return true;
		}
		
		// disable control of pill
		part.GetComponent<PillControl>().enabled = false;
		currentPillLeftHalf = null;
		
		// if half the pill is resting above the board, split the pill so only the half in the board remains
		if (y == boardSizeY)
		{
			doNotCheck = true;
			Split(false);
		}
		else if (otherHalf.y == boardSizeY)
		{
			doNotCheck = true;
			Split(true);
		}

		return false;
	}
	// overridden, adds the matches of the other half AS WELL
	public override HashSet<Block> CheckMatchesToClear()
	{
		SameColorInARow(out HashSet<Block> thisHorizontalMatches, out HashSet<Block> thisVerticalMatches);
		otherHalf.SameColorInARow(out HashSet<Block> otherHorizontalMatches, 
			out HashSet<Block> otherVerticalMatches);

		HashSet<Block> matches = new();
		
		// only add the sets if they are large enough to be cleared
		if (thisHorizontalMatches.Count >= 4)
			matches.UnionWith(thisHorizontalMatches);
		if (thisVerticalMatches.Count >= 4)
			matches.UnionWith(thisVerticalMatches);

		if (otherHorizontalMatches.Count >= 4)
			matches.UnionWith(otherHorizontalMatches);
		if (otherVerticalMatches.Count >= 4)
			matches.UnionWith(otherVerticalMatches);

		return matches;
	}
	// constructor ensures the proper renderer is set with the desired color
	public PillHalf(GameObject part, Color color, int x, int y, bool isLeftSide)
		: base(part, color, x, y)
	{
		this.isLeftHalf = isLeftSide;
		// the part is the whole pill while the renderer is just of one half
		meshRenderer = part.transform.Find("Pill" + (isLeftSide ? "Left" : "Right")).GetComponent<MeshRenderer>();
		SetColor(color);

		timesRestedOnSomething = 0;
		fallen = false;

		// all start with an angle of 0
		angle = 0;
	}
}