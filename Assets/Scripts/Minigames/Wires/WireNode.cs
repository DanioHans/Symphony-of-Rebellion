using UnityEngine;
using UnityEngine.UI;

public class WireNode : MonoBehaviour
{
    [Header("Setup")]
    public string colorId;
    public bool isLeft;
    [SerializeField] private WireTask parentTask;

    [Header("Optional refs (auto if left empty)")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;

    private bool locked = false;
    public bool IsLocked => locked;

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!buttonImage) buttonImage = GetComponent<Image>();

        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        if (locked) return;
        if (parentTask != null)
            parentTask.SelectNode(this);
    }

    public void Lock()
    {
        locked = true;
        if (button) button.interactable = false;
    }
    public Image GetButtonImage()
    {
        return buttonImage;
    }

    
}
