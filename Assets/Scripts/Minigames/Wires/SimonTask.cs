using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class SimonTask : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private SimonButton[] buttons;
    [SerializeField] private float flashTime = 0.3f;
    [SerializeField] private float gapTime = 0.12f;

    [Header("Progression")]
    [SerializeField] private int baseSequenceLength = 3; // first round length
    [SerializeField] private int roundCount = 3;         // how many rounds
    [SerializeField] private float preRoundDelay = 1.0f; // wait before "Watch..."

    [HideInInspector] public Action onTaskComplete;

    private List<int> sequence = new();
    private int inputIndex = 0;
    private bool playerTurn = false;
    private bool playingSequence = false;
    private int currentRound = 0;

    void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].Init(this, i);
        }
    }

    public void BeginSimon()
    {
        currentRound = 0;
        StartCoroutine(RunRounds());
    }

    IEnumerator RunRounds()
    {
        while (currentRound < roundCount)
        {
            BuildSequenceForRound(currentRound);

            playerTurn = false;
            playingSequence = false;
            UpdateStatus("Watch...");
            yield return new WaitForSeconds(preRoundDelay);

            yield return StartCoroutine(PlaySequence());

            playerTurn = true;
            inputIndex = 0;
            UpdateStatus("Your turn.");

            bool roundCleared = false;
            bool retryNeeded = false;

            while (!roundCleared)
            {
                if (retryNeeded)
                {
                    retryNeeded = false;
                    playerTurn = false;
                    UpdateStatus("Nope. Watch again...");
                    yield return new WaitForSeconds(0.4f);
                    yield return StartCoroutine(PlaySequence());
                    playerTurn = true;
                    inputIndex = 0;
                    UpdateStatus("Your turn.");
                }

                yield return null;

                roundCleared = (inputIndex >= sequence.Count);
            }

            currentRound++;
            UpdateStatus("Good.");
            yield return new WaitForSeconds(0.4f);
        }

        CompleteTask();
    }

    void BuildSequenceForRound(int round)
    {
        sequence.Clear();
        int length = baseSequenceLength + round;
        for (int i = 0; i < length; i++)
        {
            sequence.Add(UnityEngine.Random.Range(0, buttons.Length));
        }
    }

    IEnumerator PlaySequence()
    {
        playingSequence = true;
        inputIndex = 0;

        for (int i = 0; i < sequence.Count; i++)
        {
            int idx = sequence[i];
            yield return StartCoroutine(buttons[idx].FlashColor(flashTime));
            yield return new WaitForSeconds(gapTime);
        }

        playingSequence = false;
    }

    public void PlayerPress(int btnIndex)
    {
        if (!playerTurn || playingSequence) return;

        if (btnIndex == sequence[inputIndex])
        {
            inputIndex++;
        }
        else
        {
            StartCoroutine(HandleMistake());
        }
    }

    IEnumerator HandleMistake()
    {
        playerTurn = false;
        UpdateStatus("Nope. Watch again...");
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(PlaySequence());
        playerTurn = true;
        inputIndex = 0;
        UpdateStatus("Your turn.");
    }

    void CompleteTask()
    {
        playerTurn = false;
        UpdateStatus("Diagnostics clear.");
        onTaskComplete?.Invoke();
    }

    void UpdateStatus(string s)
    {
        if (statusText) statusText.text = s;
    }
}
