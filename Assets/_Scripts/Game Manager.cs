using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

// uses the block class (Block.cs) and derived classes of it (Virus.cs and PillHalf.cs)
public class GameManager : MonoBehaviour
{
	// the colors of the blocks
	public Material red,
		yellow,
		blue,
		grey;
	
	// the amount of viruses that initially generate
	public int virusCount;
	
	public GameObject blockPrefab,
		virusPrefab,
		pillPrefab,
		blockFolder;

	public TextMeshProUGUI scoreTMP,
		virusCountTMP;

	// in seconds, the amount of time between updating falling objects
	public float gravityInterval;

	// the sped up interval that pills move down at while the down button is held
	public float downHeldGravityInterval;

	// true when down is held and false otherwise
	private bool downHeld = false;

	// the time since the last time blocks were shifted down one
	private float timeElapsedSinceUpdate = 0.0f;

	// the blocks that need to be checked for new matches after all blocks stop falling
	private HashSet<Block> blocksToCheck;

	private bool tryMakingBlocksFallAgain = false;

	// TODO: move to virus class
	// insert a virus at the specified position and possibly the specified color
	private void InsertVirus(int x, int y, Block.Color color)
	{
		GameObject virusObject = Instantiate(virusPrefab, blockFolder.transform);

		// the new instance of the virus
		Virus virus = new(virusObject, color, x, y);

		// get the matching horizontal and vertical blocks
		virus.SameColorInARow(out HashSet<Block> horizontalMatches, out HashSet<Block> verticalMatches);
		
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

	public void UpdateScore(int score)
	{
		scoreTMP.text = $"Score: {score}";
	}

	public void UpdateVirusCount(int virusCount)
	{
		virusCountTMP.text = $"Viruses: {virusCount}";
	}

	private void RandomlyGenerateViruses()
	{
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
	}
	// simply generates viruses in a way to easily test certain edge cases with rotating
	private void GenerateRotateTestLevel()
	{
		InsertVirus(1, 0, Block.Color.Red);
		InsertVirus(1, 1, Block.Color.Red);
		InsertVirus(1, 2, Block.Color.Red);
		InsertVirus(1, 3, Block.Color.Red);
		InsertVirus(3, 1, Block.Color.Red);
		InsertVirus(3, 3, Block.Color.Red);
		InsertVirus(4, 0, Block.Color.Red);
		InsertVirus(4, 4, Block.Color.Red);
		InsertVirus(5, 1, Block.Color.Red);
		InsertVirus(5, 3, Block.Color.Red);

		InsertVirus(2, 8, Block.Color.Red);
		InsertVirus(2, 10, Block.Color.Red);
		InsertVirus(3, 8, Block.Color.Red);
		InsertVirus(3, 11, Block.Color.Red);
		InsertVirus(4, 8, Block.Color.Red);
		InsertVirus(4, 10, Block.Color.Red);
	}
	// for outside calls, allow moving the current pill from the pill control script
	public void MoveCurrentPillHorizontally(int direction)
	{
		PillHalf.currentPillLeftHalf.ShiftHorizontally(direction);
	}
	// allow pill control script to rotate current pill
	public void RotateCurrentPill()
	{
		PillHalf.currentPillLeftHalf.Rotate();
	}

	public void DownHeld()
	{
		downHeld = true;
	}

	// Start is called before the first frame update
	void Start()
	{
		// randomly generate a seed for the rng so bugs can be replicated easier by setting the seed manually
		int seed = Random.Range(1, 100000);
		//seed = 2;
		Debug.Log($"The seed is: {seed}");

		Random.InitState(seed);

		// initialize game board, lowest coordinate is (0, 0) and the highest is (7, 15)
		Block.gameBoard = new Block[8, 16];
		Block.gameManager = this;
		Block.blockFolder = this.blockFolder;
		Block.blockPrefab = this.blockPrefab;
		Block.stillFalling = new();
		Block.blocksThatMayFall = new();
		PillHalf.pillPrefab = this.pillPrefab;
		blocksToCheck = new();
		RandomlyGenerateViruses();
		//GenerateRotateTestLevel();
	}

	// Update is called once per frame
	void Update()
	{
		timeElapsedSinceUpdate += Time.deltaTime;

		// if the down button is held, allow pills to fall faster
		float currentGravityInterval = downHeld ? downHeldGravityInterval : gravityInterval;

		// enough time has elapsed, make the necessary blocks fall, the time based on if a controllable pill is falling or not
		if (timeElapsedSinceUpdate >= (PillHalf.currentPillLeftHalf == null ? 0.25f : currentGravityInterval))
		{
			// reset timer
			timeElapsedSinceUpdate = 0;

			// if there is nothing falling
			if (Block.stillFalling.Count == 0 && !tryMakingBlocksFallAgain)
			{
				/* if there are blocks to check, check them now after everything finished falling
				   this is so smaller matches aren't cleared before everything falls */
				if (blocksToCheck.Count > 0)
				{
					// the blocks that will be cleared due to being in matches >= 4
					HashSet<Block> blocksToClear = new();
					
					// append matches from each block to the set
					foreach (Block block in blocksToCheck)
					{
						blocksToClear.UnionWith(block.CheckMatchesToClear());
					}
					// clear the blocks that need to be cleared
					if (blocksToClear.Count > 0)
					{
						// ensure the first virus cleared per turn gives the minimum amount of points
						Virus.ResetBonusPointGain();

						foreach (Block block in blocksToClear)
						{
							block.Clear(blocksToClear);
						}
						// check for blocks that may need to fall after clearing these blocks
						tryMakingBlocksFallAgain = true;
					}
					else
					{
						tryMakingBlocksFallAgain = false;
					}
					// empty the set since all these blocks were checked
					blocksToCheck = new();
				}
				else
				{
					// spawn a pill one interval after checking
					PillHalf.SpawnNewPill();
					tryMakingBlocksFallAgain = false;
				}
			}
			else
			{
				// only need to check the pill in control if it still is in control to make it fall
				if (PillHalf.currentPillLeftHalf != null)
				{
					if (PillHalf.currentPillLeftHalf.Fall(out bool doNotCheck))
					{
						Block.stillFalling.Add(PillHalf.currentPillLeftHalf);
					}
					else if (Block.stillFalling.Contains(PillHalf.currentPillLeftHalf))
					{
						/* if the pill stopped falling, remove it from the list and add it to the checking list if it needs to be checked
						   it does not need to be checked if it is places halfway above the game board as the pill gets destroyed and
						   replaced with a normal block */
						Block.stillFalling.Remove(PillHalf.currentPillLeftHalf);

						if (!doNotCheck)
							blocksToCheck.Add(PillHalf.currentPillLeftHalf);
					}
				}
				else
				{
					foreach (Block block in Block.blocksThatMayFall)
					{
						if (block.Fall(out bool _))
						{
							Block.stillFalling.Add(block);
						}
						else if (Block.stillFalling.Contains(block))
						{
							Block.stillFalling.Remove(block);
							blocksToCheck.Add(block);
						}
					}
				}
				tryMakingBlocksFallAgain = false;
			}
		}
		// reset to false at the end as it will be set to true every time down is held down
		downHeld = false;
	}
}
