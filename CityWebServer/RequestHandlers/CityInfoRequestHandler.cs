﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CityWebServer.Extensibility;
using CityWebServer.Helpers;
using CityWebServer.Models;
using ColossalFramework;

namespace CityWebServer.RequestHandlers
{
    public class CityInfoRequestHandler : RequestHandlerBase
    {
        public override Guid HandlerID
        {
            get { return new Guid("eeada0d0-f1d2-43b0-9595-2a6a4d917631"); }
        }

        public override int Priority
        {
            get { return 100; }
        }

        public override String Name
        {
            get { return "City Info"; }
        }

        public override String Author
        {
            get { return "Rychard"; }
        }

        public override String MainPath
        {
            get { return "/CityInfo"; }
        }

        public override Boolean ShouldHandle(HttpListenerRequest request)
        {
            return (request.Url.AbsolutePath.Equals("/CityInfo", StringComparison.OrdinalIgnoreCase));
        }

        private Dictionary<int, int> GetBuildingBreakdownByDistrict()
        {
            var districtManager = Singleton<DistrictManager>.instance;

            Dictionary<int, int> districtBuildings = new Dictionary<int, int>();
            BuildingManager instance = Singleton<BuildingManager>.instance;
            foreach (Building building in instance.m_buildings.m_buffer)
            {
                if (building.m_flags == Building.Flags.None) { continue; }
                var districtID = (int)districtManager.GetDistrict(building.m_position);
                if (districtBuildings.ContainsKey(districtID))
                {
                    districtBuildings[districtID]++;
                }
                else
                {
                    districtBuildings.Add(districtID, 1);
                }
            }
            return districtBuildings;
        }

        private Dictionary<int, int> GetVehicleBreakdownByDistrict()
        {
            var districtManager = Singleton<DistrictManager>.instance;

            Dictionary<int, int> districtVehicles = new Dictionary<int, int>();
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            foreach (Vehicle vehicle in vehicleManager.m_vehicles.m_buffer)
            {
                if (vehicle.m_flags != Vehicle.Flags.None)
                {
                    var districtID = (int)districtManager.GetDistrict(vehicle.GetLastFramePosition());
                    if (districtVehicles.ContainsKey(districtID))
                    {
                        districtVehicles[districtID]++;
                    }
                    else
                    {
                        districtVehicles.Add(districtID, 1);
                    }
                }
            }
            return districtVehicles;
        }

        public override IResponseFormatter Handle(HttpListenerRequest request)
        {
            if (request.QueryString.HasKey("showList"))
            {
                return HandleDistrictList();
            }

            return HandleDistrict(request);
        }

        private IResponseFormatter HandleDistrictList()
        {
            var districtIDs = DistrictInfo.GetDistricts().ToArray();

            return JsonResponse(districtIDs);
        }

        private IResponseFormatter HandleDistrict(HttpListenerRequest request)
        {
            var districtIDs = GetDistrictsFromRequest(request);

            DistrictInfo globalDistrictInfo = null;
            List<DistrictInfo> districtInfoList = new List<DistrictInfo>();

            var buildings = GetBuildingBreakdownByDistrict();
            var vehicles = GetVehicleBreakdownByDistrict();

            foreach (var districtID in districtIDs)
            {
                var districtInfo = DistrictInfo.GetDistrictInfo(districtID);
                if (districtID == 0)
                {
                    districtInfo.TotalBuildingCount = buildings.Sum(obj => obj.Value);
                    districtInfo.TotalVehicleCount = vehicles.Sum(obj => obj.Value);
                    globalDistrictInfo = districtInfo;
                }
                else
                {
                    districtInfo.TotalBuildingCount = buildings.Where(obj => obj.Key == districtID).Sum(obj => obj.Value);
                    districtInfo.TotalVehicleCount = vehicles.Where(obj => obj.Key == districtID).Sum(obj => obj.Value);
                    districtInfoList.Add(districtInfo);    
                }
            }

            var simulationManager = Singleton<SimulationManager>.instance;
            
            var cityInfo = new CityInfo
            {
                Name = simulationManager.m_metaData.m_CityName,
                Time = simulationManager.m_currentGameTime.Date,
                GlobalDistrict = globalDistrictInfo,
                Districts = districtInfoList.ToArray(),
            };

            return JsonResponse(cityInfo);
        }

        private IEnumerable<int> GetDistrictsFromRequest(HttpListenerRequest request)
        {
            IEnumerable<int> districtIDs;
            if (request.QueryString.HasKey("districtID"))
            {
                List<int> districtIDList = new List<int>();
                var districtID = request.QueryString.GetInteger("districtID");
                if (districtID.HasValue)
                {
                    districtIDList.Add(districtID.Value);
                }
                districtIDs = districtIDList;
            }
            else
            {
                districtIDs = DistrictInfo.GetDistricts();
            }
            return districtIDs;
        }
    }
}