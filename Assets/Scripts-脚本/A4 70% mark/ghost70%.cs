// GhostManager.cs ���� 70%��Сʵ�֣������ְ棩
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostManager : MonoBehaviour
{
    public static GhostManager I { get; private set; }

    [Header("UI (��ѡ)")]
    public FrightUI frightUI;            // �����գ������ϻ���ʾ����ʱ

    [Header("Scoring")]
    public int eatGhostPoints = 300;     // �Թ�ӷ�

    [Header("Fright rules")]
    public float recoverThreshold = 3f;  // ��3s ���� Recovering ��ʾ
    public bool pauseGhostsOnPlayerDeath = true; // 70%���Ȳ�ʵ����������ͣ����ռλ

    private readonly List<GhostAgent> ghosts = new List<GhostAgent>();
    private float frightRemain = 0f;     // >0 ��ʾ�־嵹��ʱ������
    private bool frightenedActive = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    // ��ÿֻ���� Awake ʱ���õǼ�
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

        // ����ʱ ��3s ʱ�������� Scared �Ĺ��е� Recovering
        if (frightRemain <= recoverThreshold)
        {
            foreach (var g in ghosts)
                if (g.mode == GhostMode.Scared) g.SetMode(GhostMode.Recovering);
        }

        // ˢ��UI
        if (frightUI) frightUI.SetRemaining(frightRemain);
    }

    // ���� �Ե�������ʱ���� ���� 
    public void TriggerFrightened(float seconds)
    {
        frightRemain = Mathf.Max(0f, seconds);
        frightenedActive = frightRemain > 0f;

        foreach (var g in ghosts)
        {
            if (g.mode == GhostMode.Dead) continue; // Dead ����Ӱ��
            g.SetMode(GhostMode.Scared);
        }

        if (frightUI) frightUI.Show(frightRemain);
        // �����л����Ժ�ӣ�MusicController.I?.PlayScared();
    }

    // ���� �־�������� Dead ��ȫ���� Normal ���� 
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

    // ���� ���ײ�����ɹ�� OnTriggerEnter ���� ���� 
    public void HandlePlayerContact(GhostAgent g)
    {
        switch (g.mode)
        {
            case GhostMode.Normal:
                // ���� + ��λ��70%��С���̣�
                ScoreManager.I?.LoseLife(1);
                foreach (var gh in ghosts) gh.ResetToSpawn();
                EndFrightened(); // ���ڿ־��������
                // ����ġ������ݳ�/���ᡱ�������غϿ�����ʵ��
                break;

            case GhostMode.Scared:
            case GhostMode.Recovering:
                // �Թ��ӷ֣���� Dead��3s ��ʣ��־��̬
                ScoreManager.I?.AddScore(eatGhostPoints);
                // SfxHub.I?.PlayEatGhost(); // ��������Ч
                I.StartCoroutine(GhostEatenRoutine(g));
                break;

            case GhostMode.Dead:
                // ����
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

        // ��ѡ���ѹ��ͻء����ݡ��ٳ����������Ȳ���������70%
    }
}
