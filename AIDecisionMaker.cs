using AI_BehaviorTree_AIGameUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AI_BehaviorTree_AIImplementation
{
    public class AIDecisionMaker
    {

        /// <summary>
        /// Ne pas supprimer des fonctions, ou changer leur signature sinon la DLL ne fonctionnera plus
        /// Vous pouvez unitquement modifier l'intérieur des fonctions si nécessaire (par exemple le nom)
        /// ComputeAIDecision en fait partit
        /// </summary>
        private int AIId = -1;
        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "MyAIName"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)

        public List<AIAction> ComputeAIDecision()
        {
            List<AIAction> actionList = new List<AIAction>();
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);

            PlayerInformations target = null;
            foreach (PlayerInformations playerInfo in playerInfos)
            {
                if (!playerInfo.IsActive)
                    continue;

                if (playerInfo.PlayerId == myPlayerInfos.PlayerId)
                    continue;

                target = playerInfo;
                break;
            }

            if (target == null)
                return actionList;

            actionList.Add(new AIActionLookAtPosition(target.Transform.Position));

            if (Vector3.Distance(myPlayerInfos.Transform.Position, target.Transform.Position) > 10.0f)
                actionList.Add(new AIActionMoveToDestination(target.Transform.Position));
            else
                actionList.Add(new AIActionStopMovement());

            RaycastHit hit;
            Vector3 direction = myPlayerInfos.Transform.Rotation * Vector3.forward;
            if (Physics.Raycast(myPlayerInfos.Transform.Position, direction.normalized, out hit, 100.0f))
            {
                if (AIGameWorldUtils.PlayerLayerMask == (AIGameWorldUtils.PlayerLayerMask | (1 << hit.collider.gameObject.layer)))
                    actionList.Add(new AIActionFire());
            }

            return actionList;
        }

        public PlayerInformations GetPlayerInfos(int parPlayerId, List<PlayerInformations> parPlayerInfosList)
        {
            foreach (PlayerInformations playerInfo in parPlayerInfosList)
            {
                if (playerInfo.PlayerId == parPlayerId)
                    return playerInfo;
            }

            Assert.IsTrue(false, "GetPlayerInfos : PlayerId not Found");
            return null;
        }
    }
}
