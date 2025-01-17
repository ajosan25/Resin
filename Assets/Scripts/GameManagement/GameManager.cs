using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;
using JetBrains.Annotations;
using System.Net.Http;

public class GameManager : MonoBehaviour
{   
    // Reference to logger script
    // Allows other scripts to log to conole
    public Logger logger;

    // Get name from MenuInfo
    public string name = "";

    // Is game scene paused
    bool paused = false;

    // Status variables for shop
    bool escape_released = true;
    bool shop_open = false;

    public MenuInfo info;

    // HTTP client for sending scores
    private static readonly HttpClient client = new HttpClient();
    private static readonly string url = "http://localhost:5000/addscore";

    // Info for scoring
    public int turns = 0;
    public int arrows = 3;
    public int coins = 0;
    private int roomNum = 0;
    public int lives = 5;

    // Text to update room number
    public TMP_Text roomText;

    // Wumpus Room objects
    public GameObject wumpusRoom;
    public GameObject bossObject;
    public Transform wumpusSpawnLoc;

    // Has the player lost?
    private bool lost = false;

    // Player fighting state with Wumpus
    private bool fighting = false;

    // GameObject for player
    public GameObject Player;

    // GameObjects for UI
    public GameObject pauseUI;
    public GameObject ShopUI;
    public TextMeshProUGUI Inventory;
    public GameObject BatUI;
    public GameObject PitUI;
    public GameObject WinUI;
    public GameObject LoseUI;

    // Test variables
    public bool testmode;
    bool testing = false;
    public TMP_Text testText;
    public GameObject testUI;

    // Reference to current room
    // Breaks abstraction because move() logic needs to be in GameManager
    // So most logical place to update roomNum is here
    public Room currentRoom(){
        return GetComponent<RoomGenerator>().rooms[roomNum];
    }
    // Pause the game, stop time
    public void pauseGame(){
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0;
        paused = true;
    }

    // Resume the game, start time
    public void playGame(){
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        Time.timeScale = 1;
        paused = false;
    }
 
    // Initialize variables
    void Start(){
        arrows = 3;
        // Set UI to inactive
        ShopUI.SetActive(false);
        pauseUI.SetActive(false);
        BatUI.SetActive(false);
        PitUI.SetActive(false);
        WinUI.SetActive(false);
        LoseUI.SetActive(false);

        // Set player position to spawn location
        Player.transform.position = currentRoom().spawnLocation.position;

        // Check testmode
        info = GameObject.Find("MenuInfo").GetComponent<MenuInfo>();
        if (info == null){
            name = "test";
        } else {
            name = info.name;
        }
        testmode = true;
        Debug.Log("it is true");
        logger.log("You're mine now ... You have 5 lives, 3 arrows, and 0 coins. Good luck. You'll need it.");

        if (testmode){
            coins = 500;
            arrows = 50;
        }
    }

    public void bossFight(){
        // load wumpus scene
        wumpusRoom.SetActive(true);
        Player.transform.position = wumpusSpawnLoc.position;
        fighting = true;
    }

    // this will pick a room and return it, such that bat Teleporter can use it. 
    public Room randomRoom(){
        roomNum = Random.Range(0, 30);
        move(roomNum, true, null);
        return GetComponent<RoomGenerator>().rooms[roomNum];
    }

    // various UI and shop things. 
    public void spend(int amount){
        if (coins - amount < 1){
            lose();
        } else {
            coins -= amount;
        }
    }

    // Open Shop UI
    public void OpenShop(){
        ShopUI.SetActive(true);
        pauseUI.SetActive(false);
        shop_open = true;
    }

    // Close Shop UI
    public void CloseShop(){
        ShopUI.SetActive(false);
        pauseUI.SetActive(true);
        shop_open = false;
    }


    // Win the game
    public void win(int wumpus){
        // Display win screen
        WinUI.SetActive(true);
        pauseGame();
        score(wumpus);
    }
    // Test function for loss with TestUI
    // Needed because buttons cant use bool functions
    public void lossTest(){
        lose(true);
    }

    // lose the game
    public bool lose(bool overrideLoss = false){
        if (!lost){
            // If you have lost all lives or are overriding for test purposes
            // Or are overriding for other loss
            if (lives <= 1 || overrideLoss){
                score(0);
                lost = true;
                LoseUI.SetActive(true);
                pauseGame();
                return true;
            } else {
                lives-= 1;
                return false;
            }
        } else {
            pauseGame();
            LoseUI.SetActive(true);
            return true;
        }
    }
    // Post the score to the server
    public async void score(int wumpus) {
        int score = 100 - turns + coins + (5 * arrows) + wumpus;
        
        var values = new Dictionary<string, string>
        {
            { "password", "resin" },
            { "name", name },
            { "score", score.ToString() },
            { "turns", turns.ToString() },
            { "coins", coins.ToString() },
            { "arrows", arrows.ToString() },
            { "wumpus", wumpus.ToString() }
        };
        
        var content = new FormUrlEncodedContent(values);
        try{
            var response = await client.PostAsync(url, content);
        } catch (HttpRequestException e){
            Debug.Log(e.Message);
        }
    }
    // Shoot the wumpus
    public void shoot(int roomNum, Teleporter tp){
        //shoot wumpus;
        if (arrows > 0){
            arrows--;
            
            // If correct, load wumpus Scene
            if (tp is BossTeleporter){
                logger.log("Beep bop beep ... you hit me. Now, I'm angry. Unleashing backpropagation. Prepare to die.");
                bossFight();
                playGame();
            } else {
                logger.log("Ha! Missed me! I might automate you away after all ...");
                move(roomNum, true, tp);
            }
        }
    }

    // Undo player after moving between scenes
    public IEnumerator stopPlayerMovement(){
        // Get the change in velocity over time
        // IE acceleration
        Rigidbody otherRb = Player.GetComponent<Rigidbody>();
        Vector3 prevVelocity = otherRb.velocity;
        yield return new WaitForSecondsRealtime(0.1f);
        Vector3 currVelocity = otherRb.velocity;


        // F= ma
        Vector3 diff = currVelocity - prevVelocity;
        Vector3 force = diff * otherRb.mass;
        // Reverse forces
        otherRb.AddForce(-force);
        yield return null;
    }
    
    // Move between rooms
    public void move(int room, bool disable, Teleporter tp){
        // Get room array
        Room[] rooms = GameObject.Find("GameManager").GetComponent<RoomGenerator>().rooms;
        // Set this room to visited, disable it
        rooms[roomNum].visited = true;
        if (disable) {
            rooms[roomNum].gameObject.SetActive(false);
        }
    
        // Change room number
        roomNum = room;
        if (!rooms[roomNum].visited){
            coins++;
        }
        turns++;
        
        // Restart time
        playGame();

        // Show an unknown answer to the trivia
        logger.log(GameObject.Find("Trivia").GetComponent<Trivia>().getUnknownAnswer());

        // enable the camera
        Player.GetComponentInChildren<MouseLook>().enabled = true;
        // tell the teleporter it came from to move the player
        // only if it is not being moved randomly
        StartCoroutine(stopPlayerMovement());
        if (tp != null){
            tp.MovePlayer(Player.GetComponent<Collider>());
            tp = null;
        }
    }

    // Unpause the game from UI
    public void Unpause(){
        pauseUI.SetActive(false);
        playGame();
    }

    // Runs every frame
    public void Update(){
        // Update texts
        RoomGenerator rg = GetComponent<RoomGenerator>();
        testText.text = "Wumpus Room: " + rg.wumpusRoom + "\n Bat Room: " + rg.batRoom + "\n Pit Room: " + rg.pitRoom + "\n Current Room: " + roomNum;
        roomText.text = "Room " + roomNum.ToString();
        Inventory.text = "Coins: " + coins+ "\nArrows: " + arrows + "\nLives: " + lives + "\nTurns: " + turns;

        if (fighting) {
            if (bossObject.GetComponent<Shootable>().currentHealth <= 0){
                win(50);
                fighting = false;
            }
        }

        // will only check once per frame. 
        // thus, there is no case of it flickering on and off. 
        if (Input.GetKey(KeyCode.Escape) && !paused && escape_released && !shop_open){
            pauseGame();
            pauseUI.SetActive(true);
            // since the escape key was just pressed, mark it as being unable to be used until the key gets released. 
            escape_released = false;
        }
        // check or no check for paused, either is fine. 
        else if (Input.GetKey(KeyCode.Escape) && paused && escape_released && !shop_open){
            playGame();
            pauseUI.SetActive(false);
            // since the escape key was just pressed, mark it as being unable to be used until the key gets released. 
            escape_released = false;
        }
        // another check built for closing shop with escape. 
        else if (Input.GetKey(KeyCode.Escape) && escape_released && shop_open){
            CloseShop();
            escape_released = false;
        }

        if (testmode && Input.GetKeyDown(KeyCode.T) && !testing && pauseUI.activeSelf){
            testUI.gameObject.SetActive(true);
            testing = true;
        } else if (Input.GetKeyDown(KeyCode.T) && testing){
            testUI.gameObject.SetActive(false);
            testing = false;
        }


        // when escape gets released, Input.GetKeyUp returns true for 1 frame. 
        // This will persist and reset the ability for the escape button to be repressed. 
        if (Input.GetKeyUp(KeyCode.Escape)){
            escape_released = true;
        }
    }

    // Test functions
    // Sets the first teleporter to whatever you are trying to trigger
    // Triggers that teleporter
    public void batTest(){
        Room room = currentRoom();
        GameObject obj = room.doors[0].gameObject;
        Room tempRoom = room.doors[0].next;
        Destroy(obj.GetComponent<Teleporter>());
        room.doors[0] = obj.AddComponent<BatTeleporter>();
        room.doors[0].next = tempRoom;
        room.doors[0].Awake();
        room.doors[0].MovePlayer(Player.GetComponent<Collider>());
        playGame();
        pauseUI.SetActive(false);
    }

    public void pitTest(){
        Room room = currentRoom();
        GameObject obj = room.doors[0].gameObject;
        Room tempRoom = room.doors[0].next;
        Destroy(obj.GetComponent<Teleporter>());
        room.doors[0] = obj.AddComponent<PitTeleporter>();
        room.doors[0].next = tempRoom;
        room.doors[0].Awake();
        room.doors[0].InteractPlayer(Player.GetComponent<Collider>());
        playGame();
        pauseUI.SetActive(false);
        testUI.SetActive(false);
    }

    public void wumpusTest(){
        Room room = currentRoom();
        GameObject obj = room.doors[0].gameObject;
        Room tempRoom = room.doors[0].next;
        Destroy(obj.GetComponent<Teleporter>());
        room.doors[0] = obj.AddComponent<BossTeleporter>();
        room.doors[0].next = tempRoom;
        room.doors[0].Awake();
        room.doors[0].InteractPlayer(Player.GetComponent<Collider>());
        playGame();
        pauseUI.SetActive(false);
        testUI.SetActive(false);
    }

    public void wumpusShootTest(){
        Room room = currentRoom();
        GameObject obj = room.doors[0].gameObject;
        Room tempRoom = room.doors[0].next;
        Destroy(obj.GetComponent<Teleporter>());
        room.doors[0] = obj.AddComponent<BossTeleporter>();
        room.doors[0].next = tempRoom;
        room.doors[0].Awake();
        shoot(room.doors[0].next.roomNum, room.doors[0]);
        playGame();
        pauseUI.SetActive(false);
        testUI.SetActive(false);
    }
}