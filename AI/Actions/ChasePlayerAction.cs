using UnityEngine;
using UnityEngine.AI;

namespace ProjectHounded.AI.Actions
{
    public class ChasePlayerAction
    {
        private HunterAIController hunterController;

        public string actionName = "Chase Player";
        public string actionDescription = "Chase the player aggressively.";

        public ChasePlayerAction(HunterAIController controller)
        {
            hunterController = controller;
        }

        public void Execute()
        {
            Debug.Log("ChasePlayerAction.Execute() started");
            if (hunterController == null) return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                NavMeshAgent agent = hunterController.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = hunterController.chaseSpeed;
                    agent.SetDestination(player.transform.position);
                }
            }
            Debug.Log("ChasePlayerAction.Execute() ended");
        }
    }
}
