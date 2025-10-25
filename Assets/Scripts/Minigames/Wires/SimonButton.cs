using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimonButton : MonoBehaviour
{
    [SerializeField] private Image img;
    [SerializeField] private Button button;

    private SimonTask parent;
    private int myIndex;
    private Color baseColor;

    [SerializeField] private float highlightMultiplier = 1.6f; 

    void Awake()
    {
        if (!img) img = GetComponent<Image>();
        if (!button) button = GetComponent<Button>();
        if (img) baseColor = img.color;

        if (button)
            button.onClick.AddListener(OnClick);
    }

    public void Init(SimonTask task, int index)
    {
        parent = task;
        myIndex = index;
    }

    void OnClick()
    {
        if (parent != null)
            parent.PlayerPress(myIndex);
    }

    public IEnumerator FlashColor(float t)
    {
        if (!img) yield break;

        Color bright = baseColor * highlightMultiplier;
        bright.a = baseColor.a;

        img.color = bright;
        yield return new WaitForSeconds(t);
        img.color = baseColor;
    }
}
