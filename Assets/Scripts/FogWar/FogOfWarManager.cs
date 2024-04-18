using System;
using System.Collections;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    private PlayerController Controller;
	[SerializeField] public ETeam Team = ETeam.Red;
    [SerializeField]
    private FogOfWarSystem FOWSystem;

	public FogOfWarSystem GetFogOfWarSystem
	{
		get { return FOWSystem; }
	}

	[SerializeField]
	private float UpdateFrequency = 0.05f;

	private float LastUpdateDate = 0f;


    void Start()
    {
        Controller = FindObjectOfType<PlayerController>();
        FOWSystem.Init();
    }

    private void Update()
    {
		if ((Time.time - LastUpdateDate) > UpdateFrequency)
		{
			LastUpdateDate = Time.time;
			UpdateVisibilityTextures();
			UpdateFactoriesVisibility();
			UpdateUnitVisibility();
			UpdateBuildingVisibility();
		}
    }

	private void UpdateVisibilityTextures()
	{
		FOWSystem.ClearVisibility();
		FOWSystem.UpdateVisions(FindObjectsOfType<EntityVisibility>());
		FOWSystem.UpdateTextures(1 << (int)Team);
	}

	private void UpdateUnitVisibility()
	{
		foreach (Unit unit in GameServices.GetControllerByTeam(Team).UnitList)
		{
            if (unit.Visibility == null) { continue; }

            unit.Visibility.SetVisible(true);
		}

		foreach (Unit unit in GameServices.GetControllerByTeam(Team.GetOpponent()).UnitList)
		{
			if (unit.Visibility == null) { continue; }

			if (FOWSystem.IsVisible(1 << (int)Team, unit.Visibility.Position))
			{
				unit.Visibility.SetVisible(true);
			}
			else
			{
                unit.Visibility.SetVisible(false);
            }
        }
	}

	private void UpdateBuildingVisibility()
	{
		foreach (TargetBuilding building in GameServices.GetTargetBuildings())
		{
			if (building.Visibility == null) { continue; }

            if (FOWSystem.IsVisible(1 << (int)Team, building.Visibility.Position))
			{
				building.Visibility.SetVisibleUI(true);
			}
			else
			{
				building.Visibility.SetVisibleUI(false);
			}

			if (FOWSystem.WasVisible(1 << (int)Team, building.Visibility.Position))
			{
                building.Visibility.SetVisibleDefault(true);
            }
			else
			{
				building.Visibility.SetVisible(false);
            }
        }
	}

	private void UpdateFactoriesVisibility()
	{
		foreach (Factory factory in GameServices.GetControllerByTeam(Team).GetFactoryList)
		{
			factory.Visibility?.SetVisible(true);
		}

		foreach (Factory factory in GameServices.GetControllerByTeam(Team.GetOpponent()).GetFactoryList)
		{
			if (FOWSystem.IsVisible(1 << (int)Team, factory.Visibility.Position))
			{
				factory.Visibility.SetVisibleUI(true);
			}
			else
			{
                factory.Visibility.SetVisibleUI(false);
            }

            if (FOWSystem.WasVisible(1 << (int)Team, factory.Visibility.Position))
			{
                factory.Visibility.SetVisibleDefault(true);
            }
            else
			{
                factory.Visibility.SetVisible(false);
            }
        }
	}
}
