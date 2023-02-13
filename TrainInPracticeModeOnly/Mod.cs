using Kitchen;
using KitchenMods;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Generic;

namespace TrainInPracticeModeOnly
{
    public class TrainInPracticeModeOnly : GenericSystemBase, IModSystem
    {
        private static int SmartGrabberItemId = -1238047163;

        // Query that gets all cAppliance
        private EntityQuery Appliances;

        // SpecificType is an item ID on the CConveyPushItems component that represents the item the smart grabber is trained to grab.
        // Both SpecificType and SpecificComponents MUST be set in order to fully CHANGE the trained item of a grabber.
        private Dictionary<CAppliance, (int SpecificType, KitchenData.ItemList SpecificComponents)> GrabberToTrainedItemInfo;

        protected override void Initialise()
        {
            base.Initialise();
            Appliances = GetEntityQuery(new QueryHelper()
                    .All(typeof(CAppliance)));
            GrabberToTrainedItemInfo = new Dictionary<CAppliance, (int SpecificType, KitchenData.ItemList SpecificComponents)>();
        }

        private void Log(string message)
        {
            Debug.Log($"TrainInPracticeModeOnly: {message}");
        }


        // Ensure that each SmartGrabber is trained to the item that it was trained to when we last locked the trained items.
        private void UpdateTrainedItems(bool isLocked)
        {
            var appliances = Appliances.ToEntityArray(Allocator.TempJob);
            foreach (var appliance in appliances)
            {
                // Ensure this is a smart grabber
                if (Require(appliance, out CAppliance cAppliance) && cAppliance.ID.Equals(SmartGrabberItemId))
                {
                    var pushItems = EntityManager.GetComponentData<CConveyPushItems>(appliance);

                    // Override the current trained item with locked item when they are different, i.e. do not allow the user to update the trained item.
                    if (isLocked)
                    {
                        if (GrabberToTrainedItemInfo.ContainsKey(cAppliance) && !GrabberToTrainedItemInfo[cAppliance].SpecificType.Equals(pushItems.SpecificType))
                        {
                            Log($"TrainOnlyInSetup: Updating trained item  {pushItems.SpecificType} to {GrabberToTrainedItemInfo[cAppliance].SpecificType}");
                            pushItems.SpecificType = GrabberToTrainedItemInfo[cAppliance].SpecificType;
                            pushItems.SpecificComponents = GrabberToTrainedItemInfo[cAppliance].SpecificComponents;
                            EntityManager.SetComponentData<CConveyPushItems>(appliance, pushItems);
                        }
                    }
                    // Keep the dictionary in sync with the current trained item, i.e. allow the user to update the trained item.
                    else
                    {
                        if (!GrabberToTrainedItemInfo.ContainsKey(cAppliance) || !GrabberToTrainedItemInfo[cAppliance].SpecificType.Equals(pushItems.SpecificType))
                        {
                            Log($"TrainOnlyInSetup: Locked Smart Grabber trained item to {pushItems.SpecificType}");
                            GrabberToTrainedItemInfo[cAppliance] = (SpecificType: pushItems.SpecificType, SpecificComponents: pushItems.SpecificComponents);
                        }
                    }
                }
            }
            appliances.Dispose();
        }

        protected override void OnUpdate()
        {
            if (Has<SIsDayTime>() && !Has<SPracticeMode>())
            {
                // On the first frame, lock in the trained items.
                if (Has<SIsDayFirstUpdate>())
                {
                    GrabberToTrainedItemInfo.Clear();
                    UpdateTrainedItems(isLocked: false);
                }
                // Every other frame, ensure the smart grabbers are always set to the locked in items.
                else
                {
                    UpdateTrainedItems(isLocked: true);
                }
            }


            //// Only unlock trained items in practice mode
            //if (GameInfo.IsPreparationTime || Has<SPracticeMode>())
            //{
            //    UpdateTrainedItems(isLocked: false);
            //}
            //// This condition is only true during the actual game day
            //else
            //{
            //    UpdateTrainedItems(isLocked: true);
            //}
        }
    }
}
