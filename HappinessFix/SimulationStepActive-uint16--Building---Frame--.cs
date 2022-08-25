// CommercialBuildingAI
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

protected override void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
{
	DistrictManager instance = Singleton<DistrictManager>.instance;
	byte district = instance.GetDistrict(buildingData.m_position);
	DistrictPolicies.Services servicePolicies = instance.m_districts.m_buffer[district].m_servicePolicies;
	DistrictPolicies.Taxation taxationPolicies = instance.m_districts.m_buffer[district].m_taxationPolicies;
	DistrictPolicies.CityPlanning cityPlanningPolicies = instance.m_districts.m_buffer[district].m_cityPlanningPolicies;
	instance.m_districts.m_buffer[district].m_servicePoliciesEffect |= servicePolicies & (DistrictPolicies.Services.PowerSaving | DistrictPolicies.Services.WaterSaving | DistrictPolicies.Services.SmokeDetectors | DistrictPolicies.Services.Recycling | DistrictPolicies.Services.RecreationalUse | DistrictPolicies.Services.ExtraInsulation | DistrictPolicies.Services.NoElectricity | DistrictPolicies.Services.OnlyElectricity | DistrictPolicies.Services.FreeWifi);
	switch (m_info.m_class.m_subService)
	{
	case ItemClass.SubService.CommercialLow:
		if ((taxationPolicies & (DistrictPolicies.Taxation.TaxRaiseComLow | DistrictPolicies.Taxation.TaxLowerComLow)) != (DistrictPolicies.Taxation.TaxRaiseComLow | DistrictPolicies.Taxation.TaxLowerComLow))
		{
			instance.m_districts.m_buffer[district].m_taxationPoliciesEffect |= taxationPolicies & (DistrictPolicies.Taxation.TaxRaiseComLow | DistrictPolicies.Taxation.TaxLowerComLow);
		}
		instance.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= cityPlanningPolicies & (DistrictPolicies.CityPlanning.SmallBusiness | DistrictPolicies.CityPlanning.LightningRods);
		break;
	case ItemClass.SubService.CommercialHigh:
		if ((taxationPolicies & (DistrictPolicies.Taxation.TaxRaiseComHigh | DistrictPolicies.Taxation.TaxLowerComHigh)) != (DistrictPolicies.Taxation.TaxRaiseComHigh | DistrictPolicies.Taxation.TaxLowerComHigh))
		{
			instance.m_districts.m_buffer[district].m_taxationPoliciesEffect |= taxationPolicies & (DistrictPolicies.Taxation.TaxRaiseComHigh | DistrictPolicies.Taxation.TaxLowerComHigh);
		}
		instance.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= cityPlanningPolicies & (DistrictPolicies.CityPlanning.BigBusiness | DistrictPolicies.CityPlanning.LightningRods);
		break;
	case ItemClass.SubService.CommercialLeisure:
		instance.m_districts.m_buffer[district].m_taxationPoliciesEffect |= taxationPolicies & DistrictPolicies.Taxation.DontTaxLeisure;
		instance.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= cityPlanningPolicies & (DistrictPolicies.CityPlanning.NoLoudNoises | DistrictPolicies.CityPlanning.LightningRods);
		break;
	case ItemClass.SubService.CommercialTourist:
		instance.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= cityPlanningPolicies & DistrictPolicies.CityPlanning.LightningRods;
		break;
	case ItemClass.SubService.CommercialEco:
		instance.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= cityPlanningPolicies & DistrictPolicies.CityPlanning.LightningRods;
		break;
	}
	Citizen.BehaviourData behaviour = default(Citizen.BehaviourData);
	int aliveWorkerCount = 0;
	int totalWorkerCount = 0;
	int workPlaceCount = 0;
	int num = HandleWorkers(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount, ref workPlaceCount);
	if ((buildingData.m_flags & Building.Flags.Evacuating) != 0)
	{
		num = 0;
	}
	int width = buildingData.Width;
	int length = buildingData.Length;
	int num2 = MaxIncomingLoadSize();
	int aliveCount = 0;
	int totalCount = 0;
	GetVisitBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveCount, ref totalCount);
	int num3 = CalculateVisitplaceCount((ItemClass.Level)buildingData.m_level, new Randomizer(buildingID), width, length);
	int num4 = Mathf.Max(0, num3 - totalCount);
	int num5 = num3 * 500;
	int num6 = Mathf.Max(num5, num2 * 4);
	int num7 = CalculateProductionCapacity((ItemClass.Level)buildingData.m_level, new Randomizer(buildingID), width, length);
	TransferManager.TransferReason incomingTransferReason = GetIncomingTransferReason();
	TransferManager.TransferReason outgoingTransferReason = GetOutgoingTransferReason(buildingID);
	if (incomingTransferReason != TransferManager.TransferReason.None && num != 0 && num7 != 0)
	{
		int num8 = num6 - buildingData.m_customBuffer1;
		int num9 = Mathf.Max(0, Mathf.Min(num, (num8 * 200 + num6 - 1) / num6));
		int a = (num7 * num9 + 9) / 10;
		a = Mathf.Max(0, Mathf.Min(a, num8));
		buildingData.m_customBuffer1 += (ushort)a;
	}
	if (num != 0)
	{
		int num10 = num6;
		if (incomingTransferReason != TransferManager.TransferReason.None)
		{
			num10 = Mathf.Min(num10, buildingData.m_customBuffer1);
		}
		if (outgoingTransferReason != TransferManager.TransferReason.None)
		{
			num10 = Mathf.Min(num10, num6 - buildingData.m_customBuffer2);
		}
		num = Mathf.Max(0, Mathf.Min(num, (num10 * 200 + num6 - 1) / num6));
		int num11 = (num3 * num + 9) / 10;
		if (Singleton<SimulationManager>.instance.m_isNightTime)
		{
			num11 = num11 + 1 >> 1;
		}
		num11 = Mathf.Max(0, Mathf.Min(num11, num10));
		if (incomingTransferReason != TransferManager.TransferReason.None)
		{
			buildingData.m_customBuffer1 -= (ushort)num11;
		}
		if (outgoingTransferReason != TransferManager.TransferReason.None)
		{
			buildingData.m_customBuffer2 += (ushort)num11;
		}
		num = (num11 + 9) / 10;
	}
	GetConsumptionRates((ItemClass.Level)buildingData.m_level, new Randomizer(buildingID), num, out var electricityConsumption, out var waterConsumption, out var sewageAccumulation, out var garbageAccumulation, out var incomeAccumulation, out var mailAccumulation);
	int heatingConsumption = 0;
	if (electricityConsumption != 0 && instance.IsPolicyLoaded(DistrictPolicies.Policies.ExtraInsulation))
	{
		if ((servicePolicies & DistrictPolicies.Services.ExtraInsulation) != 0)
		{
			heatingConsumption = Mathf.Max(1, electricityConsumption * 3 + 8 >> 4);
			incomeAccumulation = incomeAccumulation * 95 / 100;
		}
		else
		{
			heatingConsumption = Mathf.Max(1, electricityConsumption + 2 >> 2);
		}
	}
	if (garbageAccumulation != 0 && (servicePolicies & DistrictPolicies.Services.Recycling) != 0)
	{
		garbageAccumulation = Mathf.Max(1, garbageAccumulation * 85 / 100);
		incomeAccumulation = incomeAccumulation * 95 / 100;
	}
	int taxRate = GetTaxRate(buildingID, ref buildingData, taxationPolicies);
	if (m_info.m_class.m_subService == ItemClass.SubService.CommercialLeisure && (cityPlanningPolicies & DistrictPolicies.CityPlanning.NoLoudNoises) != 0 && Singleton<SimulationManager>.instance.m_isNightTime)
	{
		electricityConsumption = electricityConsumption + 1 >> 1;
		waterConsumption = waterConsumption + 1 >> 1;
		sewageAccumulation = sewageAccumulation + 1 >> 1;
		garbageAccumulation = garbageAccumulation + 1 >> 1;
		mailAccumulation = mailAccumulation + 1 >> 1;
		incomeAccumulation = 0;
	}
	if (num != 0)
	{
		int maxMail = workPlaceCount * 50;
		int num12 = HandleCommonConsumption(buildingID, ref buildingData, ref frameData, ref electricityConsumption, ref heatingConsumption, ref waterConsumption, ref sewageAccumulation, ref garbageAccumulation, ref mailAccumulation, maxMail, servicePolicies);
		num = (num * num12 + 99) / 100;
		if (num != 0)
		{
			int num13 = incomeAccumulation;
			if (num13 != 0)
			{
				if (m_info.m_class.m_subService == ItemClass.SubService.CommercialLow)
				{
					if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.SmallBusiness) != 0)
					{
						Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 12, m_info.m_class);
						num13 *= 2;
					}
				}
				else if (m_info.m_class.m_subService == ItemClass.SubService.CommercialHigh && (cityPlanningPolicies & DistrictPolicies.CityPlanning.BigBusiness) != 0)
				{
					Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 25, m_info.m_class);
					num13 *= 3;
				}
				if ((servicePolicies & DistrictPolicies.Services.RecreationalUse) != 0)
				{
					num13 = (num13 * 105 + 99) / 100;
				}
				int factor = Singleton<BuildingManager>.instance.m_finalMonumentEffect[7].m_factor;
				if (factor != 0)
				{
					Vector3 position = Singleton<BuildingManager>.instance.m_finalMonumentEffect[7].m_position;
					if (VectorUtils.LengthSqrXZ(position - buildingData.m_position) < 250000f)
					{
						num13 += (num13 * factor + 50) / 100;
					}
				}
				num13 = Singleton<EconomyManager>.instance.AddPrivateIncome(num13, ItemClass.Service.Commercial, m_info.m_class.m_subService, (ItemClass.Level)buildingData.m_level, taxRate);
				int num14 = (behaviour.m_touristCount * num13 + (aliveCount >> 1)) / Mathf.Max(1, aliveCount);
				int num15 = Mathf.Max(0, num13 - num14);
				if (num15 != 0)
				{
					Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.CitizenIncome, num15, m_info.m_class);
				}
				if (num14 != 0)
				{
					Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.TourismIncome, num14, m_info.m_class);
				}
			}
			GetPollutionRates((ItemClass.Level)buildingData.m_level, num, cityPlanningPolicies, out var groundPollution, out var noisePollution);
			if (groundPollution != 0 && Singleton<SimulationManager>.instance.m_randomizer.Int32(3u) == 0)
			{
				Singleton<NaturalResourceManager>.instance.TryDumpResource(NaturalResourceManager.Resource.Pollution, groundPollution, groundPollution, buildingData.m_position, 60f);
			}
			if (noisePollution != 0)
			{
				Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, noisePollution, buildingData.m_position, 60f);
			}
			if (num12 < 100)
			{
				buildingData.m_flags |= Building.Flags.RateReduced;
			}
			else
			{
				buildingData.m_flags &= ~Building.Flags.RateReduced;
			}
			buildingData.m_flags |= Building.Flags.Active;
		}
		else
		{
			buildingData.m_flags &= ~(Building.Flags.RateReduced | Building.Flags.Active);
		}
	}
	else
	{
		electricityConsumption = 0;
		heatingConsumption = 0;
		waterConsumption = 0;
		sewageAccumulation = 0;
		garbageAccumulation = 0;
		mailAccumulation = 0;
		buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.Electricity | Notification.Problem.Water | Notification.Problem.Sewage | Notification.Problem.Flood | Notification.Problem.Heating);
		buildingData.m_flags &= ~(Building.Flags.RateReduced | Building.Flags.Active);
	}
	int num16 = 0;
	int wellbeing = 0;
	float radius = (float)(buildingData.Width + buildingData.Length) * 2.5f;
	if (behaviour.m_healthAccumulation != 0)
	{
		if (aliveWorkerCount + aliveCount != 0)
		{
			num16 = (behaviour.m_healthAccumulation + (aliveWorkerCount + aliveCount >> 1)) / (aliveWorkerCount + aliveCount);
		}
		Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Health, behaviour.m_healthAccumulation, buildingData.m_position, radius);
	}
	if (behaviour.m_wellbeingAccumulation != 0)
	{
		if (aliveWorkerCount + aliveCount != 0)
		{
			wellbeing = (behaviour.m_wellbeingAccumulation + (aliveWorkerCount + aliveCount >> 1)) / (aliveWorkerCount + aliveCount);
		}
		Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Wellbeing, behaviour.m_wellbeingAccumulation, buildingData.m_position, radius);
	}
	int num17 = Citizen.GetHappiness(num16, wellbeing) * 15 / 100;
	int num18 = aliveWorkerCount * 20 / workPlaceCount;
	if ((buildingData.m_problems & Notification.Problem.MajorProblem) == 0)
	{
		num17 += 20;
	}
	if (buildingData.m_problems == Notification.Problem.None)
	{
		num17 += 25;
	}
	num17 += Mathf.Min(num18, buildingData.m_customBuffer1 * num18 / num6);
	num17 += num18 - Mathf.Min(num18, buildingData.m_customBuffer2 * num18 / num6);
	int num19 = 8 - buildingData.m_level;
	int num20 = 11 - buildingData.m_level;
	if (m_info.m_class.m_subService == ItemClass.SubService.CommercialHigh)
	{
		num19++;
		num20++;
	}
	if (taxRate < num19)
	{
		num17 += num19 - taxRate;
	}
	if (taxRate > num20)
	{
		num17 -= taxRate - num20;
	}
	if (taxRate >= num20 + 4)
	{
		if (buildingData.m_taxProblemTimer != 0 || Singleton<SimulationManager>.instance.m_randomizer.Int32(32u) == 0)
		{
			int num21 = taxRate - num20 >> 2;
			buildingData.m_taxProblemTimer = (byte)Mathf.Min(255, buildingData.m_taxProblemTimer + num21);
			if (buildingData.m_taxProblemTimer >= 96)
			{
				buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.TaxesTooHigh | Notification.Problem.MajorProblem);
			}
			else if (buildingData.m_taxProblemTimer >= 32)
			{
				buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.TaxesTooHigh);
			}
		}
	}
	else
	{
		buildingData.m_taxProblemTimer = (byte)Mathf.Max(0, buildingData.m_taxProblemTimer - 1);
		buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.TaxesTooHigh);
	}
	GetAccumulation(new Randomizer(buildingID), num, taxRate, cityPlanningPolicies, taxationPolicies, out var entertainment, out var attractiveness);
	if (entertainment != 0)
	{
		Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Entertainment, entertainment, buildingData.m_position, radius);
	}
	if (attractiveness != 0)
	{
		Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Attractiveness, attractiveness);
	}
	num17 = Mathf.Clamp(num17, 0, 100);
	buildingData.m_health = (byte)num16;
	buildingData.m_happiness = (byte)num17;
	buildingData.m_citizenCount = (byte)(aliveWorkerCount + aliveCount);
	HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount + totalCount);
	int num22 = behaviour.m_crimeAccumulation / 10;
	if (m_info.m_class.m_subService == ItemClass.SubService.CommercialLeisure)
	{
		num22 = num22 * 5 + 3 >> 2;
	}
	if ((servicePolicies & DistrictPolicies.Services.RecreationalUse) != 0)
	{
		num22 = num22 * 3 + 3 >> 2;
	}
	HandleCrime(buildingID, ref buildingData, num22, buildingData.m_citizenCount);
	int crimeBuffer = buildingData.m_crimeBuffer;
	if (aliveWorkerCount != 0)
	{
		Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Density, aliveWorkerCount, buildingData.m_position, radius);
		int num23 = behaviour.m_educated0Count * 100 + behaviour.m_educated1Count * 50 + behaviour.m_educated2Count * 30;
		num23 = num23 / aliveWorkerCount + 50;
		buildingData.m_fireHazard = (byte)num23;
	}
	else
	{
		buildingData.m_fireHazard = 0;
	}
	crimeBuffer = ((buildingData.m_citizenCount != 0) ? ((crimeBuffer + (buildingData.m_citizenCount >> 1)) / (int)buildingData.m_citizenCount) : 0);
	int count = 0;
	int cargo = 0;
	int capacity = 0;
	int outside = 0;
	if (incomingTransferReason != TransferManager.TransferReason.None)
	{
		if (incomingTransferReason == TransferManager.TransferReason.Goods || incomingTransferReason == TransferManager.TransferReason.Food)
		{
			CalculateGuestVehicles(buildingID, ref buildingData, incomingTransferReason, TransferManager.TransferReason.LuxuryProducts, ref count, ref cargo, ref capacity, ref outside);
		}
		else
		{
			CalculateGuestVehicles(buildingID, ref buildingData, incomingTransferReason, ref count, ref cargo, ref capacity, ref outside);
		}
		buildingData.m_tempImport = (byte)Mathf.Clamp(outside, buildingData.m_tempImport, 255);
	}
	buildingData.m_tempExport = (byte)Mathf.Clamp(behaviour.m_touristCount, buildingData.m_tempExport, 255);
	buildingData.m_adults = (byte)num;
	SimulationManager instance2 = Singleton<SimulationManager>.instance;
	uint num24 = (instance2.m_currentFrameIndex & 0xF00) >> 8;
	if (num24 == (buildingID & 0xF) && (m_info.m_class.m_subService == ItemClass.SubService.CommercialLow || m_info.m_class.m_subService == ItemClass.SubService.CommercialHigh) && Singleton<ZoneManager>.instance.m_lastBuildIndex == instance2.m_currentBuildIndex && (buildingData.m_flags & Building.Flags.Upgrading) == 0)
	{
		CheckBuildingLevel(buildingID, ref buildingData, ref frameData, ref behaviour, aliveCount);
	}
	if ((buildingData.m_flags & (Building.Flags.Completed | Building.Flags.Upgrading)) == 0)
	{
		return;
	}
	Notification.Problem problem = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.NoCustomers | Notification.Problem.NoGoods);
	if (buildingData.m_customBuffer2 > num6 - (num5 >> 1) && aliveCount <= num3 >> 1)
	{
		buildingData.m_outgoingProblemTimer = (byte)Mathf.Min(255, buildingData.m_outgoingProblemTimer + 1);
		if (buildingData.m_outgoingProblemTimer >= 192)
		{
			problem = Notification.AddProblems(problem, Notification.Problem.NoCustomers | Notification.Problem.MajorProblem);
		}
		else if (buildingData.m_outgoingProblemTimer >= 128)
		{
			problem = Notification.AddProblems(problem, Notification.Problem.NoCustomers);
		}
	}
	else
	{
		buildingData.m_outgoingProblemTimer = 0;
	}
	if (buildingData.m_customBuffer1 == 0 && incomingTransferReason != TransferManager.TransferReason.None)
	{
		buildingData.m_incomingProblemTimer = (byte)Mathf.Min(255, buildingData.m_incomingProblemTimer + 1);
		problem = ((buildingData.m_incomingProblemTimer >= 64) ? Notification.AddProblems(problem, Notification.Problem.NoGoods | Notification.Problem.MajorProblem) : Notification.AddProblems(problem, Notification.Problem.NoGoods));
	}
	else
	{
		buildingData.m_incomingProblemTimer = 0;
	}
	buildingData.m_problems = problem;
	instance.m_districts.m_buffer[district].AddCommercialData(ref behaviour, num16, num17, crimeBuffer, workPlaceCount, aliveWorkerCount, Mathf.Max(0, workPlaceCount - totalWorkerCount), num3, aliveCount, num4, buildingData.m_level, electricityConsumption, heatingConsumption, waterConsumption, sewageAccumulation, garbageAccumulation, incomeAccumulation, Mathf.Min(100, (int)buildingData.m_garbageBuffer / 50), buildingData.m_waterPollution * 100 / 255, buildingData.m_finalImport, buildingData.m_finalExport, m_info.m_class.m_subService);
	if (buildingData.m_fireIntensity == 0 && incomingTransferReason != TransferManager.TransferReason.None)
	{
		int num25 = num6 - buildingData.m_customBuffer1 - capacity;
		num25 -= num2 >> 1;
		if (num25 >= 0)
		{
			TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
			offer.Priority = num25 * 8 / num2;
			offer.Building = buildingID;
			offer.Position = buildingData.m_position;
			offer.Amount = 1;
			offer.Active = false;
			if ((incomingTransferReason == TransferManager.TransferReason.Goods || incomingTransferReason == TransferManager.TransferReason.Food) && (instance2.m_currentFrameIndex & 0x300) >> 8 == (buildingID & 3))
			{
				Singleton<TransferManager>.instance.AddIncomingOffer(TransferManager.TransferReason.LuxuryProducts, offer);
			}
			else
			{
				Singleton<TransferManager>.instance.AddIncomingOffer(incomingTransferReason, offer);
			}
		}
	}
	if (buildingData.m_fireIntensity == 0 && outgoingTransferReason != TransferManager.TransferReason.None)
	{
		int num26 = buildingData.m_customBuffer2 - aliveCount * 100;
		if (num26 >= 100 && num4 > 0)
		{
			TransferManager.TransferOffer offer2 = default(TransferManager.TransferOffer);
			offer2.Priority = Mathf.Max(1, num26 * 8 / num6);
			offer2.Building = buildingID;
			offer2.Position = buildingData.m_position;
			offer2.Amount = Mathf.Min(num26 / 100, num4);
			offer2.Active = false;
			Singleton<TransferManager>.instance.AddOutgoingOffer(outgoingTransferReason, offer2);
		}
	}
	base.SimulationStepActive(buildingID, ref buildingData, ref frameData);
	HandleFire(buildingID, ref buildingData, ref frameData, servicePolicies);
}
