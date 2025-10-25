using UnityEngine;

public class ShipRepairController : MonoBehaviour
{
    [Header("External")]
    [SerializeField] private MinigameController minigame;

    [Header("Panels")]
    [SerializeField] private GameObject panelWires;
    [SerializeField] private GameObject panelSimon;
    [SerializeField] private GameObject panelRefuel;

    [Header("Tasks")]
    [SerializeField] private WireTask wireTask;
    [SerializeField] private SimonTask simonTask;
    [SerializeField] private RefuelTask refuelTask;

    private int currentStep = 0; // 0 = wires, 1 = simon, 2 = refuel, 3 = done

    void Start()
    {
        if (!minigame) minigame = FindObjectOfType<MinigameController>();

        ShowStep(0);

        wireTask.onTaskComplete += OnWireDone;
        simonTask.onTaskComplete += OnSimonDone;
        refuelTask.onTaskComplete += OnRefuelDone;
    }

    void ShowStep(int step)
    {
        currentStep = step;

        if (panelWires) panelWires.SetActive(step == 0);
        if (panelSimon) panelSimon.SetActive(step == 1);
        if (panelRefuel) panelRefuel.SetActive(step == 2);
    }

    void OnWireDone()
    {
        ShowStep(1);
        simonTask.BeginSimon();
    }

    void OnSimonDone()
    {
        ShowStep(2);
        refuelTask.BeginRefuel();
    }

    void OnRefuelDone()
    {
        GameState.I.SetFlag("cryovale_completed");
        if (minigame) minigame.OnWin();
        else Debug.Log("ShipRepairController: repair complete!");
    }
}

