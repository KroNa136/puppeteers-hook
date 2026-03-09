using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallText : MonoBehaviour
{
    [Header("References")]

    [SerializeField] TextMesh textMesh;

    [Header("Values")]

    [SerializeField] bool isVisible = false;
    [SerializeField] float fadeSpeed = 2f;

    bool wasVisible;

    Color originalColor;
    Color transparentColor;

    void Start()
    {
        GenerateText();

        originalColor = textMesh.color;
        
        transparentColor = textMesh.color;
        transparentColor.a = 0f;

        textMesh.color = transparentColor;
    }

    void Update()
    {
        if (isVisible)
        {
            if (!wasVisible)
                GenerateText();

            textMesh.color = Color.Lerp(textMesh.color, originalColor, fadeSpeed * Time.deltaTime);
        }
        else
        {
            textMesh.color = Color.Lerp(textMesh.color, transparentColor, fadeSpeed * Time.deltaTime);
        }

        wasVisible = isVisible;
    }

    public void SetVisible(bool value)
    {
        isVisible = value;
    }

    [ContextMenu("Generate Text")]
    public void GenerateText()
    {
        if (Random.Range(0, WallTexts.emptyTextChance) == 0)
            textMesh.text = "";
        else if (Random.Range(0, WallTexts.nameTextChance) == 0)
            textMesh.text = "Nashville"; //TODO: localize
        else if (Application.systemLanguage == SystemLanguage.Russian)
            textMesh.text = WallTexts.textsRus[Random.Range(0, WallTexts.textsRus.Length)];
        else
            textMesh.text = WallTexts.textsEng[Random.Range(0, WallTexts.textsEng.Length)];
    }
}
