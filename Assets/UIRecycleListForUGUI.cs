using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// 无限循环滚动List（UGUI版）
/// + Scroll Rect
/// |- ViewPort
/// |-- Content (有且只有一个组件：RectTransform)
/// |--- ItemContainer (包含UIRecycleListForUGUI组件)
/// |---- Item 1
/// |---- Item 2
/// |---- Item 3
/// </summary>
/**
 * 使用方法：
 * 
 *  //获取UIRecycleListForUGUI组件
 *  var mRecycleList = xxx.GetComponent<UIRecycleListFortUGUI>();
 *  //获取要实例化的Item数量
 *  for(int i = 0,imax = mRecycleList.CalculateInstantiateCount();i < imax; i++)
 *  {
 *      //实例化候的GameObject要放到RecycleList的下面
 *      var tGo = Instantiate(xxx);
 *      tGo.transform.SetParent(mRecycleList.transform);
 *  }
 *  
 *  //绑定回调
 *  mRecycleList.OnUpdateItemEvent = OnUpdateItem;
    //传入数据长度刷新界面
 *  mRecycleList.UpdateCount(xxx);
 *  
 *  
 *  void OnUpdateItem(GameObject pGameObject, int pDataIndex)
 *  {
 *     //在这里对pGameObject赋值
 *  }
 * 
 */

public class UIRecycleListForUGUI : MonoBehaviour, IDisposable
{
    /// <summary>
    /// 布局模式
    /// </summary>
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

    #region fields & properties
    /// <summary>
    /// Item赋值回调
    /// </summary>
    /// <param name="pGameObject">对应Item的GameObject</param>
    /// <param name="pDataIndex">对应数据的下标</param>
    public delegate void OnUpdateItem(GameObject pGameObject, int pDataIndex);

    /// <summary>
    /// 竖向滑动时是列数/横向滑动时是行数
    /// </summary>
    [Tooltip("竖向滑动时是列数/横向滑动时是行数")]
    [HideInInspector]
    [Range(1, 20)]
    public int ColumnOrRowCount = 1;

    /// <summary>
    /// Item的宽度
    /// </summary>
    [Tooltip("Item的宽度（包含间隔）")]
    [HideInInspector]
    public int ItemWidth = 100;

    /// <summary>
    /// Item的高度
    /// </summary>
    [Tooltip("Item的高度（包含间隔）")]
    [HideInInspector]
    public int ItemHeight = 100;

    /// <summary>
    /// 是否隐藏未处于可视区域的Item
    /// </summary>
    [Tooltip("是否隐藏未处于可视区域的Item")]
    [HideInInspector]
    public bool CullContent = true;

    /// <summary>
    /// 是否忽略隐藏的Item
    /// </summary>
    [Tooltip("是否忽略隐藏的Item")]
    [HideInInspector]
    public bool HideInactive = false;

    public OnUpdateItem OnUpdateItemEvent;

    protected Transform mTrans;
    protected ScrollRect mScroll;
    protected bool mHorizontal;
    protected bool mFirstTime = true;
    protected List<Transform> mChildrens = new List<Transform>();
    protected Vector3[] mCorners = new Vector3[4];
    protected int mMinIndex;
    protected int mMaxIndex;

    /// <summary>
    /// 布局模式
    /// </summary>
    LayoutType layoutType
    {
        get
        {
            return ColumnOrRowCount > 1 ? LayoutType.MultiColumn : LayoutType.SingleColumn;
        }
    }
    #endregion

    #region unity method
    void Start()
    {
        CacheScrollView();
        if (mScroll != null) mScroll.onValueChanged.AddListener(OnMove);
        mFirstTime = false;
    }

    void OnDestroy()
    {
        if (mScroll != null) mScroll.onValueChanged.RemoveListener(OnMove);
        Dispose();
    }

    void OnValidate()
    {
        if (mMaxIndex < mMinIndex)
            mMaxIndex = mMinIndex;
        if (mMinIndex > mMaxIndex)
            mMaxIndex = mMinIndex;
    }
    #endregion

    #region other
    bool CacheScrollView()
    {
        if (mTrans && mScroll) return true;
        mTrans = transform;
        mScroll = mTrans == null ? null : GetComponentInDisableParent<ScrollRect>(mTrans);
        if (mScroll == null) return false;
        if (mScroll.horizontal) mHorizontal = true;
        else if (mScroll.vertical) mHorizontal = false;
        else return false;
        return true;
    }

    void OnMove(Vector2 delta) { WrapContent(); }
    #endregion

    #region sort method
    public void SortBasedOnScrollMovement()
    {
        if (!CacheScrollView()) return;

        mChildrens.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (HideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildrens.Add(t);
        }

        if (mHorizontal) mChildrens.Sort(SortHorizontal);
        else mChildrens.Sort(SortVertical);
        ResetChildPositions();
    }

    public void SortAlphabetically()
    {
        if (!CacheScrollView()) return;

        mChildrens.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (HideInactive && !t.gameObject.activeInHierarchy) continue;
            mChildrens.Add(t);
        }

        mChildrens.Sort(SortByName);
        ResetChildPositions();
    }
    #endregion

    #region main logic
    /// <summary>
    /// 重置子物体位置
    /// </summary>
    void ResetChildPositions()
    {
        for (int i = 0, imax = mChildrens.Count; i < imax; ++i)
        {
            var tTrans = mChildrens[i];
            switch (layoutType)
            {
                case LayoutType.SingleColumn:
                    tTrans.localPosition = mHorizontal ? new Vector3(i * ItemWidth, 0f, 0f) : new Vector3(0f, -i * ItemHeight, 0f);
                    break;
                case LayoutType.MultiColumn:
                    tTrans.localPosition = mHorizontal ? new Vector3((i / ColumnOrRowCount) * ItemWidth, -(i % ColumnOrRowCount) * ItemHeight, 0) : new Vector3((i % ColumnOrRowCount) * ItemWidth, -(i / ColumnOrRowCount) * ItemHeight, 0f); ; ;
                    break;
                default:
                    throw new NotImplementedException("未实现");
            }
            UpdateItem(tTrans);
        }
    }

    /// <summary>
    /// 核心方法
    /// </summary>
    void WrapContent()
    {
        var tItemSize = mHorizontal ? ItemWidth : ItemHeight;
        mScroll.viewport.GetWorldCorners(mCorners);
        for (int i = 0; i < 4; ++i)
        {
            var tPoint = mCorners[i];
            tPoint = mScroll.viewport.InverseTransformPoint(tPoint);
            mCorners[i] = tPoint;
        }
        var tCenter = Vector3.Lerp(mCorners[0], mCorners[2], 0.5f);

        var tExtents = layoutType == LayoutType.MultiColumn ? (tItemSize * Mathf.CeilToInt(mChildrens.Count / (float)ColumnOrRowCount) * 0.5F) : (tItemSize * mChildrens.Count * 0.5f);
        var tExt2 = tExtents * 2f;

        if (mHorizontal)
        {
            var tMin = mCorners[0].x - tItemSize;
            var tMax = mCorners[2].x + tItemSize;

            for (int i = 0, imax = mChildrens.Count; i < imax; ++i)
            {
                var tChild = mChildrens[i];
                var tDistance = tChild.localPosition.x - tCenter.x + mScroll.content.localPosition.x;

                if (tDistance < -tExtents)
                {
                    var tPos = tChild.localPosition;
                    tPos.x += tExt2;
                    tDistance = tPos.x - tCenter.x;
                    var tDataIndex = layoutType == LayoutType.SingleColumn ? Mathf.RoundToInt(tPos.x / tItemSize) : Mathf.RoundToInt(-tPos.y / ItemHeight) + Mathf.RoundToInt(tPos.x / ItemWidth) * ColumnOrRowCount;

                    if (mMinIndex == mMaxIndex || (mMinIndex <= tDataIndex && tDataIndex < mMaxIndex))
                    {
                        tChild.localPosition = tPos;
                        UpdateItem(tChild);
                    }
                }
                else if (tDistance > tExtents)
                {
                    var tPos = tChild.localPosition;
                    tPos.x -= tExt2;
                    tDistance = tPos.x - tCenter.x;
                    var tDataIndex = layoutType == LayoutType.SingleColumn ? Mathf.RoundToInt(tPos.x / tItemSize) : Mathf.RoundToInt(-tPos.y / ItemHeight) + Mathf.RoundToInt(tPos.x / ItemWidth) * ColumnOrRowCount;

                    if (mMinIndex == mMaxIndex || (mMinIndex <= tDataIndex && tDataIndex < mMaxIndex))
                    {
                        tChild.localPosition = tPos;
                        UpdateItem(tChild);
                    }
                }
                else if (mFirstTime) UpdateItem(tChild);

                if (CullContent)
                {
                    NGUITools.SetActive(tChild.gameObject, i < mMaxIndex && tDistance > tMin && tDistance < tMax, false);
                }
            }
        }
        else
        {
            var tMin = mCorners[0].y - tItemSize;
            var tMax = mCorners[2].y + tItemSize;

            for (int i = 0, imax = mChildrens.Count; i < imax; ++i)
            {
                var tChild = mChildrens[i];
                var tDistance = tChild.localPosition.y - tCenter.y + mScroll.content.localPosition.y;

                if (tDistance < -tExtents)
                {
                    var tPos = tChild.localPosition;
                    tPos.y += tExt2;
                    tDistance = tPos.y - tCenter.y;
                    var tDataIndex = layoutType == LayoutType.SingleColumn ? Mathf.RoundToInt(tPos.y / -tItemSize) : Mathf.RoundToInt(tPos.x / ItemWidth) + Mathf.RoundToInt(-tPos.y / ItemHeight) * ColumnOrRowCount;

                    if (mMinIndex == mMaxIndex || (mMinIndex <= tDataIndex && tDataIndex < mMaxIndex))
                    {
                        tChild.localPosition = tPos;
                        UpdateItem(tChild);
                    }
                }
                else if (tDistance > tExtents)
                {
                    var tPos = tChild.localPosition;
                    tPos.y -= tExt2;
                    tDistance = tPos.y - tCenter.y;
                    var tDataIndex = layoutType == LayoutType.SingleColumn ? Mathf.RoundToInt(tPos.y / -tItemSize) : Mathf.RoundToInt(tPos.x / ItemWidth) + Mathf.RoundToInt(-tPos.y / ItemHeight) * ColumnOrRowCount;

                    if (mMinIndex == mMaxIndex || (mMinIndex <= tDataIndex && tDataIndex < mMaxIndex))
                    {
                        tChild.localPosition = tPos;
                        UpdateItem(tChild);
                    }
                }
                else if (mFirstTime) UpdateItem(tChild);

                if (CullContent)
                {
                    NGUITools.SetActive(tChild.gameObject, i < mMaxIndex && tDistance > tMin && tDistance < tMax, false);
                }
            }
        }
    }

    /// <summary>
    /// 事件回调
    /// </summary>
    /// <param name="pTrans"></param>
    /// <param name="pItemIndex"></param>
    void UpdateItem(Transform pTrans)
    {
        if (OnUpdateItemEvent == null) return;

        int tDataIndex = 0;
        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                tDataIndex = mHorizontal ? Mathf.RoundToInt(pTrans.localPosition.x / ItemWidth) : Mathf.RoundToInt(pTrans.localPosition.y / -ItemHeight);
                break;
            case LayoutType.MultiColumn:
                if (mHorizontal)
                {
                    tDataIndex = Mathf.RoundToInt(-pTrans.localPosition.y / ItemHeight) + Mathf.RoundToInt(pTrans.localPosition.x / ItemWidth) * ColumnOrRowCount;
                }
                else
                {
                    tDataIndex = Mathf.RoundToInt(pTrans.localPosition.x / ItemWidth) + Mathf.RoundToInt(-pTrans.localPosition.y / ItemHeight) * ColumnOrRowCount;
                }
                break;
            default:
                throw new NotImplementedException("未实现");
        }
        OnUpdateItemEvent(pTrans.gameObject, tDataIndex);
    }

    /// <summary>
    /// 计算要实例化的Item数量
    /// </summary>
    /// <returns></returns>
    public int CalculateInstantiateCount()
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
                    tCount = Mathf.CeilToInt(tSize.x / ItemWidth);
                }
                else
                {
                    tCount = Mathf.CeilToInt(tSize.y / ItemHeight);
                }
                //多生成两个
                return tCount + 2;
            case LayoutType.MultiColumn:
                if (mHorizontal)
                {
                    //多生成两列
                    tCount = (Mathf.CeilToInt(tSize.x / ItemWidth) + 2) * ColumnOrRowCount;
                }
                else
                {
                    //多生成两行
                    tCount = (Mathf.CeilToInt(tSize.y / ItemHeight) + 2) * ColumnOrRowCount;
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
        mMinIndex = 0;
        mMaxIndex = pItemCount;
        SortBasedOnScrollMovement();
        WrapContent();

        var tContent = mScroll.content.GetComponent<RectTransform>();

        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                {
                    if (mHorizontal)
                    {
                        tContent.sizeDelta = new Vector2(ItemWidth * mMaxIndex, tContent.sizeDelta.y);
                    }
                    else
                    {
                        tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, ItemHeight * mMaxIndex);
                    }
                }
                break;
            case LayoutType.MultiColumn:
                {
                    if (mHorizontal)
                    {
                        tContent.sizeDelta = new Vector2(ItemWidth * Mathf.CeilToInt((float)mMaxIndex / ColumnOrRowCount), tContent.sizeDelta.y);
                    }
                    else
                    {
                        tContent.sizeDelta = new Vector2(tContent.sizeDelta.x, ItemHeight * Mathf.CeilToInt((float)mMaxIndex / ColumnOrRowCount));
                    }
                }
                break;
            default:
                throw new NotImplementedException("未实现");
        }

        if (pResetPos && mScroll)
        {
            tContent.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        OnUpdateItemEvent = null;
        mFirstTime = false;
        mChildrens.Clear();
        mCorners = new Vector3[4];
        mMinIndex = 0;
        mMaxIndex = 0;
    }
    #endregion

    #region static method
    static public int SortByName(Transform a, Transform b) { return string.Compare(a.name, b.name); }
    static public int SortHorizontal(Transform a, Transform b) { return a.localPosition.x.CompareTo(b.localPosition.x); }
    static public int SortVertical(Transform a, Transform b) { return b.localPosition.y.CompareTo(a.localPosition.y); }
    static public T GetComponentInDisableParent<T>(Transform tr) where T : class
    {
        if (!tr) return default(T);
        var t = tr.GetComponent<T>();
        if (t != null) return t;
        return GetComponentInDisableParent<T>(tr.parent);
    }
    #endregion
}

#region editor script
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
        mItemWidthSp = serializedObject.FindProperty("ItemWidth");
        mItemHeightSp = serializedObject.FindProperty("ItemHeight");
        mColumnOrRowCountSp = serializedObject.FindProperty("ColumnOrRowCount");
        mCullContentSp = serializedObject.FindProperty("CullContent");
        mHideInactiveSp = serializedObject.FindProperty("HideInactive");

        var tRecycleList = target as UIRecycleListForUGUI;
        mScrollRect = UIRecycleListForUGUI.GetComponentInDisableParent<ScrollRect>(tRecycleList.transform);
        mTargetTransfrom = tRecycleList.GetComponent<RectTransform>() ?? tRecycleList.gameObject.AddComponent<RectTransform>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("1、点击“初始化配置”\n2、调整好ItemContainer的Pos", MessageType.Info);

        if (!mScrollRect)
        {
            EditorGUILayout.HelpBox("ScrollRect找不到", MessageType.Error);
            return;
        }

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
            tContent.sizeDelta = tScrollRectSize;
            tContent.anchoredPosition = Vector3.zero;

            tContent.gameObject.AddMissingComponent<RectTransform>();
            var tComponents = tContent.GetComponents(typeof(Component));
            foreach (var item in tComponents)
            {
                if (item is Transform || item is RectTransform) continue;
                DestroyImmediate(item);
            }


            var tViewPort = tContent.parent.GetComponent<RectTransform>();
            tViewPort.anchorMin = Vector2.zero;
            tViewPort.anchorMax = Vector2.one;
            tViewPort.pivot = Vector2.one * 0.5F;
            tViewPort.rotation = Quaternion.identity;
            tViewPort.localScale = Vector3.one;
            tViewPort.anchoredPosition = Vector3.zero;
            tViewPort.sizeDelta = Vector2.zero;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endregion
