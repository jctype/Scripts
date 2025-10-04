using UnityEngine;
using UnityEngine.AI;
using ProjectHounded.AI.Core;

namespace ProjectHounded.AI.Actions
{
    public class FollowScentAction
    {
        private HunterAIController hunterController;
        private ScentSystem scentSystem;

        public string actionName = "Follow Scent";
        public string actionDescription = "Follow the strongest scent trail.";

        public FollowScentAction(HunterAIController controller, ScentSystem scentSys)
        {
            hunterController = controller;
            scentSystem = scentSys;
        }

        public void Execute()
        {
            Debug.Log("FollowScentAction.Execute() started");
            if (hunterController == null || scentSystem == null) return;

            ScentData scentData = scentSystem.GetMostRelevantScent();
            if (scentData.IsValid)
            {
                NavMeshAgent agent = hunterController.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = hunterController.patrolSpeed;
                    agent.SetDestination(scentData.WorldPosition);
                }
            }
            Debug.Log("FollowScentAction.Execute() ended");
        }
    }
}
