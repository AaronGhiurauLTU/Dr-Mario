using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* derived from the block, visually has an angry face and generates at the start of the game
   the only main difference from block is that it gives more points when cleared, can change color during initial generation, 
   and decrements the remaining virus counter when destroyed */
public class Virus : Block
{
	// allow changing the color during generation to avoid more than 2 matching viruses in a row
	public void ChangeColor(Color color)
	{
		SetColor(color);
	}
	// this constructor ensures the mesh renderer is properly set
	public Virus(GameObject part, Color color, int x, int y) : base(part, color, x, y)
	{
		meshRenderer = part.transform.Find("Cube").GetComponent<MeshRenderer>();
		SetColor(color);
	}
}