using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GalaxyAttackController : SpaceShooterBase
{
    [Header("Refs")]
    [SerializeField] MinigameController minigame;
    [SerializeField] Transform gameplayRoot;
    [SerializeField] TMP_Text livesText, scoreText, waveText;

    [Header("Prefabs")]
    [SerializeField] PlayerShip playerPrefab;
    [SerializeField] EnemyUnit enemyPrefab;
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] Asteroid asteroidPrefab;
    [SerializeField] PowerUp powerUpPrefab;

    [Header("Playfield")]
    [SerializeField] Vector2 boundsMin = new(-8f, -4.5f);
    [SerializeField] Vector2 boundsMax = new( 8f,  4.5f);
    [SerializeField] float playerStartY = -3.3f;

    [Header("Waves & Difficulty")]
    [SerializeField] int targetWaves = 7;
    [SerializeField] float enemyBaseSpeed = 3f;
    [SerializeField] float enemyFireChancePerSecond = 0.25f;
    [SerializeField] float asteroidPerSecond = 0.12f;
    [SerializeField] float powerUpChanceOnKill = 0.10f;
    public Vector2 BoundsMin => boundsMin;


    [Header("Variety")]
    [SerializeField] bool shuffleWaveOrder = false;
    [SerializeField] int seed = 0;

    PlayerShip player;
    readonly List<EnemyUnit> enemies = new();
    bool started, playing;
    public int lives = 5;
    int maxLives = 5;
    int score = 0, waveIdx = 0;
    List<int> waveOrder;

    void Start()
    {
        if (!minigame) minigame = FindObjectOfType<MinigameController>();
        if (!gameplayRoot && minigame && minigame.gameplayRoot)
            gameplayRoot = minigame.gameplayRoot.transform;

        BuildWaveOrder();
        UpdateHUD();
    }

    void Update()
    {
        if (!started && minigame && minigame.playing) { started = true; StartRun(); }
        if (!playing) return;

        if (Random.value < asteroidPerSecond * Time.deltaTime)
            SpawnAsteroid();
    }

    void BuildWaveOrder()
    {
        waveOrder = new List<int>();
        for (int i = 1; i <= targetWaves; i++) waveOrder.Add(i);

        if (shuffleWaveOrder)
        {
            var rng = (seed == 0) ? new System.Random() : new System.Random(seed);
            for (int i = 0; i < waveOrder.Count; i++)
            {
                int j = rng.Next(i, waveOrder.Count);
                (waveOrder[i], waveOrder[j]) = (waveOrder[j], waveOrder[i]);
            }
        }
    }

    void StartRun()
    {
        playing = true;

        if (!player)
        {
            player = Instantiate(playerPrefab, new Vector3(0f, playerStartY, 0f), Quaternion.identity, gameplayRoot);
            player.Init(this, projectilePrefab, boundsMin, boundsMax);
        }

        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        waveIdx = 0;
        while (playing && waveIdx < waveOrder.Count)
        {
            int waveNumber = waveOrder[waveIdx];
            UpdateHUD(waveNumber);

            yield return StartCoroutine(SpawnWave(waveNumber));

            waveIdx++;
            if (waveIdx < waveOrder.Count) yield return new WaitForSeconds(0.8f);
        }
        if (playing) Win();
    }

    IEnumerator SpawnWave(int waveNumber)
    {
        enemies.Clear();

        // Difficulty scaling per wave
        float speed = enemyBaseSpeed + 0.1f * (waveNumber - 1);
        float firePS = enemyFireChancePerSecond * (1f + 0.20f * (waveNumber - 1));

        switch (waveNumber)
        {
            case 1: yield return StartCoroutine(Wave1(speed, firePS)); break;
            case 2: yield return StartCoroutine(Wave2(speed, firePS)); break;
            case 3: yield return StartCoroutine(Wave3(speed, firePS)); break;
            case 4: yield return StartCoroutine(Wave4(speed, firePS)); break;
            case 5: yield return StartCoroutine(Wave5(speed, firePS)); break;
            case 6: yield return StartCoroutine(Wave6(speed, firePS)); break;
            case 7: yield return StartCoroutine(Wave7(speed, firePS)); break;
            default: yield return StartCoroutine(Wave1(speed, firePS)); break;
        }

        while (AnyEnemyAlive()) yield return null;
    }


    IEnumerator Wave1(float speed, float firePS)
    {
        int lanes = 5;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(boundsMin.x + 1f, boundsMax.x - 1f, i / (float)(lanes - 1));
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.Straight, speed, firePS);
        }
        yield return new WaitForSeconds(0.6f);
    }

    // W2: Sine squad stream
    IEnumerator Wave2(float speed, float firePS)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = Random.Range(boundsMin.x + 1.2f, boundsMax.x - 1.2f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.Sine, speed, firePS);
            yield return new WaitForSeconds(0.18f);
        }
    }

    // W3: Zig-zag flankers
    IEnumerator Wave3(float speed, float firePS)
    {
        for (int i = 0; i < 6; i++)
        {
            float x = (i % 2 == 0) ? boundsMin.x + 1.0f : boundsMax.x - 1.0f;
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.6f, 0f), EnemyPathMover.PathType.ZigZag, speed, firePS);
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < 6; i++)
        {
            float x = Random.Range(boundsMin.x + 1.2f, boundsMax.x - 1.2f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.6f, 0f), EnemyPathMover.PathType.Straight, speed, firePS);
        }
    }

    // W4: Rain (random top spawn), denser fire
    IEnumerator Wave4(float speed, float firePS)
    {
        int count = 12;
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(boundsMin.x + 0.8f, boundsMax.x - 0.8f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.7f, 0f), EnemyPathMover.PathType.Straight, speed, firePS * 1.1f);
            yield return new WaitForSeconds(0.12f);
        }
    }

    // W5: V-formation (two Vs)
    IEnumerator Wave5(float speed, float firePS)
    {
        yield return StartCoroutine(SpawnV(speed, firePS, leftToRight: true));
        yield return new WaitForSeconds(0.35f);
        yield return StartCoroutine(SpawnV(speed + 0.2f, firePS, leftToRight: false));
    }

    IEnumerator SpawnV(float speed, float firePS, bool leftToRight)
    {
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1); // 0..1
            float x = Mathf.Lerp(boundsMin.x + 1.2f, boundsMax.x - 1.2f, leftToRight ? t : 1f - t);
            float y = boundsMax.y + 0.4f + Mathf.Abs(t - 0.5f) * 0.6f; // shallow V
            SpawnEnemy(new Vector3(x, y, 0f), EnemyPathMover.PathType.Straight, speed, firePS);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // W6: Mixed sine+zig pairs, fast
    IEnumerator Wave6(float speed, float firePS)
    {
        for (int i = 0; i < 4; i++)
        {
            float xl = Mathf.Lerp(boundsMin.x + 1.1f, boundsMax.x - 1.1f, i / 3f);
            SpawnEnemy(new Vector3(xl, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.Sine, speed, firePS * 1.1f);
            yield return new WaitForSeconds(0.12f);
            SpawnEnemy(new Vector3(xl, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.ZigZag, speed, firePS * 1.1f);
            yield return new WaitForSeconds(0.12f);
        }
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < 4; i++)
        {
            float x = Random.Range(boundsMin.x + 1.2f, boundsMax.x - 1.2f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.Straight, speed, firePS * 1.1f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // W7: Gauntlet — many spawns, highest pace
    IEnumerator Wave7(float speed, float firePS)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = Random.Range(boundsMin.x + 1.0f, boundsMax.x - 1.0f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.8f, 0f), EnemyPathMover.PathType.Straight, speed, firePS * 1.3f);
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.2f);
        for (int i = 0; i < 10; i++)
        {
            var path = (i % 2 == 0) ? EnemyPathMover.PathType.Sine : EnemyPathMover.PathType.ZigZag;
            float x = Random.Range(boundsMin.x + 1.2f, boundsMax.x - 1.2f);
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.6f, 0f), path, speed, firePS * 1.25f);
            yield return new WaitForSeconds(0.1f);
        }
        int lanes = 6;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(boundsMin.x + 0.9f, boundsMax.x - 0.9f, i / (float)(lanes - 1));
            SpawnEnemy(new Vector3(x, boundsMax.y + 0.5f, 0f), EnemyPathMover.PathType.Straight, speed, firePS * 1.4f);
        }
        yield return null;
    }


    void SpawnEnemy(Vector3 pos, EnemyPathMover.PathType path, float speed, float firePS)
    {
        var e = Instantiate(enemyPrefab, pos, Quaternion.identity, gameplayRoot);
        e.Init(this, projectilePrefab, boundsMin, boundsMax);
        var mover = e.GetComponent<EnemyPathMover>();
        if (!mover) mover = e.gameObject.AddComponent<EnemyPathMover>();
        mover.Setup(path, speed, 1.2f, 2.2f);
        enemies.Add(e);
        StartCoroutine(EnemyFireLoop(e, firePS));
    }

    IEnumerator EnemyFireLoop(EnemyUnit e, float firePS)
    {
        float t = Random.Range(0.15f, 0.6f);
        while (playing && e && e.gameObject.activeInHierarchy)
        {
            t -= Time.deltaTime;
            if (t <= 0f) { e.Fire(); t = Random.Range(0.35f, 0.85f) / Mathf.Max(0.2f, firePS); }
            yield return null;
        }
    }

    void SpawnAsteroid()
    {
        var a = SimplePool.Spawn(asteroidPrefab, new Vector3(Random.Range(boundsMin.x, boundsMax.x), boundsMax.y + 0.8f, 0f), Quaternion.identity);
        a.Launch(boundsMin, boundsMax);
    }

    bool AnyEnemyAlive()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
            if (!enemies[i]) enemies.RemoveAt(i);
        foreach (var e in enemies)
            if (e && e.gameObject.activeInHierarchy) return true;
        return false;
    }

    void UpdateHUD(int currentWave = 1)
    {
        if (livesText) livesText.text = $"Lives: {Mathf.Max(0, lives)}";
        if (scoreText) scoreText.text = $"Score: {score}";
        if (waveText)  waveText.text  = $"Wave: {currentWave}/{targetWaves}";
    }

    void Lose() { playing = false; minigame?.OnLose(); }
    void Win()  { 
        GameState.I.SetFlag("harmonia_completed");
        playing = false; minigame?.OnWin();
        
    }

    public override void PlayerHit()
    {
        if (!playing) return;
        lives--;
        UpdateHUD(waveIdx < waveOrder.Count ? waveOrder[waveIdx] : targetWaves);
        if (lives <= 0) { Lose(); return; }
        if (player) player.Respawn(new Vector3(0f, playerStartY, 0f));
    }

    public override void OnPickup(PowerUp.Kind k)
    {
        if (k == PowerUp.Kind.Health)
        {
            lives = maxLives;
            UpdateHUD(waveIdx < waveOrder.Count ? waveOrder[waveIdx] : targetWaves);
            if (player) player.BeginInvincibility(1.0f);
        }
    }


    public void OnEnemyLeaked(EnemyUnit e)
    {
        enemies.Remove(e);
        PlayerHit();
    }

    public void OnEnemyKilled(EnemyUnit e)
    {
        score += 10;
        enemies.Remove(e);
        UpdateHUD(waveIdx < waveOrder.Count ? waveOrder[waveIdx] : targetWaves);

        if (powerUpPrefab && Random.value < powerUpChanceOnKill)
        {
            var p = SimplePool.Spawn(powerUpPrefab, e.transform.position, Quaternion.identity);
            p.Drop(e.transform.position, boundsMin, boundsMax);
        }
    }
}
