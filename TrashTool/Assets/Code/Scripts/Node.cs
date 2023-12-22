using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] NodeState state;
    public bool IsSelected { get; set; }

    public void SetState(NodeState state)
    {
        // Any state changing logic...
        print("Changing node state");

        this.state = state;
    }

    public NodeState GetState()
    {
        return state;
    }
}
