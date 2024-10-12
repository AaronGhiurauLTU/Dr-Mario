using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

/* derived from the block, visually has an angry face and generates at the start of the game
   the only main difference from block is that it gives points when cleared, can change color during initial generation, 
   and decrements the remaining virus counter when destroyed */
public class Virus : Block
{
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

	// do nothing as viruses never fall
	public override bool Fall(out bool doNotCheck) 
	{ 
		doNotCheck = false;
		return false;
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
	// this constructor ensures the mesh renderer is properly set and updates the virus count
	public Virus(GameObject part, Color color, int x, int y) : base(part, color, x, y)
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