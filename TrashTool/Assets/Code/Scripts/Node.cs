using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] NodeState state;
    [SerializeField] ColorCombo[] stateColors;
    //public bool IsSelected { get; set; }

    public Renderer mRenderer;
    private bool isSelected;

    public bool IsSelected { get { return isSelected; } }

    private void Start()
    {
        mRenderer = this.GetComponent<MeshRenderer>();
        SetSelect(false);
    }

    /// <summary>
    /// Iterate to next possible state of this node 
    /// </summary>
    public void SetNextState()
    {
        int size = System.Enum.GetValues(typeof(NodeState)).Length;

        int next = (int)state + 1;
        if(next >= size)
        {
            next = 0;
        }

        state = (NodeState)next;
        SetSelect(isSelected);
    }

    public void SetState(NodeState state)
    {
        // Any state changing logic...
        print("Changing node state");

        this.state = state;
        SetSelect(isSelected);
    }

    /// <summary>
    /// Set this node to a particular state 
    /// </summary>
    /// <param name="isSelected"></param>
    public void SetSelect(bool isSelected)
    {
        if(isSelected)
        {
            mRenderer.material.SetColor("_Albedo", stateColors[(int)state].SelectColor);
        }
        else
        {
            mRenderer.material.SetColor("_Albedo", stateColors[(int)state].UnSelectColor);
        }

        this.isSelected = isSelected;
    }

    public NodeState GetState()
    {
        return state;
    }


    [System.Serializable]
    private class ColorCombo
    {
        [SerializeField] public Color SelectColor;
        [SerializeField] public Color UnSelectColor;
    }
}
