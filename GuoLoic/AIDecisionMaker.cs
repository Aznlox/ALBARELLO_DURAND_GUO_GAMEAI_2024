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
        public List<string> whiteList = new List<string> { "GodSlayer", "GOD", "KS", "Risitas" };
        public bool isTeam = false;
        public Vector3 lastPosition = Vector3.zero;
        public float lastTime = 0f; 

        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        private Vector3 oldPos;
        private PlayerInformations target = null;

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
            int id = -1;
            for(int i = 0; i < playerInfos.Count; i++)
            {
                if (playerInfos[i].PlayerId == myPlayerInfos.PlayerId)
                {
                    id = i;
                    continue;
                }
                if (!whiteList.Contains(playerInfos[i].Name))
                {
                    isTeam = true;
                }
            }
            playerInfos.RemoveAt(id);

            if (actionList == null)
            {
                actionList = new List<AIAction>();

                Sequence sequenceAlways = new Sequence();
                
                sequenceAlways.AddChild(new Node(AimTarget));
                sequenceAlways.AddChild(new Node(ShootReload));

                Selector selectorMove = new Selector();

                Sequence sequenceMovetoBuff = new Sequence();
                sequenceMovetoBuff.AddChild(new Node(MovetoBuff));
                sequenceMovetoBuff.AddChild(new Node(Dash));

                Sequence sequenceStrafe = new Sequence();
                sequenceStrafe.AddChild(new Node(PredictJammer));
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

        Vector3 PredictTargetPosition(Vector3 positionTarget, Vector3 nonNormalizeDirectionTarget, float timeElapsed)
        {
            float bulletSpeed = 40.0f * Mathf.Pow(2, myPlayerInfos.BonusOnPlayer[EBonusType.BulletSpeed]);
            // Vitesse de la cible en m/s
            Vector3 targetVelocity = nonNormalizeDirectionTarget / timeElapsed;

            // Calcul du temps
            Vector3 toTarget = positionTarget - myPlayerInfos.Transform.Position;
            float distanceToTarget = toTarget.magnitude;
            float timeToReachTarget = distanceToTarget / bulletSpeed;

            // Position predite de la cible
            Vector3 futureTargetPosition = positionTarget + targetVelocity * timeToReachTarget;

            return futureTargetPosition;
        }

        public State ShootReload()
        {
            if (target == null)
            {
                if (myPlayerInfos.SalvoRemainingAmount < 10)
                {
                    actionList.Add(new AIActionReload());
                }
                return State.Success;
            }
            bool seeTarget = false;
            Vector3 finalPosition = target.Transform.Position;
            Vector3 direction = -(lastPosition - target.Transform.Position);
            finalPosition = PredictTargetPosition(target.Transform.Position, direction, Time.time - lastTime);
            lastPosition = target.Transform.Position;
            lastTime = Time.time;
            actionList.Add(new AIActionLookAtPosition(finalPosition));

            RaycastHit hit;
            if (Physics.Raycast(myPlayerInfos.Transform.Position, (target.Transform.Position - myPlayerInfos.Transform.Position).normalized, out hit, 150.0f))
            {
                if (AIGameWorldUtils.PlayerLayerMask == (AIGameWorldUtils.PlayerLayerMask | (1 << hit.transform.gameObject.layer)))
                {
                    seeTarget = true;
                }
            }
            if (seeTarget)
            {
                float cos = Mathf.Cos(15 * Mathf.Deg2Rad);  // 15 degrés en radians
                if (Vector3.Dot((myPlayerInfos.Transform.Rotation * Vector3.forward).normalized, (finalPosition - myPlayerInfos.Transform.Position).normalized) > cos)
                {
                    actionList.Add(new AIActionFire());
                }
            }
            else if (myPlayerInfos.SalvoRemainingAmount < 10)
            {
                actionList.Add(new AIActionReload());
            }
            return State.Success;
        }

        public State AimTarget()
        {
            target = null;
            foreach(PlayerInformations playerInfo in playerInfos)
            {
                if (isTeam && whiteList.Contains(playerInfo.Name))
                {
                    continue;
                }

                if (target == null || playerInfo.CurrentHealth < target.CurrentHealth && playerInfo.IsActive)
                {
                    target = playerInfo;
                }
                
            }
            if(target == null)
            {
                return State.Success;
            }
            
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

        public State PredictJammer()
        {
            Vector3 moveTo = myPlayerInfos.Transform.Position + UnityEngine.Random.insideUnitSphere * 60f;
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
