using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

/* derived from the block, visually has an angry face and generates at the start of the game
 * the only main difference from block is that it gives points when cleared, can change color during initial generation, 
 * and decrements the remaining virus counter when destroyed 
 *
 * ChatGPT was used to get more information about virtual methods and overriding them as well as debugging errors related to accessing variables outside of the class */
public class Virus : Block
{
	public static GameObject virusPrefab;

	// the amount of viruses left to destroy
	public static int virusesRemaining = 0;

	// the amount of points gained per virus clear, increases as more viruses are cleared in a single clear
	private static int currentPointGain = 100;

	// the possible colors a virus can be, duplicates are to give more weight to the 3 main colors
	public static readonly List<Color> virusColorList = new() {
		Color.Red,
		Color.Red,
		Color.Red,
		Color.Yellow,
		Color.Yellow,
		Color.Yellow,
		Color.Blue,
		Color.Blue,
		Color.Blue,
		Color.Grey,
		Color.Grey
	};
	// reset the point gain for the next turn
	public static void ResetBonusPointGain(int speedLevel)
	{
		// more points are given at higher speeds
		currentPointGain = 100 * speedLevel; 
	}
	// allow changing the color during generation to avoid more than 2 matching viruses in a row
	public void ChangeColor(Color color)
	{
		SetColor(color);
	}
	// update score and virus count appropriately, blocksToClear will only be null if the virus failed to initially generate
	public override void Clear(HashSet<Block> blocksToClear, out int garbageCount)
	{
		garbageCount = 0;
		DestroyBlock();

		virusesRemaining--;
		gameManager.UpdateVirusCount(virusesRemaining);

		// only update score and possibly spawn garbage if the virus was cleared after the viruses generated
		if (blocksToClear != null)
		{
			score += currentPointGain;
			gameManager.UpdateScore(score);

			// for every virus cleared in one turn, the score gained per virus doubles
			currentPointGain *= 2;

			// spawn 1-3 garbage blocks for custom mechanic
			if (color == Color.Grey)
				garbageCount = Random.Range(1, 4);
		}
	}
	// do nothing since viruses cannot fall
	public override bool TryToFall(out bool doNotCheck)
	{
		doNotCheck = false;
		return false;
	}

	/* insert a virus at the specified position and possibly the specified color, use this instead of a constructor
	 * if this virus creates a match that is 3 or greater, it will change colors until it no longer does 
	 * if every color creates a match that is 3 or greater, it will return false; otherwise, return true*/
	public static bool InsertVirus(int x, int y, Color color)
	{
		GameObject virusObject = Object.Instantiate(virusPrefab, blockFolder.transform);

		// the new instance of the virus
		Virus virus = new(virusObject, color, x, y);

		// get the matching horizontal and vertical blocks
		virus.SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches);

		List<Color> possibleColors = new(virusColorList);

		// remove all occurrences of the chosen color from the list, code from: https://www.techiedelight.com/remove-all-occurrences-of-an-item-from-a-list-in-csharp/
		possibleColors.RemoveAll(item => item == color);

		// if more than 2 blocks (viruses only at the start) are in a row, change the color to avoid generation that is too easy
		while (possibleColors.Count > 0 && (horizontalMatches.Count > 2 || verticalMatches.Count > 2))
		{
			// randomly select a different color
			Color newColor =  GetRandomColor(ref possibleColors);

			virus.ChangeColor(newColor);

			// check the matching parts again as it is technically possible for multiple colors to match at this position
			virus.SameColorInARow(out horizontalMatches, out verticalMatches);
		}
		// check to see if it tried every color and that every color creates a match that is too big 
		if (possibleColors.Count == 0 && (horizontalMatches.Count > 2 || verticalMatches.Count > 2))
		{
			// destroy this virus since it would create a match that is too large
			SetBoardValue(x, y, null);
			virus.Clear(null, out int _);
			return false;
		}

		return true;
	}
	// this constructor ensures the mesh renderer is properly set and updates the virus count, private since InsertVirus is used instead
	private Virus(GameObject part, Color color, int x, int y) : base(part, color, x, y)
	{
		meshRenderer = part.transform.Find("Cube").GetComponent<MeshRenderer>();
		SetColor(color);

		// update virus count
		virusesRemaining++;
		gameManager.UpdateVirusCount(virusesRemaining);
	}
}