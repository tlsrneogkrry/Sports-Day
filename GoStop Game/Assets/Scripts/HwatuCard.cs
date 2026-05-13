using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ── 카드 위치 열거형 ─────────────────────────────────────────────────
public enum CardLocation { Deck, PlayerHand, Field, PlayerCapture }

// ── 카드 타입 열거형 ─────────────────────────────────────────────────
public enum CardType { Gwang, Yul, Pi5, Pi }

// =====================================================================
// HwatuCard.cs
//
// 프리팹 구조:
//   HwatuCard (이 스크립트 + Button + Image)
//   ├── FrontFace  (Image 컴포넌트)  ← 화투 앞면
//   └── BackFace   (Image 컴포넌트)  ← 뒷면 (back card)
// =====================================================================
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class HwatuCard : MonoBehaviour
{
    // ── 자식 오브젝트 참조 (Inspector에서 연결) ──────────────────────
    [Header("카드 앞/뒷면")]
    public Image frontFace;   // FrontFace 자식의 Image
    public Image backFace;    // BackFace  자식의 Image

    // ── 카드 데이터 (Init으로 설정됨) ───────────────────────────────
    public int          month        { get; private set; }  // 1~12월
    public int          indexInMonth { get; private set; }  // 월 내 인덱스 0~3
    public CardType     cardType     { get; private set; }
    public int          globalIndex  { get; private set; }  // 0~47

    // ── 위치 상태 ────────────────────────────────────────────────────
    public CardLocation location;

    // ── 내부 ─────────────────────────────────────────────────────────
    private Button      _button;
    private Image       _rootImage;      // 루트 Image (하이라이트용)
    private bool        _isFaceUp  = false;
    private Vector2     _basePosition;   // 손패 정렬 기준 위치

    // ── 선택 강조 설정 ───────────────────────────────────────────────
    [Header("선택 강조")]
    public float selectedYOffset = 20f;  // 선택 시 위로 올라가는 픽셀
    public Color selectedColor   = new Color(1f, 1f, 0.5f);

    // ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        _button    = GetComponent<Button>();
        _rootImage = GetComponent<Image>();

        _button.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCardClicked(this);
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  초기화 — GameManager.BuildDeck()에서 호출
    // ══════════════════════════════════════════════════════════════════
    public void Init(int month, int indexInMonth, CardType type,
                     int globalIdx, Sprite faceSprite, Sprite backSprite)
    {
        this.month        = month;
        this.indexInMonth = indexInMonth;
        this.cardType     = type;
        this.globalIndex  = globalIdx;

        if (frontFace != null && faceSprite != null)
            frontFace.sprite = faceSprite;

        if (backFace != null && backSprite != null)
            backFace.sprite = backSprite;

        SetFaceUp(false);
    }

    // ══════════════════════════════════════════════════════════════════
    //  앞/뒷면 전환
    // ══════════════════════════════════════════════════════════════════
    public void SetFaceUp(bool faceUp)
    {
        _isFaceUp = faceUp;

        if (frontFace != null) frontFace.gameObject.SetActive(faceUp);
        if (backFace  != null) backFace.gameObject.SetActive(!faceUp);
    }

    public bool IsFaceUp => _isFaceUp;

    // ══════════════════════════════════════════════════════════════════
    //  선택 강조 (손패에서 카드 선택 시)
    // ══════════════════════════════════════════════════════════════════
    public void SetSelected(bool selected)
    {
        if (_rootImage != null)
            _rootImage.color = selected ? selectedColor : Color.white;

        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 pos = _basePosition;
            pos.y += selected ? selectedYOffset : 0f;
            rect.anchoredPosition = pos;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  인터랙션 활성/비활성
    // ══════════════════════════════════════════════════════════════════
    public void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
    }

    // ══════════════════════════════════════════════════════════════════
    //  손패 기준 위치 저장 — GameManager.LayoutHand()에서 호출
    // ══════════════════════════════════════════════════════════════════
    public void SaveBasePosition()
    {
        var rect = GetComponent<RectTransform>();
        if (rect != null)
            _basePosition = rect.anchoredPosition;
    }
}
