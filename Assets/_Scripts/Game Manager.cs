using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// uses the block class (Block.cs) and derived classes of it (Virus.cs and PillHalf.cs)
public class GameManager : MonoBehaviour
{
	// the colors of the blocks
	public Material red,
		yellow,
		blue,
		grey;
	
	public GameObject blockPrefab,
		virusPrefab,
		pillPrefab,
		blockFolder,
		borderPrefab,
		virusBurstPrefab;

	public TextMeshProUGUI scoreTMP,
		virusCountTMP,
		levelLabelTMP,
		speedLevelTMP,
		gameEndTMP;

	public Button backToMenuButton;

	public GameObject gameUICanvas;

	public float lowInterval = 40/60;
	// 36
	// 32
	// 28
	// 24
	public float medInterval = 19/60;
	public float hiInterval = 14/60;

	// the sped up interval that pills move down at while the down button is held
	public float downHeldGravityInterval;

	public float clearAnimationInterval = .1f;

	public int clearAnimationIntervalCount = 5;

	// while true, nothing in the game should update
	private bool gameEnded = true;

	// in seconds, the amount of time between updating falling objects
	private float gravityInterval;

	// true when down is held and false otherwise
	private bool downHeld = false;

	// the time since the last time blocks were shifted down one
	private float timeElapsedSinceUpdate = 0.0f,

	// the time since the last clear animation update
		timeElapsedSinceClearAnimationUpdate = 0.0f;

	// the amount of times the current clear animation has been updated
	private int animationUpdatesCount = 0;

	// the amount of viruses that are currently on the board
	private int virusCount;

	// the blocks that need to be checked for new matches after all blocks stop falling
	private HashSet<Block> blocksToCheck;

	// the key is the actual object that will be animated and the value is a bool that is true if the part was a virus
	private Dictionary<GameObject, bool> blocksToAnimateClearing;

	// true after blocks were cleared to check if anything needs to fall
	private bool tryMakingBlocksFallAgain = false;

	// each value in the queue represents how many garbage drops will be dropped at once
	private readonly Queue<int> queuedGarbage = new();

	// update the UI to scow the new score
	public void UpdateScore(int score)
	{
		scoreTMP.text = $"Score: {score}";
	}
	// update the UI to show the new virus count
	public void UpdateVirusCount(int virusCount)
	{
		virusCountTMP.text = $"Viruses: {virusCount}";

		this.virusCount = virusCount;
	}
	// generate the amount of viruses based on the specified level
	private void RandomlyGenerateViruses(int level)
	{
		// add 4 viruses per level
		int virusCount = 4 + (4 * level);

		// it is always at least 10 but increases up to 13 at higher levels
		int maxHeight = Mathf.Max(10, 3 + ((level + 1) / 2));

		// this list contains all available spots for a virus to generate
		List<Vector2> possibleVirusSpots = new();

		// fill the possible virus spots list
		for (int x = 0; x < 8; x++)
		{
			// viruses only generate up to the 13th level for fair game-play
			for (int y = 0; y < maxHeight; y++)
			{
				possibleVirusSpots.Add(new(x, y));
			}
		}

		// loop to place the specified amount of viruses
		for (int virusesPlaced = 0; virusesPlaced < virusCount; virusesPlaced++)
		{
			// randomly select a position in an empty location
			int positionIndex = Random.Range(0, possibleVirusSpots.Count);
			Vector2 chosenPosition = possibleVirusSpots.ElementAt(positionIndex);

			// randomly select a color for the virus
			Block.Color color = Block.GetRandomColor(Virus.virusColorList);

			// try inserting virus at specified position, is it didn't successfully place decrement counter and try again
			if (!Virus.InsertVirus((int)chosenPosition.x, (int)chosenPosition.y, color))
				virusesPlaced--;

			// remove the position from the list of available positions since a virus is now there
			possibleVirusSpots.RemoveAt(positionIndex);
		}
	}
	// for outside calls, allow moving the current pill from the pill control script
	public void MoveCurrentPillHorizontally(int direction)
	{
		PillHalf.currentPillLeftHalf.ShiftHorizontally(direction);
	}
	// allow pill control script to rotate current pill
	public void RotateCurrentPill()
	{
		// no pill is in control when garbage blocks are dropping or when blocks that aren't the pill are falling
		if (PillHalf.currentPillLeftHalf != null)
			PillHalf.currentPillLeftHalf.Rotate();
	}
	// properly stop the game from running and show proper end UI
	public void EndGame(bool gameOver)
	{
		if (gameOver)
		{
			gameEndTMP.text = "Game Over!";
		}
		else
		{
			gameEndTMP.text = "Game Won!";
		}
		gameEndTMP.gameObject.SetActive(true);
		gameEnded = true;
		backToMenuButton.gameObject.SetActive(true);
	}
	// hide the game UI, reset score, other part of method in Menu.cs
	public void OnBackToMenuClicked()
	{
		gameUICanvas.SetActive(false);
		Block.score = 0;
		UpdateScore(Block.score);
	}
	// reset all the proper variables to restart the game and display the proper UI elements
	public void ResetGame(int level, string speed)
	{
		// set the appropriate speed of gravity
		switch (speed)
		{
			case "LOW":
				gravityInterval = lowInterval;
				break;
			case "MED":
				gravityInterval = medInterval;
				break;
			case "HI":
				gravityInterval = hiInterval;
				break;
		}
		levelLabelTMP.text = $"Speed: {speed}";

		gameUICanvas.SetActive(true);
		gameEndTMP.gameObject.SetActive(false);

		gameEnded = false;
		
		levelLabelTMP.text = $"Level: {level}";
		backToMenuButton.gameObject.SetActive(false);
		
		Block.stillFalling = new();
		
		blocksToCheck = new();
		blocksToAnimateClearing = new();

		timeElapsedSinceClearAnimationUpdate = 0;
		animationUpdatesCount = 0;

		timeElapsedSinceUpdate = 0;

		Block.gameBoard = new Block[Block.boardSizeX, Block.boardSizeY];
		PillHalf.nextPillLeftHalf = null;
		PillHalf.currentPillLeftHalf = null;
		Virus.virusesRemaining = 0;
		
		// destroy all the block objects, code from https://stackoverflow.com/a/60391826
		while (blockFolder.transform.childCount > 0) {
   			DestroyImmediate(blockFolder.transform.GetChild(0).gameObject);
		}

		// virus amount generated based on the level
		RandomlyGenerateViruses(level);
	}
	// called by pill control, set to true when down is held
	public void DownHeld()
	{
		downHeld = true;
	}
	// Start is called before the first frame update
	void Start()
	{
		// randomly generate a seed for the rng so bugs can be replicated easier by setting the seed manually
		int seed = Random.Range(1, 100000);
		//seed = 73170;
		Debug.Log($"The seed is: {seed}");

		Random.InitState(seed);

		Block.gameManager = this;
		Block.blockFolder = this.blockFolder;
		Block.blockPrefab = this.blockPrefab;
		Virus.virusPrefab = virusPrefab;
		PillHalf.pillPrefab = this.pillPrefab;
		blocksToCheck = new();
		blocksToAnimateClearing = new();
	}
	// Update is called once per frame
	void Update()
	{
		// don't update anything while the game is ended
		if (gameEnded)
			return;

		// animate cleared parts
		if (blocksToAnimateClearing.Count > 0)
		{
			timeElapsedSinceClearAnimationUpdate += Time.deltaTime;

			// update animation if enough time has passed
			if (timeElapsedSinceClearAnimationUpdate >= clearAnimationInterval)
			{
				timeElapsedSinceClearAnimationUpdate = 0;
				animationUpdatesCount++;

				// toggle the white border of each part to make the blocks look like they're flashing
				foreach (KeyValuePair<GameObject, bool> partIsVirusPair in blocksToAnimateClearing)
				{
					partIsVirusPair.Key.GetComponent<MeshRenderer>().enabled = !partIsVirusPair.Key.GetComponent<MeshRenderer>().enabled;
				}
				// stop animating if it updated enough times
				if (animationUpdatesCount >= clearAnimationIntervalCount)
				{
					foreach (KeyValuePair<GameObject, bool> partIsVirusPair in blocksToAnimateClearing)
					{
						// create a particle burst where viruses were cleared with the same color as the virus
						if (partIsVirusPair.Value)
						{
							GameObject virusBurstParticles = Instantiate(virusBurstPrefab);
							ParticleSystem.MainModule main = virusBurstParticles.GetComponent<ParticleSystem>().main;
							main.startColor =  partIsVirusPair.Key.transform.GetChild(0).GetComponent<MeshRenderer>().material.color;
							virusBurstParticles.transform.position = partIsVirusPair.Key.transform.position;
						}

						// destroy the part
						Destroy(partIsVirusPair.Key);
					}

					animationUpdatesCount = 0;
					blocksToAnimateClearing = new();

					// end the game since all viruses were cleared
					if (virusCount == 0)
						EndGame(false);
				}
			}
			// do not update anything else while animating these blocks getting cleared
			return;
		}

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
				 * this is so smaller matches aren't cleared before everything falls */
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
						foreach (Block block in blocksToClear)
						{
							// create the part that will be animated to show what is cleared in the place of the original 
							GameObject animationPart = Instantiate(borderPrefab);
							animationPart.transform.position = new(block.x, block.y, 0);

							// set the color of the inner part to the same color of the block being cleared
							animationPart.transform.GetChild(0).GetComponent<MeshRenderer>().material = Block.GetMaterial(block.GetColor);

							block.Clear(blocksToClear, out int garbageCount);

							if (garbageCount > 0)
								queuedGarbage.Enqueue(garbageCount);

							/* the key is the game object and the value is a bool that is true if the part is a virus
							 * help for comparing type from: https://stackoverflow.com/a/708240 */
							blocksToAnimateClearing.Add(animationPart, block is Virus);
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
				else if (queuedGarbage.Count > 0)
				{
					Block.SpawnGarbageBlocks(queuedGarbage.Dequeue());
					tryMakingBlocksFallAgain = true;
				}
				else
				{
					// spawn a pill one interval after checking
					PillHalf.SpawnNewPill();
					tryMakingBlocksFallAgain = false;

					// allow one extra interval of time for when the pill first spawns in to react and move left/right
					timeElapsedSinceUpdate -= currentGravityInterval;

					// ensure the first virus cleared per turn gives the minimum amount of points
					Virus.ResetBonusPointGain();
				}
			}
			else
			{
				// a copy of the reference is made because if the pill is split from being partly above the board the original is set to null
				PillHalf currentPillLeftHalf = PillHalf.currentPillLeftHalf;

				// only need to check the pill in control if it still is in control to make it fall
				if (currentPillLeftHalf != null)
				{
					if (currentPillLeftHalf.TryToFall(out bool doNotCheck))
					{
						Block.stillFalling.Add(currentPillLeftHalf);
					}
					else if (Block.stillFalling.Contains(currentPillLeftHalf))
					{
						/* if the pill stopped falling, remove it from the list and add it to the checking list if it needs to be checked
						 * it does not need to be checked if it is places halfway above the game board as the pill gets destroyed and
						 * replaced with a normal block */
						Block.stillFalling.Remove(currentPillLeftHalf);

						if (!doNotCheck)
							blocksToCheck.Add(currentPillLeftHalf);
					}
				}
				else
				{
					/* iterate through every spot in the board and call the fall function of each block that exists
					 * it is important to start from the bottom and move upwards so the blocks above don't fall and "land" onto a falling block below */
					for (int y = 0; y < Block.boardSizeY; y++)
					{
						for (int x = 0; x < Block.boardSizeX; x++)
						{
							Block block = Block.gameBoard[x, y];

							if (block == null)
								continue;
								
							if (block.TryToFall(out bool _))
							{
								Block.stillFalling.Add(block);
							}
							else if (Block.stillFalling.Contains(block))
							{
								// if the block stopped falling, remove it from the still falling set
								Block.stillFalling.Remove(block);

								// add the block to this set to later check it for matches
								blocksToCheck.Add(block);
							}
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