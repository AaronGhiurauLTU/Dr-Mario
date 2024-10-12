using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* derived from the block, visually has an angry face and generates at the start of the game
   the only main difference from block is that it gives points when cleared, can change color during initial generation, 
   and decrements the remaining virus counter when destroyed */
public class Virus : Block
{
	public static int virusesRemaining = 0;
	// allow changing the color during generation to avoid more than 2 matching viruses in a row
	public void ChangeColor(Color color)
	{
		SetColor(color);
	}

	// do nothing as viruses never fall
	public override void UpdateFallingSet(HashSet<Block>[] blocksToClear = null) { }
	public override void Clear(HashSet<Block>[] blocksToClear)
	{
		if (part == null)
			return;

		UpdateAboveAndBelowBlocks(blocksToClear);	
		SetBoardValue(x, y, null);
		Object.Destroy(part);
		part = null;
		virusesRemaining--;
		gameManager.UpdateVirusCount();
	}

	// this constructor ensures the mesh renderer is properly set
	public Virus(GameObject part, Color color, int x, int y) : base(part, color, x, y)
	{
		meshRenderer = part.transform.Find("Cube").GetComponent<MeshRenderer>();
		SetColor(color);
		virusesRemaining++;
		gameManager.UpdateVirusCount();
	}
}