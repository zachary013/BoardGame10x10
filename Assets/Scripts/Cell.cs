using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Cell : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image backgroundImage;
    public Button button;

    private int row;
    private int col;
    private int value;
    private bool isAvailable;

    public void Initialize(int row, int col, Action<int, int> onClickCallback)
    {
        this.row = row;
        this.col = col;
        button.onClick.AddListener(() => onClickCallback(row, col));
        Reset();
    }

    public void SetValue(int newValue)
    {
        value = newValue;
        valueText.text = value.ToString();
        backgroundImage.color = Color.yellow; // Always set to yellow when a value is assigned
        isAvailable = false;
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;
        if (value == 0) // Only change color if the cell is not already selected
        {
            backgroundImage.color = available ? new Color(1f, 0.5f, 0f) : Color.gray;
        }
    }

    public bool IsEmpty()
    {
        return value == 0;
    }

    public void Reset()
    {
        value = 0;
        valueText.text = "";
        backgroundImage.color = Color.gray;
        isAvailable = false;
    }
}