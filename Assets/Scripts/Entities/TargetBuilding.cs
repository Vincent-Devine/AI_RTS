using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class TargetBuilding : MonoBehaviour
{
    [SerializeField]
    float CaptureGaugeStart = 100f;
    [SerializeField]
    float CaptureGaugeSpeed = 1f;
    [SerializeField]
    int BuildPoints = 5;
    [SerializeField]
    Material BlueTeamMaterial = null;
    [SerializeField]
    Material RedTeamMaterial = null;

    [SerializeField]
    public float influenceStrenght = 10f;

    [SerializeField]
    public float influenceRadius = 7f;

    [SerializeField] Text pointCanva;

    Material NeutralMaterial = null;
    MeshRenderer BuildingMeshRenderer = null;
    Image GaugeImage;
    Image MinimapImage;

    int[] TeamScore;
    float CaptureGaugeValue;
    ETeam OwningTeam = ETeam.Neutral;
    ETeam CapturingTeam = ETeam.Neutral;
    public ETeam GetTeam() { return OwningTeam; }

    private EntityVisibility _Visibility;

    private bool recharged = true;
    public EntityVisibility Visibility
    {
        get
        {
            if (_Visibility == null)
            {
                _Visibility = GetComponent<EntityVisibility>();
            }
            return _Visibility;
        }
    }


    #region MonoBehaviour methods
    void Start()
    {
        BuildingMeshRenderer = GetComponentInChildren<MeshRenderer>();
        NeutralMaterial = BuildingMeshRenderer.material;

        GaugeImage = GetComponentInChildren<Image>();
        if (GaugeImage)
            GaugeImage.fillAmount = 0f;
        CaptureGaugeValue = CaptureGaugeStart;
        TeamScore = new int[2];
        TeamScore[0] = 0;
        TeamScore[1] = 0;

        Transform minimapTransform = transform.Find("MinimapCanvas");
        if (minimapTransform != null)
            MinimapImage = minimapTransform.GetComponentInChildren<Image>();
    }
    void Update()
    {
        if (OwningTeam != ETeam.Neutral && recharged)
        {
            recharged = false;
            UnitController teamController = GameServices.GetControllerByTeam(OwningTeam);
            if (teamController != null)
            {
                teamController._TotalBuildPoints ++;

                // if(pointCanva != null)
                //     pointCanva.text = "Build Points : " + teamController._TotalBuildPoints.ToString();
            }

            StartCoroutine(Timer(4));
        }

        if (CapturingTeam == OwningTeam || CapturingTeam == ETeam.Neutral)
            return;

        CaptureGaugeValue -= TeamScore[(int)CapturingTeam] * CaptureGaugeSpeed * Time.deltaTime;

        GaugeImage.fillAmount = 1f - CaptureGaugeValue / CaptureGaugeStart;

        if (CaptureGaugeValue <= 0f)
        {
            CaptureGaugeValue = 0f;
            OnCaptured(CapturingTeam);
        }

    }
    #endregion

    IEnumerator Timer(float secondToWait)
    {
        yield return new WaitForSeconds(secondToWait);
        recharged = true;
    }

    #region Capture methods
    public void StartCapture(Unit unit)
    {
        if (unit == null)
            return;

        TeamScore[(int)unit.GetTeam()] += unit.Cost;

        if (CapturingTeam == ETeam.Neutral)
        {
            CapturingTeam = unit.GetTeam();
            GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            //if (TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] == 0)
            //{
            //}
        }
        else
        {
            if (TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] > 0)
                ResetCapture();
        }
    }

    public void StartCaptureThisBuilding(Unit unit, TargetBuilding buildingToBeCaptured)
    {
        if (unit == null)
            return;

        buildingToBeCaptured.TeamScore[(int)unit.GetTeam()] += unit.Cost;

        if (buildingToBeCaptured.CapturingTeam == ETeam.Neutral)
        {
            if (buildingToBeCaptured.TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] == 0)
            {
                buildingToBeCaptured.CapturingTeam = unit.GetTeam();
                buildingToBeCaptured.GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
        else
        {
            if (buildingToBeCaptured.TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] > 0)
                ResetCapture();
        }
    }
    public void StopCapture(Unit unit)
    {
        if (unit == null)
            return;

        TeamScore[(int)unit.GetTeam()] -= unit.Cost;
        if (TeamScore[(int)unit.GetTeam()] == 0)
        {
            ETeam opponentTeam = GameServices.GetOpponent(unit.GetTeam());
            if (TeamScore[(int)opponentTeam] == 0)
            {
                ResetCapture();
            }
            else
            {
                CapturingTeam = opponentTeam;
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
    }
    void ResetCapture()
    {
        CaptureGaugeValue = CaptureGaugeStart;
        CapturingTeam = ETeam.Neutral;
        GaugeImage.fillAmount = 0f;
    }
    void OnCaptured(ETeam newTeam)
    {
        Debug.Log("target captured by " + newTeam.ToString());
        if (OwningTeam != newTeam)
        {
            UnitController teamController = GameServices.GetControllerByTeam(newTeam);
            if (teamController != null)
                teamController.CaptureTarget(BuildPoints);

            if (OwningTeam != ETeam.Neutral)
            {
                // remove points to previously owning team
                teamController = GameServices.GetControllerByTeam(OwningTeam);
                if (teamController != null)
                    teamController.LoseTarget(BuildPoints);
            }
        }

        ResetCapture();
        OwningTeam = newTeam;

        //Mod resource map HERE
        ResourcesMap.Instance.AddToCapturedRessources(newTeam, this);

        if (Visibility) { Visibility.Team = OwningTeam; }
        if (MinimapImage) { MinimapImage.color = GameServices.GetTeamColor(OwningTeam); }
        BuildingMeshRenderer.material = newTeam == ETeam.Blue ? BlueTeamMaterial : RedTeamMaterial;

    }
    #endregion
}
