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

    [Header("Oct Tree Generation")]
    [SerializeField] float startOctSize;
    [SerializeField] float minOctSize;
    [SerializeField] LayerMask testingLayer;

    private List<OctNode> idealVolume;

    [Header("Spawning")]
    [SerializeField] bool regenerate;
    [SerializeField] GameObject templateObj;
    [SerializeField] List<GameObject> heldObjects;

    [Header("Edits")]
    [SerializeField] bool activateEditTool;
    [Space]
    [SerializeField] bool createCut;
    [SerializeField] LayerMask editLayer;
    [SerializeField] Vector2 intersectSize;
    [SerializeField] Vector3 intersectPos;
    [Space]
    [SerializeField] Material selectedMat;
    [SerializeField] Material ditherMat;

    public List<GameObject> currentSlice;

    [Header("Gizmos")]
    [SerializeField] DisplayStates displayState;

    private enum DisplayStates
    { 
        All,
        Leafs,
        IdealVolume,
        Nothing
    }


    private OctNode root;

    /// <summary>
    /// Generates a volume based on collision within this tool's range 
    /// </summary>
    public void RegenerateGrid()
    {
        // Reset current slice volums 
        foreach (GameObject current in currentSlice)
        {
            current.GetComponent<Renderer>().material = ditherMat;
        }
        currentSlice.Clear();

        GeneratOctTree();
        GenerateTrashObjs(idealVolume);
    }

    private void Awake()
    {
        idealVolume = new List<OctNode>();
        currentSlice = new List<GameObject>();
    }

    void Start()
    {
        RegenerateGrid();
    }

    private void Update()
    {
        if(regenerate)
        {
            RegenerateGrid();
            regenerate = false;
        }

        EditTool();
    }

    /// <summary>
    /// This allows you to make 3D slices of the mesh 
    /// to then select and edit individual volumes 
    /// </summary>
    private void EditTool()
    {
        if (!activateEditTool)
            return;

        if (createCut)
        {
            createCut = false;

            foreach (GameObject current in currentSlice)
            {
                current.GetComponent<Renderer>().material = ditherMat;
            }
            currentSlice.Clear();

            Collider[] nodes = Physics.OverlapBox(intersectPos, intersectSize, Quaternion.identity, editLayer);
            foreach (Collider node in nodes)
            {
                node.GetComponent<Renderer>().material = selectedMat;
                currentSlice.Add(node.gameObject);
            }
        }
    }

    private void GenerateTrashObjs(List<OctNode> nodes)
    {
        // Reset list 
        if(heldObjects.Count > 0)
        {
            for (int i = 0; i < heldObjects.Count; i++)
            {
                Destroy(heldObjects[i]);
            }
            heldObjects.Clear();
        }

        foreach (OctNode node in nodes)
        {
            GameObject current = Instantiate(templateObj, node.Position, Quaternion.identity);
            //current.transform.parent = this.transform;

            heldObjects.Add(current);
        }
    }

    private void GeneratOctTree()
    {
        idealVolume.Clear();

        // What layer to stop at 
        int targetIndex = (int)Mathf.Log(startOctSize / minOctSize, 2.0f);

        root = new OctNode(this.transform.position, startOctSize, 0, targetIndex, testingLayer, idealVolume);
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

        public OctNode(Vector3 position, float size, int index, int stoppingIndex, LayerMask layer, List<OctNode> outputList)
        {
            this.position = position;
            this.size = size;
            this.index = index;
            this.stoppingIndex = stoppingIndex;

            if (index != stoppingIndex)
            {
                TrySubdivide(layer, outputList);
            }
            else
            {
                // One last check 
                containsCollision = Physics.CheckBox(position, Vector3.one * size / 2.0f, Quaternion.identity, layer);
                if(containsCollision)
                    outputList.Add(this);
            }
        }

        public void TrySubdivide(LayerMask layer, List<OctNode> outputList)
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
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, -nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(nextSize / 2.0f, -nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex, 
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, -nextSize / 2.0f, nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
                children.Add(new OctNode(
                    position + new Vector3(-nextSize / 2.0f, -nextSize / 2.0f, -nextSize / 2.0f),
                    nextSize,
                    nextIndex, stoppingIndex,
                    layer,
                    outputList));
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
        if(activateEditTool)
        {
            Gizmos.DrawWireCube(intersectPos, intersectSize);
        }


        if (root == null)
            return;

        if(displayState != DisplayStates.Nothing) 
        { 
            root.GizmosRecursiveDisplay(displayState);
        }
    }
}
