using System;
using PlumSword.Core.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Currency
{
    /// <summary>
    /// 보유 금화 한 줄을 보여 준다. 글자는 GoldText.Format 이 돌려준 것을 그대로 쓴다.
    /// 처음 한 번, 그 뒤로 Wallet.BalanceChanged 가 올 때마다 바꾼다. 매 프레임 다시 그리지 않는다.
    /// 사양: docs/spec/currency.md "화면".
    /// </summary>
    public sealed class CurrencyView : MonoBehaviour
    {
        // 위치와 크기, 글자 크기는 임시값 (docs/spec/currency.md "화면").
        private static readonly Vector2 LabelPosition = new Vector2(8f, 73f);
        private static readonly Vector2 LabelSize = new Vector2(200f, 16f);
        private const float LabelFontSize = 16f;

        private Wallet _wallet;
        private TextMeshProUGUI _label;
        private RectTransform _plaque, _amountRect;
        private bool _hasArtwork;
        private bool _warned;
        private long _displayed, _start, _target;
        private float _elapsed;
        private bool _counting;
        private const float CountDuration = .45f;

        /// <summary>
        /// canvas 아래에 보유 금화 글자를 만들고 wallet.Balance 로 바로 표시한다.
        /// wallet.BalanceChanged 를 구독하고, OnDestroy 에서 구독을 끊는다.
        /// </summary>
        public void Initialize(Wallet wallet, Canvas canvas)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            Unsubscribe();
            _wallet = wallet;

            CreateLabel(canvas);
            _displayed = _target = _wallet.Balance;
            _counting = false;
            SetText(_displayed);

            _wallet.BalanceChanged += OnBalanceChanged;
        }

        private void CreateLabel(Canvas canvas)
        {
            try
            {
                var plaqueSprite = Resources.Load<Sprite>("Art/UI/StatUpgrade/currency_plaque");
                var coinSprite = Resources.Load<Sprite>("Art/UI/StatUpgrade/coin");
                _hasArtwork = plaqueSprite != null && coinSprite != null;
                Transform parent = canvas.transform;
                if (_hasArtwork)
                {
                    _plaque = (RectTransform)new GameObject("GoldPlaque", typeof(RectTransform), typeof(Image)).transform;
                    _plaque.SetParent(canvas.transform, false);
                    _plaque.anchorMin = _plaque.anchorMax = Vector2.zero;
                    _plaque.pivot = new Vector2(0, 1); _plaque.anchoredPosition = new Vector2(8, 86);
                    _plaque.sizeDelta = new Vector2(116, 26);
                    var background = _plaque.GetComponent<Image>();
                    background.sprite = plaqueSprite; background.color = new Color(1, 1, 1, .9f); background.raycastTarget = false;
                    var coin = (RectTransform)new GameObject("Coin", typeof(RectTransform), typeof(Image)).transform;
                    coin.SetParent(_plaque, false); coin.anchorMin = coin.anchorMax = coin.pivot = new Vector2(0, .5f);
                    coin.anchoredPosition = new Vector2(7, 0); coin.sizeDelta = new Vector2(17, 17);
                    var coinImage = coin.GetComponent<Image>(); coinImage.sprite = coinSprite; coinImage.preserveAspect = true; coinImage.raycastTarget = false;
                    parent = _plaque;
                }
                var labelObject = new GameObject("GoldLabel", typeof(RectTransform));
                var rect = (RectTransform)labelObject.transform;
                rect.SetParent(parent, false);
                if (_hasArtwork)
                {
                    rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 1);
                    rect.offsetMin = new Vector2(27, 2); rect.offsetMax = new Vector2(-10, -2);
                    _amountRect = rect;
                }
                else
                {
                    rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = LabelPosition; rect.sizeDelta = LabelSize;
                }

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = _hasArtwork ? 13f : LabelFontSize;
                label.color = _hasArtwork ? new Color(1f, .92f, .78f) : Color.white;
                label.alignment = _hasArtwork ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Left;
                if (_hasArtwork) { label.enableAutoSizing = true; label.fontSizeMin = 5; label.fontSizeMax = 13; }
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;
                _label = label;
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private void OnBalanceChanged(long balance)
        {
            // 지출은 즉시 반영한다. 연출 도중 지출해도 이전 목표까지 계속 올라가지 않는다.
            if (balance < _target)
            {
                _target = _displayed = balance; _counting = false; SetText(balance); return;
            }
            _target = balance; _start = _displayed; _elapsed = 0;
            _counting = _target > _start;
        }

        private void Update()
        {
            if (!_counting) return;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= CountDuration)
            {
                _displayed = _target; _counting = false;
            }
            else
            {
                decimal ratio = (decimal)(_elapsed / CountDuration);
                _displayed = _start + (long)decimal.Floor(((decimal)_target - _start) * ratio);
            }
            SetText(_displayed);
        }

        private void SetText(long balance)
        {
            if (_label == null) return;

            try
            {
                var number = GoldText.FormatNumber(balance);
                if (_hasArtwork && _plaque != null && _amountRect != null)
                {
                    _plaque.sizeDelta = new Vector2(Mathf.Min(200, 116 + Mathf.Max(0, number.Length - 8) * 5), 26);
                    _label.text = number;
                }
                else _label.text = GoldText.Format(balance);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private void WarnOnce(Exception e)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(
                "CurrencyView: 보유 금화 글자를 표시하지 못했습니다. TMP Essentials 가 필요합니다 " +
                "(Window > TextMeshPro > Import TMP Essential Resources). " + e.Message);
        }

        private void Unsubscribe()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _wallet = null;
        }
    }
}
