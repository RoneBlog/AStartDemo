﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{

    public static bool mouseButton = false;
    public static bool waitonce = false;
    public static bool isClickStart = false;
    public static bool isClickEnd = false;

    private  List<string> optSetting = new List<string>() {"A Star Search", "Jump Point Search" };
    private Map map;
    private Transform mapRoot;
    private GridLayoutGroup grid;
    private GameObject modlePointer;

    private BaseSearch search;
    private SearchData searchData;
    private Coroutine coroutine = null;

    [SerializeField]
    private Toggle tgl_ShowPos;
    [SerializeField]
    private Toggle tgl_ShowFGH;
    [SerializeField]
    private Toggle tgl_IgnoreCorner;
    [SerializeField]
    private Dropdown drpd_Opt;  
    [SerializeField]
    private Slider sld_viewScale;  
    [SerializeField]
    private Slider sld_interval;    
    [SerializeField]
    private Button btn_reset;
    [SerializeField]
    private Button btn_start;    
    [SerializeField]
    private Button btn_switch;
    [SerializeField]
    private GameObject root_ctrl;
    [SerializeField]
    private RectTransform rect_ScrollView;


    private void Awake()
    {
        mapRoot = transform.Find("Scroll View/Viewport/Content");
        modlePointer = transform.Find("Pointer").gameObject;
        grid = mapRoot.GetComponent<GridLayoutGroup>();
        drpd_Opt.AddOptions(optSetting);
        drpd_Opt.onValueChanged.AddListener(OnClickDropDown);
        btn_start.onClick.AddListener(onClickAsynFinder);
        sld_viewScale.onValueChanged.AddListener(OnClickSlider);
        btn_reset.onClick.AddListener(() => {
            OnClickDropDown(drpd_Opt.value);
        });
        btn_switch.onClick.AddListener(onClickSwitch);
    }
    private void Start()
    {
        OnClickDropDown(0);
    }

    private void OnDestroy()
    {

    }

    private void Update()
    {
        bool mouseButton_ = Input.GetMouseButton(0);
        if (mouseButton_ != mouseButton)
        {
            mouseButton = mouseButton_;
            if (waitonce)
            {
                waitonce = false;
            }
            else if (!mouseButton)
            {
                isClickStart = false;
                isClickEnd = false;
            }
        }
    }

    private void cleanMap()
    {
        for (int i = 0; i < mapRoot.childCount; i++)
        {
            var child = mapRoot.GetChild(i);
            GameObject.Destroy(child.gameObject);
        }
    }

    public void CreateMap(Map map)
    {
        tgl_ShowPos.onValueChanged.RemoveAllListeners();
        tgl_ShowFGH.onValueChanged.RemoveAllListeners();
        var mapData = map.mapData;
        int h = mapData.GetLength(0);
        int w = mapData.GetLength(1);
        grid.constraintCount = w;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                createSigleObj(map, x, y);
            }
        }
    }

    private void createSigleObj(Map map, int x, int y)
    {
        GameObject go = GameObject.Instantiate(modlePointer);
        go.SetActive(true);
        go.transform.SetParent(mapRoot, false);
        var pointer = go.GetComponent<Pointer>();
        pointer?.SetPointer(map, x, y,tgl_ShowPos,tgl_ShowFGH);
    }

    private void OnClickSlider(float value)
    {
        mapRoot.localScale = Vector3.one * value;
    }
    private void OnClickDropDown(int op)
    {
        cleanMap();
        map = new Map();
        searchData = new SearchData();
        searchData.cmpltCllBck = drawFinalPath;
        if (op == 0)
        {
            //A Star
            search = new AStarSearch(map);
            search.SearchCallBack = SearchCallBack;
            search.PointCallBack = PointCallBack;
            drawMap();
        }
        else if (op == 1)
        {
            search = new JumpPointSearch(map);
            search.SearchCallBack = SearchCallBack;
            search.PointCallBack = PointCallBack;
            drawMap();
        }
    }

    private void drawMap()
    {
        StopAllCoroutines();
        int width = 20;
        int height = 20;
        map.RebuildMap(width, height);
        CreateMap(map);
    }

    private void onClickAsynFinder()
    {
        YieldInstruction interval = sld_interval.value == 0 ? null : new WaitForSeconds(sld_interval.value);
        searchData.interval = interval;
        searchData.start = new Point((int)map.start.x, (int)map.start.y);
        searchData.end = new Point((int)map.end.x, (int)map.end.y);
        searchData.isIgnoreCorner = tgl_IgnoreCorner.isOn;

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        coroutine = StartCoroutine(search.AsynFindPath(searchData));
        onClickSwitch();
    }

    private void SearchCallBack(Point point)
    {
        string rootName = point.ToPoint();
        Transform root = mapRoot.transform.Find(rootName);
        Pointer pointer = root?.GetComponent<Pointer>();
        if (pointer)
        {
            pointer.SetSearch(point);
        }
    }

    private void PointCallBack(Point point)
    {
        string rootName = point.ToPoint();
        Transform root = mapRoot.transform.Find(rootName);
        Pointer pointer = root?.GetComponent<Pointer>();
        if (pointer)
        {
            pointer.ShowGHF(point);
        }
    }

    private bool is_on = true;

    private void onClickSwitch()
    {
        is_on = !is_on;
        root_ctrl.SetActive(is_on);
        var anchorMin = rect_ScrollView.anchorMin;
        if (is_on)
        {
            rect_ScrollView.anchorMin = new Vector2(0.25f, anchorMin.y);
        }
        else
        {
            rect_ScrollView.anchorMin = new Vector2(0, anchorMin.y);
        }
    }

    private void drawFinalPath(Point point)
    {
        List<Point> path = new List<Point>();
        if (point != null)
        {
            var parent = point;
            while (parent != null)
            {
                path.Add(parent);
                parent = parent.ParentPoint;
            }
            coroutine = null;   
        }
        if (path.Count > 0)
        {
            Point proPoint = null;
            for (int i = 0; i < path.Count; i++)
            {
                var p = path[i];
                if (proPoint == null)
                {
                    string rootName = p.ToPoint();
                    setPointer(rootName);
                    proPoint = p;
                    continue;
                }
                int xDiff = p.X - proPoint.X;
                int yDiff = p.Y - proPoint.Y;
                int xStep = Mathf.Clamp(xDiff, -1, 1);
                int yStep = Mathf.Clamp(yDiff, -1, 1);
                xDiff = Mathf.Abs(xDiff);
                yDiff = Mathf.Abs(yDiff);
                for (int j = 0; j < Mathf.Max(xDiff, yDiff); j++)
                {
                    int posX = j < xDiff ? proPoint.X + (xStep * j) : p.X;
                    int posY = j < yDiff ? proPoint.Y + (yStep * j) : p.Y;
                    string rootName = string.Format("(x:{0},y:{1})", posX, posY);
                    setPointer(rootName);
                }
                proPoint = p;
            }
        }
    }

    private void setPointer(string rootName)
    {
        Transform root = mapRoot.transform.Find(rootName);
        Pointer pointer = root?.GetComponent<Pointer>();
        pointer?.SetSelect();
    }
}
