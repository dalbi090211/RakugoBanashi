using TMPro;
using UnityEngine;

public class ChoiceButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    private choiceRes curData;
    private ChoiceEventManager manager;

    public void setData(choiceRes data, ChoiceEventManager eventManager)
    {
        text.text = data.textName;
        curData = data;
        manager = eventManager;
    }

    public void OnClickButton()
    {
        manager.OnChoiceSelected(curData);
    }
}