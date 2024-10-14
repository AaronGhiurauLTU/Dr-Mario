using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

/* derived from the block, visually has an angry face and generates at the start of the game
   the only main difference from block is that it gives points when cleared, can change color during initial generation, 
   and decrements the remaining virus counter when destroyed */
public class Virus : Block
{
	public static GameObject virusPrefab;
	private static int virusesRemaining = 0;
	private static int currentPointGain = 100;

	public static void ResetBonusPointGain()
	{
		currentPointGain = 100;
	}
	// allow changing the color during generation to avoid more than 2 matching viruses in a row
	public void ChangeColor(Color color)
	{
		SetColor(color);
	}
	// update score and virus count appropriately
	public override void Clear(HashSet<Block> blocksToClear)
	{
		DestroyBlock();

		virusesRemaining--;
		gameManager.UpdateVirusCount(virusesRemaining);

		score += currentPointGain;
		gameManager.UpdateScore(score);

		// for every virus cleared in one turn, the score gained per virus doubles
		currentPointGain *= 2;
	}

	// insert a virus at the specified position and possibly the specified color, use this instead of a constructor
	public static void InsertVirus(int x, int y, Color color)
	{
		GameObject virusObject = Object.Instantiate(virusPrefab, blockFolder.transform);

		// the new instance of the virus
		Virus virus = new(virusObject, color, x, y);

		// get the matching horizontal and vertical blocks
		virus.SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches);

		// the possible colors remaining
		List<Color> possibleColors = new()
		{
			Color.Red,
			Color.Yellow,
			Color.Blue,
		};

		possibleColors.Remove(color);

		// if more than 2 blocks (viruses only at the start) are in a row, change the color to avoid generation that is too easy
		while (possibleColors.Count > 0 && (horizontalMatches.Count > 2 || verticalMatches.Count > 2))
		{
			// randomly select a different color
			int colorIndex = Random.Range(0, possibleColors.Count);
			Color newColor = possibleColors.ElementAt(colorIndex);

			virus.ChangeColor(newColor);

			// check the matching parts again as it is technically possible for multiple colors to match at this position
			virus.SameColorInARow(out horizontalMatches, out verticalMatches);
			possibleColors.RemoveAt(colorIndex);
		}
	}
	// this constructor ensures the mesh renderer is properly set and updates the virus count, private since InsertVirus is used instead
	private Virus(GameObject part, Color color, int x, int y) : base(part, color, x, y)
	{
		meshRenderer = part.transform.Find("Cube").GetComponent<MeshRenderer>();
		SetColor(color);

		// update virus count
		virusesRemaining++;
		gameManager.UpdateVirusCount(virusesRemaining);

		// base constructor adds this to the set so remove it since viruses never fall
		blocksThatMayFall.Remove(this);
	}
}