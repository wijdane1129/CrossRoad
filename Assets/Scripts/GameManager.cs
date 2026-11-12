using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game objects")]
    [SerializeField] private Transform character;
    [SerializeField] private Transform characterModel;
    [SerializeField] private Transform terrainHolder;
    [SerializeField] private TMPro.TextMeshProUGUI scoreText;


    [Header("Terrain objects")]
    [SerializeField]
    private Grass grassPrefab;
    [SerializeField]
    private Road roadPrefab;



    [Header("Game parameters")]
   [SerializeField] private float moveDuration = 0.2f;

   
   [SerializeField] private int  spawnDistance = 20;

    enum GameState
    {
        Ready,
        Moving,
        Dead
    }

    private GameState gameState;
    private Vector2Int characterPos;
    private int spawnLocation;
    private List<(float terrainHeight, HashSet<int> locations)> obstacles = new ();
    private int score = 0;
    void Awake()
    {
        // Initialise all the starting state.
        NewLevel();
    }

    private void NewLevel()
    {
        gameState = GameState.Ready;

        //Reset character position
        characterPos = new Vector2Int(0, -1);
        character.position = new Vector3(0, 0.2f, -1);
        character.GetComponent<Character>().Reset();




        score = 0;
        scoreText.text = "0";

        // Remove all terrain
        obstacles.Clear();
        foreach (Transform child in terrainHolder)
        {
            Destroy(child.gameObject);
        }


        // Reset level, and regenerate
        spawnLocation = 0;
        for (int i = 0; i < spawnDistance; i++)
        {
            SpawnObstacle();
        }
    
}
    private void SpawnObstacle()
    {
        float roadProbability = Mathf.Lerp(0.5f , 0.9f, spawnLocation / 250f);
        if (Random.value < roadProbability)
        {
            // Create a new road object
            Road road = Instantiate(roadPrefab, terrainHolder);
            obstacles.Add((0.1f, road.Init(spawnLocation)));

        }
        else
        {
            // Create a new grass object
            Grass grass = Instantiate(grassPrefab, terrainHolder);

            // Record the height and locations of obstacles for this row.
            obstacles.Add((0.2f, grass.Init(spawnLocation)));
        }
       
        // Move to the next row.
        spawnLocation++;
    }

    private bool InStartArea(Vector2Int location)
    {
        // Movement anywhere in the starting region is allowed.
        if ((location.y > -5) && (location.y < 0) && (location.x > -6) && (location.x < 6))
        {
            return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        // Detect arrow key presses.
        if (gameState == GameState.Ready)
        {
            Vector2Int moveDirection = Vector2Int.zero;

            // Single if/else don't want to move diagonally.
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                character.localRotation = Quaternion.identity;
                moveDirection.y = 1;
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                character.localRotation = Quaternion.Euler(0, 180, 0);
                moveDirection.y = -1;
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                character.localRotation = Quaternion.Euler(0, -90, 0);
                moveDirection.x = -1;
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                character.localRotation = Quaternion.Euler(0, 90, 0);
                moveDirection.x = 1;
            }

            // If the user wants to move
            if (moveDirection != Vector2Int.zero)
            {
                Vector2Int destination = characterPos + moveDirection;

                // In the start area there are no obstacles so you can move anywhere.
                if (InStartArea(destination) ||((destination.y>=0)  && !obstacles[destination.y].locations.Contains(destination.x)))
                {
                    characterPos = destination;
                    StartCoroutine(MoveCharacter());
                    if ((destination.y+1) > score)
                    {
                        score = destination.y + 1;
                        scoreText.text = $"{score}";
                    }
                }

                while (obstacles.Count < (characterPos.y + spawnDistance))
                {
                    SpawnObstacle();
                }
            }
        }

        if (gameState == GameState.Dead &&  Keyboard.current.spaceKey.wasPressedThisFrame)
       
            {
                NewLevel();
            }
        
        //camera follo at (5,11,-13)
        Vector3 cameraPosition = new Vector3(character.position.x + 5, 11, character.position.z - 13);

        cameraPosition.x = Mathf.Clamp(cameraPosition.x, -1, 5);
        Camera.main.transform.position= cameraPosition;
    }

    private IEnumerator MoveCharacter()
    {
        gameState = GameState.Moving;
        float elapsedTime = 0f;


        // The yHeight changes if we're on grass or road.
        float yHeight = 0.2f;
        if (characterPos.y >= 0)
        {
            yHeight = obstacles[characterPos.y].terrainHeight;
        }
        Vector3 startPos = character.position;
        Vector3 endPos = new(characterPos.x, yHeight, characterPos.y);
        Quaternion startRotation = characterModel.localRotation;

        while (elapsedTime < moveDuration)
        {
            // How far through the animation are we.
            float percent = elapsedTime / moveDuration;

            // Update the character position
            Vector3 newPos = Vector3.Lerp(startPos, endPos, percent);
            newPos.y = yHeight + (0.5f * Mathf.Sin(percent * Mathf.PI));
            character.position = newPos;

            Vector3 rotation= characterModel.localRotation.eulerAngles;
            characterModel.localRotation = Quaternion.Euler(-5f * Mathf.PI *Mathf.Cos(Mathf.PI * percent),rotation.y, rotation.z);

            // Update the elapsed time
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we're at the end.
        character.position = endPos;

        // Need to check we're still in moving at the end.
        // If we're dead we don't want to go back to ready.
        if (gameState == GameState.Moving)
        {
            gameState = GameState.Ready;
        }
    }
    public void PlayerCollision()
    {
        gameState = GameState.Dead;
        
    }

   
    }
