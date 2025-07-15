using UnityEngine;
using TMPro;  

[RequireComponent(typeof(Canvas))]
public class WordLabel : MonoBehaviour
{
    [Tooltip("Drag the child TMP Text component here")]
    public TMP_Text uiText;   

    Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
    }

    public void SetText(string word)
    {
        if (uiText != null)
            uiText.text = word;
        else
            Debug.LogWarning($"WordLabel on '{name}' has no TMP_Text assigned.");
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
