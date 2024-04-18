using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
public sealed class PlayerController : UnitController
{
    public enum InputMode
    {
        Orders,
        FactoryPositioning
    }

    [SerializeField]
    GameObject TargetCursorPrefab = null;
    [SerializeField]
    float TargetCursorFloorOffset = 0.2f;
    [SerializeField]
    EventSystem SceneEventSystem = null;

    [SerializeField, Range(0f, 1f)]
    float FactoryPreviewTransparency = 0.3f;

    PointerEventData MenuPointerEventData = null;

    // Build Menu UI
    MenuController PlayerMenuController;

    // Camera
    TopCamera TopCameraRef = null;
    bool CanMoveCamera = false;
    Vector2 CameraInputPos = Vector2.zero;
    Vector2 CameraPrevInputPos = Vector2.zero;
    Vector2 CameraFrameMove = Vector2.zero;

    // Selection
    Vector3 SelectionStart = Vector3.zero;
    Vector3 SelectionEnd = Vector3.zero;
    bool SelectionStarted = false;
    float SelectionBoxHeight = 50f;
    LineRenderer SelectionLineRenderer;
    GameObject TargetCursor = null;

    // Factory build
    InputMode CurrentInputMode = InputMode.Orders;
    int WantedFactoryId = 0;
    GameObject WantedFactoryPreview = null;
    [SerializeField]
    Shader PreviewShader = null;

    // Mouse events
    Action OnMouseLeftPressed = null;
    Action OnMouseLeft = null;
    Action OnMouseLeftReleased = null;
    Action OnUnitActionStart = null;
    Action OnUnitActionEnd = null;
    Action OnCameraDragMoveStart = null;
    Action OnCameraDragMoveEnd = null;

    Action<Vector3> OnFactoryPositioned = null;
    Action<float> OnCameraZoom = null;
    Action<float> OnCameraMoveHorizontal = null;
    Action<float> OnCameraMoveVertical = null;

    // Keyboard events
    Action OnFocusBasePressed = null;
    Action OnCancelBuildPressed = null;
    Action OnDestroyEntityPressed = null;
    Action OnCancelFactoryPositioning = null;
    Action OnSelectAllPressed = null;
    Action [] OnCategoryPressed = new Action[9];

    [SerializeField] Vector3[] formationOffsets;

    GameObject GetTargetCursor()
    {
        if (TargetCursor == null)
        {
            TargetCursor = Instantiate(TargetCursorPrefab);
            TargetCursor.name = TargetCursor.name.Replace("(Clone)", "");
        }
        return TargetCursor;
    }
    void SetTargetCursorPosition(Vector3 pos)
    {
        SetTargetCursorVisible(true);
        pos.y += TargetCursorFloorOffset;
        GetTargetCursor().transform.position = pos;
    }
    void SetTargetCursorVisible(bool isVisible)
    {
        GetTargetCursor().SetActive(isVisible);
    }
    void SetCameraFocusOnMainFactory()
    {
        if (FactoryList.Count > 0)
            TopCameraRef.FocusEntity(FactoryList[0]);
    }
    void CancelCurrentBuild()
    {
        SelectedFactory?.CancelCurrentBuild();
        PlayerMenuController.HideAllFactoryBuildQueue();
    }

    #region MonoBehaviour methods
    protected override void Awake()
    {
        base.Awake();

        PlayerMenuController = GetComponent<MenuController>();
        if (PlayerMenuController == null)
            Debug.LogWarning("could not find MenuController component !");

        OnBuildPointsUpdated += PlayerMenuController.UpdateBuildPointsUI;
        OnCaptureTarget += PlayerMenuController.UpdateCapturedTargetsUI;

        TopCameraRef = Camera.main.GetComponent<TopCamera>();
        SelectionLineRenderer = GetComponent<LineRenderer>();

        PlayerMenuController = GetComponent<MenuController>();
       
        if (SceneEventSystem == null)
        {
            Debug.LogWarning("EventSystem not assigned in PlayerController, searching in current scene...");
            SceneEventSystem = FindObjectOfType<EventSystem>();
        }
        // Set up the new Pointer Event
        MenuPointerEventData = new PointerEventData(SceneEventSystem);
    }

    override protected void Start()
    {
        base.Start();

        // left click : selection
        OnMouseLeftPressed += StartSelection;
        OnMouseLeft += UpdateSelection;
        OnMouseLeftReleased += EndSelection;

        // right click : Unit actions (move / attack / capture ...)
        OnUnitActionEnd += ComputeUnitsAction;

        // Camera movement
        // middle click : camera movement
        OnCameraDragMoveStart += StartMoveCamera;
        OnCameraDragMoveEnd += StopMoveCamera;

        OnCameraZoom += TopCameraRef.Zoom;
        OnCameraMoveHorizontal += TopCameraRef.KeyboardMoveHorizontal;
        OnCameraMoveVertical += TopCameraRef.KeyboardMoveVertical;

        // Gameplay shortcuts
        OnFocusBasePressed += SetCameraFocusOnMainFactory;
        OnCancelBuildPressed += CancelCurrentBuild;

        OnCancelFactoryPositioning += ExitFactoryBuildMode;

        OnFactoryPositioned += (floorPos) =>
        {
            if (RequestFactoryBuild(WantedFactoryId, floorPos))
            {
                ExitFactoryBuildMode();
            }
        };

        // Destroy selected unit command
        OnDestroyEntityPressed += () =>
        {
            Unit[] unitsToBeDestroyed = SelectedUnitList.ToArray();
            foreach (Unit unit in unitsToBeDestroyed)
            {
                (unit as IDamageable).Destroy();
            }

            if (SelectedFactory)
            {
                Factory factoryRef = SelectedFactory;
                UnselectCurrentFactory();
                factoryRef.Destroy();
            }
        };

        // Selection shortcuts
        OnSelectAllPressed += SelectAllUnits;

        for(int i = 0; i < OnCategoryPressed.Length; i++)
        {
            // store typeId value for event closure
            int typeId = i;
            OnCategoryPressed[i] += () =>
            {
                SelectAllUnitsByTypeId(typeId);
            };
        }
    }
    override protected void Update()
    {
        switch (CurrentInputMode)
        {
            case InputMode.FactoryPositioning:
                UpdateFactoryPositioningInput();
                break;
            case InputMode.Orders:
                UpdateSelectionInput();
                UpdateActionInput();
                break;
        }

        UpdateCameraInput();
        UpdateMoveCamera();

        int j = 0;
        //Formation Move
        for (int i = 1; i < SelectedUnitList.Count; i++)
        {
            if(SelectedUnitList.Count <= formationOffsets.Length)
            {
                j++;
                SelectedUnitList[i].SetTargetPos
                (SelectedUnitList[0].gameObject.transform.position + 
                SelectedUnitList[0].gameObject.transform.TransformDirection(formationOffsets[j] * 2));
            }
            else
            {
                SelectedUnitList[i].SetTargetPos
                (SelectedUnitList[0].gameObject.transform.position +
                SelectedUnitList[0].gameObject.transform.TransformDirection(new Vector3(i *6f, 0f, 0f)));
            }
            //if (i + 1 >= formationOffsets.Length)
            //    j = 0;
            //else
            //    j++;

        }
    }
    #endregion

    #region Update methods
    void UpdateFactoryPositioningInput()
    {
        Vector3 floorPos = ProjectFactoryPreviewOnFloor();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelFactoryPositioning?.Invoke();
        }
        if (Input.GetMouseButtonDown(0))
        {
            OnFactoryPositioned?.Invoke(floorPos);
        }
    }
    void UpdateSelectionInput()
    {
        // Update keyboard inputs

        if (Input.GetKeyDown(KeyCode.A))
            OnSelectAllPressed?.Invoke();

        for (int i = 0; i < OnCategoryPressed.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Keypad1 + i) || Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                OnCategoryPressed[i]?.Invoke();
                break;
            }
        }

        // Update mouse inputs
#if UNITY_EDITOR
        if (EditorWindow.focusedWindow != EditorWindow.mouseOverWindow)
            return;
#endif
        if (Input.GetMouseButtonDown(0))
            OnMouseLeftPressed?.Invoke();
        if (Input.GetMouseButton(0))
            OnMouseLeft?.Invoke();
        if (Input.GetMouseButtonUp(0))
            OnMouseLeftReleased?.Invoke();

    }
    void UpdateActionInput()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
            OnDestroyEntityPressed?.Invoke();

        // cancel build
        if (Input.GetKeyDown(KeyCode.C))
            OnCancelBuildPressed?.Invoke();

        // Contextual unit actions (attack / capture ...)
        if (Input.GetMouseButtonDown(1))
            OnUnitActionStart?.Invoke();
        if (Input.GetMouseButtonUp(1))
            OnUnitActionEnd?.Invoke();
    }
    void UpdateCameraInput()
    {
        // Camera focus

        if (Input.GetKeyDown(KeyCode.F))
            OnFocusBasePressed?.Invoke();

        // Camera movement inputs

        // keyboard move (arrows)
        float hValue = Input.GetAxis("Horizontal");
        if (hValue != 0)
            OnCameraMoveHorizontal?.Invoke(hValue);
        float vValue = Input.GetAxis("Vertical");
        if (vValue != 0)
            OnCameraMoveVertical?.Invoke(vValue);

        // zoom in / out (ScrollWheel)
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");
        if (scrollValue != 0)
            OnCameraZoom?.Invoke(scrollValue);

        // drag move (mouse button)
        if (Input.GetMouseButtonDown(2))
            OnCameraDragMoveStart?.Invoke();
        if (Input.GetMouseButtonUp(2))
            OnCameraDragMoveEnd?.Invoke();
    }
    #endregion

    #region Unit selection methods
    void StartSelection()
    {
        // Hide target cursor
        SetTargetCursorVisible(false);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int factoryMask = 1 << LayerMask.NameToLayer("Factory");
        int unitMask = 1 << LayerMask.NameToLayer("Unit");
        int floorMask = 1 << LayerMask.NameToLayer("Floor");

        // *** Ignore Unit selection when clicking on UI ***
        // Set the Pointer Event Position to that of the mouse position
        MenuPointerEventData.position = Input.mousePosition;

        //Create a list of Raycast Results
        List<RaycastResult> results = new List<RaycastResult>();
        PlayerMenuController.BuildMenuRaycaster.Raycast(MenuPointerEventData, results);
        if (results.Count > 0)
            return;

        RaycastHit raycastInfo;
        // factory selection
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, factoryMask))
        {
            Factory factory = raycastInfo.transform.GetComponent<Factory>();
            if (factory != null)
            {
                if (factory.GetTeam() == Team && SelectedFactory != factory)
                {
                    UnselectCurrentFactory();
                    SelectFactory(factory);
                }
            }
        }
        // unit selection / unselection
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, unitMask))
        {
            bool isShiftBtPressed = Input.GetKey(KeyCode.LeftShift);
            bool isCtrlBtPressed = Input.GetKey(KeyCode.LeftControl);

            UnselectCurrentFactory();

            Unit selectedUnit = raycastInfo.transform.GetComponent<Unit>();
            if (selectedUnit != null && selectedUnit.GetTeam() == Team)
            {
                if (isShiftBtPressed)
                {
                    UnselectUnit(selectedUnit);
                }
                else if (isCtrlBtPressed)
                {
                    SelectUnit(selectedUnit);
                }
                else
                {
                    UnselectAllUnits();
                    SelectUnit(selectedUnit);
                }
            }
        }
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            UnselectCurrentFactory();
            SelectionLineRenderer.enabled = true;

            SelectionStarted = true;

            SelectionStart.x = raycastInfo.point.x;
            SelectionStart.y = 0.0f;//raycastInfo.point.y + 1f;
            SelectionStart.z = raycastInfo.point.z;
        }
    }

    /*
     * Multi selection methods
     */
    void UpdateSelection()
    {
        if (SelectionStarted == false)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int floorMask = 1 << LayerMask.NameToLayer("Floor");

        RaycastHit raycastInfo;
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            SelectionEnd = raycastInfo.point;
        }

        SelectionLineRenderer.SetPosition(0, new Vector3(SelectionStart.x, SelectionStart.y, SelectionStart.z));
        SelectionLineRenderer.SetPosition(1, new Vector3(SelectionStart.x, SelectionStart.y, SelectionEnd.z));
        SelectionLineRenderer.SetPosition(2, new Vector3(SelectionEnd.x, SelectionStart.y, SelectionEnd.z));
        SelectionLineRenderer.SetPosition(3, new Vector3(SelectionEnd.x, SelectionStart.y, SelectionStart.z));
    }
    void EndSelection()
    {
        if (SelectionStarted == false)
            return;

        UpdateSelection();
        SelectionLineRenderer.enabled = false;
        Vector3 center = (SelectionStart + SelectionEnd) / 2f;
        Vector3 size = Vector3.up * SelectionBoxHeight + SelectionEnd - SelectionStart;
        size.x = Mathf.Abs(size.x);
        size.y = Mathf.Abs(size.y);
        size.z = Mathf.Abs(size.z);

        UnselectAllUnits();
        UnselectCurrentFactory();

        int unitLayerMask = 1 << LayerMask.NameToLayer("Unit");
        int factoryLayerMask = 1 << LayerMask.NameToLayer("Factory");
        Collider[] colliders = Physics.OverlapBox(center, size / 2f, Quaternion.identity, unitLayerMask | factoryLayerMask, QueryTriggerInteraction.Ignore);
        foreach (Collider col in colliders)
        {
            //Debug.Log("collider name = " + col.gameObject.name);
            ISelectable selectedEntity = col.transform.GetComponent<ISelectable>();
            if (selectedEntity.GetTeam() == GetTeam())
            {
                if (selectedEntity is Unit)
                {
                    SelectUnit((selectedEntity as Unit));
                }
                else if (selectedEntity is Factory)
                {
                    // Select only one factory at a time
                    if (SelectedFactory == null)
                        SelectFactory(selectedEntity as Factory);
                }
            }
        }

        SelectionStarted = false;
        SelectionStart = Vector3.zero;
        SelectionEnd = Vector3.zero;
    }
    #endregion

    #region Factory / build methods
    public void UpdateFactoryBuildQueueUI(int entityIndex)
    {
        PlayerMenuController.UpdateFactoryBuildQueueUI(entityIndex, SelectedFactory);
    }
    protected override void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        base.SelectFactory(factory);

        PlayerMenuController.UpdateFactoryMenu(SelectedFactory, RequestUnitBuild, EnterFactoryBuildMode);
    }
    public override void UnselectCurrentFactory()
    {
        //Debug.Log("UnselectCurrentFactory");

        if (SelectedFactory)
        {
            PlayerMenuController.UnregisterBuildButtons(SelectedFactory.AvailableUnitsCount, SelectedFactory.AvailableFactoriesCount);
        }

        PlayerMenuController.HideFactoryMenu();

        base.UnselectCurrentFactory();
    }
    void EnterFactoryBuildMode(int factoryId)
    {
        if (SelectedFactory.GetFactoryCost(factoryId) > TotalBuildPoints)
            return;

        //Debug.Log("EnterFactoryBuildMode");

        CurrentInputMode = InputMode.FactoryPositioning;

        WantedFactoryId = factoryId;

        // Create factory preview

        // Load factory prefab for preview
        GameObject factoryPrefab = SelectedFactory.GetFactoryPrefab(factoryId);
        if (factoryPrefab == null)
        {
            Debug.LogWarning("Invalid factory prefab for factoryId " + factoryId);
        }
        WantedFactoryPreview = Instantiate(factoryPrefab.transform.GetChild(0).gameObject); // Quick and dirty access to mesh GameObject
        WantedFactoryPreview.name = WantedFactoryPreview.name.Replace("(Clone)", "_Preview");
        // Set transparency on materials
        foreach(Renderer rend in WantedFactoryPreview.GetComponentsInChildren<MeshRenderer>())
        {
            Material mat = rend.material;
            mat.shader = PreviewShader;
            Color col = mat.color;
            col.a = FactoryPreviewTransparency;
            mat.color = col;
        }

        // Project mouse position on ground to position factory preview
        ProjectFactoryPreviewOnFloor();
    }
    void ExitFactoryBuildMode()
    {
        CurrentInputMode = InputMode.Orders;
        Destroy(WantedFactoryPreview);
    }
    Vector3 ProjectFactoryPreviewOnFloor()
    {
        if (CurrentInputMode == InputMode.Orders)
        {
            Debug.LogWarning("Wrong call to ProjectFactoryPreviewOnFloor : CurrentInputMode = " + CurrentInputMode.ToString());
            return Vector3.zero;
        }

        Vector3 floorPos = Vector3.zero;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int floorMask = 1 << LayerMask.NameToLayer("Floor");
        RaycastHit raycastInfo;
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {
            floorPos = raycastInfo.point;
            WantedFactoryPreview.transform.position = floorPos;
        }
        return floorPos;
    }
    #endregion

    #region Entity targetting (attack / capture) and movement methods
    void ComputeUnitsAction()
    {
        if (SelectedUnitList.Count == 0)
            return;

        int damageableMask = (1 << LayerMask.NameToLayer("Unit")) | (1 << LayerMask.NameToLayer("Factory"));
        int targetMask = 1 << LayerMask.NameToLayer("Target");
        int floorMask = 1 << LayerMask.NameToLayer("Floor");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastInfo;

        // Set unit / factory attack target
        if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, damageableMask))
        {
            BaseEntity other = raycastInfo.transform.GetComponent<BaseEntity>();
            if (other != null)
            {
                if (other.GetTeam() != GetTeam())
                {
                    foreach (Unit unit in SelectedUnitList)
                        unit.SetAttackTarget(other);
                }
                else if (other.NeedsRepairing())
                {
                    foreach (Unit unit in SelectedUnitList)
                        unit.SetRepairTarget(other);
                }
            }
        }
        // Set capturing target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, targetMask))
        {
            TargetBuilding target = raycastInfo.transform.GetComponent<TargetBuilding>();
            if (target != null && target.GetTeam() != GetTeam())
            {
                // Direct call to capturing task $$$ to be improved by AI behaviour
                foreach (Unit unit in SelectedUnitList)
                    unit.SetCaptureTarget(target);
            }
        }
        // Set unit move target
        else if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorMask))
        {

            Vector3 newPos = raycastInfo.point;
            SetTargetCursorPosition(newPos);

            for (int i = 0; i < SelectedUnitList.Count; i++)
            {
                if(i == 0)
                {
                    SelectedUnitList[0].behaviorTree.stopTask = true;

                    SelectedUnitList[0].SetTargetPos(newPos);
                }
            }
        }
    }
    #endregion

    #region Camera methods
    void StartMoveCamera()
    {
        CanMoveCamera = true;
        CameraInputPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        CameraPrevInputPos = CameraInputPos;
    }
    void StopMoveCamera()
    {
        CanMoveCamera = false;
    }
    void UpdateMoveCamera()
    {
        if (CanMoveCamera)
        {
            CameraInputPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            CameraFrameMove = CameraPrevInputPos - CameraInputPos;
            TopCameraRef.MouseMove(CameraFrameMove);
            CameraPrevInputPos = CameraInputPos;
        }
    }
    #endregion
}
