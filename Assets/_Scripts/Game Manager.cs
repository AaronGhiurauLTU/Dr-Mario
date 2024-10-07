using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	// the colors of the blocks
	public Material red,
		yellow,
		blue,
		grey;
	
	// the amount of viruses that initially generate
	public int virusCount;
	
	public GameObject virusPrefab,
		pillPrefab,
		blockFolder;

	// in seconds, the amount of time between updating falling objects
	public float gravityInterval;

	// stores information about the position and color while providing methods to check for matching adjacent blocks
	public class Block
	{
		// stores the blocks at each position in this 2D array, shared across all instances of this class
		public static Block[,] gameBoard;

		public static List<Block> fallingBlocks;

		// the possible colors a block can be
		public enum Color
		{
			Red,
			Yellow,
			Blue,
			Grey
		}
		// the current color of the block
		protected Color color;

		// the game object of this block
		protected GameObject part;

		// the current instance of the game manager script
		protected GameManager gameManager;

		// the mesh renderer component that takes the material to show the color
		protected MeshRenderer meshRenderer;

		// the coordinates of the block
		protected int x, y;

		// only allow publicly getting the color but not setting it as blocks in general will not need to change color
		public Color GetColor
		{
			get { return color; }
		}

		// the out variables are lists of all the positions of the same color in a row horizontally or vertically
		public void SameColorInARow(out List<Vector2> horizontalMatches, out List<Vector2> verticalMatches)
		{
			// start both lists with the current part that this function was called from (we'll call it the root block) so the count represents how many in a row
			horizontalMatches = new() { new(x, y) };
			verticalMatches = new() { new(x, y) };
			
			// the direction to check for matches
			Vector2 checkingDirection = Vector2.left;

			// append the result of checking the color in that certain direction
			horizontalMatches.AddRange(CheckColorsInDirection(checkingDirection, color));

			checkingDirection = Vector2.right;
			horizontalMatches.AddRange(CheckColorsInDirection(checkingDirection, color));
			
			checkingDirection = Vector2.down;
			verticalMatches.AddRange(CheckColorsInDirection(checkingDirection, color));

			checkingDirection = Vector2.up;
			verticalMatches.AddRange(CheckColorsInDirection(checkingDirection, color));
		}

		// this returns the list of all blocks with the same color in the direction until a different color, empty space, or edge of game board is reached
		protected List<Vector2> CheckColorsInDirection(Vector2 direction, Color color)
		{
			// the coordinate of the current block being checked
			Vector2 currentCoordinate = new Vector2(x, y) + direction;

			// the list that will be returned
			List<Vector2> matches = new();

			// ensure the coordinates are integers for the array
			int currentX = (int)currentCoordinate.x;
			int currentY = (int)currentCoordinate.y;

			// loop until the edge of the game board is reached
			while (currentX >= 0 && currentX < 8 && currentY >= 0 && currentY < 16)
			{
				Block checkingBlock = gameBoard[currentX, currentY];

				// if a block is there and it has the same color as the root, add this part to the return list
				if (checkingBlock != null && checkingBlock.GetColor == color)
				{
					matches.Add(currentCoordinate);	
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
		// overridable for pill, falls until it reaches the bottom or a block is directly below it
		public virtual void Fall()
		{
			// check if it has room to fall down
			if (y > 0 && gameBoard[x, y - 1] == null)
			{
				// lower block by one
				gameBoard[x, y ] = null;
				
				y--;
				
				gameBoard[x, y] = this;

				part.transform.position += Vector3.down;
			}
			else
			{
				// remove from list since it is resting on something
				fallingBlocks.Remove(this);
			}
		}
		// constructor
		public Block(GameObject part, Color color, int x, int y, GameManager gameManager)
		{
			this.part = part;
			this.gameManager = gameManager;
			this.x = x;
			this.y = y;
			
			gameBoard[x, y] = this;

			meshRenderer = part.GetComponent<MeshRenderer>();
			SetColor(color);
		}
	}
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
		public Virus(GameObject part, Color color, int x, int y, GameManager gameManager) : base(part, color, x, y, gameManager)
		{
			meshRenderer = part.transform.Find("Cube").GetComponent<MeshRenderer>();
			SetColor(color);
		}
	}
	// class for half a pill, contains overridden fall method for checking both halves at once and has a variable for the other half's instance
	public class PillHalf : Block
	{
		// the instance of the other half of the pill
		public PillHalf otherHalf;


		public override void Fall()
		{
			int otherX = otherHalf.x;
			int otherY = otherHalf.y;

			// ensure both instances of y are above 0 (above bottom of pill bottom) and that there is no part directly below
			if (y > 0 && gameBoard[x, y - 1] == null && otherY > 0 && (gameBoard[otherX, otherY - 1] == null || gameBoard[otherX, otherY - 1] == this))
			{
				// lower both halves
				gameBoard[x, y] = null;
				gameBoard[otherX, otherY] = null;

				y--;
				otherY--;
				otherHalf.y = otherY;

				gameBoard[x, y] = this;
				gameBoard[otherX, otherY] = otherHalf;

				// only need to lower the part once since it is the whole pill
				part.transform.position += Vector3.down;
			}
			else
			{
				// remove this part from the list since it reached the bottom or another part
				fallingBlocks.Remove(this);
			}
		}
		// the part is the whole pill while the renderer is just of one half
		public PillHalf(GameObject part, Color color, int x, int y, bool leftSide, GameManager gameManager) 
			: base(part, color, x, y, gameManager)
		{
			meshRenderer = part.transform.Find("Pill" + (leftSide ? "Left" : "Right")).GetComponent<MeshRenderer>();
			SetColor(color);
		}
	}
	private float timeElapsedSinceUpdate = 0.0f;

	// insert a virus at the specified position and possibly the specified color
	private void InsertVirus(int x, int y, Block.Color color)
	{
		GameObject virusObject = Instantiate(virusPrefab, blockFolder.transform);

		// the positions were set so the world position is the index in gameBoard
		virusObject.transform.position = new Vector3(x, y, 0);

		// the new instance of the virus
		Virus virus = new(virusObject, color, x, y, this);

		// get the matching horizontal and vertical blocks
		virus.SameColorInARow(out List<Vector2> horizontalMatches, out List<Vector2> verticalMatches);
		
		// the possible colors remaining
		List<Block.Color> possibleColors = new() 
		{ 
			Block.Color.Red, 
			Block.Color.Yellow,
			Block.Color.Blue,
		};

		possibleColors.Remove(color);

		// if more than 2 blocks (viruses only at the start) are in a row, change the color to avoid generation that is too easy
		while (possibleColors.Count > 0 && (horizontalMatches.Count > 2 || verticalMatches.Count > 2))
		{
			// randomly select a different color
			int colorIndex = Random.Range(0, possibleColors.Count);
			Block.Color newColor = possibleColors.ElementAt(colorIndex);

			virus.ChangeColor(newColor);
			
			// check the matching parts again as it is technically possible for multiple colors to match at this position
			virus.SameColorInARow(out horizontalMatches, out verticalMatches);
			possibleColors.RemoveAt(colorIndex);
		}
	}
	// creates a new pill with random colors for each side
	private void SpawnNewPill()
	{
		GameObject newPillObj = Instantiate(pillPrefab, blockFolder.transform);

		// randomly select a color for each half, can be the same color for both
		List<Block.Color> possibleColors = new() 
		{ 
			Block.Color.Red, 
			Block.Color.Yellow,
			Block.Color.Blue,
		};
		// randomly select a color for the virus
		int colorIndex = Random.Range(0, possibleColors.Count);

		PillHalf leftHalf = new(newPillObj, possibleColors.ElementAt(colorIndex), 3, 16, true, this);

		colorIndex = Random.Range(0, possibleColors.Count);
		PillHalf rightHalf = new(newPillObj, possibleColors.ElementAt(colorIndex), 4, 16, false, this);

		leftHalf.otherHalf = rightHalf;
		rightHalf.otherHalf = leftHalf;
		
		// only add left half since the fall function accounts for the other half
		Block.fallingBlocks.Add(leftHalf);
	}
	// Start is called before the first frame update
	void Start()
	{
		// initialize game board, lowest coordinate is (0, 0) and the highest is (7, 16), y has 17 levels for the level that the pill starts at
		Block.gameBoard = new Block[8, 17];

		Block.fallingBlocks = new();

		// this list contains all available spots for a virus to generate
		List<Vector2> possibleVirusSpots = new();

		// fill the possible virus spots list
		for (int x = 0; x < 8; x++)
		{
			// viruses only generate up to the 13th level for fair game-play
			for (int y = 0; y < 13; y++)
			{
				possibleVirusSpots.Add(new(x, y));
			}
		}
		
		// loop to place the specified amount of viruses
		for (int i = 0; i < virusCount; i++)
		{
			// randomly select a position in an empty location
			int positionIndex = Random.Range(0, possibleVirusSpots.Count);
			Vector2 chosenPosition = possibleVirusSpots.ElementAt(positionIndex);

			List<Block.Color> possibleColors = new() 
			{ 
				Block.Color.Red, 
				Block.Color.Yellow,
				Block.Color.Blue,
			};
			// randomly select a color for the virus
			int colorIndex = Random.Range(0, possibleColors.Count);
			Block.Color color = possibleColors.ElementAt(colorIndex);

			// place the virus
			InsertVirus((int)chosenPosition.x, (int)chosenPosition.y, color);

			// remove the position from the list of available positions since a virus is now there
			possibleVirusSpots.RemoveAt(positionIndex);
		}

		SpawnNewPill();
	}

	// Update is called once per frame
	void Update()
	{
		timeElapsedSinceUpdate += Time.deltaTime;
		// enough time has elapsed, make the necessary blocks fall
		if (timeElapsedSinceUpdate >= gravityInterval)
		{
			// iterate through list of blocks that need to fall and call their fall function
			for (int i = 0; i < Block.fallingBlocks.Count; i++)
			{
				Block currentBlock = Block.fallingBlocks.ElementAt(i);
				currentBlock.Fall();
			}
			// reset timer
			timeElapsedSinceUpdate = 0;
		}
	}
}
