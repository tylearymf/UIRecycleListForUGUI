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
        for (int i = 0; i < 5; i++)
        {
            NGUITools.AddChild(test.gameObject, prefab);
        }

        test.onUpdateItem = OnUpdateItem;
        test.UpdateCount(13);
    }

    public void OnUpdateItem(GameObject pGameObject, int pItemIndex, int pDataIndex)
    {
        pGameObject.GetComponentInChildren<Text>().text = pDataIndex.ToString();
    }
}
