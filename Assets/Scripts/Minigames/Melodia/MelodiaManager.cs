using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MelodiaManager : MonoBehaviour
{
    [Header("Planet data")]
    [SerializeField] PlanetData planetData;

    [Tooltip("Name of the scene you want to return to when the player leaves (probably your starmap scene).")]
    [SerializeField] string returnSceneName = "StarMap";

    [Tooltip("Which areaId is considered the NPC area. (Should match the Id field in PlanetData.Areas for the NPC entry.)")]
    [SerializeField] string npcAreaId = "npc";

    [Header("UI bindings")]
    [Tooltip("Assign the DialogueUI prefab instance in this scene.")]
    [SerializeField] DialogueUI dialogueUI;

    [Tooltip("Optional overlay/panel you show AFTER finishing the NPC final conversation + storyboard. Can be left null.")]
    [SerializeField] GameObject winPanel;

    [Header("Areas in this scene")]
    [Tooltip("List every area root in this scene (home, school, pub, npc, etc.).")]
    [SerializeField] List<AreaBinding> areas = new List<AreaBinding>();

    [System.Serializable]
    public class AreaBinding
    {
        [Tooltip("Must match PlanetData.Areas[i].id exactly, e.g. 'home', 'school', 'pub', 'npc'.")]
        public string areaId;

        [Tooltip("Root GameObject for this area's UI (background images, buttons, etc).")]
        public GameObject areaRoot;

        [Header("Memory collect button (NOT for npc)")]
        [Tooltip("Button that collects the memory in this area. Leave null for npc area.")]
        public Button collectButton;
        [Tooltip("Text on the collect button so we can swap 'Recover memory' -> 'Memory recovered'. Optional.")]
        public TMP_Text collectButtonLabel;

        [Header("NPC talk button (ONLY for npc area)")]
        [Tooltip("Button inside the NPC area that starts NPC dialogue. Leave null for normal areas.")]
        public Button npcTalkButton;

        [Header("Navigation")]
        [Tooltip("A 'Back' button in this area that should leave this planet scene or go back to starmap.")]
        public Button backButton;
    }

    [Header("NPC Dialogue Sequences")]
    [Tooltip("Dialogue shown if you talk to the NPC before all memories are recovered.")]
    [SerializeField] DialogueSequence npcBeforeAllMemories;

    [Tooltip("Dialogue shown when you HAVE all memories but haven't 'finished' this planet yet.")]
    [SerializeField] DialogueSequence npcReadyToComplete;

    [Tooltip("Dialogue shown if you come back to the NPC after finishing the planet once.")]
    [SerializeField] DialogueSequence npcAfterComplete;

    [Header("Final Storyboard")]
    [Tooltip("Storyboard to play ONCE when player completes the planet with the NPC. Optional.")]
    [SerializeField] StoryboardAsset completionStoryboard;

    [Header("Sound")]
    [Tooltip("Sound to play when collecting a memory.")]
    [SerializeField] AudioSource Audio;
    [SerializeField] AudioClip Song;


    Dictionary<string, AreaBinding> areaLookup = new Dictionary<string, AreaBinding>();

    void Awake()
    {
        foreach (var a in areas)
        {
            if (!string.IsNullOrEmpty(a.areaId) && !areaLookup.ContainsKey(a.areaId))
            {
                areaLookup.Add(a.areaId, a);
            }
        }
    }

    void Start()
    {
        if (dialogueUI) dialogueUI.gameObject.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        HookupAreaButtons();

        string startArea = GameState.I.currentAreaId;
        if (string.IsNullOrEmpty(startArea))
        {
            // fallback to first area in planetData if somehow not set
            if (planetData != null && planetData.areas != null && planetData.areas.Length > 0)
                startArea = planetData.areas[0].id;
            else
                startArea = npcAreaId; // last fallback
        }

        ShowArea(startArea);

        RefreshCollectButtons();
    }

    void HookupAreaButtons()
    {
        foreach (var a in areas)
        {
            // Collect memory
            if (a.collectButton != null)
            {
                string capturedId = a.areaId;
                a.collectButton.onClick.RemoveAllListeners();
                a.collectButton.onClick.AddListener(() => {
                    CollectMemory(capturedId);
                });
            }

            // NPC talk
            if (a.npcTalkButton != null)
            {
                a.npcTalkButton.onClick.RemoveAllListeners();
                a.npcTalkButton.onClick.AddListener(() => {
                    HandleNPCTalk();
                });
            }

            // Back / leave planet
            if (a.backButton != null)
            {
                a.backButton.onClick.RemoveAllListeners();
                a.backButton.onClick.AddListener(() => {
                    LeavePlanet();
                });
            }
        }

        if (dialogueUI != null)
        {
            dialogueUI.onCommand = HandleDialogueCommand;
        }
    }


    void HideAllAreas()
    {
        foreach (var a in areas)
        {
            if (a.areaRoot) a.areaRoot.SetActive(false);
        }
        if (winPanel) winPanel.SetActive(false);
    }

    public void ShowArea(string areaId)
    {
        HideAllAreas();

        if (areaLookup.TryGetValue(areaId, out var binding))
        {
            if (binding.areaRoot) binding.areaRoot.SetActive(true);
        }
        else
        {
            if (areaLookup.TryGetValue(npcAreaId, out var npcBinding) && npcBinding.areaRoot)
                npcBinding.areaRoot.SetActive(true);
        }

        GameState.I.currentAreaId = areaId;
        RefreshCollectButtons();
    }


    void CollectMemory(string areaId)
    {
        GameState.I.MarkAreaComplete(GameState.I.currentPlanetId, areaId);

        RefreshCollectButtons();
    }

    void RefreshCollectButtons()
    {
        foreach (var a in areas)
        {
            bool alreadyDone = GameState.I.IsAreaComplete(GameState.I.currentPlanetId, a.areaId);

            if (a.collectButton != null)
            {
                a.collectButton.interactable = !alreadyDone;
            }

            if (a.collectButtonLabel != null)
            {
                a.collectButtonLabel.text = alreadyDone ? "Memory recovered" : "Recover memory";
            }
        }
    }

    bool AllRequiredMemoriesCollected()
    {
        if (planetData == null || planetData.areas == null) return false;

        foreach (var a in planetData.areas)
        {
            if (a.id == npcAreaId) continue;
            if (!GameState.I.IsAreaComplete(GameState.I.currentPlanetId, a.id))
                return false;
        }
        return true;
    }


    void HandleNPCTalk()
    {
        bool hasAllMemories = AllRequiredMemoriesCollected();
        bool planetAlreadyComplete = GameState.I.HasFlag(CompleteFlagKey());

        // Case 1: still missing memories
        if (!hasAllMemories)
        {
            PlayNPCDialogue(npcBeforeAllMemories, () => {
                // after talking, just remain in npc view
            });
            return;
        }

        // Case 2: you have all memories but haven't "finished" this planet yet
        if (!planetAlreadyComplete)
        {
            Audio.Play();
            PlayNPCDialogue(npcReadyToComplete, () => {
                FinishPlanet();
            });
            return;
        }

        // Case 3: you already finished this planet once
        PlayNPCDialogue(npcAfterComplete, () => {
            // done talking, stay in npc
        });
    }

    void PlayNPCDialogue(DialogueSequence seq, System.Action after)
    {
        if (!dialogueUI)
        {
            after?.Invoke();
            return;
        }

        dialogueUI.gameObject.SetActive(true);
        dialogueUI.Play(seq, () => {
            after?.Invoke();
        });
    }

    void FinishPlanet()
    {
        GameState.I.SetFlag(CompleteFlagKey());

        GameState.I.MarkAreaComplete(GameState.I.currentPlanetId, npcAreaId);

        if (completionStoryboard != null && StoryboardService.I != null)
        {
            StoryboardService.I.Play(completionStoryboard, () => {
                ShowWinOrStay();
            });
        }
        else
        {
            ShowWinOrStay();
        }
    }

    void ShowWinOrStay()
    {
        if (winPanel != null)
        {
            HideAllAreas();
            winPanel.SetActive(true);
        }
        else
        {
            GameState.I.SetFlag(CompleteFlagKey());
            PlayNPCDialogue(npcAfterComplete, () => {
                LeavePlanet();
            });
        }
    }

    string CompleteFlagKey()
    {

        string pid = GameState.I.currentPlanetId;
        if (string.IsNullOrEmpty(pid) && planetData != null)
        {
            pid = planetData.planetId;
            GameState.I.currentPlanetId = pid;
        }
        return pid + "_completed";
    }


    void HandleDialogueCommand(string cmd)
    {
        switch ((cmd ?? "").ToLowerInvariant())
        {
            case "exit":
                if (dialogueUI) dialogueUI.gameObject.SetActive(false);
                break;

            case "leave":
                if (dialogueUI) dialogueUI.gameObject.SetActive(false);
                LeavePlanet();
                break;

            case "finish":
                if (dialogueUI) dialogueUI.gameObject.SetActive(false);
                FinishPlanet();
                break;

            default:
                if (dialogueUI) dialogueUI.gameObject.SetActive(false);
                break;
        }
    }


    public void LeavePlanet()
    {
        if (!string.IsNullOrEmpty(returnSceneName))
        {
            if (SceneLoader.I != null)
            {
                SceneLoader.I.LoadSceneWithFade(returnSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(returnSceneName);
            }
        }
    }
}
