using UnityEngine;

public class BreakoutGame : MonoBehaviour
{
    private const float HalfWidth = 8.5f;
    private const float HalfHeight = 5f;

    private GameObject paddle;
    private GameObject ball;
    private Rigidbody2D ballBody;
    private Vector3 ballStartPosition;

    private int remainingBlocks;
    private int lives = 3;
    private bool finished;

    [SerializeField] private float paddleSpeed = 10f;
    [SerializeField] private float ballSpeed = 7f;

    private void Start()
    {
        CreateBounds();
        CreatePaddle();
        CreateBlocks();
        CreateBall();
        ResetBall();
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        MovePaddle();

        if (ball.transform.position.y < -HalfHeight - 1f)
        {
            lives--;
            if (lives <= 0)
            {
                finished = true;
                Debug.Log("Game Over");
                ballBody.linearVelocity = Vector2.zero;
                return;
            }

            Debug.Log($"Ball reset. Lives remaining: {lives}");
            ResetBall();
        }
    }

    public void HitBlock(GameObject block)
    {
        if (finished)
        {
            return;
        }

        Destroy(block);
        remainingBlocks--;

        if (remainingBlocks <= 0)
        {
            finished = true;
            Debug.Log("Clear!");
            ballBody.linearVelocity = Vector2.zero;
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

    private void ResetBall()
    {
        ball.transform.position = ballStartPosition;
        ballBody.linearVelocity = Vector2.zero;

        Vector2 launchDirection = new Vector2(Random.Range(-0.4f, 0.4f), -1f).normalized;
        ballBody.AddForce(launchDirection * ballSpeed, ForceMode2D.Impulse);
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
    }

    private void CreateBall()
    {
        ball = new GameObject("Ball");
        ballStartPosition = new Vector3(0f, -3.3f, 0f);
        ball.transform.position = ballStartPosition;

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

                SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateSquareSprite();
                renderer.color = Color.Lerp(Color.yellow, Color.red, row / (float)(rows - 1));

                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                block.AddComponent<BlockMarker>();
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
        BlockMarker block = collision.gameObject.GetComponent<BlockMarker>();
        if (block != null)
        {
            game.HitBlock(collision.gameObject);
        }
    }
}

public class BlockMarker : MonoBehaviour
{
}
