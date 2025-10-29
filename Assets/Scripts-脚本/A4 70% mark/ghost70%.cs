// GhostManager.cs —— 70%最小实现（无音乐版）
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostManager : MonoBehaviour
{
    public static GhostManager I { get; private set; }

    [Header("UI (可选)")]
    public FrightUI frightUI;            // 可留空；若连上会显示倒计时

    [Header("Scoring")]
    public int eatGhostPoints = 300;     // 吃鬼加分

    [Header("Fright rules")]
    public float recoverThreshold = 3f;  // ≤3s 进入 Recovering 提示
    public bool pauseGhostsOnPlayerDeath = true; // 70%可先不实现真正“暂停”，占位

    private readonly List<GhostAgent> ghosts = new List<GhostAgent>();
    private float frightRemain = 0f;     // >0 表示恐惧倒计时进行中
    private bool frightenedActive = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    // 被每只鬼在 Awake 时调用登记
    public void Register(GhostAgent g)
    {
        if (!ghosts.Contains(g)) ghosts.Add(g);
    }

    void Update()
    {
        if (!frightenedActive) return;

        frightRemain -= Time.deltaTime;
        if (frightRemain <= 0f)
        {
            EndFrightened();
            return;
        }

        // 倒计时 ≤3s 时，把仍是 Scared 的鬼切到 Recovering
        if (frightRemain <= recoverThreshold)
        {
            foreach (var g in ghosts)
                if (g.mode == GhostMode.Scared) g.SetMode(GhostMode.Recovering);
        }

        // 刷新UI
        if (frightUI) frightUI.SetRemaining(frightRemain);
    }

    // —— 吃到能量豆时调用 —— 
    public void TriggerFrightened(float seconds)
    {
        frightRemain = Mathf.Max(0f, seconds);
        frightenedActive = frightRemain > 0f;

        foreach (var g in ghosts)
        {
            if (g.mode == GhostMode.Dead) continue; // Dead 不受影响
            g.SetMode(GhostMode.Scared);
        }

        if (frightUI) frightUI.Show(frightRemain);
        // 音乐切换可稍后接：MusicController.I?.PlayScared();
    }

    // —— 恐惧结束：除 Dead 外全部回 Normal —— 
    private void EndFrightened()
    {
        frightenedActive = false;
        frightRemain = 0f;

        foreach (var g in ghosts)
        {
            if (g.mode == GhostMode.Dead) continue;
            g.SetMode(GhostMode.Normal);
        }

        if (frightUI) frightUI.Hide();
        // MusicController.I?.PlayNormal();
    }

    // —— 玩家撞到鬼：由鬼的 OnTriggerEnter 调用 —— 
    public void HandlePlayerContact(GhostAgent g)
    {
        switch (g.mode)
        {
            case GhostMode.Normal:
                // 扣命 + 复位（70%最小流程）
                ScoreManager.I?.LoseLife(1);
                foreach (var gh in ghosts) gh.ResetToSpawn();
                EndFrightened(); // 若在恐惧中则结束
                // 这里的“死亡演出/冻结”可留到回合控制里实现
                break;

            case GhostMode.Scared:
            case GhostMode.Recovering:
                // 吃鬼：加分，鬼进 Dead，3s 后按剩余恐惧回态
                ScoreManager.I?.AddScore(eatGhostPoints);
                // SfxHub.I?.PlayEatGhost(); // 若接了音效
                I.StartCoroutine(GhostEatenRoutine(g));
                break;

            case GhostMode.Dead:
                // 忽略
                break;
        }
    }

    private IEnumerator GhostEatenRoutine(GhostAgent g)
    {
        g.SetMode(GhostMode.Dead);
        yield return new WaitForSeconds(3f);

        if (frightenedActive)
            g.SetMode(frightRemain <= recoverThreshold ? GhostMode.Recovering : GhostMode.Scared);
        else
            g.SetMode(GhostMode.Normal);

        // 可选：把鬼送回“鬼屋”再出来，这里先不做以满足70%
    }
}
