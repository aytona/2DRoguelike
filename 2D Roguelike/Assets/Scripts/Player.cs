using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Player : MovingObject {

	public float restartLevelDelay = 1f;					// Delay time in seconds to restart level
	public int wallDamage = 1;								// How much damage a player does to a wall when chopping it
	public int pointsPerFood = 10;							// Number of points to add to player food points when picking up a food object
	public int pointsPerSoda = 20;							// Number of points to add to player food points when picking up a soda object
	public Text foodText;									// UI Text to display current player food total
	public AudioClip moveSound1;							// 1 of 2 Audio clips to play when player moves
	public AudioClip moveSound2;							// 2 of 2 Audio clips to play when player moves
	public AudioClip eatSound1;								// 1 of 2 Audio clips to play when player collects a food object
	public AudioClip eatSound2;								// 2 of 2 Audio clips to play when player collects a food object
	public AudioClip drinkSound1;							// 1 of 2 Audio clips to play when player collects a soda object
	public AudioClip drinkSound2;							// 2 of 2 Audio clips to play when player collects a soda object
	public AudioClip gameOverSound;							// Audio clip to play when player dies
	
	private Animator animator;								// Used to store a reference to the Player's animator component
	private int food;										// Used to store player food points total during level

	// Start overrides the Start fucntion of MovingObject
	protected override void Start()
	{
		// Get a component reference to the Player's animator component
		animator = GetComponent<Animator>();

		// Get the current food point total stored in GameManager.instnace between levels
		food = GameManager.instance.playerFoodPoints;

		foodText.text = "Food: " + food;

		// Call the Start function of the MovingObject base class
		base.Start();
	}

	// This function is called when the behaviour becomes disabled or inactive
	private void OnDisable()
	{
		// When Player object is disabled, store the current local food total in the GameMnager so it can be re-loaded in next level
		GameManager.instance.playerFoodPoints = food;
	}

	void Update()
	{
		// If it's not the player's turn, exit the function
		if(!GameManager.instance.playersTurn) return;

		int horizontal = 0;									// Used to store the horizontal move direction
		int vertical = 0;									// Used to store the vertical move direction

		// Get input from the input manager, round it to an interger and store in horizontal to set x axis move direction
		horizontal = (int) Input.GetAxisRaw("Horizontal");

		// Get input from the input manager, round it to an integer and store in vertical to set y axis move direction
		vertical = (int) Input.GetAxisRaw("Vertical");

		// Check if moving horizontally, if so set vertical to zero
		if (horizontal !=0)
		{
			vertical = 0;
		}

		// Check if we have a non-zero for horizontal or vertical
		if (horizontal != 0 || vertical != 0)
		{
			// Call AttemptMove passing in the generic parameter Wall, since that is what Player may interact with if they encounter one (by attacking it)
			// Pass in horizontal and vertical as parameters to specify the direction to mvoe Player in
			AttemptMove<Wall> (horizontal, vertical);
		}
	}

	// AttemptMove overrides the AttemptMove fucntion in the base class MovingObject
	// AttemptMove takes a generic parameter T which for Player will be of the type Wall, it also takes inegers for x and y direction to move in
	protected override void AttemptMove <T> (int xDir, int yDir)
	{
		// Every time player moves, subtract from food poitns total
		food--;

		foodText.text = "Food: " + food;

		// Call the AttemptMove method of the base class, passing in the component T (in this case Wall) and x and y direction to move
		base.AttemptMove <T> (xDir, yDir);

		// Hit allows us to reference the result of the Linecast done in Move
		RaycastHit2D hit;

		// If Move returns true, meaning Player was able to move into an empty space
		if (Move (xDir, yDir, out hit))
		{
			// Call RandomzeSfx of SoundManager to player the move sound, passing in tvo audio clips to choose from
			SoundManager.instance.RandomizeSfx(moveSound1, moveSound2);
		}

		// Since the player has moved and lose food points, check if the game has ended
		CheckIfGameOver();

		// Set the playerTurn boolean of GameManager to false now that players turn is over
		GameManager.instance.playersTurn = false;
	}

	// OnCantMove overrides the abstract function OnCantMove in MovingObject
	// It takes a generic parameter T which in the case of Player is a Wall which th eplayer can attack and destroy
	protected override void OnCantMove <T> (T component)
	{
		// Set hitWall to equal the component passed in as a parameter
		Wall hitWall = component as Wall;

		// Call the DamageWall function of the Wall we are hitting
		hitWall.DamageWall(wallDamage);

		// Set the attack trigger of the player's animation controller in order to play the player's attack animation
		animator.SetTrigger("playerChop");
	}

	// Restart reloads the scene when called
	private void Restart()
	{
		// Load the last scene loaded, in this case Main, the only scne in the game
		Application.LoadLevel(Application.loadedLevel);
	}

	// CheckIfGameOver checks if the player is out of food points and if so ends the game
	private void CheckIfGameOver()
	{
		// Check if food point total is less than or equal to zero
		if (food <=0)
		{
			SoundManager.instance.PlaySingle(gameOverSound);

			SoundManager.instance.musicSource.Stop();

			// Call the GameOver function of GameManager
			GameManager.instance.GameOver();
		}
	}

	// OnTriggerEnter2D is sent when another object enters a trigger collider attached to this object
	private void OnTriggerEnter2D (Collider2D other)
	{
		// Check if the tag of the trigger collided with is Exit
		if (other.tag == "Exit")
		{
			// Invoke the Restart function to start the next levle with a delay of restartLevelDelay (default 1 second)
			Invoke ("Restart", restartLevelDelay);

			// Disable the player object since levle is over
			enabled = false;
		}

		// Check if the tag of the trigger collider with is Food
		else if (other.tag == "Food")
		{
			// Add pointsPerFood to the player current food total
			food += pointsPerFood;

			foodText.text = "+" + pointsPerFood + " Food: " + food;

			SoundManager.instance.RandomizeSfx(eatSound1, eatSound2);

			// Disable the food object the player collided with
			other.gameObject.SetActive(false);
		}

		// Check if the tag of the trigger collided with is Soda
		else if (other.tag == "Soda")
		{
			// Add pointsPerSoda to players food points total
			food += pointsPerSoda;

			foodText.text = "+" + pointsPerSoda + " Food : " + food;

			SoundManager.instance.RandomizeSfx(drinkSound1, drinkSound2);

			// Disable the soda object the player collided with
			other.gameObject.SetActive(false);
		}
	}

	// LoseFood is called when an enemy attacks the player
	// It takes parameter loss which specifies how many points to lose
	public void LoseFood (int loss)
	{
		// Set the trigger for the player animator to transition to the playerHit animation
		animator.SetTrigger("playerHit");

		// Subtract lost food points from the players total
		food -= loss;

		foodText.text = "-" + loss + " Food: " + food;

		// Check to see if teh game ended
		CheckIfGameOver();
	}
}
