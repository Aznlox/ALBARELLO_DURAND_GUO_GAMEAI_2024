using AI_BehaviorTree_AIGameUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = System.Random;

namespace AI_BehaviorTree_AIImplementation
{
    public class AIDecisionMaker
    {

        private List<AIAction> actionList;
        private NodeManager nodeManager;

        private List<PlayerInformations> playerInfos;
        private PlayerInformations myPlayerInfos;
        private int actualPV;
        private float proximityThreshold = 10.0f;

        private List<ProjectileInformations> projectile;

        private int AIId = -1;
        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        public void SetAIId(int parAIId) { AIId = parAIId; }

        public string GetName() { return "Risitas"; }

        public void OnMyAIDeath() {}

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }


        public List<AIAction> ComputeAIDecision()
        {
            playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            projectile = AIGameWorldUtils.GetProjectileInfosList();

            if (actionList == null)
            {
                actionList = new List<AIAction>();

                Sequence baseSequence = new Sequence();

                baseSequence.AddChild(new Node(AimTheClosest));
                

                Selector selector = new Selector();

                Sequence bonus = new Sequence();
                Sequence fight = new Sequence();
                Sequence nigerundayo = new Sequence();

                bonus.AddChild(new Node(GoToBonus));
                fight.AddChild(new Node(FightDashDiago));

                nigerundayo.AddChild(new Node(FuckGoback));
                nigerundayo.AddChild(new Node(NigerundayoDash));

                selector.AddChild(nigerundayo);
                selector.AddChild(bonus);
                selector.AddChild(fight);

                baseSequence.AddChild(selector);

                actualPV = myPlayerInfos.CurrentHealth;

                nodeManager = new NodeManager(baseSequence);

            }


            actionList.Clear();


            nodeManager.update();
            actualPV = myPlayerInfos.CurrentHealth;


            PlayerInformations target = null;
            foreach (PlayerInformations playerInfo in playerInfos)
            {                
                if (!playerInfo.IsActive)
                    continue;

                if (playerInfo.PlayerId == myPlayerInfos.PlayerId)
                    continue;
                
                
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


        public PlayerInformations FindClosestPlayer()
        {
            PlayerInformations closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var player in playerInfos)
            {
                if (player.PlayerId == myPlayerInfos.PlayerId || !player.IsActive)
                {
                    continue;
                }

                float distance = Vector3.Distance(myPlayerInfos.Transform.Position, player.Transform.Position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
            return closestPlayer;
        }

        public State AimTheClosest() 
        {
            if (playerInfos == null || playerInfos.Count == 0)
            {
                return State.Failure;
            }

            PlayerInformations closestPlayer = FindClosestPlayer();

            if (closestPlayer == null)
            {
                return State.Failure;
            }

            actionList.Add(new AIActionMoveToDestination(closestPlayer.Transform.Position));
            actionList.Add(new AIActionLookAtPosition(closestPlayer.Transform.Position));
            actionList.Add(new AIActionFire());

            return State.Success;
        }

       

        public State GoToBonus()
        {
            const float maxDistanceToBonus = 60.0f;

            List<Vector3> bonusPositions = new List<Vector3>();
            foreach (var bonus in AIGameWorldUtils.GetBonusInfosList())
            {
                bonusPositions.Add(bonus.Position);
            }

            if (bonusPositions.Count == 0)
            {
                return State.Failure;
            }

            Vector3 closestBonusPosition = Vector3.zero;
            float closestBonusDistance = float.MaxValue;

            foreach (var bonusPosition in bonusPositions)
            {
                float distance = Vector3.Distance(myPlayerInfos.Transform.Position, bonusPosition);

                if (distance < maxDistanceToBonus && distance < closestBonusDistance)
                {
                    closestBonusDistance = distance;
                    closestBonusPosition = bonusPosition;
                }
            }

            if (closestBonusPosition != Vector3.zero)
            {
                actionList.Add(new AIActionMoveToDestination(closestBonusPosition));
                return State.Success;
            }
            else
            {
                return State.Failure;
            }
        }

       

        public State FightDashDiago()
        {
            if(myPlayerInfos.CurrentHealth < actualPV)
            {
                Vector3 dashDirection = myPlayerInfos.Transform.Rotation * (Vector3.forward + (UnityEngine.Random.Range(0,1) >= 0.5f ? Vector3.right : Vector3.left));
                actionList.Add(new AIActionDash(dashDirection));
                return State.Success;
            }
            return State.Failure;

        }

        public State FuckGoback()
        {
            PlayerInformations closestPlayer = FindClosestPlayer();

            int proximityCount = 0;

            foreach (var player in playerInfos)
            {
                if (player.PlayerId == myPlayerInfos.PlayerId) continue;
                if (Vector3.Distance(myPlayerInfos.Transform.Position, player.Transform.Position) < proximityThreshold)
                {
                    proximityCount++;
                }

                if (proximityCount >= 2)
                {
                    break;
                }
            }

            if (proximityCount >= 2)
            {
                Vector3 oppositeDirection = myPlayerInfos.Transform.Position - closestPlayer.Transform.Position;
                Vector3 destination = myPlayerInfos.Transform.Position + oppositeDirection;

                actionList.Add(new AIActionMoveToDestination(destination));

                actionList.Add(new AIActionLookAtPosition(closestPlayer.Transform.Position));
                actionList.Add(new AIActionFire());
                return State.Success;
            }

            return State.Failure;
        }

        public State NigerundayoDash()
        {
            int proximityCount = 0;

            foreach (var player in playerInfos)
            {
                if (player.PlayerId == myPlayerInfos.PlayerId) continue;
                if (Vector3.Distance(myPlayerInfos.Transform.Position, player.Transform.Position) < proximityThreshold)
                {
                    proximityCount++;
                }

                if (proximityCount >= 2)
                {
                    break;
                }
            }

            if (proximityCount >= 2)
            {
                System.Random random = new System.Random();
                Vector3 dashDirection = myPlayerInfos.Transform.Rotation * (Vector3.back + (UnityEngine.Random.Range(0, 1) >= 0.5f ? Vector3.left : Vector3.right));
                actionList.Add(new AIActionDash(dashDirection));
                return State.Success;
            }
            return State.Failure;
        }

    }
}
