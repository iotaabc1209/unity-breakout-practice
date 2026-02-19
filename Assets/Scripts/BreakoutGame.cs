using UnityEngine;

public class BreakoutGame : MonoBehaviour
{
    private const float HalfWidth = 8.5f;
    private const float HalfHeight = 5f;

    private const int InitialLives = 3;
    private const int SingleHitScore = 100;
    private const int DoubleHitScore = 250;

    private GameObject paddle;
    private GameObject ball;
    private Rigidbody2D ballBody;
    private Vector3 ballOffsetFromPaddle;

    private int remainingBlocks;
    private int lives;
    private int score;

    private bool waitingForLaunch;
    private bool finished;
    private string finishMessage = string.Empty;

    [SerializeField] private float paddleSpeed = 10f; // CONFLICT_PRACTICE
    [SerializeField] private float ballSpeed = 8f;

    private void Start()
    {
        CreateBounds();
        CreatePaddle();
        CreateBlocks();
        CreateBall();
        BeginNewRound();
    }

    private void Update()
    {
        MovePaddle();

        if (finished)
        {
            if (IsLaunchPressed())
            {
                RestartGame();
            }

            return;
        }

        if (waitingForLaunch)
        {
            StickBallToPaddle();
            if (IsLaunchPressed())
            {
                LaunchBall();
            }

            return;
        }

        KeepBallSpeedConstant();

        if (ball.transform.position.y < -HalfHeight - 1f)
        {
            lives--;
            if (lives <= 0)
            {
                EndGame("GAME OVER");
            }
            else
            {
                BeginRoundWait();
            }
        }
    }

    private void OnGUI()
    {
        GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(12f, 10f, 250f, 30f), $"Lives: {lives}", hudStyle);
        GUI.Label(new Rect(12f, 40f, 250f, 30f), $"Score: {score}", hudStyle);

        if (waitingForLaunch && !finished)
        {
            DrawCenterText("SPACE / Click to Launch");
        }

        if (finished)
        {
            DrawCenterText($"{finishMessage}\nSPACE / Click to Restart");
        }
    }

    public void OnBallCollision(Collision2D collision)
    {
        if (finished || waitingForLaunch)
        {
            return;
        }

        if (collision.gameObject.GetComponent<BlockMarker>() is BlockMarker block)
        {
            bool destroyed = block.Hit();
            if (destroyed)
            {
                score += block.Durability == 1 ? SingleHitScore : DoubleHitScore;
                remainingBlocks--;

                if (remainingBlocks <= 0)
                {
                    EndGame("CLEAR!");
                }
            }

            AddSmallBounceJitter();
            return;
        }

        if (collision.gameObject.GetComponent<PaddleMarker>() != null)
        {
            ReflectFromPaddle(collision);
        }
    }

    private void MovePaddle()
    {
        float input = Input.GetAxisRaw("Horizontal");
        Vector3 position = paddle.transform.position;
        position.x += input * paddleSpeed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, -HalfWidth + 1.2f, HalfWidth - 1.2f);
        paddle.transform.position = position;
    }

    private void BeginNewRound()
    {
        lives = InitialLives;
        score = 0;
        finished = false;
        finishMessage = string.Empty;
        BeginRoundWait();
    }

    private void BeginRoundWait()
    {
        waitingForLaunch = true;
        StickBallToPaddle();
        ballBody.linearVelocity = Vector2.zero;
    }

    private void StickBallToPaddle()
    {
        ball.transform.position = paddle.transform.position + ballOffsetFromPaddle;
    }

    private void LaunchBall()
    {
        waitingForLaunch = false;
        Vector2 launchDirection = new Vector2(Random.Range(-0.35f, 0.35f), 1f).normalized;
        ballBody.linearVelocity = launchDirection * ballSpeed;
    }

    private void KeepBallSpeedConstant()
    {
        if (ballBody.linearVelocity.sqrMagnitude <= 0.0001f)
        {
            ballBody.linearVelocity = Vector2.up * ballSpeed;
            return;
        }

        ballBody.linearVelocity = ballBody.linearVelocity.normalized * ballSpeed;
    }

    private void ReflectFromPaddle(Collision2D collision)
    {
        Vector2 paddleCenter = paddle.transform.position;
        float paddleWidth = paddle.transform.localScale.x;
        float contactX = collision.GetContact(0).point.x;
        float offset = Mathf.Clamp((contactX - paddleCenter.x) / (paddleWidth * 0.5f), -1f, 1f);

        Vector2 direction = new Vector2(offset, Mathf.Sqrt(1f - Mathf.Min(0.95f, offset * offset))).normalized;
        direction.x += Random.Range(-0.06f, 0.06f);
        if (direction.y < 0.2f)
        {
            direction.y = 0.2f;
        }

        ballBody.linearVelocity = direction.normalized * ballSpeed;
    }

    private void AddSmallBounceJitter()
    {
        Vector2 currentDirection = ballBody.linearVelocity.normalized;
        currentDirection.x += Random.Range(-0.03f, 0.03f);

        if (Mathf.Abs(currentDirection.x) < 0.06f)
        {
            currentDirection.x = 0.06f * Mathf.Sign(Random.Range(-1f, 1f));
        }

        ballBody.linearVelocity = currentDirection.normalized * ballSpeed;
    }

    private void EndGame(string message)
    {
        finished = true;
        waitingForLaunch = false;
        finishMessage = message;
        ballBody.linearVelocity = Vector2.zero;
        Debug.Log(message);
    }

    private void RestartGame()
    {
        CleanupRuntimeObjects();
        CreatePaddle();
        CreateBlocks();
        CreateBall();
        BeginNewRound();
    }

    private bool IsLaunchPressed()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
    }

    private void DrawCenterText(string text)
    {
        GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), text, centerStyle);
    }

    private void CleanupRuntimeObjects()
    {
        if (paddle != null)
        {
            Destroy(paddle);
        }

        if (ball != null)
        {
            Destroy(ball);
        }

        BlockMarker[] blocks = FindObjectsByType<BlockMarker>(FindObjectsSortMode.None);
        foreach (BlockMarker block in blocks)
        {
            if (block != null)
            {
                Destroy(block.gameObject);
            }
        }
    }

    private void CreateBounds()
    {
        CreateWall("WallLeft", new Vector2(-HalfWidth - 0.25f, 0f), new Vector2(0.5f, HalfHeight * 2.5f));
        CreateWall("WallRight", new Vector2(HalfWidth + 0.25f, 0f), new Vector2(0.5f, HalfHeight * 2.5f));
        CreateWall("WallTop", new Vector2(0f, HalfHeight + 0.25f), new Vector2(HalfWidth * 2.5f, 0.5f));
    }

    private void CreatePaddle()
    {
        paddle = new GameObject("Paddle");
        paddle.transform.position = new Vector3(0f, -4f, 0f);

        SpriteRenderer renderer = paddle.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = new Color(0.2f, 0.9f, 1f);
        paddle.transform.localScale = new Vector3(2.2f, 0.35f, 1f);

        BoxCollider2D collider = paddle.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        Rigidbody2D body = paddle.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        paddle.AddComponent<PaddleMarker>();
    }

    private void CreateBall()
    {
        ball = new GameObject("Ball");
        ballOffsetFromPaddle = new Vector3(0f, 0.45f, 0f);
        ball.transform.position = paddle.transform.position + ballOffsetFromPaddle;

        SpriteRenderer renderer = ball.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = Color.white;
        ball.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        CircleCollider2D collider = ball.AddComponent<CircleCollider2D>();
        PhysicsMaterial2D material = new PhysicsMaterial2D("BallBounce")
        {
            bounciness = 1f,
            friction = 0f
        };
        collider.sharedMaterial = material;

        ballBody = ball.AddComponent<Rigidbody2D>();
        ballBody.gravityScale = 0f;
        ballBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ballBody.freezeRotation = true;

        BallCollision ballCollision = ball.AddComponent<BallCollision>();
        ballCollision.Setup(this);
    }

    private void CreateBlocks()
    {
        const int rows = 4;
        const int cols = 8;

        remainingBlocks = rows * cols;
        float startX = -6f;
        float startY = 3.2f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                GameObject block = new GameObject($"Block_{row}_{col}");
                block.transform.position = new Vector3(startX + (col * 1.7f), startY - (row * 0.7f), 0f);
                block.transform.localScale = new Vector3(1.5f, 0.45f, 1f);

                int durability = (row % 2 == 0) ? 1 : 2;
                Color baseColor = durability == 1 ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.35f, 0.35f);

                SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateSquareSprite();
                renderer.color = baseColor;

                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                BlockMarker marker = block.AddComponent<BlockMarker>();
                marker.Setup(durability, renderer);
            }
        }
    }

    private void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private Sprite CreateSquareSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}

public class BallCollision : MonoBehaviour
{
    private BreakoutGame game;

    public void Setup(BreakoutGame breakoutGame)
    {
        game = breakoutGame;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        game.OnBallCollision(collision);
    }
}

public class PaddleMarker : MonoBehaviour
{
}

public class BlockMarker : MonoBehaviour
{
    public int Durability { get; private set; }

    private int currentHp;
    private SpriteRenderer blockRenderer;

    public void Setup(int durability, SpriteRenderer renderer)
    {
        Durability = Mathf.Max(1, durability);
        currentHp = Durability;
        blockRenderer = renderer;
    }

    public bool Hit()
    {
        currentHp--;

        if (currentHp <= 0)
        {
            Destroy(gameObject);
            return true;
        }

        if (Durability > 1 && blockRenderer != null)
        {
            blockRenderer.color = new Color(1f, 0.6f, 0.2f);
        }

        return false;
    }
}
