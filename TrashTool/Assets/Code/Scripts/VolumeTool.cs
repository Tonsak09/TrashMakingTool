using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class VolumeTool : MonoBehaviour
{
    /*
     * As a note the starting size of the oct tree must be, or at least
     * should be a multiple of the min size leaves that we desire. This
     * will be calculated in the intial stages when iterate through the 
     * verticies 
     */

    [SerializeField] float startOctSize;
    [SerializeField] float minOctSize;
    [SerializeField] LayerMask testingLayer;

    [Header("Gizmos")]
    [SerializeField] DisplayStates displayState;
    [SerializeField] bool regenerate;

    private enum DisplayStates
    { 
        All,
        Leafs,
        IdealVolume
    }


    private OctNode root;

    void Start()
    {
        GeneratOctTree();
    }

    private void Update()
    {
        if(regenerate)
        {
            GeneratOctTree();
            regenerate = false;
        }
    }

    private void GeneratOctTree()
    {
        // What layer to stop at 
        int targetIndex = (int)Mathf.Log(startOctSize / minOctSize, 2.0f);

        root = new OctNode(this.transform.position, startOctSize, 0, targetIndex, testingLayer);
        //root.TrySubdivide();

    }
    
    /// <summary>
    /// This octnode is used specifically to indicate is a space is occupied 
    /// by geometry. They do not hold any objects in memory and simply help
    /// divide the geometry space into volumetric blocks
    /// </summary>
    private class OctNode
    {
        private Vector3 position;
        private float size;

        private int index;
        private bool hasChildren = false;
        private bool containsCollision = false;
        private List<OctNode> children;

        private int stoppingIndex;

        public Vector3 Position { get { return position; } }
        public float Size { get { return size; } }

        public OctNode(Vector3 position, float size, int index, int stoppingIndex, LayerMask layer)
        {
            this.position = position;
            this.size = size;
            this.index = index;
            this.stoppingIndex = stoppingIndex;

            if (index != stoppingIndex)
            {
                TrySubdivide(layer);
            }
            else
            {
                // One last check 
                containsCollision = Physics.CheckBox(position, Vector3.one * size / 2.0f, Quaternion.identity, layer);
                print(position);
            }
        }

        public void TrySubdivide(LayerMask layer)
        {
            if(Physics.CheckBox(position, Vector3.one * size / 2.0f, Quaternion.identity, layer))
            {
                hasChildren = true;
                containsCollision = true;
                children = new List<OctNode>();

                // Get the center of each child node position 
                float nextSize = size / 2.0f;
                int nextIndex = index + 1;
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, nextSize / 2.0f, nextSize / 2.0f), 
                    nextSize, 
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, -nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, -nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex, 
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, -nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, -nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer));
            }
        }

        /// <summary>
        /// Using gizmos, draw a wireframe representing this node and then
        /// call draws recussively on all children 
        /// </summary>
        public void GizmosRecursiveDisplay(DisplayStates displayState)
        {
            GizmosDisplayNode(displayState);

            if (!hasChildren)
                return;

            // Display children 
            foreach (OctNode node in children)
            {
                node.GizmosRecursiveDisplay(displayState);
            }
        }

        /// <summary>
        /// Draw a wireframe representing this partitioning in space 
        /// </summary>
        public void GizmosDisplayNode(DisplayStates displayState)
        {

            switch (displayState)
            {
                case DisplayStates.All:
                    Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
                case DisplayStates.Leafs:
                    if(index == stoppingIndex)
                        Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
                case DisplayStates.IdealVolume:
                    if (index == stoppingIndex && containsCollision == true)
                        Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
            }
        }

    }

    private void OnDrawGizmos()
    {
        if (root == null)
            return;

        root.GizmosRecursiveDisplay(displayState);
    }
}
