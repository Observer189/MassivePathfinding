using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

public class WorldTextManager : MMSingleton<WorldTextManager>
{
    public Camera mainCamera;
    public Font font;
    public float textScale = 0.3f;
    public Color textColor = Color.white;

    private Canvas canvas;

    private Queue<Text> textPool;
    private List<Text> currentActive;

    protected override void Awake()
    {
        base.Awake();
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvas.worldCamera = mainCamera;
        }

        textPool = new Queue<Text>();
        currentActive = new List<Text>();
    }
    
    

    public static void DisplayText(string textToDisplay, Vector3 worldPosition)
    { 
        Text textComponent = null;
        if (Instance.textPool.Count > 0)
        {
            textComponent = Instance.textPool.Dequeue();
            textComponent.gameObject.SetActive(true);
        }
        else
        {
            GameObject textObject = new GameObject("WorldText", typeof(Text));
            textObject.transform.SetParent(Instance.transform, false);

            textComponent = textObject.GetComponent<Text>();    
        }
        
        Instance.currentActive.Add(textComponent);
        textComponent.font = Instance.font;
        textComponent.color = Instance.textColor;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.text = textToDisplay;

        RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
        rectTransform.position = worldPosition;
        rectTransform.localScale = Vector3.one * Instance.textScale;
    }

    public static void ReturnAll()
    {
        for (int i = 0; i < Instance.currentActive.Count; i++)
        {
            Instance.currentActive[i].gameObject.SetActive(false);
            Instance.textPool.Enqueue(Instance.currentActive[i]);
        }
        Instance.currentActive.Clear();
    }
}
