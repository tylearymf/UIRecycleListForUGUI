using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public UIRecycleListForUGUI test;
    public GameObject prefab;

    void Start()
    {
        for (int i = 0, imax = test.CalculateInstantiateCount(); i < imax; i++)
        {
            NGUITools.AddChild(test.gameObject, prefab);
        }

        test.OnUpdateItemEvent = OnUpdateItem;
        test.UpdateCount(101);
    }

    public void OnUpdateItem(GameObject pGameObject, int pDataIndex)
    {
        pGameObject.GetComponentInChildren<Text>().text = pDataIndex.ToString();
        pGameObject.name = pDataIndex.ToString();
    }
}
