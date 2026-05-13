using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
// GameManager.cs  -  혼자 하는 화투 (1인 솔로 플레이)
//
// 흐름:
//   게임 시작 → 덱 섞기 → 플레이어 10장 배분 → 필드 8장 배치
//   ① 손패 카드 선택 → ② 필드 카드 클릭(매칭) 또는 덱 뒤집기
//   → 족보 달성 시 "결산" 버튼 표시
//   → 결산 버튼 클릭 → 최종 점수 표시 → 재시작
// =====================================================================
public class GameManager : MonoBehaviour
{
    // ── 싱글톤 ──────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Inspector 연결 ───────────────────────────────────────────────
    [Header("UI 영역")]
    public Transform playerHandArea;
    public Transform fieldArea;
    public Transform playerCaptureArea;

    [Header("텍스트 UI")]
    public TMP_Text deckCountText;
    public TMP_Text playerScoreText;
    public TMP_Text comboText;
    public TMP_Text turnCountText;      // 남은 턴 수

    [Header("버튼")]
    public Button goButton;             // 결산(고) 버튼
    public Button restartButton;        // 재시작 버튼

    [Header("결과 패널")]
    public GameObject resultPanel;      // 게임 종료 결과창
    public TMP_Text   resultText;       // 결과 텍스트

    [Header("프리팹 & 스프라이트")]
    public GameObject cardPrefab;
    public Sprite     cardBackSprite;
    public Sprite[]   cardSprites;      // 48장, 월 순서대로

    // ── 내부 상태 ────────────────────────────────────────────────────
    private List<HwatuCard> _deck            = new List<HwatuCard>();
    private List<HwatuCard> _playerHand      = new List<HwatuCard>();
    private List<HwatuCard> _field           = new List<HwatuCard>();
    private List<HwatuCard> _playerCapture   = new List<HwatuCard>();

    private HwatuCard _selectedHandCard  = null;  // 손패에서 선택된 카드
    private bool      _waitingFieldPick  = false; // 필드 카드 선택 대기 중
    private bool      _gameOver          = false;

    private int _turnCount = 0;         // 사용한 턴 수
    private const int MAX_TURNS = 10;   // 플레이어 총 10턴

    // 카드 타입 정의 (월별 4장: 광·열끗·띠·피)
    // 광이 없는 월은 Pi5 대신 Yul, 광 없으면 Pi 등 실제 화투 규칙 반영
    private static readonly CardType[][] CARD_TYPES = new CardType[][]
    {
        // 1월: 솔 - 광, 열끗, 띠, 피
        new[]{ CardType.Gwang, CardType.Yul, CardType.Pi5, CardType.Pi },
        // 2월: 매화 - 열끗(고도리), 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 3월: 벚꽃 - 광, 열끗, 띠, 피
        new[]{ CardType.Gwang, CardType.Yul, CardType.Pi5, CardType.Pi },
        // 4월: 등나무 - 열끗(고도리), 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 5월: 창포 - 열끗, 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 6월: 모란 - 열끗, 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 7월: 홍싸리 - 열끗, 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 8월: 공산 - 광, 열끗(고도리), 띠, 피
        new[]{ CardType.Gwang, CardType.Yul, CardType.Pi5, CardType.Pi },
        // 9월: 국화 - 열끗, 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 10월: 단풍 - 열끗, 띠, 피, 피
        new[]{ CardType.Yul,   CardType.Pi5, CardType.Pi,  CardType.Pi },
        // 11월: 오동 - 광, 피, 피, 피
        new[]{ CardType.Gwang, CardType.Pi,  CardType.Pi,  CardType.Pi },
        // 12월: 비 - 광(비광), 열끗, 피, 피
        new[]{ CardType.Gwang, CardType.Yul, CardType.Pi,  CardType.Pi },
    };

    // ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        goButton?.onClick.AddListener(OnGoClicked);
        restartButton?.onClick.AddListener(StartGame);
        goButton?.gameObject.SetActive(false);
        resultPanel?.SetActive(false);
        StartGame();
    }

    // ══════════════════════════════════════════════════════════════════
    //  게임 시작 / 초기화
    // ══════════════════════════════════════════════════════════════════
    public void StartGame()
    {
        _gameOver = false;
        _turnCount = 0;
        _selectedHandCard = null;
        _waitingFieldPick = false;

        ClearArea(playerHandArea);
        ClearArea(fieldArea);
        ClearArea(playerCaptureArea);

        _deck.Clear();
        _playerHand.Clear();
        _field.Clear();
        _playerCapture.Clear();

        resultPanel?.SetActive(false);
        goButton?.gameObject.SetActive(false);
        comboText.text = "";

        BuildDeck();
        ShuffleDeck();
        DealCards();
        RefreshUI();
    }

    // ── 덱 생성 (48장) ───────────────────────────────────────────────
    void BuildDeck()
    {
        for (int m = 0; m < 12; m++)
        {
            for (int i = 0; i < 4; i++)
            {
                int globalIdx = m * 4 + i;
                Sprite face = (cardSprites != null && globalIdx < cardSprites.Length)
                              ? cardSprites[globalIdx] : null;

                var obj  = Instantiate(cardPrefab, playerHandArea); // 임시 부모
                var card = obj.GetComponent<HwatuCard>();
                card.Init(m + 1, i, CARD_TYPES[m][i], globalIdx, face, cardBackSprite);
                card.location = CardLocation.Deck;
                card.SetFaceUp(false);
                obj.SetActive(false);
                _deck.Add(card);
            }
        }
    }

    // ── 덱 섞기 ───────────────────────────────────────────────────────
    void ShuffleDeck()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    // ── 카드 배분: 플레이어 10장, 필드 8장 ───────────────────────────
    void DealCards()
    {
        // 플레이어 10장
        for (int i = 0; i < 10; i++)
        {
            var card = DrawFromDeck();
            MoveCardTo(card, CardLocation.PlayerHand);
        }
        // 필드 8장
        for (int i = 0; i < 8; i++)
        {
            var card = DrawFromDeck();
            MoveCardTo(card, CardLocation.Field);
        }
        LayoutHand();
        LayoutField();
    }

    // ══════════════════════════════════════════════════════════════════
    //  카드 클릭 처리 (HwatuCard에서 호출)
    // ══════════════════════════════════════════════════════════════════
    public void OnCardClicked(HwatuCard card)
    {
        if (_gameOver) return;

        // ── 손패 카드 클릭 ──────────────────────────────────────────
        if (card.location == CardLocation.PlayerHand)
        {
            SelectHandCard(card);
            return;
        }

        // ── 필드 카드 클릭 (매칭 시도) ──────────────────────────────
        if (card.location == CardLocation.Field && _waitingFieldPick)
        {
            TryMatchField(card);
            return;
        }
    }

    // ── 손패 카드 선택 ────────────────────────────────────────────────
    void SelectHandCard(HwatuCard card)
    {
        // 이미 선택된 카드 클릭 → 덱 뒤집기 모드로 진행
        if (_selectedHandCard == card)
        {
            ExecuteHandCard(card);
            return;
        }

        // 다른 카드 선택
        if (_selectedHandCard != null)
            _selectedHandCard.SetSelected(false);

        _selectedHandCard = card;
        card.SetSelected(true);

        // 같은 월 필드 카드 하이라이트
        HighlightMatchingField(card.month);
        _waitingFieldPick = true;

        // 필드에 같은 월이 없으면 → 즉시 덱 뒤집기로 진행 안내
        bool hasMatch = _field.Any(f => f.month == card.month);
        if (!hasMatch)
        {
            comboText.text = "같은 월이 없습니다. 카드를 다시 클릭하면 덱에서 뒤집습니다.";
        }
    }

    // ── 필드 매칭 시도 ────────────────────────────────────────────────
    void TryMatchField(HwatuCard fieldCard)
    {
        if (_selectedHandCard == null) return;

        if (fieldCard.month != _selectedHandCard.month)
        {
            comboText.text = "❌ 같은 월끼리만 매칭됩니다.";
            return;
        }

        // 매칭 성공
        CaptureCards(_selectedHandCard, fieldCard);
        _selectedHandCard = null;
        _waitingFieldPick = false;
        ClearHighlights();

        // 덱에서 1장 뒤집기
        FlipFromDeck();
    }

    // ── 손패 카드 낼 때 필드 매칭 없으면 덱 뒤집기 ───────────────────
    void ExecuteHandCard(HwatuCard handCard)
    {
        // 필드에 같은 월 없음 → 필드에 버리기
        bool hasMatch = _field.Any(f => f.month == handCard.month);
        if (!hasMatch)
        {
            // 손패에서 필드로 이동
            _playerHand.Remove(handCard);
            _field.Add(handCard);
            handCard.location = CardLocation.Field;
            handCard.transform.SetParent(fieldArea, false);
            handCard.SetFaceUp(true);
            handCard.SetSelected(false);

            _selectedHandCard = null;
            _waitingFieldPick = false;
            ClearHighlights();
            LayoutField();

            // 덱 뒤집기
            FlipFromDeck();
        }
    }

    // ── 덱에서 카드 뒤집기 ────────────────────────────────────────────
    void FlipFromDeck()
    {
        if (_deck.Count == 0)
        {
            EndGame();
            return;
        }

        var flipped = DrawFromDeck();
        flipped.SetFaceUp(true);

        // 필드에 같은 월 있으면 자동 매칭
        var fieldMatch = _field.FirstOrDefault(f => f.month == flipped.month);
        if (fieldMatch != null)
        {
            // 덱 카드를 잠깐 필드에 보여준 뒤 캡처
            flipped.transform.SetParent(fieldArea, false);
            flipped.location = CardLocation.Field;
            _field.Add(flipped);
            LayoutField();
            StartCoroutine(DelayedCaptureDeckCard(flipped, fieldMatch));
        }
        else
        {
            // 필드에 버리기
            MoveCardTo(flipped, CardLocation.Field);
            LayoutField();
            FinishTurn();
        }
    }

    IEnumerator DelayedCaptureDeckCard(HwatuCard deckCard, HwatuCard fieldCard)
    {
        yield return new WaitForSeconds(0.5f);
        _field.Remove(deckCard);
        _field.Remove(fieldCard);

        _playerCapture.Add(deckCard);
        _playerCapture.Add(fieldCard);
        deckCard.location    = CardLocation.PlayerCapture;
        fieldCard.location   = CardLocation.PlayerCapture;
        deckCard.transform.SetParent(playerCaptureArea, false);
        fieldCard.transform.SetParent(playerCaptureArea, false);
        deckCard.SetFaceUp(true);
        fieldCard.SetFaceUp(true);

        LayoutCapture();
        FinishTurn();
    }

    // ── 카드 캡처 (손패 + 필드 → 내 먹은 카드) ───────────────────────
    void CaptureCards(HwatuCard hand, HwatuCard field)
    {
        _playerHand.Remove(hand);
        _field.Remove(field);

        _playerCapture.Add(hand);
        _playerCapture.Add(field);
        hand.location  = CardLocation.PlayerCapture;
        field.location = CardLocation.PlayerCapture;
        hand.transform.SetParent(playerCaptureArea, false);
        field.transform.SetParent(playerCaptureArea, false);
        hand.SetFaceUp(true);
        field.SetFaceUp(true);
        hand.SetSelected(false);

        LayoutHand();
        LayoutField();
        LayoutCapture();
    }

    // ── 턴 종료 처리 ──────────────────────────────────────────────────
    void FinishTurn()
    {
        _turnCount++;
        RefreshUI();
        CheckGameEnd();
    }

    // ── 게임 종료 조건 체크 ───────────────────────────────────────────
    void CheckGameEnd()
    {
        bool deckEmpty = _deck.Count == 0;
        bool handEmpty = _playerHand.Count == 0;

        // 족보 달성 시 결산 버튼 표시
        bool hasCombo = ScoreCalculator.HasAnyCombo(_playerCapture);
        goButton?.gameObject.SetActive(hasCombo);

        if (deckEmpty || handEmpty || _turnCount >= MAX_TURNS)
        {
            EndGame();
        }
    }

    // ── 결산(고) 버튼 클릭 ────────────────────────────────────────────
    void OnGoClicked()
    {
        EndGame();
    }

    // ── 게임 종료 & 결과 표시 ─────────────────────────────────────────
    void EndGame()
    {
        _gameOver = true;
        goButton?.gameObject.SetActive(false);

        var result = ScoreCalculator.Calculate(_playerCapture);

        string msg = "=== 결과 ===\n";
        if (result.combos.Count > 0)
            msg += string.Join("\n", result.combos) + "\n\n";
        else
            msg += "달성한 족보 없음\n\n";

        msg += $"최종 점수: <b>{result.totalScore}점</b>\n";
        msg += $"먹은 카드: {_playerCapture.Count}장  |  사용 턴: {_turnCount}턴";

        if (resultText  != null) resultText.text = msg;
        resultPanel?.SetActive(true);

        // 모든 카드 버튼 비활성화
        foreach (var c in _playerHand) c.SetInteractable(false);
        foreach (var c in _field)     c.SetInteractable(false);
    }

    // ══════════════════════════════════════════════════════════════════
    //  UI 갱신
    // ══════════════════════════════════════════════════════════════════
    void RefreshUI()
    {
        if (deckCountText   != null) deckCountText.text   = $"덱: {_deck.Count}장";
        if (turnCountText   != null) turnCountText.text   = $"턴: {_turnCount}/{MAX_TURNS}";

        var res = ScoreCalculator.Calculate(_playerCapture);
        if (playerScoreText != null) playerScoreText.text = $"점수: {res.totalScore}점";

        // 족보 알림
        if (comboText != null && res.combos.Count > 0)
            comboText.text = "🎉 " + string.Join("  ", res.combos);
    }

    // ══════════════════════════════════════════════════════════════════
    //  레이아웃 헬퍼
    // ══════════════════════════════════════════════════════════════════
    void LayoutHand()
    {
        LayoutCards(_playerHand, playerHandArea, spacing: 110f);
        foreach (var c in _playerHand) c.SaveBasePosition();
    }

    void LayoutField()
    {
        LayoutCards(_field, fieldArea, spacing: 110f);
    }

    void LayoutCapture()
    {
        LayoutCards(_playerCapture, playerCaptureArea, spacing: 55f);
    }

    void LayoutCards(List<HwatuCard> cards, Transform parent, float spacing)
    {
        int count = cards.Count;
        float totalWidth = spacing * (count - 1);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            cards[i].gameObject.SetActive(true);
            cards[i].transform.SetParent(parent, false);
            var rect = cards[i].GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(startX + spacing * i, 0);
        }
    }

    // ── 하이라이트 ────────────────────────────────────────────────────
    void HighlightMatchingField(int month)
    {
        foreach (var c in _field)
        {
            var img = c.GetComponent<Image>();
            if (img == null) continue;
            img.color = (c.month == month) ? new Color(1f, 1f, 0.5f) : Color.gray;
        }
    }

    void ClearHighlights()
    {
        foreach (var c in _field)
        {
            var img = c.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
        foreach (var c in _playerHand)
        {
            var img = c.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    // ── 덱 뽑기 ───────────────────────────────────────────────────────
    HwatuCard DrawFromDeck()
    {
        if (_deck.Count == 0) return null;
        var card = _deck[_deck.Count - 1];
        _deck.RemoveAt(_deck.Count - 1);
        return card;
    }

    // ── 카드 위치 이동 ────────────────────────────────────────────────
    void MoveCardTo(HwatuCard card, CardLocation loc)
    {
        card.location = loc;
        switch (loc)
        {
            case CardLocation.PlayerHand:
                _playerHand.Add(card);
                card.transform.SetParent(playerHandArea, false);
                card.SetFaceUp(true);
                card.gameObject.SetActive(true);
                card.SetInteractable(true);
                break;

            case CardLocation.Field:
                _field.Add(card);
                card.transform.SetParent(fieldArea, false);
                card.SetFaceUp(true);
                card.gameObject.SetActive(true);
                card.SetInteractable(true);
                break;

            case CardLocation.PlayerCapture:
                _playerCapture.Add(card);
                card.transform.SetParent(playerCaptureArea, false);
                card.SetFaceUp(true);
                card.gameObject.SetActive(true);
                card.SetInteractable(false);
                break;
        }
    }

    // ── 영역 초기화 ───────────────────────────────────────────────────
    void ClearArea(Transform area)
    {
        if (area == null) return;
        foreach (Transform child in area)
            Destroy(child.gameObject);
    }
}
