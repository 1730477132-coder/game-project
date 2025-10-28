using UnityEngine;

[RequireComponent(typeof(GridNavigator2D))]
public class PacStudentController : MonoBehaviour
{
    [Header("Move")]
    public float tilesPerSecond = 6f;

    [Header("Presentation (optional)")]
    public Animator animator;     // MoveX(float), MoveY(float), Moving(bool)
    public AudioSource sfxMove;   // 普通移动音
    public AudioSource sfxMunch;  // 吃豆/将吃豆移动音
    public ParticleSystem dustFx; // 尘土粒子（子物体或同级均可）

    [Header("Animator Direction Int (用于PL-up/left/right/down切换)")]
    public bool useDirectionInt = true;            // 勾上则同时设置 Direction
    public string directionParamName = "Direction"; // 0=Up,1=Right,2=Down,3=Left
    public string movingParamName = "Moving";       // 若你Animator里不是"Moving"，可改名

    [Header("SFX - Bump")]
    public AudioSource sfxBump;   // 撞墙音效
    public float bumpCooldown = 0.12f; // 防止连续触发过密
    float nextBumpTime = 0f;

    [Header("Dust Settings")]
    public float dustMovingRate = 18f;     // 移动时发射速率（粒子/秒）
    public float dustIdleRate = 0f;        // 静止时发射速率
    public float dustYOffset = -0.08f;     // 尘土相对脚底的下移量
    public bool dustFaceBackwards = true; // 让粒子喷向移动反方向

    GridNavigator2D nav;
    Vector2Int currentDir = Vector2Int.right; // 开局朝右
    Vector2Int lastInput = Vector2Int.zero;

    Vector3 fromPos, toPos;
    float t = 1f; // 0..1

    // 粒子缓存
    ParticleSystem.EmissionModule dustEmission;
    bool dustValid = false;

    void Awake()
    {
        nav = GetComponent<GridNavigator2D>();
        transform.position = nav.SnapToCell(transform.position);
        fromPos = toPos = transform.position;

        if (dustFx != null)
        {
            dustEmission = dustFx.emission;
            SetDustRate(dustIdleRate);
            dustValid = true;
        }
    }

    void Update()
    {
        ReadInput();

        if (t >= 1f)
        {
            transform.position = toPos = nav.SnapToCell(transform.position);
            fromPos = toPos;

            if (TryStartStep(lastInput)) { /* 优先缓冲方向 */ }
            else if (TryStartStep(currentDir)) { /* 否则直行 */ }
            else
            {
                SetMoving(false);
            }
        }
        else
        {
            t += Time.deltaTime * tilesPerSecond;
            transform.position = Vector3.Lerp(fromPos, toPos, Mathf.Clamp01(t));
        }

        UpdateAnim();
        UpdateDust();  // ← 每帧根据移动状态/方向刷新粒子
    }

    void ReadInput()
    {
        float hx = Input.GetAxisRaw("Horizontal");
        float vy = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(hx) > Mathf.Abs(vy))
            lastInput = hx > 0 ? Vector2Int.right : (hx < 0 ? Vector2Int.left : lastInput);
        else if (Mathf.Abs(vy) > 0)
            lastInput = vy > 0 ? Vector2Int.up : Vector2Int.down;
    }

    bool TryStartStep(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return false;

        if (nav.IsBlocked(dir))
        {
            PlayBump();   // 撞墙时播放音效
            return false;
        }

        bool turning = (t >= 1f && dir != currentDir);
        currentDir = dir;

        fromPos = nav.SnapToCell(transform.position);
        Vector3 step = (Vector3)((Vector2)dir * nav.cellSize);
        toPos = fromPos + step;
        t = 0f;

        if (turning && dustValid) dustFx.Play(true); // 转向瞬间打一小 puff
        SetMoving(true);
        PlayMoveSfx();
        return true;
    }

    void UpdateAnim()
    {
        if (!animator) return;

        bool isMoving = t < 1f;

        animator.SetBool(movingParamName, isMoving);

        if (useDirectionInt)
        {
            int dirInt = 1; // Right
            if (currentDir == Vector2Int.up) dirInt = 0;
            else if (currentDir == Vector2Int.right) dirInt = 1;
            else if (currentDir == Vector2Int.down) dirInt = 2;
            else if (currentDir == Vector2Int.left) dirInt = 3;

            animator.SetInteger(directionParamName, dirInt);
        }
    }

    void SetMoving(bool moving)
    {
        if (animator) animator.SetBool(movingParamName, moving);
        if (!moving)
        {
            if (sfxMove && sfxMove.isPlaying) sfxMove.Stop();
            if (sfxMunch && sfxMunch.isPlaying) sfxMunch.Stop();
        }
    }

    void PlayMoveSfx()
    {
        bool willEatDot = false; // TODO: 接你的豆子表判断“下一格是否有豆子”
        if (willEatDot)
        {
            if (sfxMove && sfxMove.isPlaying) sfxMove.Stop();
            if (sfxMunch && !sfxMunch.isPlaying) sfxMunch.Play();
        }
        else
        {
            if (sfxMunch && sfxMunch.isPlaying) sfxMunch.Stop();
            if (sfxMove && !sfxMove.isPlaying) sfxMove.Play();
        }
    }

    void PlayBump()
    {
        if (sfxBump && Time.time >= nextBumpTime)
        {
            sfxBump.PlayOneShot(sfxBump.clip);
            nextBumpTime = Time.time + bumpCooldown;
        }
    }

    // ---------- Dust control ----------
    void UpdateDust()
    {
        if (!dustValid) return;

        bool isMoving = t < 1f;
        SetDustRate(isMoving ? dustMovingRate : dustIdleRate);

        // 放到脚底
        Vector3 p = transform.position;
        dustFx.transform.position = new Vector3(p.x, p.y + dustYOffset, p.z);

        // 朝向：让粒子喷向移动反方向（更像尘土被拖在后面）
        if (dustFaceBackwards && isMoving)
        {
            Vector2 dir = (Vector2)currentDir;
            if (dir.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
                dustFx.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    void SetDustRate(float rate)
    {
        var curve = dustEmission.rateOverTime;
        if (!Mathf.Approximately(curve.constant, rate))
        {
            curve.constant = rate;
            dustEmission.rateOverTime = curve;
        }
    }
}
