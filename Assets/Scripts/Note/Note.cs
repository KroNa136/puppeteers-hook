using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;

public class Note : NetworkBehaviour
{
    private const string CORRUPTED_CHARACTERS = "јаЅб¬в √гƒд≈е ®Є∆ж«з »и…й к ЋлћмЌн ќоѕп–р —с“т”у ‘ф’х÷ц „чЏъџы №ьЁэёю я€1234 567890 .!?,:; -()";

    private static List<string> s_loreTexts;
    private static string s_candlesticksPuzzleText;
    private static string s_holdablesPuzzleText;
    private static string s_statuesPuzzleText;
    private static string s_rotatingMirrorsPuzzleText;
    private static string s_clocksPuzzleText;

    [SerializeField] private NoteAudioController _audioController;

    [SyncVar(hook = nameof(OnClientTextChanged))]
    public string Text;

    private string _text;
    private NoteReader _noteReader;

    [Client]
    public void OnClientTextChanged(string oldValue, string newValue)
    {
        _text = newValue;
    }

    [Client]
    public override void OnStartClient()
    {
        if (!isClient)
            return;

        if (TryGetComponent(out Interactable interactable))
        {
            interactable.OnPredictInteraction.AddListener(ClientRead);
            interactable.OnFailInteraction.AddListener(ClientStopReading);
        }
    }

    [Client]
    public void ClientRead()
    {
        if (!isClient)
            return;

        if (_noteReader == null)
            _ = NetworkClient.localPlayer.TryGetComponent(out _noteReader);

        _ = _noteReader.Bind((cnr, note) => cnr.OpenNoteReadingMenu(note), this);
        _ = _audioController.Bind(c => c.PlayStartReadingSound());
    }

    [Client]
    public void ClientStopReading()
    {
        if (!isClient)
            return;

        _ = _noteReader.Bind(cnr => cnr.CloseNoteReadingMenu());
        _ = _audioController.Bind(c => c.PlayStopReadingSound());
    }

    public string GetCorruptedText(float corruptionFraction)
    {
        if (corruptionFraction == 0f)
            return _text;

        float clampedCorruptionFraction = Mathf.Clamp01(corruptionFraction);

        int charactersToCorrupt = Mathf.FloorToInt(clampedCorruptionFraction * _text.Length);
        var characterIndices = Enumerable.Range(0, _text.Length).ToList();

        StringBuilder sb = new(_text);

        for (int i = 0; i < charactersToCorrupt; i++)
        {
            int characterIndex = characterIndices.UnityRandomItem();
            _ = characterIndices.Remove(characterIndex);

            sb[characterIndex] = CORRUPTED_CHARACTERS.UnityRandomItem();
        }

        return sb.ToString();
    }

    [Server]
    public void ServerSetLoreText()
    {
        if (!isServer)
            return;

        if (s_loreTexts == null || s_loreTexts.None())
            GetNotesFromResources();

        int randomIndex = UnityEngine.Random.Range(0, s_loreTexts.Count);

        _text = s_loreTexts[randomIndex];
        RpcSetLoreText(randomIndex);
    }

    [ClientRpc]
    public void RpcSetLoreText(int index)
    {
        if (!isClient)
            return;

        if (s_loreTexts == null || s_loreTexts.None())
            GetNotesFromResources();

        _text = s_loreTexts[index];
    }

    [Server]
    public void ServerSetFullyCorruptedText(int length)
    {
        if (!isServer)
            return;

        StringBuilder sb = new();

        for (int i = 0; i < length; i++)
            _ = sb.Append(CORRUPTED_CHARACTERS.UnityRandomItem());

        _text = sb.ToString();
        Text = _text;
    }

    [Server]
    public void ServerSetCandlesticksPuzzleText()
    {
        if (!isServer)
            return;

        if (string.IsNullOrEmpty(s_candlesticksPuzzleText))
            GetNotesFromResources();

        _text = s_candlesticksPuzzleText;
        Text = _text;
    }

    [Server]
    public void ServerSetHoldablesPuzzleText(List<HoldablePlacementTarget> placementTargets)
    {
        if (!isServer)
            return;

        if (string.IsNullOrEmpty(s_holdablesPuzzleText))
            GetNotesFromResources();

        var holdableTypeNames = placementTargets
            .Select(t => t.HoldableType)
            .Select(t => t switch
                {
                    HoldableType.Skull => "череп",
                    HoldableType.Globe => "глобус",
                    HoldableType.Crystal => "аметистовый кристалл",
                    _ => "null"
                }
            )
            .ToList();

        var placementTargetNames = placementTargets
            .Select(t => t.DisplayName)
            .ToList();

        _text = string.Format(s_holdablesPuzzleText, holdableTypeNames[0], placementTargetNames[0], holdableTypeNames[1], placementTargetNames[1], holdableTypeNames[2], placementTargetNames[2]);
        Text = _text;
    }

    [Server]
    public void ServerSetStatuesPuzzleText(float targetStatueAngle, float windRoseAngle)
    {
        if (!isServer)
            return;

        if (string.IsNullOrEmpty(s_statuesPuzzleText))
            GetNotesFromResources();

        float angleDelta = Mathf.DeltaAngle(windRoseAngle, targetStatueAngle);

        string direction = angleDelta switch
        {
            > -22.5f and < 22.5f => "север",
            > -67.5f and <= -22.5f => "северо-запад",
            > -112.5f and <= -67.5f => "запад",
            > -157.5f and <= -112.5f => "юго-запад",
            >= 22.5f and < 67.5f => "северо-восток",
            >= 67.5f and < 112.5f => "восток",
            >= 112.5f and < 157.5f => "юго-восток",
            _ => "юг"
        };

        _text = string.Format(s_statuesPuzzleText, direction);
        Text = _text;
    }

    [Server]
    public void ServerSetRotatingMirrorsPuzzleText()
    {
        if (!isServer)
            return;

        if (string.IsNullOrEmpty(s_rotatingMirrorsPuzzleText))
            GetNotesFromResources();

        _text = s_rotatingMirrorsPuzzleText;
        Text = _text;
    }

    [Server]
    public void ServerSetClocksPuzzleText()
    {
        if (!isServer)
            return;

        if (string.IsNullOrEmpty(s_clocksPuzzleText))
            GetNotesFromResources();

        _text = s_clocksPuzzleText;
        Text = _text;
    }

    private void GetNotesFromResources()
    {
        TextAsset notesResource = Resources.Load<TextAsset>("Notes");

        if (notesResource == null)
        {
            Debug.LogError("Notes resource file was not found.");
            return;
        }

        NoteCollection noteCollection = null;

        try
        {
            noteCollection = JsonUtility.FromJson<NoteCollection>(notesResource.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to deserialize Notes resource file: {ex.Message}");
        }

        s_loreTexts = noteCollection.LoreTexts;
        s_candlesticksPuzzleText = noteCollection.CandlesticksPuzzleText;
        s_holdablesPuzzleText = noteCollection.HoldablesPuzzleText;
        s_statuesPuzzleText = noteCollection.StatuesPuzzleText;
        s_rotatingMirrorsPuzzleText = noteCollection.RotatingMirrorsPuzzleText;
        s_clocksPuzzleText = noteCollection.ClocksPuzzleText;
    }
}
