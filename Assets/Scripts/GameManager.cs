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
    [SerializeField] private TMPro.TextMeshProUGUI levelText; // optional
    [SerializeField] private GameObject gameOverPanel;              // le panneau GAME OVER
    [SerializeField] private TMPro.TextMeshProUGUI finalScoreText;  // optionnel : score affiché à la fin


    [Header("Terrain objects")]
    [SerializeField]
    private Grass grassPrefab;
    [SerializeField]
    private Road roadPrefab;

    [SerializeField]
    private Coin coinPrefab; // optional, a gold disc is generated if empty

    [Header("Coins")]
    [SerializeField] private int coinValue = 5;               // points per coin
    [SerializeField][Range(0f, 1f)] private float coinChance = 0.25f; // chance a row has a coin

    [Header("Levels (driven by score)")]
    [SerializeField] private int pointsPerLevel = 20;  // score needed per level
    [SerializeField] private int maxLevel = 20;        // difficulty stops growing here

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
    private Dictionary<Vector2Int, Coin> coins = new();
    private int score = 0;
    private int furthestRow = 0;   // distance part of the score
    private int coinsCollected = 0;
    private int level = 1;
    private Material coinMaterial;
    void Awake()
    {
        // Initialise all the starting state.
        NewLevel();
    }

    private void NewLevel()
    {
        gameState = GameState.Ready;
        Road.isStopped = false;                                   // les voitures roulent
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // on cache GAME OVER

        //Reset character position
        characterPos = new Vector2Int(0, -1);
        character.position = new Vector3(0, 0.2f, -1);
        character.GetComponent<Character>().Reset();




        furthestRow = 0;
        coinsCollected = 0;
        UpdateScore();

        // Remove all terrain (coins are children of the terrain holder too)
        obstacles.Clear();
        coins.Clear();
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
    // 0 at level 1, 1 at maxLevel. Everything that gets harder uses this.
    private float Difficulty()
    {
        return Mathf.Clamp01((level - 1) / (float)Mathf.Max(1, maxLevel - 1));
    }

    private void SpawnObstacle()
    {
        float difficulty = Difficulty();
        float roadProbability = Mathf.Lerp(0.5f, 0.9f, difficulty);
        HashSet<int> locations;
        if (Random.value < roadProbability)
        {
            // Create a new road object
            Road road = Instantiate(roadPrefab, terrainHolder);
            locations = road.Init(spawnLocation, difficulty);
            obstacles.Add((0.1f, locations));
        }
        else
        {
            // Create a new grass object
            Grass grass = Instantiate(grassPrefab, terrainHolder);

            // Record the height and locations of obstacles for this row.
            locations = grass.Init(spawnLocation);
            obstacles.Add((0.2f, locations));
        }

        TrySpawnCoin(spawnLocation, obstacles[spawnLocation].terrainHeight, locations);

        // Move to the next row.
        spawnLocation++;
    }

    private void TrySpawnCoin(int row, float terrainHeight, HashSet<int> blocked)
    {
        if (Random.value >= coinChance) return;

        // Pick a free column (-5..5), not occupied by a tree.
        int x = Random.Range(-5, 6);
        if (blocked.Contains(x)) return;

        Vector3 pos = new Vector3(x, terrainHeight + 0.6f, row);
        Coin coin = coinPrefab != null
            ? Instantiate(coinPrefab, pos, Quaternion.identity, terrainHolder)
            : CreateDefaultCoin(pos);
        coins[new Vector2Int(x, row)] = coin;
    }

    // Fallback so the feature works without making a prefab first.
    private Coin CreateDefaultCoin(Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Coin";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(terrainHolder);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (coinMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            coinMaterial = new Material(shader);
            coinMaterial.color = new Color(1f, 0.82f, 0.1f);
        }
        go.GetComponent<Renderer>().sharedMaterial = coinMaterial;
        return go.AddComponent<Coin>();
    }

    private void CollectCoinAt(Vector2Int pos)
    {
        if (coins.TryGetValue(pos, out Coin coin))
        {
            coins.Remove(pos);
            if (coin != null) Destroy(coin.gameObject);
            coinsCollected++;
            UpdateScore();
        }
    }

    // score = distance + coins. The level comes from the score.
    private void UpdateScore()
    {
        score = furthestRow + coinsCollected * coinValue;
        scoreText.text = $"{score}";

        level = 1 + score / Mathf.Max(1, pointsPerLevel);
        if (levelText != null) levelText.text = $"Level {level}";
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
                    if ((destination.y + 1) > furthestRow)
                    {
                        furthestRow = destination.y + 1;
                        UpdateScore();
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
            CollectCoinAt(characterPos);
        }
    }
    public void PlayerCollision()
    {
        gameState = GameState.Dead;

        // Arrête toutes les voitures
        Road.isStopped = true;

        // Affiche le score final
        if (finalScoreText != null)
            finalScoreText.text = $"Score : {score}\nPièces : {coinsCollected}\nNiveau : {level}";

        // Affiche l'écran GAME OVER
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }


}
