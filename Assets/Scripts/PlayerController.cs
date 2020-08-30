using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region インスペクターで設定する
    [SerializeField, Header("接地判定")] private GroundCheck groundCheck;
    [SerializeField, Header("頭をぶつけた判定")] private GroundCheck ceilingCheck;
    [SerializeField, Header("移動速度")] private float walkSpeed;
    [SerializeField, Header("ジャンプ速度")] private float jumpSpeed;
    [SerializeField, Header("重力")] private float gravity;
    [SerializeField, Header("ジャンプする高さ")] private float maxJumpHeight;
    [SerializeField, Header("ジャンプ制限時間")] private float jumpTimeLimit;
    [SerializeField, Header("移動の速さ表現")] private AnimationCurve walkingCurve;
    [SerializeField, Header("ジャンプの速さ表現")] private AnimationCurve jumpCurve;
    [SerializeField, Header("踏みつけ判定の高さの割合")] private float stepOnRate;
    #endregion

    #region プライベート変数
    private Animator animator = null;
    private Rigidbody2D rigidbody2d = null;
    private CapsuleCollider2D capcol = null;
    private float currentJumpHeight;
    private float currentJumpTime;
    private float walkingTime, jumpingTime;
    private float beforeKey;
    private float otherJumpHeight;
    private bool isJumping;
    private bool isOtherJumping;
    private bool isGrounded;
    private bool isRunning;
    private bool isDown;
    private string enemyTag;
    #endregion

    void Start()
    {
        animator = GetComponent<Animator>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        capcol = GetComponent<CapsuleCollider2D>();
        currentJumpHeight = 0f;
        currentJumpTime = 0f;
        isJumping = false;
        isOtherJumping = false;
        isGrounded = false;
        isRunning = false;
        isDown = false;
        enemyTag = "Enemy";
    }

    private void FixedUpdate()
    {
        // ダウン処理
        if (!isDown) 
        {
            // 接地判定を得る
            isGrounded = groundCheck.GetComponent<GroundCheck>().IsGrounded();

            // 各種座標軸の速度を求める
            float xSpeed = GetXSpeed();
            float ySpeed = GetYSpeed();

            // アニメーションを適用
            SetAnimation();

            // 移動速度を設定
            rigidbody2d.velocity = new Vector2(xSpeed, ySpeed);
        }
        else 
        {
            rigidbody2d.velocity = new Vector2(0, -gravity);
        }
    }

    bool CanJump() 
    {
        return JumpKeyDown() && InJumpingHeight() && IsJumpingTime() && !ceilingCheck.IsGrounded();
    }

    bool InJumpingHeight() 
    {
        float dynamicBorderOfJumpHeight = currentJumpHeight + maxJumpHeight;
        return dynamicBorderOfJumpHeight > transform.position.y;
    }

    bool JumpKeyDown() 
    {
        float verticalKey = Input.GetAxis("Vertical");
        return verticalKey > 0;
    }

    bool IsJumpingTime() 
    {
        return currentJumpTime < jumpTimeLimit;
    }

    /// <summary>
    /// X成分で必要な計算をし、速度を返す。
    /// </summary>
    /// <returns>X軸の速さ</returns>
    private float GetXSpeed() 
    {
        float horizontalKey = Input.GetAxis("Horizontal");

        float xSpeed = 0;

        if (horizontalKey > 0)
        {
            isRunning = true;
            transform.localScale = new Vector3(1, 1, 1);
            xSpeed = walkSpeed;
            walkingTime += Time.deltaTime;
        }
        else if (horizontalKey < 0)
        {
            isRunning = true;
            transform.localScale = new Vector3(-1, 1, 1);
            xSpeed = -walkSpeed;
            walkingTime += Time.deltaTime;
        }
        else
        {
            isRunning = false;
            xSpeed = 0;
            walkingTime = 0;
        }

        // 前回の入力から歩行の反転を判断して速度を変える
        if (horizontalKey > 0 && beforeKey < 0)
        {
            walkingTime = 0;
        }
        else if (horizontalKey < 0 && beforeKey > 0)
        {
            walkingTime = 0;
        }

        beforeKey = horizontalKey;
        // アニメーションカーブを速度に適用
        xSpeed *= walkingCurve.Evaluate(walkingTime);

        return xSpeed;
    }

    /// <summary>
    /// Y成分で必要な計算をし、速度を返す。
    /// </summary>
    /// <returns>Y軸の速さ</returns>
    private float GetYSpeed() 
    {
        float verticalKey = Input.GetAxis("Vertical");
        float ySpeed = -gravity;

        if (isOtherJumping)
        {
            // 現在の高さが飛べる高さより下か
            bool canHeight = currentJumpHeight + otherJumpHeight > transform.position.y;

            // ジャンプ時間が長くなりすぎてないか
            bool canTime = jumpTimeLimit > jumpingTime;

            if (canHeight && canTime && !ceilingCheck.IsGrounded())
            {
                ySpeed = jumpSpeed;
                jumpingTime += Time.deltaTime;
            }
            else
            {
                isOtherJumping = false;
                jumpingTime = 0f;
            }
        }

        if (isGrounded)
        {
            if (JumpKeyDown())
            {
                ySpeed = jumpSpeed;
                currentJumpHeight = transform.position.y;
                isJumping = true;
            }
            else
                isJumping = false;
        }
        else if (isJumping)
        {
            if (CanJump())
            {
                ySpeed = jumpSpeed;
                currentJumpTime += Time.deltaTime;
            }
            else
            {
                isJumping = false;
                currentJumpTime = 0f;
            }
        } // ctrl shift /

        // Adjust with animation curve
        if (isJumping || isOtherJumping)
        {
            ySpeed *= jumpCurve.Evaluate(jumpingTime);
        }



        return ySpeed;
    }

    /// <summary>
    /// アニメーションを設定する
    /// </summary>
    private void SetAnimation() 
    {
        animator.SetBool("jump", isJumping || isOtherJumping);
        animator.SetBool("grounded", isGrounded);
        animator.SetBool("walking", isRunning);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag != enemyTag)
            return;

        float stepOnHeight = (capcol.size.y * (stepOnRate / 100f));
        float judgePos = transform.position.y - (capcol.size.y / 2f) + stepOnHeight;
        ObjectCollision o = collision.gameObject.GetComponent<ObjectCollision>();

        foreach(ContactPoint2D contact in collision.contacts) 
        {
            if (contact.point.y < judgePos) 
            {
                isOtherJumping = true;

                if (o == null) 
                {
                    Debug.Log("ObjectCollisionが付いてないよ!");
                }
                else 
                {
                    otherJumpHeight = o.boundHeight;
                    o.playerStepOn = true;
                    currentJumpHeight = transform.position.y;
                    isOtherJumping = true;
                    isJumping = false;
                    jumpingTime = 0;
                }
            }
            else 
            {
                // ダウンする
                animator.Play("player_down");
                isDown = true;
                break;
            }
        }
    }
}
