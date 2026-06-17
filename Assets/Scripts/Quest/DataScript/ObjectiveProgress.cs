using UnityEngine;

[System.Serializable]
public class ObjectiveProgress
{
    public string objectiveId;
    public string description;
    public int targetAmount;
    public int currentAmount;
    public bool isCompleted;
}