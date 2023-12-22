using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using TMPro;

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
    [SerializeField] GameObject objBuildAround;

    // The final leaf nodes that highlight the mesh 
    private List<OctNode> idealVolume;

    [Header("Spawning")]
    [SerializeField] GameObject templateObj;
    [SerializeField] List<Node> heldObjects;

    [Header("Selection")]
    [SerializeField] Vector2 intersectSize;
    [SerializeField] Vector3 intersectPos;
    [SerializeField] Transform slicer;
    [SerializeField] float sliceMoveSpeed;
    [Space]
    [SerializeField] Color selectColor;
    [SerializeField] Color unSelectColor;
    [SerializeField] int selectLayer;
    [SerializeField] int unSelectLayer;
    [Space]
    [SerializeField] LayerMask editLayer;
    [SerializeField] TMP_Dropdown volumeDisDrop;
    [SerializeField] VolumeDisplayState volumeDisplay;

    public List<Node> currentSlice;

    private VolumeDisplayState holdVolumeDisplay;

    private enum VolumeDisplayState
    {
        Solid, 
        Dither, 
        Selected
    }


    [Header("Gizmos")]
    [SerializeField] GizmosDisplayStates displayState;

    private enum GizmosDisplayStates
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
        foreach (Node node in heldObjects)
        {
            Destroy(node.gameObject);
        }
        heldObjects.Clear();
        currentSlice.Clear();

        GeneratOctTree();
        GenerateTrashObjs(idealVolume);
        VolumeDisplaySM();
    }

    /// <summary>
    /// Selects a slice through of the nodes 
    /// </summary>
    public void SelectSlice()
    {
        // Reset all previously selected nodes 
        foreach (Node current in currentSlice)
        {
            current.IsSelected = false;

            // Set old slice to default visualization dictated by volume display 
            switch (volumeDisplay)
            {
                case VolumeDisplayState.Selected:
                    // Make sure not enabled 
                    current.GetComponent<Renderer>().material.SetFloat("_isEnabled", 0);
                    break;
                case VolumeDisplayState.Dither:
                    current.GetComponent<Renderer>().material.SetFloat("_isDither", 1);
                    break;
                case VolumeDisplayState.Solid:
                default:
                    break;
            }
            current.GetComponent<Renderer>().material.SetColor("_Albedo", unSelectColor);
            current.gameObject.layer = unSelectLayer;
        }

        currentSlice.Clear();

        // Get all new selected nodes 
        Collider[] nodes = Physics.OverlapBox(intersectPos, intersectSize / 2.0f, Quaternion.identity, editLayer);
        foreach (Collider node in nodes)
        {

            Node current = node.GetComponent<Node>();
            current.IsSelected = true;
            currentSlice.Add(current);

            // Visualize 
            switch (volumeDisplay)
            {
                case VolumeDisplayState.Selected:
                    current.GetComponent<Renderer>().material.SetFloat("_isEnabled", 1);
                    node.GetComponent<Renderer>().material.SetFloat("_isDither", 0);
                    break;
                case VolumeDisplayState.Dither:
                    node.GetComponent<Renderer>().material.SetFloat("_isDither", 0);
                    break;
                case VolumeDisplayState.Solid:
                default:
                    break;
            }
            node.GetComponent<Renderer>().material.SetColor("_Albedo", selectColor);
            current.gameObject.layer = selectLayer;
        }
    }

    private void Awake()
    {
        idealVolume = new List<OctNode>();
        currentSlice = new List<Node>();

        holdVolumeDisplay = volumeDisplay;
    }

    void Start()
    {
        RegenerateGrid();
    }

    private void Update()
    {
        VolumeDisplay();
        SelectorControls();

        if (Input.GetKeyDown(KeyCode.H))
        {
            objBuildAround.SetActive(!objBuildAround.activeInHierarchy);
        }

        
    }

    /// <summary>
    /// Allows us to move the selctor in 3D space 
    /// </summary>
    private void SelectorControls()
    {
        slicer.localScale = (Vector3)intersectSize - Vector3.forward;
        slicer.position = intersectPos;

        if (Input.GetKey(KeyCode.W))
        {
            intersectPos += Vector3.up * sliceMoveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            intersectPos -= Vector3.up * sliceMoveSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            intersectPos += Vector3.right * sliceMoveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            intersectPos -= Vector3.right * sliceMoveSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.E))
        {
            intersectPos += Vector3.forward * sliceMoveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            intersectPos -= Vector3.forward * sliceMoveSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// How do we want to display the trash objects 
    /// </summary>
    private void VolumeDisplay()
    {
        volumeDisplay = (VolumeDisplayState)volumeDisDrop.value;


        if (holdVolumeDisplay == volumeDisplay)
            return;

        VolumeDisplaySM();

        holdVolumeDisplay = volumeDisplay;
    }

    private void VolumeDisplaySM()
    {
        // Cleaup if necessary 
        switch (holdVolumeDisplay)
        {
            case VolumeDisplayState.Solid:
                break;
            case VolumeDisplayState.Dither:
                break;
            case VolumeDisplayState.Selected:
                CleaupSelected(heldObjects);
                break;
        }

        // How do we want to update all objects 
        switch (volumeDisplay)
        {
            case VolumeDisplayState.Solid:
                VolumeSolid();
                break;
            case VolumeDisplayState.Dither:
                VolumeDither();
                break;
            case VolumeDisplayState.Selected:
                VolumeSelected(heldObjects);
                break;
        }
    }

    /// <summary>
    /// Change all meshes to solid material 
    /// </summary>
    private void VolumeSolid()
    {
        foreach (Node node in heldObjects)
        {
            node.GetComponent<Renderer>().material.SetFloat("_isDither", 0);
        }
    }

    /// <summary>
    /// Change non-selected meshes to a dither 
    /// </summary>
    private void VolumeDither()
    {
        foreach (Node node in heldObjects)
        {
            node.GetComponent<Renderer>().material.SetFloat("_isDither", node.IsSelected ? 0 : 1);
        }
    }

    /// <summary>
    /// Only display objects selected by slice tool 
    /// </summary>
    private void VolumeSelected(List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            if (node.IsSelected)
                continue;

            node.GetComponent<Renderer>().material.SetFloat("_isEnabled", 0);
        }
    }

    /// <summary>
    /// Enables all meshes given 
    /// </summary>
    private void CleaupSelected(List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            node.GetComponent<Renderer>().material.SetFloat("_isEnabled", 1);
        }
    }

    /// <summary>
    /// Generate a objects to occupy a node 
    /// </summary>
    /// <param name="nodes"></param>
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
            Node current = Instantiate(templateObj, node.Position, Quaternion.identity).GetComponent<Node>();
            current.transform.parent = this.transform;

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
        public void GizmosRecursiveDisplay(GizmosDisplayStates displayState)
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
        public void GizmosDisplayNode(GizmosDisplayStates displayState)
        {

            switch (displayState)
            {
                case GizmosDisplayStates.All:
                    Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
                case GizmosDisplayStates.Leafs:
                    if(index == stoppingIndex)
                        Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
                case GizmosDisplayStates.IdealVolume:
                    if (index == stoppingIndex && containsCollision == true)
                        Gizmos.DrawWireCube(position, Vector3.one * size);
                    break;
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(intersectPos, intersectSize);

        Gizmos.matrix = transform.localToWorldMatrix;

        if (root == null)
            return;

        if(displayState != GizmosDisplayStates.Nothing) 
        { 
            root.GizmosRecursiveDisplay(displayState);
        }
    }
}
