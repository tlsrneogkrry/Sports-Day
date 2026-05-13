using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// =====================================================
///  맞고 파이터 타이틀 애니메이션 (CanvasGroup 미사용)
/// =====================================================
///  [Inspector 연결]
///  Char Objects  Size = 5
///    Element 0 = 맞 TextMeshProUGUI
///    Element 1 = 고 TextMeshProUGUI
///    Element 2 = 파 TextMeshProUGUI
///    Element 3 = 이 TextMeshProUGUI
///    Element 4 = 터 TextMeshProUGUI
///
///  Diamond Left  = 왼쪽 빨간 다이아몬드 Image
///  Diamond Right = 오른쪽 카드 다이아몬드 Image
/// =====================================================
/// </summary>
public class MatgoFighterTitleAnim : MonoBehaviour
{
    [Header("──── 글자 오브젝트 (맞·고·파·이·터) ────")]
    public TextMeshProUGUI[] charObjects = new TextMeshProUGUI[5];

    [Header("──── 다이아몬드 카드 ────")]
    public Image diamondLeft;
    public Image diamondRight;

    [Header("──── 재시작 버튼 (선택) ────")]
    public Button replayButton;

    [Header("──── 타이밍 (초) ────")]
    public float startDelay         = 0.3f;
    public float charInterval       = 0.21f;
    public float cardInterval       = 0.20f;

    [Header("──── 글자 애니메이션 ────")]
    public float dropStartY         = 160f;
    public float bounceHeight       = 22f;
    public float bounceUpDuration   = 0.20f;
    public float bounceLandDuration = 0.15f;

    [Header("──── 카드 애니메이션 ────")]
    public float cardPopDuration    = 0.28f;

    // ── 내부 변수 ──
    private Vector2[]  _originPos;
    private Coroutine  _mainCoroutine;

    // ============================================================
    void Start()
    {
        _originPos = new Vector2[charObjects.Length];
        for (int i = 0; i < charObjects.Length; i++)
        {
            if (charObjects[i] != null)
            {
                RectTransform rt = charObjects[i].GetComponent<RectTransform>();
                _originPos[i] = rt.anchoredPosition;
                Debug.Log($"[Matgo] [{i}] 위치저장 OK : {_originPos[i]}  텍스트='{charObjects[i].text}'");
            }
            else
            {
                Debug.LogError($"[Matgo] charObjects[{i}] 가 비어있음! Inspector 확인 필요");
            }
        }

        if (replayButton != null)
            replayButton.onClick.AddListener(PlayAnimation);

        PlayAnimation();
    }

    // ============================================================
    //  외부 호출 / 버튼 OnClick 연결용
    // ============================================================
    public void PlayAnimation()
    {
        if (_mainCoroutine != null)
            StopCoroutine(_mainCoroutine);

        ResetAll();
        _mainCoroutine = StartCoroutine(RunAnimation());
    }

    // ============================================================
    //  리셋 : 모두 투명 + 위 위치
    // ============================================================
    void ResetAll()
    {
        for (int i = 0; i < charObjects.Length; i++)
        {
            if (charObjects[i] == null) continue;

            // 색상 alpha = 0
            Color c = charObjects[i].color;
            c.a = 0f;
            charObjects[i].color = c;

            // 위치 + 스케일
            RectTransform rt = charObjects[i].GetComponent<RectTransform>();
            rt.anchoredPosition = _originPos[i] + new Vector2(0, dropStartY);
            rt.localScale = new Vector3(0.4f, 0.4f, 1f);
        }

        ResetCard(diamondLeft);
        ResetCard(diamondRight);
    }

    void ResetCard(Image img)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = 0f;
        img.color = c;
        img.rectTransform.localScale = Vector3.zero;
    }

    // ============================================================
    //  메인 코루틴
    // ============================================================
    IEnumerator RunAnimation()
    {
        Debug.Log("[Matgo] 애니메이션 시작");
        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < charObjects.Length; i++)
        {
            if (charObjects[i] == null) continue;
            StartCoroutine(BounceInChar(i));
            yield return new WaitForSeconds(charInterval);
        }

        // 마지막 글자 착지 대기
        yield return new WaitForSeconds(bounceUpDuration + bounceLandDuration + 0.1f);

        Debug.Log("[Matgo] 카드 등장 시작");

        if (diamondLeft  != null) StartCoroutine(PopInCard(diamondLeft));
        yield return new WaitForSeconds(cardInterval);
        if (diamondRight != null) StartCoroutine(PopInCard(diamondRight));
    }

    // ============================================================
    //  글자 하나 튕기기
    // ============================================================
    IEnumerator BounceInChar(int idx)
    {
        TextMeshProUGUI tmp = charObjects[idx];
        RectTransform   rt  = tmp.GetComponent<RectTransform>();

        Vector2 startPos  = _originPos[idx] + new Vector2(0,  dropStartY);
        Vector2 bouncePos = _originPos[idx] - new Vector2(0,  bounceHeight);
        Vector2 endPos    = _originPos[idx];

        // ① 내려오며 튕김 (EaseOutBack)
        float t = 0f;
        while (t < bounceUpDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bounceUpDuration);
            float e = EaseOutBack(p);

            rt.anchoredPosition = Vector2.Lerp(startPos,  bouncePos, e);
            rt.localScale       = Vector3.Lerp(new Vector3(0.4f, 0.4f, 1f),
                                               new Vector3(1.2f, 1.2f, 1f), e);
            SetAlpha(tmp, Mathf.Clamp01(p * 3f));
            yield return null;
        }

        // ② 착지 (EaseInQuad)
        t = 0f;
        while (t < bounceLandDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bounceLandDuration);

            rt.anchoredPosition = Vector2.Lerp(bouncePos, endPos, EaseInQuad(p));
            rt.localScale       = Vector3.Lerp(new Vector3(1.2f, 1.2f, 1f), Vector3.one, p);
            yield return null;
        }

        rt.anchoredPosition = endPos;
        rt.localScale       = Vector3.one;
        SetAlpha(tmp, 1f);

        Debug.Log($"[Matgo] 글자[{idx}] '{tmp.text}' 착지완료");
    }

    // ============================================================
    //  카드 팝인
    // ============================================================
    IEnumerator PopInCard(Image img)
    {
        float t = 0f;
        while (t < cardPopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / cardPopDuration);
            float e = EaseOutBack(p);

            img.rectTransform.localScale = Vector3.one * e;
            SetAlpha(img, Mathf.Clamp01(p * 4f));
            yield return null;
        }

        img.rectTransform.localScale = Vector3.one;
        SetAlpha(img, 1f);
    }

    // ============================================================
    //  알파 헬퍼
    // ============================================================
    void SetAlpha(TextMeshProUGUI tmp, float a)
    {
        Color c = tmp.color; c.a = a; tmp.color = c;
    }

    void SetAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }

    // ============================================================
    //  이징
    // ============================================================
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseInQuad(float t) => t * t;
}
