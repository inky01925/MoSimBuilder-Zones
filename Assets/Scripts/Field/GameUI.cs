using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class GameUI : MonoBehaviour
{
    [SerializeField] protected GameObject gameUI;
    protected FMS fms;
    protected GameObject UI;
    protected GameObject spawnedUI;
    
    // Start is called before the first frame update
    void Start()
    {
        UI = GameObject.Find("GameUi");
        fms = GameObject.Find("FieldHolder").GetComponentInChildren<FMS>();
        spawnedUI = Instantiate(gameUI, UI.transform.GetChild(0));
        Init();
    }

    private void Awake()
    {
        UI = GameObject.Find("GameUi");
        fms = GameObject.Find("FieldHolder").GetComponentInChildren<FMS>();
        spawnedUI = Instantiate(gameUI, UI.transform.GetChild(0));
        Init();
        
    }

    protected virtual void Init()
    {
        
    }
}
