using UnityEngine;
using UnityEngine.AI;
using ProjectHounded.AI.Core;

namespace ProjectHounded.AI.Actions
{
    public class InvestigateSoundAction
    {
        private HunterAIController hunterController;
        private SoundSystem soundSystem;

        public string actionName = "Investigate Sound";
        public string actionDescription = "Investigate the source of the loudest sound.";

        public InvestigateSoundAction(HunterAIController controller, SoundSystem soundSys)
        {
            hunterController = controller;
            soundSystem = soundSys;
        }

        public void Execute()
        {
            Debug.Log("InvestigateSoundAction.Execute() started");
            if (hunterController == null || soundSystem == null) return;

            SoundEvent soundEvent = soundSystem.GetMostRelevantSound();
            if (soundEvent.IsValid)
            {
                NavMeshAgent agent = hunterController.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = hunterController.patrolSpeed;
                    agent.SetDestination(soundEvent.WorldPosition);
                }
            }
            Debug.Log("InvestigateSoundAction.Execute() ended");
        }
    }
}
