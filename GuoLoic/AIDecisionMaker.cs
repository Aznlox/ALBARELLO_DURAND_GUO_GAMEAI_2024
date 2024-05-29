using AI_BehaviorTree_AIGameUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;

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
        private List<AIAction> actionList;
        private NodeManager nodeManager;
        public List<PlayerInformations> playerInfos;
        public PlayerInformations myPlayerInfos;
        public List<ProjectileInformations> projectiles;

        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        private Vector3 oldPos;

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "KS"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)

        public void OnMyAIDeath() {}

        public List<AIAction> ComputeAIDecision()
        {
            playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            for(int i = 0; i < playerInfos.Count; i++)
            {
                if (playerInfos[i].PlayerId == myPlayerInfos.PlayerId)
                {
                    playerInfos.RemoveAt(i);
                    break;
                }
            }
            playerInfos.RemoveAt(AIId);
            projectiles = AIGameWorldUtils.GetProjectileInfosList();

            if (actionList == null)
            {
                actionList = new List<AIAction>();

                Sequence sequenceAlways = new Sequence();
                sequenceAlways.AddChild(new Node(AlwaysShoot));
                sequenceAlways.AddChild(new Node(AimTarget));

                Selector selectorMove = new Selector();

                Sequence sequenceMovetoBuff = new Sequence();
                sequenceMovetoBuff.AddChild(new Node(MovetoBuff));
                sequenceMovetoBuff.AddChild(new Node(Dash));

                Sequence sequenceStrafe = new Sequence();
                sequenceStrafe.AddChild(new Node(Strafe));
                sequenceStrafe.AddChild(new Node(Dash));

                selectorMove.AddChild(sequenceMovetoBuff);
                selectorMove.AddChild(sequenceStrafe);

                sequenceAlways.AddChild(selectorMove);


                nodeManager = new NodeManager(sequenceAlways);
            }

            actionList.Clear();
            
        
            nodeManager.update();
            oldPos = myPlayerInfos.Transform.Position;

            return actionList;
        }

        public State AlwaysShoot()
        {

            actionList.Add(new AIActionFire());
            return State.Success;
        }

        public State AimTarget()
        {
            PlayerInformations target = playerInfos[0];
            foreach(PlayerInformations playerInfo in playerInfos)
            {
                target = playerInfo;

                if (playerInfo.PlayerId == myPlayerInfos.PlayerId)
                {
                    continue;
                }

                if (playerInfo.CurrentHealth < target.CurrentHealth && playerInfo.IsActive)
                {
                    target = playerInfo;
                }
            }
            actionList.Add(new AIActionLookAtPosition(target.Transform.Position));
            return State.Success;
        }

        public State MovetoBuff()
        {
            List<BonusInformations> bonus = AIGameWorldUtils.GetBonusInfosList();

            if (bonus.Count < 1)
            {
                return State.Failure;
            }
            Vector3 moveTo;
            moveTo = bonus[0].Position;
            foreach (BonusInformations bonu in bonus)
            {

                if (Vector3.Distance(bonu.Position, myPlayerInfos.Transform.Position) < Vector3.Distance(moveTo, myPlayerInfos.Transform.Position))
                {
                    moveTo = bonu.Position;
                }
            }
            actionList.Add(new AIActionMoveToDestination(moveTo));
            return State.Success;
        }

        public State Strafe()
        {
            Vector3 moveTo = myPlayerInfos.Transform.Position + UnityEngine.Random.insideUnitSphere * 30f;
            NavMeshHit nhit;
            if (NavMesh.SamplePosition(moveTo, out nhit, Mathf.Infinity, NavMesh.AllAreas))
            {
                moveTo = nhit.position;
            }
            actionList.Add(new AIActionMoveToDestination(moveTo));
            return State.Success;
        }

        public State Dash()
        {
            if(oldPos == null)
            {
                return State.Success;
            }
            Vector3 dash = (myPlayerInfos.Transform.Position - oldPos).normalized;
            if (dash == Vector3.zero)
            {
                return State.Success;
            }
            actionList.Add(new AIActionDash(dash));
            return State.Success;
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
