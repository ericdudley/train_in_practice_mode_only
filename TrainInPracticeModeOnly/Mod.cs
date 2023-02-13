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

        // Maps SmartGrabber CAppliance's to the item ID that they are trained to
        private Dictionary<CAppliance, int> GrabberToTrainedItemId;

        protected override void Initialise()
        {
            base.Initialise();
            Appliances = GetEntityQuery(new QueryHelper()
                    .All(typeof(CAppliance)));
            GrabberToTrainedItemId = new Dictionary<CAppliance, int>();
        }

        
        // Ensure that each SmartGrabber is trained to the item that it was trained to when we last locked the trained items.
        private void UpdateTrainedItems(bool isLocked)
        {
            var appliances = Appliances.ToEntityArray(Allocator.TempJob);
            foreach (var appliance in appliances)
            {

                // Ensure this is an appliance
                if (!Require(appliance, out CAppliance cAppliance))
                {
                    continue;
                }

                // Ensure this is a smart grabber
                if (cAppliance.ID.Equals(SmartGrabberItemId))
                {
                    var pushItems = EntityManager.GetComponentData<CConveyPushItems>(appliance);

                    // Override the current trained item with locked item when they are different, i.e. do not allow the user to update the trained item.
                    if (isLocked)
                    {
                        if (GrabberToTrainedItemId.ContainsKey(cAppliance) && !GrabberToTrainedItemId[cAppliance].Equals(pushItems.SpecificType))
                        {
                            Debug.Log($"TrainOnlyInSetup: Updating trained item  {pushItems.SpecificType} to {GrabberToTrainedItemId[cAppliance]}");
                            pushItems.SpecificType = GrabberToTrainedItemId[cAppliance];
                            EntityManager.SetComponentData<CConveyPushItems>(appliance, pushItems);
                        }
                    }
                    // Keep the dictionary in sync with the current trained item, i.e. allow the user to update the trained item.
                    else {
                            if (!GrabberToTrainedItemId.ContainsKey(cAppliance) || !GrabberToTrainedItemId[cAppliance].Equals(pushItems.SpecificType))
                            {
                                GrabberToTrainedItemId[cAppliance] = pushItems.SpecificType;
                                Debug.Log($"TrainOnlyInSetup: Locked Smart Grabber trained item to {pushItems.SpecificType}");
                            }
                    }
                }
            }
            appliances.Dispose();
        }

        protected override void OnUpdate()
        {
            // Only unlock trained items in practice mode
            if (GameInfo.IsPreparationTime || Has<SPracticeMode>())
            {
                UpdateTrainedItems(isLocked: false);
            }
            // This condition is only true during the actual game day
            else
            {
                UpdateTrainedItems(isLocked: true);
            }
        }
    }
}
