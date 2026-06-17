using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveType
{
    Interact,
    Collect,
    Kill,
    Reach,
    Talk,
    Cutscene
}

public enum MinigameType
{
    Timing,
    Lockpick,
    Puzzle,
    ButtonMash,
    Rhythm
}

[Serializable]
public class ObjectiveData
{
    public string objectiveId;
    public string description;
    public ObjectiveType type;
    public int targetAmount;
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public List<ObjectiveData> objectives;
}