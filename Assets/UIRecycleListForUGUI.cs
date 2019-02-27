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
    public enum LayoutType
    {
        /// <summary>
        /// 单列
        /// </summary>
        SingleColumn,
        /// <summary>
        /// 多列
        /// </summary>
        MultiColumn,
    }

    public delegate void OnUpdateItem(GameObject pGameObject, int pItemIndex, int pDataIndex);

    /// <summary>
    /// 竖向滑动时是列数/横向滑动时是行数
    /// </summary>
    [Tooltip("竖向滑动时是列数/横向滑动时是行数")]
    [HideInInspector]
    [Range(1, 20)]
    public int columnOrRowCount = 1;

    /// <summary>
    /// Item的宽度
    /// </summary>
    [Tooltip("Item的宽度（包含间隔）")]
    [HideInInspector]
    public int itemWidth = 100;

    /// <summary>
    /// Item的高度
    /// </summary>
    [Tooltip("Item的高度（包含间隔）")]
    [HideInInspector]
    public int itemHeight = 100;

    /// <summary>
    /// 是否隐藏未处于可视区域的Item
    /// </summary>
    [Tooltip("是否隐藏未处于可视区域的Item")]
    [HideInInspector]
    public bool cullContent = true;

    /// <summary>
    /// 是否忽略隐藏的Item
    /// </summary>
    [Tooltip("是否忽略隐藏的Item")]
    [HideInInspector]
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

    /// <summary>
    /// 布局模式
    /// </summary>
    LayoutType layoutType
    {
        get
        {
            return columnOrRowCount > 1 ? LayoutType.MultiColumn : LayoutType.SingleColumn;
        }
    }

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

    [ContextMenu("Sort Based on Scroll Movement")]
    public virtual void SortBasedOnScrollMovement()
    {
        if (!CacheScrollView()) return;

        mChildren.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (hideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildren.Add(t);
        }

        if (mHorizontal) mChildren.Sort(SortHorizontal);
        else mChildren.Sort(SortVertical);
        ResetChildPositions();
    }

    [ContextMenu("Sort Alphabetically")]
    public virtual void SortAlphabetically()
    {
        if (!CacheScrollView()) return;

        mChildren.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (hideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildren.Add(t);
        }

        mChildren.Sort(SortByName);
        ResetChildPositions();
    }

    protected bool CacheScrollView()
    {
        if (mTrans && mScroll) return true;
        mTrans = transform;
        mScroll = gameObject == null ? null : gameObject.GetComponentInParent<ScrollRect>();
        if (mScroll == null) return false;
        if (mScroll.horizontal) mHorizontal = true;
        else if (mScroll.vertical) mHorizontal = false;
        else return false;
        return true;
    }

    protected virtual void ResetChildPositions()
    {
        for (int i = 0, imax = mChildren.Count; i < imax; ++i)
        {
            Transform t = mChildren[i];
            switch (layoutType)
            {
                case LayoutType.SingleColumn:
                    t.localPosition = mHorizontal ? new Vector3(i * itemWidth, 0f, 0f) : new Vector3(0f, -i * itemHeight, 0f);
                    break;
                case LayoutType.MultiColumn:
                    t.localPosition = mHorizontal ? new Vector3((i / columnOrRowCount) * itemWidth, -(i % columnOrRowCount) * itemHeight, 0) : new Vector3((i % columnOrRowCount) * itemWidth, -(i / columnOrRowCount) * itemHeight, 0f); ; ;
                    break;
                default:
                    throw new NotImplementedException("未实现");
            }
            UpdateItem(t, i);
        }
    }

    protected virtual void WrapContent()
    {
        var tItemSize = mHorizontal ? itemWidth : itemHeight;
        mScroll.viewport.GetWorldCorners(mCorners);
        for (int i = 0; i < 4; ++i)
        {
            var v = mCorners[i];
            v = mScroll.viewport.InverseTransformPoint(v);
            mCorners[i] = v;
        }
        var tCenter = Vector3.Lerp(mCorners[0], mCorners[2], 0.5f);

        if (layoutType == LayoutType.MultiColumn)
        {
            var tExtents = tItemSize * Mathf.CeilToInt(mChildren.Count / (float)columnOrRowCount) * 0.5F;
            var tExt2 = tExtents * 2f;

            if (mHorizontal)
            {
                var min = mCorners[0].x - tItemSize;
                var max = mCorners[2].x + tItemSize;

                for (int i = 0, imax = mChildren.Count; i < imax; ++i)
                {
                    Transform t = mChildren[i];
                    float distance = t.localPosition.x - tCenter.x + mScroll.content.localPosition.x;

                    if (distance < -tExtents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.x += tExt2;
                        distance = pos.x - tCenter.x;
                        var tDataIndex = Mathf.RoundToInt(-pos.y / itemHeight) + Mathf.RoundToInt(pos.x / itemWidth) * columnOrRowCount;

                        if (minIndex == maxIndex || (minIndex <= tDataIndex && tDataIndex < maxIndex))
                        {
                            t.localPosition = pos;
                            UpdateItem(t, i);
                        }
                    }
                    else if (distance > tExtents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.x -= tExt2;
                        distance = pos.x - tCenter.x;
                        var tDataIndex = Mathf.RoundToInt(-pos.y / itemHeight) + Mathf.RoundToInt(pos.x / itemWidth) * columnOrRowCount;

                        if (minIndex == maxIndex || (minIndex <= tDataIndex && tDataIndex < maxIndex))
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
                var min = mCorners[0].y - tItemSize;
                var max = mCorners[2].y + tItemSize;

                for (int i = 0, imax = mChildren.Count; i < imax; ++i)
                {
                    Transform t = mChildren[i];
                    float distance = t.localPosition.y - tCenter.y + mScroll.content.localPosition.y;

                    if (distance < -tExtents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.y += tExt2;
                        distance = pos.y - tCenter.y;
                        var tDataIndex = Mathf.RoundToInt(pos.x / itemWidth) + Mathf.RoundToInt(-pos.y / itemHeight) * columnOrRowCount;

                        if (minIndex == maxIndex || (minIndex <= tDataIndex && tDataIndex < maxIndex))
                        {
                            t.localPosition = pos;
                            UpdateItem(t, i);
                        }
                    }
                    else if (distance > tExtents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.y -= tExt2;
                        distance = pos.y - tCenter.y;
                        var tDataIndex = Mathf.RoundToInt(pos.x / itemWidth) + Mathf.RoundToInt(-pos.y / itemHeight) * columnOrRowCount;

                        if (minIndex == maxIndex || (minIndex <= tDataIndex && tDataIndex < maxIndex))
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
        else if (layoutType == LayoutType.SingleColumn)
        {
            float extents = tItemSize * mChildren.Count * 0.5f;
            float ext2 = extents * 2f;

            if (mHorizontal)
            {
                float min = mCorners[0].x - tItemSize;
                float max = mCorners[2].x + tItemSize;

                for (int i = 0, imax = mChildren.Count; i < imax; ++i)
                {
                    Transform t = mChildren[i];
                    float distance = t.localPosition.x - tCenter.x + mScroll.content.localPosition.x;

                    if (distance < -extents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.x += ext2;
                        distance = pos.x - tCenter.x;
                        int realIndex = Mathf.RoundToInt(pos.x / tItemSize);

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
                        distance = pos.x - tCenter.x;
                        int realIndex = Mathf.RoundToInt(pos.x / tItemSize);

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
                float min = mCorners[0].y - tItemSize;
                float max = mCorners[2].y + tItemSize;

                for (int i = 0, imax = mChildren.Count; i < imax; ++i)
                {
                    Transform t = mChildren[i];
                    float distance = t.localPosition.y - tCenter.y + mScroll.content.localPosition.y;

                    if (distance < -extents)
                    {
                        Vector3 pos = t.localPosition;
                        pos.y += ext2;
                        distance = pos.y - tCenter.y;
                        int realIndex = Mathf.RoundToInt(pos.y / -tItemSize);

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
                        distance = pos.y - tCenter.y;
                        int realIndex = Mathf.RoundToInt(pos.y / -tItemSize);

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
    }

    void OnValidate()
    {
        if (maxIndex < minIndex)
            maxIndex = minIndex;
        if (minIndex > maxIndex)
            maxIndex = minIndex;
    }

    protected virtual void UpdateItem(Transform pTrans, int pItemIndex)
    {
        if (onUpdateItem == null) return;

        int tDataIndex = 0;
        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                tDataIndex = mHorizontal ? Mathf.RoundToInt(pTrans.localPosition.x / itemWidth) : Mathf.RoundToInt(pTrans.localPosition.y / -itemHeight);
                break;
            case LayoutType.MultiColumn:
                if (mHorizontal)
                {
                    tDataIndex = Mathf.RoundToInt(-pTrans.localPosition.y / itemHeight) + Mathf.RoundToInt(pTrans.localPosition.x / itemWidth) * columnOrRowCount;
                }
                else
                {
                    tDataIndex = Mathf.RoundToInt(pTrans.localPosition.x / itemWidth) + Mathf.RoundToInt(-pTrans.localPosition.y / itemHeight) * columnOrRowCount;
                }
                break;
            default:
                throw new NotImplementedException("未实现");
        }
        onUpdateItem(pTrans.gameObject, pItemIndex, tDataIndex);
    }

    /// <summary>
    /// 计算要实例化的Item数量
    /// </summary>
    /// <returns></returns>
    public int CaclculateInstantiateCount()
    {
        if (!CacheScrollView())
        {
            throw new NotImplementedException("获取ScrollRect组件失败");
        }

        var tSize = mScroll.GetComponent<RectTransform>().sizeDelta;
        var tCount = 0;

        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                if (mHorizontal)
                {
                    tCount = Mathf.CeilToInt(tSize.x / itemWidth);
                }
                else
                {
                    tCount = Mathf.CeilToInt(tSize.y / itemHeight);
                }
                //多生成两个
                return tCount + 2;
            case LayoutType.MultiColumn:
                if (mHorizontal)
                {
                    //多生成两列
                    tCount = (Mathf.CeilToInt(tSize.x / itemWidth) + 2) * columnOrRowCount;
                }
                else
                {
                    //多生成两行
                    tCount = (Mathf.CeilToInt(tSize.y / itemHeight) + 2) * columnOrRowCount;
                }
                break;
            default:
                throw new NotImplementedException("未实现");
        }

        return tCount;
    }

    /// <summary>
    /// 刷新页面
    /// </summary>
    /// <param name="pItemCount">传入总数量</param>
    /// <param name="pResetPos">是否重置ScrollRect的位置</param>
    public void UpdateCount(int pItemCount, bool pResetPos = false)
    {
        minIndex = 0;
        maxIndex = pItemCount;
        SortBasedOnScrollMovement();
        WrapContent();

        if (pResetPos && mScroll)
        {
            if (mHorizontal) mScroll.horizontalNormalizedPosition = 0;
            else mScroll.verticalNormalizedPosition = 0;
        }

        var tContent = mScroll.content.GetComponent<RectTransform>();

        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                {
                    if (mHorizontal)
                    {
                        tContent.sizeDelta = new Vector2(itemWidth * maxIndex, tContent.sizeDelta.y);
                    }
                    else
                    {
                        tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, itemHeight * maxIndex);
                    }
                }
                break;
            case LayoutType.MultiColumn:
                {
                    if (mHorizontal)
                    {
                        tContent.sizeDelta = new Vector2(itemWidth * Mathf.CeilToInt((float)maxIndex / columnOrRowCount), tContent.sizeDelta.y);
                    }
                    else
                    {
                        tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, itemHeight * Mathf.CeilToInt((float)maxIndex / columnOrRowCount));
                    }
                }
                break;
            default:
                throw new NotImplementedException("未实现");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (mScroll != null) mScroll.onValueChanged.RemoveListener(OnMove);
        onUpdateItem = null;
    }

    static public int SortByName(Transform a, Transform b) { return string.Compare(a.name, b.name); }
    static public int SortHorizontal(Transform a, Transform b) { return a.localPosition.x.CompareTo(b.localPosition.x); }
    static public int SortVertical(Transform a, Transform b) { return b.localPosition.y.CompareTo(a.localPosition.y); }
}


#region 编辑器相关
#if UNITY_EDITOR
[CustomEditor(typeof(UIRecycleListForUGUI))]
class UIRecycleListForUGUIEditor : Editor
{
    SerializedProperty mItemWidthSp;
    SerializedProperty mItemHeightSp;
    SerializedProperty mColumnOrRowCountSp;
    SerializedProperty mCullContentSp;
    SerializedProperty mHideInactiveSp;
    ScrollRect mScrollRect;
    RectTransform mTargetTransfrom;

    void OnEnable()
    {
        mItemWidthSp = serializedObject.FindProperty("itemWidth");
        mItemHeightSp = serializedObject.FindProperty("itemHeight");
        mColumnOrRowCountSp = serializedObject.FindProperty("columnOrRowCount");
        mCullContentSp = serializedObject.FindProperty("cullContent");
        mHideInactiveSp = serializedObject.FindProperty("hideInactive");

        var tRecycleList = target as UIRecycleListForUGUI;
        mScrollRect = tRecycleList.transform.parent.parent.parent.GetComponent<ScrollRect>();
        mTargetTransfrom = tRecycleList.GetComponent<RectTransform>() ?? tRecycleList.gameObject.AddComponent<RectTransform>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("1、点击“初始化配置”\n2、调整好ItemContainer的Pos", MessageType.Info);

        EditorGUILayout.PropertyField(mColumnOrRowCountSp, new GUIContent(mScrollRect.horizontal ? "Row Count" : "Column Count"));
        EditorGUILayout.PropertyField(mItemWidthSp, new GUIContent("Item Width"));
        EditorGUILayout.PropertyField(mItemHeightSp, new GUIContent("Item Height"));
        EditorGUILayout.PropertyField(mCullContentSp, new GUIContent("Cull Content"));
        EditorGUILayout.PropertyField(mHideInactiveSp, new GUIContent("Hide Inactive"));

        if (GUILayout.Button("初始化配置"))
        {
            var tScrollRectSize = mScrollRect.GetComponent<RectTransform>().sizeDelta;

            mTargetTransfrom.name = "ItemContainer";
            mTargetTransfrom.anchorMin = new Vector2(0, 1);
            mTargetTransfrom.anchorMax = new Vector2(0, 1);
            mTargetTransfrom.pivot = new Vector2(0, 1);
            mTargetTransfrom.rotation = Quaternion.identity;
            mTargetTransfrom.localScale = Vector3.one;
            mTargetTransfrom.sizeDelta = tScrollRectSize;


            var tContent = mTargetTransfrom.parent.GetComponent<RectTransform>();
            tContent.anchorMin = new Vector2(0, 1);
            tContent.anchorMax = new Vector2(0, 1);
            tContent.pivot = new Vector2(0, 1);
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

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endregion