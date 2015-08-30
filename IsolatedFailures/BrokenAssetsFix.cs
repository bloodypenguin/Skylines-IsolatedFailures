
using System.Collections;
using System.Reflection;

using ColossalFramework.Threading;
using ColossalFramework.Globalization;

using UnityEngine;

namespace IsolatedFailures
{
    public class BrokenAssetsFix
    {
        public static void Init()
        {
            Debug.Log("BrokenAssetsFix");
            new EnumerableActionThread(FixEnumerableThread);
        }

        private static IEnumerator FixEnumerableThread(ThreadBase t)
        {
            SimulationManager.instance.ForcedSimulationPaused = true;

            try
            {
                uint brokenCount = 0;
                uint confusedCount = 0;

                // Fix broken offers
                TransferManager.TransferOffer[] incomingOffers = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];
                TransferManager.TransferOffer[] outgoingOffers = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];

                ushort[] incomingCount = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];
                ushort[] outgoingCount = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];

                int[] incomingAmount = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];
                int[] outgoingAmount = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];

                // Based on TransferManager.RemoveAllOffers
                for (int i = 0; i < 64; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        int num = i * 8 + j;
                        int num2 = (int)incomingCount[num];
                        for (int k = num2 - 1; k >= 0; k--)
                        {
                            int num3 = num * 256 + k;
                            if (IsInfoNull(incomingOffers[num3]))
                            {
                                incomingAmount[i] -= incomingOffers[num3].Amount;
                                incomingOffers[num3] = incomingOffers[--num2];
                                brokenCount++;
                            }
                        }
                        incomingCount[num] = (ushort)num2;
                        int num4 = (int)outgoingCount[num];
                        for (int l = num4 - 1; l >= 0; l--)
                        {
                            int num5 = num * 256 + l;
                            if (IsInfoNull(outgoingOffers[num5]))
                            {
                                outgoingAmount[i] -= outgoingOffers[num5].Amount;
                                outgoingOffers[num5] = outgoingOffers[--num4];
                                brokenCount++;
                            }
                        }
                        outgoingCount[num] = (ushort)num4;
                    }

                    yield return null;
                }

                if (brokenCount > 0) Debug.Log("Removed " + brokenCount + " broken transfer offers.");

                // Fix broken vehicles
                Array16<Vehicle> vehicles = VehicleManager.instance.m_vehicles;
                for (int i = 0; i < vehicles.m_size; i++)
                {
                    if (vehicles.m_buffer[i].m_flags != Vehicle.Flags.None)
                    {
                        bool exists = (vehicles.m_buffer[i].m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None;

                        // Vehicle validity
                        InstanceID target;
                        bool isInfoNull = vehicles.m_buffer[i].Info == null;
                        bool isLeading = vehicles.m_buffer[i].m_leadingVehicle == 0;
                        bool isWaiting = !exists && (vehicles.m_buffer[i].m_flags & Vehicle.Flags.WaitingSpace) != Vehicle.Flags.None;
                        bool isConfused = exists && isLeading && !isInfoNull && vehicles.m_buffer[i].Info.m_vehicleAI.GetLocalizedStatus((ushort)i, ref vehicles.m_buffer[i], out target) == Locale.Get("VEHICLE_STATUS_CONFUSED");

                        if (isInfoNull || isWaiting || isConfused)
                        {
                            try
                            {
                                VehicleManager.instance.ReleaseVehicle((ushort)i);
                                if (isInfoNull) brokenCount++;
                                if (isConfused) confusedCount++;
                            }
                            catch { }
                        }
                    }
                    if (i % 256 == 255) yield return null;
                }

                if (confusedCount > 0) Debug.Log("Removed " + confusedCount + " confused vehicle instances.");

                Array16<VehicleParked> vehiclesParked = VehicleManager.instance.m_parkedVehicles;
                for (int i = 0; i < vehiclesParked.m_size; i++)
                {
                    if (vehiclesParked.m_buffer[i].Info == null)
                    {
                        try
                        {
                            VehicleManager.instance.ReleaseParkedVehicle((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return null;
                }

                if (brokenCount > 0) Debug.Log("Removed " + brokenCount + " broken vehicle instances.");
                brokenCount = 0;

                // Fix broken buildings
                Array16<Building> buildings = BuildingManager.instance.m_buildings;
                for (int i = 0; i < buildings.m_size; i++)
                {
                    if (buildings.m_buffer[i].Info == null)
                    {
                        try
                        {
                            BuildingManager.instance.ReleaseBuilding((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return null;
                }

                if (brokenCount > 0) Debug.Log("Removed " + brokenCount + " broken building instances.");
                brokenCount = 0;

                // Fix broken props
                Array16<PropInstance> props = PropManager.instance.m_props;
                for (int i = 0; i < props.m_size; i++)
                {
                    if (props.m_buffer[i].Info == null)
                    {
                        try
                        {
                            PropManager.instance.ReleaseProp((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return null;
                }

                if (brokenCount > 0) Debug.Log("Removed " + brokenCount + " broken prop instances.");
                brokenCount = 0;

                // Fix broken trees
                Array32<TreeInstance> trees = TreeManager.instance.m_trees;
                for (int i = 0; i < trees.m_size; i++)
                {
                    if (trees.m_buffer[i].Info == null)
                    {
                        try
                        {
                            TreeManager.instance.ReleaseTree((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return null;
                }

                if (brokenCount > 0) Debug.Log("Removed " + brokenCount + " broken tree instances.");
                brokenCount = 0;
            }
            finally
            {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }
        }

        private static bool IsInfoNull(TransferManager.TransferOffer offer)
        {
            if (!offer.Active) return false;

            if (offer.Vehicle != 0)
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].Info == null;

            if (offer.Building != 0)
                return BuildingManager.instance.m_buildings.m_buffer[offer.Building].Info == null;

            return false;
        }
    }
}
