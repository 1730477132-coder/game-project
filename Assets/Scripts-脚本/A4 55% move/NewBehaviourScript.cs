using UnityEngine;

[RequireComponent(typeof(GridNavigator2D))]
public class PacStudentController : MonoBehaviour
{
    [Header("Move")]
    public float tilesPerSecond = 6f;   // 每秒跨过多少个格

    [Header("Presentation (optional)")]
    public Animator animator;           // Animator：Moving(bool)、Direction(int) 或自定义
    public AudioSource sfxMove;         // 普通移动循环音
    public AudioSource sfxMunch;        // 吃豆/将吃豆时的移动音
    public ParticleSystem dustFx;       // 尘土粒子（子物体或同级均可）

    [Header("Animator Params")]
    public bool useDirectionInt = true;          // 是否设置Direction整型
    public string directionParamName = "Direction"; // 0=Up,1=Right,2=Down,3=Left
    public string movingParamName = "Moving";

    [Header("SFX - Bump")]
    public AudioSource sfxBump;          // 撞墙短音
    public float bumpCooldown = 0.12f;   // 撞墙音效冷却（防机枪）
    private float nextBumpTime = 0f;

    [Header("Bump FX")]
    public GameObject bumpFxPrefab;      // 一次性Puff预制（ParticleSystem，StopAction=Destroy）
    public float bumpFxOffset = 0.4f;    // 撞击点相对格中心的前移（≈ cellSize * 0.5）
    public float bumpFxScale = 1.0f;    // 粒子缩放
    public float bumpShake = 0.08f;   // 轻微回弹幅度（0 关闭）

    [Header("Bump FX Debounce")]
    public float bumpFxCooldown = 0.12f; // 粒子冷却（建议与音效一致）
    private float _nextBumpFxTime = 0f;
    private Vector2Int _lastBumpDir = Vector2Int.zero; // 上次撞墙方向
    private Vector3 _lastBumpCell;                      // 上次撞墙所在格中心（fromPos）

    [Header("Dust Settings")]
    public float dustMovingRate = 18f;     // 移动时发射速率
    public float dustIdleRate = 0f;        // 静止时发射速率
    public float dustYOffset = -0.08f;     // 尘土相对脚底偏移
    public bool dustFaceBackwards = true; // 粒子喷向移动反方向

    // —— 内部状态 ——
    GridNavigator2D nav;
    Vector2Int currentDir = Vector2Int.right; // 开局朝右
    Vector2Int lastInput = Vector2Int.zero;

    Vector3 fromPos, toPos;
    float t = 1f; // 0..1，插值参数

    // 尘土缓存
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
            // 到达格中心，尝试开始下一个步进
            transform.position = toPos = nav.SnapToCell(transform.position);
            fromPos = toPos;

            if (TryStartStep(lastInput)) { /* 优先缓冲方向 */ }
            else if (TryStartStep(currentDir)) { /* 否则直行 */ }
            else
            {
                // 无法移动：确保停住（动画与尘土关）
                SetMoving(false);
                if (dustValid) SetDustRate(dustIdleRate);
            }
        }
        else
        {
            // —— 途中再检测：前方是否“临时变墙”（如鬼屋门合上）——
            if (nav.IsBlocked(currentDir))
            {
                // 强制停步：回到fromPos并结束本次插值
                transform.position = fromPos = nav.SnapToCell(transform.position);
                toPos = fromPos;
                t = 1f;

                SetMoving(false);                 // 立刻关动画
                if (dustValid) SetDustRate(dustIdleRate); // 立刻关尘土
                PlayBump(currentDir);             // 首帧反馈（带冷却与去抖）
                UpdateDust();
                return; // 本帧结束
            }

            // 正常插值移动
            t += Time.deltaTime * tilesPerSecond;
            transform.position = Vector3.Lerp(fromPos, toPos, Mathf.Clamp01(t));
        }

        UpdateAnim();
        UpdateDust();  // 根据移动状态/方向刷新粒子
    }

    // —— 输入读取：保留最近一次按键为 lastInput（与 currentDir 解耦）——
    void ReadInput()
    {
        float hx = Input.GetAxisRaw("Horizontal");
        float vy = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(hx) > Mathf.Abs(vy))
            lastInput = hx > 0 ? Vector2Int.right : (hx < 0 ? Vector2Int.left : lastInput);
        else if (Mathf.Abs(vy) > 0)
            lastInput = vy > 0 ? Vector2Int.up : Vector2Int.down;
    }

    // —— 试图从当前格朝 dir 迈出一步；失败则触发撞墙反馈（带去抖）——
    bool TryStartStep(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return false;

        if (nav.IsBlocked(dir))
        {
            SetMoving(false);                 // 起步就撞：立刻停动画
            if (dustValid) SetDustRate(dustIdleRate); // 关尘土
            PlayBump(dir);                    // 音效+粒子（节流/去抖）
            return false;
        }

        bool turning = (t >= 1f && dir != currentDir);
        currentDir = dir;

        fromPos = nav.SnapToCell(transform.position);
        Vector3 step = (Vector3)((Vector2)dir * nav.cellSize);
        toPos = fromPos + step;
        t = 0f;

        if (turning && dustValid) dustFx.Play(true); // 转向打一口puff（可选）
        SetMoving(true);
        PlayMoveSfx();
        return true;
    }

    // —— 动画参数刷新 —— 
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

    // —— 开/关移动表现（动画与移动音）、不含撞墙音 —— 
    void SetMoving(bool moving)
    {
        if (animator) animator.SetBool(movingParamName, moving);
        if (!moving)
        {
            if (sfxMove && sfxMove.isPlaying) sfxMove.Stop();
            if (sfxMunch && sfxMunch.isPlaying) sfxMunch.Stop();
        }
    }

    // —— 依据“下一格是否有豆子”切换移动音（此处留接口，按需改造）——
    void PlayMoveSfx()
    {
        bool willEatDot = false; // TODO: 接入你的豆子数据，判断下一格是否有豆子
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

    // —— 撞墙：立停 + 动画关 + 尘土关 + 音效/粒子节流与“同格同向去抖” —— 
    void PlayBump(Vector2Int dir)
    {
        // 1) 立刻停掉一切“移动表现”
        SetMoving(false);
        if (dustValid) SetDustRate(dustIdleRate);

        // 2) 判定是否需要触发（同一格 + 同一方向按住不重复；也受冷却控制）
        bool sameCell = (Vector3.Distance(fromPos, _lastBumpCell) < 0.001f);
        bool sameDir = (dir == _lastBumpDir);
        bool canFx = Time.time >= _nextBumpFxTime;

        if (!(sameCell && sameDir) || canFx)
        {
            // 音效节流
            if (sfxBump && Time.time >= nextBumpTime)
            {
                sfxBump.PlayOneShot(sfxBump.clip);
                nextBumpTime = Time.time + bumpCooldown;
            }

            // 粒子节流
            if (Time.time >= _nextBumpFxTime)
            {
                SpawnBumpFX(dir);
                _nextBumpFxTime = Time.time + bumpFxCooldown;
            }

            _lastBumpDir = dir;
            _lastBumpCell = fromPos;
        }
    }

    // —— 生成撞墙粒子 + 可选轻微回弹 —— 
    void SpawnBumpFX(Vector2Int dir)
    {
        if (bumpFxPrefab == null) return;

        // 撞击点：从当前格中心沿撞击方向前移
        Vector3 hitPos = fromPos + (Vector3)((Vector2)dir * bumpFxOffset);

        var go = Instantiate(bumpFxPrefab, hitPos, Quaternion.identity);
        go.transform.localScale = Vector3.one * bumpFxScale;

        // 朝“反弹方向”旋转（可选）
        float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 轻微回弹手感（不需要就把 bumpShake 设为 0）
        if (bumpShake > 0f)
            transform.position = fromPos + (Vector3)((Vector2)(-dir) * bumpShake * 0.2f);
    }

    // —— 尘土控制 —— 
    void UpdateDust()
    {
        if (!dustValid) return;

        bool isMoving = t < 1f;
        SetDustRate(isMoving ? dustMovingRate : dustIdleRate);

        // 放在脚底
        Vector3 p = transform.position;
        dustFx.transform.position = new Vector3(p.x, p.y + dustYOffset, p.z);

        // 朝向：喷向移动反方向，更像拖尾
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
