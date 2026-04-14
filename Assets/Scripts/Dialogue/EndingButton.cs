using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    private static readonly Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color unlockedColor = Color.white;

    public void Init(endingRes data, bool isUnlocked, Action<endingRes> onSelect)
    {
        titleText.text = data.title;
        conditionText.text = data.requiredFlow > 0
            ? $"조건 : 분위기 {data.requiredFlow} 이상"
            : "";

        button.interactable = isUnlocked;
        buttonImage.color = isUnlocked ? unlockedColor : lockedColor;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelect(data));
    }
}