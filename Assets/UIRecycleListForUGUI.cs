using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// + Scroll Rect
/// |- ViewPort
/// |-- Content
/// |--- ItemContainer
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

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex <= maxIndex))
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

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex <= maxIndex))
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

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex <= maxIndex))
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

                    if (minIndex == maxIndex || (minIndex <= realIndex && realIndex <= maxIndex))
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
            tContent.sizeDelta = new Vector2(itemWidgetSize * (maxIndex + 1), tContent.sizeDelta.y);
        }
        else
        {
            tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, itemWidgetSize * (maxIndex + 1));
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
