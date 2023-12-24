using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeModifier : MonoBehaviour
{
    [Header("Edits")]
    [SerializeField] Camera camera;
    [SerializeField] LayerMask nodeRayCastLayer;
    [SerializeField] float timeForClick = 0.2f;

    private bool inClick;
    private float timer;


    private void ClickLogic()
    {
        RaycastHit hit;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 100, nodeRayCastLayer))
        {
            Transform objectHit = hit.transform;
            //hit.collider.GetComponent<Renderer>().material.SetColor("_Albedo", Color.red);
            hit.collider.GetComponent<Node>().SetNextState();
        }
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            inClick= true;

        if (!inClick)
            return;

        timer += Time.deltaTime;

        if(timer > timeForClick)
        {
            inClick = false;
            timer = 0;
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ClickLogic();
        }
    }

    
}
