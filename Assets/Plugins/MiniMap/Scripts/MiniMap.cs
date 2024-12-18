using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations;
using UnityEngine.UI;
using AmagiSakuya.MiniMap.EditorClass;

namespace AmagiSakuya.MiniMap
{
    [System.Serializable]
    public class MiniMapAssociation
    {
        public GameObject target;
        public string itemName;
        public bool miniMapStrongPrompt;
        public float scaleInMinMap = 1f;
        public float scaleInWorldMap = 1.5f;
        [ReadOnly] public bool initedInMini;
        [ReadOnly] public bool initedInWorld;

        GameObject itemLinkInMini;
        GameObject itemLinkInWorld;
        public GameObject ItemLinkInMini { get { return itemLinkInMini; } set { itemLinkInMini = value; } }
        public GameObject ItemLinkInWorld { get { return itemLinkInWorld; } set { itemLinkInWorld = value; } }
    }

    public enum MiniMapType
    {
        FixedNorth,
        FixedPlayerForward
    }

    public enum MiniMapDirEnum
    {
        Forward,
        Up,
        Right
    }

    [ExecuteInEditMode]
    public class MiniMap : MonoBehaviour
    {
        [Header("地图必要配置")]
        public Transform mainPlayer;
        public MiniMapDirEnum mainPlayerForwardDir;
        [SerializeField] WorldMpaCoordinateReference_Define worldMapCoordinateReference_Define;
        public MiniMapBaseMap_Define baseMapDefine;

        [Header("地图跟踪点配置")]
        public List<MiniMapAssociation> associations;

        [SerializeField] MiniMapItem_Define itemDefine;

        [Header("小地图配置")]
        public MiniMapType miniMapType;
        public bool disableMiniMap;
        [SerializeField] float mainPlayerSizeItem_mini = 1f;

        [Header("世界地图配置")]
        [SerializeField] KeyCode activeCode = KeyCode.M;
        public bool disableWorldMap;
        [SerializeField] float mainPlayerSizeItem_world = 1f;

        [Header("小地图引用")]
        [SerializeField] Camera miniMapRenderCam;
        [SerializeField] RectTransform mainPlayerUI;
        [SerializeField] GameObject itemPrefab;
        [SerializeField] RectTransform minimap_NormalItemArea;
        [SerializeField] RectTransform emphasizeItemArea;
        [SerializeField] RectTransform miniMapBase;
        [SerializeField] GameObject miniMapPlaneMapContainer;

        [Header("世界地图引用")]
        [SerializeField] PositionConstraint positionConstraint;
        [SerializeField] RectTransform activeBase;
        [SerializeField] RectTransform worldmap_NormalItemArea;
        [SerializeField] RectTransform mainPlayerUI_world;
        [SerializeField] Image worldMapBaseImageComp;

        [Header("轨迹记录")]
        [SerializeField] bool pathRecordActive;
        [SerializeField] LineRenderer pathLineRenderer;
        [SerializeField][ReadOnly] List<Vector3> mainPlayerPosition;
        [SerializeField] float recordTimeInterval = 0.3f;
        [SerializeField] float maxOfRecordNum = 20;
        [SerializeField] float startWidth = 5f;

        [Header("世界地图Debug")]
        [ReadOnly][SerializeField] float sceneToUIImageScale;
        [ReadOnly][SerializeField] Vector2 uiOrigin;  // UI 中基准点（anchor）的坐标
        [ReadOnly][SerializeField] bool active;

        void Start()
        {
            if (!Application.isPlaying) return;
            itemPrefab.SetActive(false);
            if (!disableWorldMap)
                InitWorldMap();
            UpdateBaseMap();
            mainPlayerPosition = new List<Vector3>();
            pathLineRenderer.startWidth = startWidth; // 起始宽度
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                UpdatePositionConstraint();
                UpdateBaseMap();
                return;
            }

            if (!disableWorldMap)
                UpdateWorldMap();

            if (!disableMiniMap)
                UpdateMiniMap();

            if (pathRecordActive)
            {
                UpdatePlayerHistoryPath();
                pathLineRenderer.gameObject.SetActive(true);
            }
            else
            {
                pathLineRenderer.gameObject.SetActive(false);
            }

        }

        #region 关联数组

        /// <summary>
        /// 添加方法：如果 associations 中没有目标 obj，则添加
        /// </summary>
        public void AddToAssociations(GameObject obj, string itemName, float scaleInMinMap = 1.0f, float scaleInWorldMap = 1.0f)
        {
            // 查找是否已经包含该对象
            bool exists = associations.Exists(a => a.target == obj);

            // 如果不存在，添加新的 MiniMapAssociation
            if (!exists)
            {
                associations.Add(new MiniMapAssociation { target = obj, itemName = itemName, scaleInMinMap = scaleInMinMap, scaleInWorldMap = scaleInWorldMap });
                //Debug.Log(obj.name + " added to associations.");
            }
        }

        /// <summary>
        /// 删除方法：如果 associations 中包含目标 obj，则移除
        /// </summary>
        /// <param name="obj"></param>
        public void RemoveFromAssociations(GameObject obj)
        {
            // 查找是否已经包含该对象
            MiniMapAssociation association = associations.Find(a => a.target == obj);

            // 如果找到，移除该项
            if (association != null)
            {
                Destroy(association.ItemLinkInMini);
                Destroy(association.ItemLinkInWorld);

                associations.Remove(association);
                //Debug.Log(obj.name + " removed from associations.");
            }
        }

        /// <summary>
        /// 根据一个物体 查找关联配置
        /// </summary>
        public MiniMapAssociation FindAssociationByGameObject(GameObject target)
        {
            return associations.Find(a => a.target == target);
        }
        #endregion

        #region 世界地图

        void InitWorldMap()
        {
            // 计算场景中的距离
            float sceneDistance = Vector3.Distance(worldMapCoordinateReference_Define.basePoint, worldMapCoordinateReference_Define.upPoint);

            // 计算 UI 中两个点之间的距离（UI RectTransform 里的 anchoredPosition）
            float uiDistance = Vector2.Distance(worldMapCoordinateReference_Define.basePoint_ui, worldMapCoordinateReference_Define.upPoint_ui);

            // 计算比例：将场景距离映射到 UI 中的距离
            sceneToUIImageScale = uiDistance / sceneDistance;

            // 记录 UI 中基准点的 anchoredPosition
            uiOrigin = worldMapCoordinateReference_Define.basePoint_ui;

            UpdatePositionConstraint();
        }

        void UpdatePositionConstraint()
        {
            if (mainPlayer == null || positionConstraint == null) return;
            // 创建 ConstraintSource 并设置 sourceTransform 和 weight
            ConstraintSource newSource = new ConstraintSource();
            newSource.sourceTransform = mainPlayer.transform;
            newSource.weight = 1;
            // 获取现有的 sources 列表
            List<ConstraintSource> sources = new List<ConstraintSource>();
            positionConstraint.GetSources(sources);
            // 如果 sources 列表不为空，则更新第一个 source
            if (sources.Count > 0)
            {
                sources[0] = newSource; // 更新第一个 source
            }
            else
            {
                // 如果 sources 为空，添加新的 source
                positionConstraint.AddSource(newSource);
            }

            // 设置更新后的 sources 列表
            positionConstraint.SetSources(sources);
        }

        void UpdateWorldMap()
        {
            if (Input.GetKeyDown(activeCode))
            {
                active = !active;
            }

            //mainPlayerUI_world
            mainPlayerUI_world.anchoredPosition = PlaceUIElementAtWorldPosition(mainPlayer.gameObject);
            mainPlayerUI_world.up = RotWorldUIElementFromWorldDir(GetObjWorldDirByMiniMapDirEnum(mainPlayer.transform, mainPlayerForwardDir));

            if (mainPlayerSizeItem_world > 0)
            {
                mainPlayerUI_world.localScale = new Vector3(mainPlayerSizeItem_world, mainPlayerSizeItem_world, 1f);
            }

            activeBase.gameObject.SetActive(active);
            if (active)
            {
                for (int i = 0; i < associations.Count; i++)
                {
                    if (!associations[i].initedInWorld)
                    {
                        associations[i].ItemLinkInWorld = CreateItem(associations[i], true);
                    }
                    associations[i].ItemLinkInWorld.GetComponent<RectTransform>().anchoredPosition = PlaceUIElementAtWorldPosition(associations[i].target);
                }
            }
        }

        GameObject CreateItem(MiniMapAssociation asso, bool isWorld)
        {
            Transform parent = isWorld ? worldmap_NormalItemArea : minimap_NormalItemArea;
            var resultObj = Instantiate(itemPrefab, parent);
            var settings = GetMiniMapItemDBSettingsByName(asso.itemName);
            var item_comp = resultObj.GetComponent<MiniMapRingItem>();
            item_comp.promptRot.gameObject.SetActive(false);
            if (settings.disableBG)
            {
                item_comp.selfImg.enabled = false;
            }
            if (settings.changeBGColor)
            {
                item_comp.selfImg.color = settings.overrideBGColor;
            }
            item_comp.img.sprite = settings.img;
            if (isWorld)
            {
                if (asso.scaleInWorldMap > 0)
                    resultObj.transform.localScale = new Vector3(asso.scaleInWorldMap, asso.scaleInWorldMap, 1f);
                resultObj.SetActive(true);
                asso.initedInWorld = true;
            }
            else
            {
                if (asso.scaleInMinMap > 0)
                    resultObj.transform.localScale = new Vector3(asso.scaleInMinMap, asso.scaleInMinMap, 1f);
                resultObj.SetActive(true);
                asso.initedInMini = true;
            }
            return resultObj;
        }

        MiniMapItemDBSettings GetMiniMapItemDBSettingsByName(string name)
        {
            for (int i = 0; i < itemDefine.itemDB.Length; i++)
            {
                if (itemDefine.itemDB[i].name == name)
                {
                    return itemDefine.itemDB[i];
                }
            }
            Debug.LogError($"itemDB不包含{name}的定义", gameObject);
            return default;
        }

        public Vector2 PlaceUIElementAtWorldPosition(GameObject sceneObject)
        {
            // 获取场景中的 basePoint 和 upPoint 的位置
            Vector2 posInBuildCoordinates = GetLocalCoordinates(sceneObject.transform.position);

            // 转换成 UI 坐标，通过缩放比例转换
            Vector2 uiPosition = new Vector2(posInBuildCoordinates.x * sceneToUIImageScale, posInBuildCoordinates.y * sceneToUIImageScale);

            // 将局部坐标转换为相对于底图锚点的 UI 坐标
            Vector2 finalUIPosition = uiOrigin + uiPosition;

            return finalUIPosition;
        }

        Vector3 RotWorldUIElementFromWorldDir(Vector3 worldDirection)
        {
            // 获取原点、X 轴点和 Y 轴点的世界坐标
            Vector3 originPoint = worldMapCoordinateReference_Define.basePoint;
            Vector3 upPoint = worldMapCoordinateReference_Define.upPoint;
            Vector3 rightPoint = worldMapCoordinateReference_Define.rightPoint;

            // 计算 X 轴和 Y 轴方向的单位向量
            Vector3 xAxis = (rightPoint - originPoint).normalized;
            Vector3 yAxis = (upPoint - originPoint).normalized;

            // 计算 Z 轴方向的单位向量（通过叉乘 X 和 Y 轴获得平面法线方向）
            Vector3 zAxis = Vector3.Cross(xAxis, yAxis).normalized;

            // 将世界方向向量投影到局部坐标系的 X, Y, Z 轴上
            float localX = Vector3.Dot(worldDirection, xAxis);
            float localY = Vector3.Dot(worldDirection, yAxis);
            float localZ = Vector3.Dot(worldDirection, zAxis);

            // 返回世界方向在局部坐标系中的分量
            return new Vector3(localX, localY, localZ);
        }

        Vector2 GetLocalCoordinates(Vector3 scenePoint)
        {
            // 获取原点、X 轴点和 Y 轴点的世界坐标
            Vector3 originPoint = worldMapCoordinateReference_Define.basePoint;
            Vector3 upPoint = worldMapCoordinateReference_Define.upPoint;
            Vector3 rightPoint = worldMapCoordinateReference_Define.rightPoint;

            // 计算 X 轴和 Y 轴方向的单位向量
            Vector3 xAxis = (rightPoint - originPoint).normalized;
            Vector3 yAxis = (upPoint - originPoint).normalized;

            // 计算第四个点相对于原点的向量
            Vector3 relativeScenePosition = scenePoint - originPoint;

            // 计算该点在 X 轴和 Y 轴上的投影（点乘）
            float xProjection = Vector3.Dot(relativeScenePosition, xAxis);
            float yProjection = Vector3.Dot(relativeScenePosition, yAxis);

            // 返回该点在局部坐标系中的二维投影坐标
            return new Vector2(xProjection, yProjection);
        }
        #endregion

        #region 小地图
        void UpdateMiniMap()
        {
            //更新Camera方向
            UpdateMiniMapCamDir();

            //调整主角朝向
            AlignMiniMapUIDir(mainPlayer, mainPlayerForwardDir, mainPlayerUI, miniMapRenderCam);

            if (mainPlayerSizeItem_mini > 0)
            {
                mainPlayerUI.localScale = new Vector3(mainPlayerSizeItem_mini, mainPlayerSizeItem_mini, 1);
            }

            //调整元素的位置
            for (int i = 0; i < associations.Count; i++)
            {
                if (!associations[i].initedInMini)
                {
                    associations[i].ItemLinkInMini = CreateItem(associations[i], false);
                }

                if (associations[i].miniMapStrongPrompt)
                {
                    //如果是强提示
                    if (IsInMiniMapVP(associations[i].ItemLinkInMini.GetComponent<RectTransform>().anchoredPosition, miniMapBase.sizeDelta.x / 2 - 10))
                    {
                        associations[i].ItemLinkInMini.transform.SetParent(minimap_NormalItemArea);
                        associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().prompt = false;
                        associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().promptRing = true;
                    }
                    else
                    {
                        associations[i].ItemLinkInMini.transform.SetParent(emphasizeItemArea);
                        associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().prompt = true;
                        associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().promptRing = false;
                    }

                    //if (IsInMiniMapVP(associations[i].ItemLinkInMini.GetComponent<RectTransform>().anchoredPosition, miniMapBase.sizeDelta.x / 4))
                    //{
                    //    associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().prompt = false;
                    //}
                    //else
                    //{
                    //    associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().prompt = true;
                    //}

                    AlignMiniMapUIPos(associations[i].target.transform, associations[i].ItemLinkInMini.GetComponent<RectTransform>(), miniMapRenderCam, miniMapBase.sizeDelta.x / 2);

                    Vector2 screenDirection = associations[i].ItemLinkInMini.GetComponent<RectTransform>().anchoredPosition;
                    // 设置 UI 元素的上方向
                    associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().promptRot.up = screenDirection;

                }
                else
                {
                    associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().promptRing = false;
                    associations[i].ItemLinkInMini.GetComponent<MiniMapRingItem>().prompt = false;
                    associations[i].ItemLinkInMini.transform.SetParent(minimap_NormalItemArea);
                    AlignMiniMapUIPos(associations[i].target.transform, associations[i].ItemLinkInMini.GetComponent<RectTransform>(), miniMapRenderCam);
                }


            }
        }

        void UpdateMiniMapCamDir()
        {
            Quaternion rotation;
            if (miniMapType == MiniMapType.FixedNorth)
            {
                rotation = Quaternion.FromToRotation(miniMapRenderCam.transform.up, Vector3.forward);
            }
            else
            {
                rotation = Quaternion.FromToRotation(miniMapRenderCam.transform.up, GetObjWorldDirByMiniMapDirEnum(mainPlayer, mainPlayerForwardDir));
            }
            miniMapRenderCam.transform.rotation = rotation * miniMapRenderCam.transform.rotation;
        }

        bool IsInMiniMapVP(Vector2 uiPosition, float limitRange)
        {
            return uiPosition.magnitude <= limitRange;
        }

        void AlignMiniMapUIPos(Transform trackObj, RectTransform uiRect, Camera camera, float limitRange = -1)
        {
            // 1. 将物体的世界坐标转换为视口坐标 (0到1之间)
            Vector3 viewportPosition = camera.WorldToViewportPoint(trackObj.position);
            RectTransform parentRect = uiRect.parent as RectTransform;
            Vector2 uiPosition = new Vector2(
                (viewportPosition.x - 0.5f) * parentRect.rect.width,  // X坐标
                (viewportPosition.y - 0.5f) * parentRect.rect.height  // Y坐标
            );

            if (limitRange > 0 && !IsInMiniMapVP(uiPosition, limitRange))
            {
                //将 uiPosition 限制在半径为 limitRange 的圆范围内
                uiPosition = uiPosition.normalized * limitRange;
            }
            uiRect.anchoredPosition = uiPosition;
        }

        void AlignMiniMapUIDir(Transform trackObj, MiniMapDirEnum mainPlayerForwardDir, RectTransform uiRect, Camera camera)
        {
            // 获取物体的世界 forward 方向
            Vector3 worldForward = GetObjWorldDirByMiniMapDirEnum(trackObj, mainPlayerForwardDir);
            // 将世界方向转换为屏幕空间的方向
            Vector3 worldForwardPoint = trackObj.position + worldForward;
            Vector3 screenPosition = camera.WorldToScreenPoint(trackObj.position);
            Vector3 screenForwardPoint = camera.WorldToScreenPoint(worldForwardPoint);
            Vector3 screenDirection = (screenForwardPoint - screenPosition).normalized;
            // 设置 UI 元素的上方向
            uiRect.up = new Vector3(screenDirection.x, screenDirection.y, 0);
        }

        Vector3 GetObjWorldDirByMiniMapDirEnum(Transform trackObj, MiniMapDirEnum direction)
        {
            switch (direction)
            {
                case MiniMapDirEnum.Forward:
                    return trackObj.forward;
                case MiniMapDirEnum.Up:
                    return trackObj.up;
                case MiniMapDirEnum.Right:
                    return trackObj.right;
                default:
                    return trackObj.forward; // 默认返回 forward
            }
        }
        #endregion

        #region 底图
        public void UpdateBaseMap()
        {
            if (baseMapDefine != null)
            {
                worldMapBaseImageComp.GetComponent<RectTransform>().anchoredPosition = baseMapDefine.rectPosOfImage;
                worldMapBaseImageComp.GetComponent<RectTransform>().sizeDelta = baseMapDefine.sizeOfImage;
                worldMapBaseImageComp.GetComponent<RectTransform>().localScale = baseMapDefine.rectScaleOfImage;
                worldMapBaseImageComp.sprite = baseMapDefine.worldMapBaseImage;

                miniMapRenderCam.cullingMask = baseMapDefine.cameraCullingMask;
                if (baseMapDefine.usePlaneMap)
                {
                    miniMapPlaneMapContainer.SetActive(true);
                    if (!Application.isPlaying)
                    {
                        miniMapPlaneMapContainer.GetComponent<Renderer>().sharedMaterial = baseMapDefine.planeMat;
                    }
                    else
                    {
                        miniMapPlaneMapContainer.GetComponent<Renderer>().material = baseMapDefine.planeMat;
                    }

                    miniMapPlaneMapContainer.transform.localPosition = baseMapDefine.planePos;
                    miniMapPlaneMapContainer.transform.localEulerAngles = baseMapDefine.planeRot;
                    miniMapPlaneMapContainer.transform.localScale = baseMapDefine.planeScale;
                }
                else
                {
                    miniMapPlaneMapContainer.SetActive(false);
                }
            }
        }

        #endregion

        #region 轨迹记录
        private float timeSinceLastRecord = 0f;
        void UpdatePlayerHistoryPath()
        {
            timeSinceLastRecord += Time.deltaTime;
            if (timeSinceLastRecord >= recordTimeInterval)
            {
                // 记录玩家当前位置
                RecordPlayerPosition();
                timeSinceLastRecord = 0f;
            }
            // 更新 LineRenderer 以绘制路径
            UpdateLineRenderer();
        }
        Vector3 lastPos;
        void RecordPlayerPosition()
        {
            Vector3 currentPlayerPosition = mainPlayer.position;
            if (Vector3.Distance(lastPos, currentPlayerPosition) < 1f)
            {
                return;
            }
            lastPos = currentPlayerPosition;


            // 如果超过最大记录数量，删除第一个位置
            if (mainPlayerPosition.Count >= maxOfRecordNum)
            {
                mainPlayerPosition.RemoveAt(0);
            }

            // 添加当前玩家位置
            mainPlayerPosition.Add(currentPlayerPosition);
        }

        void UpdateLineRenderer()
        {
            // 更新 LineRenderer 的顶点数
            pathLineRenderer.positionCount = mainPlayerPosition.Count;

            // 将玩家位置设置为 LineRenderer 的点
            for (int i = mainPlayerPosition.Count - 1; i >= 0; i--)
            {
                pathLineRenderer.SetPosition(i, mainPlayerPosition[i]);
            }
        }

        #endregion

    }

}