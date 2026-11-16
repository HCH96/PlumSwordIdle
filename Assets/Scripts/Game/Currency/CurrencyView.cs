using System;
using PlumSword.Core.Currency;
using TMPro;
using UnityEngine;

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
        private static readonly Vector2 LabelPosition = new Vector2(8f, 116f);
        private static readonly Vector2 LabelSize = new Vector2(200f, 16f);
        private const float LabelFontSize = 16f;

        private Wallet _wallet;
        private TextMeshProUGUI _label;
        private bool _warned;

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
            SetText(_wallet.Balance);

            _wallet.BalanceChanged += OnBalanceChanged;
        }

        private void CreateLabel(Canvas canvas)
        {
            try
            {
                var labelObject = new GameObject("GoldLabel", typeof(RectTransform));
                var rect = (RectTransform)labelObject.transform;
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = LabelPosition;
                rect.sizeDelta = LabelSize;

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = LabelFontSize;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Left;
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
            SetText(balance);
        }

        private void SetText(long balance)
        {
            if (_label == null) return;

            try
            {
                _label.text = GoldText.Format(balance);
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
