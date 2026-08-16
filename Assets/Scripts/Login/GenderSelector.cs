using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GenderSelector : MonoBehaviour
{
    [Header("UI References")]
    public Button maleButton;
    public Button femaleButton;

    [Header("Visual Feedback")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    private string selectedGender = "Male"; // ברירת מחדל

    void Start()
    {
        // הגדרת Listeners לכפתורים
        maleButton.onClick.AddListener(() => SelectGender("Male"));
        femaleButton.onClick.AddListener(() => SelectGender("Female"));

        // בחירה ראשונית
        SelectGender("Male");
    }

    public void SelectGender(string gender)
    {
        selectedGender = gender;

        // עדכון ויזואלי
        if (gender == "Male")
        {
            maleButton.GetComponent<Image>().color = selectedColor;
            femaleButton.GetComponent<Image>().color = normalColor;
        }
        else
        {
            femaleButton.GetComponent<Image>().color = selectedColor;
            maleButton.GetComponent<Image>().color = normalColor;
        }

        // שמירה ל-PlayerPrefs כדי להעביר לסצנה הבאה
        PlayerPrefs.SetString("SelectedGender", selectedGender);
        PlayerPrefs.Save();

        Debug.Log("Selected Gender: " + selectedGender);
    }

    public string GetSelectedGender()
    {
        return selectedGender;
    }
}