using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =====================================================================
// ScoreCalculator.cs  -  족보 판정 & 점수 계산 (1인용)
// =====================================================================
public static class ScoreCalculator
{
    // ── 족보 결과 컨테이너 ────────────────────────────────────────────
    public class ScoreResult
    {
        public int   baseScore;          // 기본 합산
        public int   totalScore;         // 배율 적용 후
        public float multiplier = 1f;
        public List<string> combos = new List<string>();

        public override string ToString()
            => combos.Count > 0
               ? string.Join(" + ", combos) + $"  (x{multiplier:F1}) = {totalScore}점"
               : $"{totalScore}점";
    }

    // ── 메인 계산 ─────────────────────────────────────────────────────
    public static ScoreResult Calculate(List<HwatuCard> captured)
    {
        var result = new ScoreResult();
        if (captured == null || captured.Count == 0) return result;

        var gwang = captured.Where(c => c.cardType == CardType.Gwang).ToList();
        var yul   = captured.Where(c => c.cardType == CardType.Yul).ToList();
        var pi5   = captured.Where(c => c.cardType == CardType.Pi5).ToList();
        var pi    = captured.Where(c => c.cardType == CardType.Pi).ToList();

        int score = 0;
        float mult = 1f;

        // ── 광 족보 ───────────────────────────────────────────────────
        if (gwang.Count >= 5)
        {
            score += 15;
            result.combos.Add("오광(15)");
        }
        else if (gwang.Count == 4)
        {
            score += 4;
            result.combos.Add("사광(4)");
        }
        else if (gwang.Count == 3)
        {
            bool hasBi = gwang.Any(c => c.month == 12); // 12월=비광
            score += hasBi ? 2 : 3;
            result.combos.Add(hasBi ? "비삼광(2)" : "삼광(3)");
        }

        // ── 고도리 ────────────────────────────────────────────────────
        var godoriMonths = new HashSet<int> { 2, 4, 8 };
        bool hasGodori = godoriMonths.All(m => yul.Any(c => c.month == m));
        if (hasGodori)
        {
            score += 5;
            result.combos.Add("고도리(5)");
        }

        // ── 청단 ──────────────────────────────────────────────────────
        var cheongdanMonths = new HashSet<int> { 1, 2, 3 };
        bool hasCheong = cheongdanMonths.All(m => pi5.Any(c => c.month == m));
        if (hasCheong)
        {
            score += 3;
            result.combos.Add("청단(3)");
        }

        // ── 홍단 ──────────────────────────────────────────────────────
        var hongdanMonths = new HashSet<int> { 7, 8, 9 };
        bool hasHong = hongdanMonths.All(m => pi5.Any(c => c.month == m));
        if (hasHong)
        {
            score += 3;
            result.combos.Add("홍단(3)");
        }

        // ── 초단 ──────────────────────────────────────────────────────
        var chodanMonths = new HashSet<int> { 4, 5, 6 };
        bool hasChodan = chodanMonths.All(m => pi5.Any(c => c.month == m));
        if (hasChodan)
        {
            score += 3;
            result.combos.Add("초단(3)");
        }

        // ── 열끗 (5장 기준, 초과 1장당 +1) ───────────────────────────
        if (yul.Count >= 5)
        {
            int yulScore = 1 + (yul.Count - 5);
            score += yulScore;
            result.combos.Add($"열끗({yulScore})");
        }

        // ── 띠 (5장 기준, 초과 1장당 +1) ─────────────────────────────
        if (pi5.Count >= 5)
        {
            int pi5Score = 1 + (pi5.Count - 5);
            score += pi5Score;
            result.combos.Add($"띠({pi5Score})");
        }

        // ── 피 (10장 기준, 초과 1장당 +1) ────────────────────────────
        int totalPi = pi.Count + pi5.Count; // 띠도 피로 카운트
        if (totalPi >= 10)
        {
            int piScore = 1 + (totalPi - 10);
            score += piScore;
            result.combos.Add($"피({piScore})");
        }

        // ── 쌍피 보너스 (같은 월 피 2장) ──────────────────────────────
        var piByMonth = pi.GroupBy(c => c.month);
        foreach (var grp in piByMonth)
        {
            if (grp.Count() >= 2)
            {
                score += 1;
                result.combos.Add($"쌍피({grp.Key}월+1)");
            }
        }

        // ── 배율: 뻑·멍따·고 같은 상황은 GameManager에서 별도 처리 ──
        result.baseScore  = score;
        result.multiplier = mult;
        result.totalScore = Mathf.RoundToInt(score * mult);
        return result;
    }

    // ── 족보 달성 여부만 빠르게 체크 (고/스톱 판단용) ─────────────────
    public static bool HasAnyCombo(List<HwatuCard> captured)
    {
        var res = Calculate(captured);
        return res.combos.Count > 0;
    }

    // ── 달성된 족보 이름 목록 ─────────────────────────────────────────
    public static List<string> GetComboNames(List<HwatuCard> captured)
        => Calculate(captured).combos;
}
