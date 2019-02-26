using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBehaviourScript : MonoBehaviour
{
    public UIRecycleListForUGUI test;
    public GameObject prefab;

    private void Start()
    {
        for (int i = 0; i < 7; i++)
        {
            NGUITools.AddChild(test.gameObject, prefab);
        }

        test.onUpdateItem = OnUpdateItem;
        test.UpdateCount(50);
    }

    private void OnUpdateItem(GameObject go, int wrapIndex, int realIndex)
    {
        go.GetComponentInChildren<Text>().text = realIndex.ToString();
    }
}
