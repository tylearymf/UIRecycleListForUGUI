using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// + Scroll Rect
/// |- ViewPort
///             RectTransform
///             Anchor Presets:stretch,stretch
///             Left:0 Top:0 PosZ:0 Right:0 Bottom:0
///             Anchors Min:0,0 Max:1,1 Pivot:0.5,0.5
///             Rotation:0,0,0 Scale:1,1,1
/// 
/// |-- Content (有且只有一个组件：RectTransform)
///             RectTransform
///             Anchor Presets:top,center
///             PosX:0 PosY:0 PosZ:0 Width:ScrollRect'Width Height:ScrollRect'Height
///             Anchors Min:0.5,1 Max:0.5,1 Pivot:0.5,1
///             Rotation:0,0,0 Scale:1,1,1
/// 
/// |--- ItemContainer (包含UIRecycleListForUGUI组件)
///             RectTransform
///             Anchor Presets:top,center
///             PosX:Custom PosY:Custom PosZ:0 Width:ScrollRect'Width Height:ScrollRect'Height
///             Anchors Min:0.5,1 Max:0.5,1 Pivot:0.5,1
///             Rotation:0,0,0 Scale:1,1,1
/// 
/// |---- Item 1
/// |---- Item 2
/// |---- Item 3
/// </summary>
public class UIRecycleListForUGUI : MonoBehaviour, IDisposable
{
    public delegate void OnUpdateItem(GameObject pGameObject, int pItemIndex, int pDataIndex);

    [Tooltip("Item的宽或高")]
    /// <summary>
    /// Item的宽或高
    /// </summary>
    public int itemWidgetSize = 100;

    [Tooltip("是否隐藏未处于可视区域的Item")]
    /// <summary>
    /// 是否隐藏未处于可视区域的Item
    /// </summary>
    public bool cullContent = true;

    [Tooltip("是否忽略隐藏的Item")]
    /// <summary>
    /// 是否忽略隐藏的Item
    /// </summary>
    public bool hideInactive = false;

    public OnUpdateItem onUpdateItem;
    protected int minIndex = 0;
    protected int maxIndex = 0;

    protected Transform mTrans;
    protected ScrollRect mScroll;
    protected bool mHorizontal = false;
    protected bool mFirstTime = true;
    protected List<Transform> mChildren = new List<Transform>();
    protected Vector3[] mCorners = new Vector3[4];

    protected virtual void Start()
    {
        CacheScrollView();
        if (mScroll != null) mScroll.onValueChanged.AddListener(OnMove);
        mFirstTime = false;
    }

    protected virtual void OnDestroy()
    {
        Dispose();
    }

    protected virtual void OnMove(Vector2 delta) { WrapContent(); }

    /// <summary>
    /// Immediately reposition all children.
    /// </summary>
    [ContextMenu("Sort Based on Scroll Movement")]
    public virtual void SortBasedOnScrollMovement()
    {
        if (!CacheScrollView()) return;

        // Cache all children and place them in order
        mChildren.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (hideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildren.Add(t);
        }

        // Sort the list of children so that they are in order
        if (mHorizontal) mChildren.Sort(SortHorizontal);
        else mChildren.Sort(SortVertical);
        ResetChildPositions();
    }

    /// <summary>
    /// Immediately reposition all children, sorting them alphabetically.
    /// </summary>
    [ContextMenu("Sort Alphabetically")]
    public virtual void SortAlphabetically()
    {
        if (!CacheScrollView()) return;

        // Cache all children and place them in order
        mChildren.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (hideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildren.Add(t);
        }

        // Sort the list of children so that they are in order
        mChildren.Sort(SortByName);
        ResetChildPositions();
    }

    /// <summary>
    /// Cache the scroll view and return 'false' if the scroll view is not found.
    /// </summary>
    protected bool CacheScrollView()
    {
        mTrans = transform;
        mScroll = gameObject == null ? null : gameObject.GetComponentInParent<ScrollRect>();
        if (mScroll == null) return false;
        if (mScroll.horizontal) mHorizontal = true;
        else if (mScroll.vertical) mHorizontal = false;
        else return false;
        return true;
    }

    /// <summary>
    /// Helper function that resets the position of all the children.
    /// </summary>
    protected virtual void ResetChildPositions()
    {
        for (int i = 0, imax = mChildren.Count; i < imax; ++i)
        {
            Transform t = mChildren[i];
            t.localPosition = mHorizontal ? new Vector3(i * itemWidgetSize, 0f, 0f) : new Vector3(0f, -i * itemWidgetSize, 0f);
            UpdateItem(t, i);
        }
    }

    /// <summary>
    /// Wrap all content, repositioning all children as needed.
    /// </summary>
    public virtual void WrapContent()
    {
        float extents = itemWidgetSize * mChildren.Count * 0.5f;
        mScroll.viewport.GetWorldCorners(mCorners);

        for (int i = 0; i < 4; ++i)
        {
            Vector3 v = mCorners[i];
            v = mScroll.viewport.InverseTransformPoint(v);
            mCorners[i] = v;
        }

        Vector3 center = Vector3.Lerp(mCorners[0], mCorners[2], 0.5f);
        float ext2 = extents * 2f;

        if (mHorizontal)
        {
            float min = mCorners[0].x - itemWidgetSize;
            float max = mCorners[2].x + itemWidgetSize;

            for (int i = 0, imax = mChildren.Count; i < imax; ++i)
            {
                Transform t = mChildren[i];
                float distance = t.localPosition.x - center.x + mScroll.content.localPosition.x;

                if (distance < -extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.x += ext2;
                    distance = pos.x - center.x;
                    int realIndex = Mathf.RoundToInt(pos.x / itemWidgetSize);

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex < maxIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                }
                else if (distance > extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.x -= ext2;
                    distance = pos.x - center.x;
                    int realIndex = Mathf.RoundToInt(pos.x / itemWidgetSize);

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex < maxIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                }
                else if (mFirstTime) UpdateItem(t, i);

                if (cullContent)
                {
                    NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
                }
            }
        }
        else
        {
            float min = mCorners[0].y - itemWidgetSize;
            float max = mCorners[2].y + itemWidgetSize;

            for (int i = 0, imax = mChildren.Count; i < imax; ++i)
            {
                Transform t = mChildren[i];
                float distance = t.localPosition.y - center.y + mScroll.content.localPosition.y;

                if (distance < -extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.y += ext2;
                    distance = pos.y - center.y;
                    int realIndex = Mathf.RoundToInt(pos.y / -itemWidgetSize);

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex < maxIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                }
                else if (distance > extents)
                {
                    Vector3 pos = t.localPosition;
                    pos.y -= ext2;
                    distance = pos.y - center.y;
                    int realIndex = Mathf.RoundToInt(pos.y / -itemWidgetSize);

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex < maxIndex))
                    {
                        t.localPosition = pos;
                        UpdateItem(t, i);
                    }
                }
                else if (mFirstTime) UpdateItem(t, i);

                if (cullContent)
                {
                    NGUITools.SetActive(t.gameObject, (distance > min && distance < max), false);
                }
            }
        }
    }

    /// <summary>
    /// Sanity checks.
    /// </summary>
    void OnValidate()
    {
        if (maxIndex < minIndex)
            maxIndex = minIndex;
        if (minIndex > maxIndex)
            maxIndex = minIndex;
    }

    /// <summary>
    /// Want to update the content of items as they are scrolled? Override this function.
    /// </summary>
    protected virtual void UpdateItem(Transform pTrans, int pItemIndex)
    {
        if (onUpdateItem != null)
        {
            int tDataIndex = (mScroll.vertical) ?
                Mathf.RoundToInt(pTrans.localPosition.y / -itemWidgetSize) :
                Mathf.RoundToInt(pTrans.localPosition.x / itemWidgetSize);
            onUpdateItem(pTrans.gameObject, pItemIndex, tDataIndex);
        }
    }

    internal void UpdateCount(int pItemCount, bool pReset = false)
    {
        minIndex = 0;
        maxIndex = pItemCount;
        SortBasedOnScrollMovement();
        WrapContent();

        if (pReset && mScroll)
        {
            if (mHorizontal) mScroll.horizontalNormalizedPosition = 0;
            else mScroll.verticalNormalizedPosition = 0;
        }

        var tContent = mScroll.content.GetComponent<RectTransform>();

        if (mHorizontal)
        {
            tContent.sizeDelta = new Vector2(itemWidgetSize * maxIndex, tContent.sizeDelta.y);
        }
        else
        {
            tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, itemWidgetSize * maxIndex);
        }
    }

    public void Dispose()
    {
        if (mScroll != null) mScroll.onValueChanged.RemoveListener(OnMove);
        onUpdateItem = null;
    }

    static public int SortByName(Transform a, Transform b) { return string.Compare(a.name, b.name); }
    static public int SortHorizontal(Transform a, Transform b) { return a.localPosition.x.CompareTo(b.localPosition.x); }
    static public int SortVertical(Transform a, Transform b) { return b.localPosition.y.CompareTo(a.localPosition.y); }
}


#if UNITY_EDITOR
[CustomEditor(typeof(UIRecycleListForUGUI))]
class UIRecycleListForUGUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var tRecycleList = target as UIRecycleListForUGUI;

        if (GUILayout.Button("初始化配置"))
        {
            var tScrollRect = tRecycleList.transform.parent.parent.parent.GetComponent<ScrollRect>();
            var tScrollRectSize = tScrollRect.GetComponent<RectTransform>().sizeDelta;

            tRecycleList.transform.name = "ItemContainer";
            var tItemContainer = tRecycleList.GetComponent<RectTransform>() ?? tRecycleList.gameObject.AddComponent<RectTransform>();
            tItemContainer.anchorMin = new Vector2(0.5F, 1);
            tItemContainer.anchorMax = new Vector2(0.5F, 1);
            tItemContainer.pivot = new Vector2(0.5F, 1);
            tItemContainer.rotation = Quaternion.identity;
            tItemContainer.localScale = Vector3.one;
            tItemContainer.localPosition = Vector3.zero;
            tItemContainer.sizeDelta = tScrollRectSize;


            var tContent = tItemContainer.parent.GetComponent<RectTransform>();
            tContent.anchorMin = new Vector2(0.5F, 1);
            tContent.anchorMax = new Vector2(0.5F, 1);
            tContent.pivot = new Vector2(0.5F, 1);
            tContent.rotation = Quaternion.identity;
            tContent.localScale = Vector3.one;
            tContent.localPosition = Vector3.zero;
            tContent.sizeDelta = tScrollRectSize;


            var tViewPort = tContent.parent.GetComponent<RectTransform>();
            tViewPort.anchorMin = Vector2.zero;
            tViewPort.anchorMax = Vector2.one;
            tViewPort.pivot = Vector2.one * 0.5F;
            tViewPort.rotation = Quaternion.identity;
            tViewPort.localScale = Vector3.one;
            tViewPort.localPosition = Vector3.zero;
            tViewPort.sizeDelta = Vector2.zero;
        }
    }
}
#endif