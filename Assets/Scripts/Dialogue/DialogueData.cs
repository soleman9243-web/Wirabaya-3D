using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Tooltip("List of all dialogue nodes. The dialogue starts at index 0.")]
    public List<DialogueNode> nodes = new List<DialogueNode>();
}
