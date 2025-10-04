using UnityEngine;
using UnityEngine.AI;

namespace ProjectHounded.AI.Actions
{
    public class PatrolAction
    {
        private HunterAIController hunterController;

        public string actionName = "Patrol";
        public string actionDescription = "Patrol randomly within the designated area.";

        public PatrolAction(HunterAIController controller)
        {
            hunterController = controller;
        }

        public void Execute()
        {
            Debug.Log("PatrolAction.Execute() started");
            if (hunterController == null) return;

            NavMeshAgent agent = hunterController.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Patrol logic: move to a random point within patrolRadius
                Vector3 randomDirection = Random.insideUnitSphere * hunterController.patrolRadius;
                randomDirection += hunterController.transform.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, hunterController.patrolRadius, NavMesh.AllAreas))
                {
                    agent.speed = hunterController.patrolSpeed;
                    agent.SetDestination(hit.position);
                }
            }
            Debug.Log("PatrolAction.Execute() ended");
        }
    }
}
