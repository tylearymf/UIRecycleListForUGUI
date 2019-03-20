using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public UIRecycleListForUGUI test;
    public GameObject prefab;
    public int Count;

    void Start()
    {
        for (int i = 0, imax = test.CalculateInstantiateCount(); i < imax; i++)
        {
            var tGo = Instantiate(prefab);
            var tTr = tGo.transform;
            tTr.parent = test.transform;
            tTr.localPosition = Vector3.zero;
            tTr.localRotation = Quaternion.identity;
            tTr.localScale = Vector3.one;
        }

        test.OnUpdateItemEvent = OnUpdateItem;
        test.UpdateCount(101);
    }

    public void OnUpdateItem(GameObject pGameObject, int pDataIndex)
    {
        pGameObject.GetComponentInChildren<Text>().text = pDataIndex.ToString();
        pGameObject.name = pDataIndex.ToString();
    }

    [ContextMenu("UpdateCount,false")]
    public void Test1()
    {
        test.UpdateCount(Count);
    }

    [ContextMenu("UpdateCount,true")]
    public void Test2()
    {
        test.UpdateCount(Count, true);
    }
}
