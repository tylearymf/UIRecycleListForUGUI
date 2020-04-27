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
    public bool IgnoreInactive = false;

    public OnUpdateItem OnUpdateItemEvent;

    protected Transform mTrans;
    protected ScrollRect mScroll;
    protected bool mHorizontal;
    protected List<Transform> mChildrens = new List<Transform>();
    protected Vector3[] mCorners = new Vector3[4];
    protected int mMaxIndex;
    protected int mCurIndex;

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
        if (mScroll != null)
        {
            mScroll.onValueChanged.AddListener(OnMove);

            if (mHorizontal)
            {
                if (mScroll.horizontalScrollbar)
                {
                    mScroll.horizontalScrollbar.onValueChanged.AddListener(OnScroll);
                }
            }
            else
            {
                if (mScroll.verticalScrollbar)
                {
                    mScroll.verticalScrollbar.onValueChanged.AddListener(OnScroll);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (mScroll != null)
        {
            mScroll.onValueChanged.RemoveListener(OnMove);

            if (mHorizontal)
            {
                if (mScroll.horizontalScrollbar)
                {
                    mScroll.horizontalScrollbar.onValueChanged.RemoveListener(OnScroll);
                }
            }
            else
            {
                if (mScroll.verticalScrollbar)
                {
                    mScroll.verticalScrollbar.onValueChanged.RemoveListener(OnScroll);
                }
            }
        }
        Dispose();
    }

    void OnValidate()
    {
        if (mMaxIndex < 0)
            mMaxIndex = 0;
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
    void OnScroll(float delta) { WrapContent(); }
    #endregion

    #region sort method
    public void SortBasedOnScrollMovement()
    {
        if (!CacheScrollView()) return;

        mChildrens.Clear();
        for (int i = 0; i < mTrans.childCount; ++i)
        {
            Transform t = mTrans.GetChild(i);
            if (IgnoreInactive && !t.gameObject.activeInHierarchy) continue;
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
            if (IgnoreInactive && !t.gameObject.activeInHierarchy) continue;
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
            var tIndex = i + mCurIndex;
            switch (layoutType)
            {
                case LayoutType.SingleColumn:
                    tTrans.localPosition = mHorizontal ? new Vector3(tIndex * ItemWidth, 0f, 0f) : new Vector3(0f, -tIndex * ItemHeight, 0f);
                    break;
                case LayoutType.MultiColumn:
                    tTrans.localPosition = mHorizontal ? new Vector3((tIndex / ColumnOrRowCount) * ItemWidth, -(tIndex % ColumnOrRowCount) * ItemHeight, 0) : new Vector3((tIndex % ColumnOrRowCount) * ItemWidth, -(tIndex / ColumnOrRowCount) * ItemHeight, 0f); ; ;
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
            for (int i = 0, imax = mChildrens.Count; i < imax; ++i)
            {
                var tChild = mChildrens[i];
                var tPos = tChild.localPosition;
                var tDistance = tPos.x - tCenter.x + mScroll.content.localPosition.x + +mTrans.localPosition.x;
                var tDataIndex = -1;
                var hasChange = false;

                while (tDistance < -tExtents || tDistance > tExtents)
                {
                    hasChange = true;
                    tPos.x += tDistance < -tExtents ? tExt2 : -tExt2;
                    tDistance = tPos.x - tCenter.x + mScroll.content.localPosition.x + +mTrans.localPosition.x;
                }

                if (hasChange)
                {
                    tDataIndex = GetIndex(tPos);
                }

                if (tDataIndex >= 0 && tDataIndex < mMaxIndex)
                {
                    tChild.localPosition = tPos;
                    UpdateItem(tChild);
                }
            }
        }
        else
        {
            for (int i = 0, imax = mChildrens.Count; i < imax; ++i)
            {
                var tChild = mChildrens[i];
                var tPos = tChild.localPosition;
                var tDataIndex = -1;
                var tDistance = tPos.y - tCenter.y + mScroll.content.localPosition.y + mTrans.localPosition.y;
                var hasChange = false;

                while (tDistance < -tExtents || tDistance > tExtents)
                {
                    hasChange = true;
                    tPos.y += tDistance < -tExtents ? tExt2 : -tExt2;
                    tDistance = tPos.y - tCenter.y + mScroll.content.localPosition.y + mTrans.localPosition.y;
                }

                if (hasChange)
                {
                    tDataIndex = GetIndex(tPos);
                }

                if (tDataIndex >= 0 && tDataIndex < mMaxIndex)
                {
                    tChild.localPosition = tPos;
                    UpdateItem(tChild);
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

        var tDataIndex = GetIndex(pTrans);
        OnUpdateItemEvent(pTrans.gameObject, tDataIndex);

        if (CullContent)
        {
            pTrans.gameObject.SetActive(tDataIndex >= 0 && tDataIndex < mMaxIndex);
        }
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
                    //多生成一列
                    tCount = (Mathf.CeilToInt(tSize.x / ItemWidth) + 1) * ColumnOrRowCount;
                }
                else
                {
                    //多生成一行
                    tCount = (Mathf.CeilToInt(tSize.y / ItemHeight) + 1) * ColumnOrRowCount;
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
        if (pResetPos)
        {
            mCurIndex = 0;
        }
        else if (mChildrens.Count > 0)
        {
            if (mHorizontal) mChildrens.Sort(SortHorizontal);
            else mChildrens.Sort(SortVertical);
            mCurIndex = GetIndex(mChildrens[0].localPosition);

            //当前index超出总数量时，定位到最末尾
            if (mCurIndex >= pItemCount)
            {
                mCurIndex = pItemCount - CalculateInstantiateCount();
            }
            //当前index偏移下
            else if (mMaxIndex > pItemCount)
            {
                mCurIndex -= mMaxIndex - pItemCount;
            }

            if (mCurIndex < 0) mCurIndex = 0;
        }

        mMaxIndex = pItemCount;

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

        if (pResetPos)
        {
            tContent.anchoredPosition = Vector2.zero;
        }

        SortBasedOnScrollMovement();
        WrapContent();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        OnUpdateItemEvent = null;
        mChildrens.Clear();
        mCorners = new Vector3[4];
        mMaxIndex = 0;
        mCurIndex = 0;
    }
    #endregion

    #region common method
    int SortByName(Transform a, Transform b) { return string.Compare(a.name, b.name); }
    int SortHorizontal(Transform a, Transform b)
    {
        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                return a.localPosition.x.CompareTo(b.localPosition.x);
            case LayoutType.MultiColumn:
                var aIndex = GetIndex(a);
                var bIndex = GetIndex(b);
                return aIndex.CompareTo(bIndex);
            default:
                throw new NotImplementedException("未实现");
        }
    }
    int SortVertical(Transform a, Transform b)
    {
        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                return b.localPosition.y.CompareTo(a.localPosition.y);
            case LayoutType.MultiColumn:
                var aIndex = GetIndex(a);
                var bIndex = GetIndex(b);
                return aIndex.CompareTo(bIndex);
            default:
                throw new NotImplementedException("未实现");
        }
    }
    /// <summary>
    /// 获取index
    /// </summary>
    /// <param name="pTrans"></param>
    /// <returns></returns>
    int GetIndex(Transform pTrans)
    {
        if (!pTrans) return 0;
        return GetIndex(pTrans.localPosition);
    }
    /// <summary>
    /// 获取index
    /// </summary>
    /// <param name="pPos"></param>
    /// <returns></returns>
    int GetIndex(Vector3 pPos)
    {
        switch (layoutType)
        {
            case LayoutType.SingleColumn:
                if (mHorizontal)
                {
                    return Mathf.RoundToInt(pPos.x / ItemWidth);
                }
                else
                {
                    return -Mathf.RoundToInt(pPos.y / ItemHeight);
                }
            case LayoutType.MultiColumn:
                if (mHorizontal)
                {
                    return Mathf.RoundToInt(-pPos.y / ItemHeight) + Mathf.RoundToInt(pPos.x / ItemWidth) * ColumnOrRowCount;
                }
                else
                {
                    return Mathf.RoundToInt(pPos.x / ItemWidth) + Mathf.RoundToInt(-pPos.y / ItemHeight) * ColumnOrRowCount;
                }
            default:
                throw new NotImplementedException("未实现");
        }
    }

    public T GetComponentInDisableParent<T>(Transform tr) where T : class
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
    SerializedProperty mIgnoreInactiveSp;
    ScrollRect mScrollRect;
    RectTransform mTargetTransfrom;

    void OnEnable()
    {
        mItemWidthSp = serializedObject.FindProperty("ItemWidth");
        mItemHeightSp = serializedObject.FindProperty("ItemHeight");
        mColumnOrRowCountSp = serializedObject.FindProperty("ColumnOrRowCount");
        mCullContentSp = serializedObject.FindProperty("CullContent");
        mIgnoreInactiveSp = serializedObject.FindProperty("IgnoreInactive");

        var tRecycleList = target as UIRecycleListForUGUI;
        mScrollRect = tRecycleList.GetComponentInDisableParent<ScrollRect>(tRecycleList.transform);
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
        EditorGUILayout.PropertyField(mIgnoreInactiveSp, new GUIContent("Hide Inactive"));

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
