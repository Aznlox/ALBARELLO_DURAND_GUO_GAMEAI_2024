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
        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        public void OnMyAIDeath() { }
        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "GOD"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }        

        Vector3 CalculateTangent(Vector3 v)
        {
            Vector3 arbitrary = (v != Vector3.up) ? Vector3.up : Vector3.right;
            Vector3 tangent = Vector3.Cross(v, arbitrary).normalized;
            return tangent;
        }

        public State SickDodge()
        {
            Vector3 movePosition = myPlayerInfos.Transform.Position;
            RaycastHit hit;

            foreach (ProjectileInformations projectile in projectiles)
            {
               if (Physics.Raycast(projectile.Transform.Position, projectile.Transform.Rotation * Vector3.forward,out hit,Mathf.Infinity))
               {                    
                    if(hit.transform.gameObject.transform.position == myPlayerInfos.Transform.Position)
                    {
                        Vector3 tangent = CalculateTangent((projectile.Transform.Rotation * Vector3.forward).normalized) * 10.0f;
                        float dist = Mathf.Infinity;
                        if (Physics.Raycast(myPlayerInfos.Transform.Position, tangent.normalized, out hit, Mathf.Infinity))
                        {
                            dist = hit.distance;
                        }
                        if (Physics.Raycast(myPlayerInfos.Transform.Position, -tangent.normalized, out hit, Mathf.Infinity))
                        {
                            if(dist < hit.distance)
                            {
                                tangent = -tangent;
                            }
                        }
                        movePosition += tangent;
                    }
               }
            }

            NavMeshHit nhit;
            if (NavMesh.SamplePosition(movePosition, out nhit, Mathf.Infinity, NavMesh.AllAreas))
            {
                movePosition = nhit.position;
            }
            if (countDodge == 0)
            {                
                actionList.Add(new AIActionMoveToDestination(movePosition));                
            }
            countDodge++;
            if(countDodge > 10)
            {
                countDodge = 0;
            }            
            return State.Success;
        }

        public State FindTarget()
        {
            idTarget = -1;
            float dist;
            float lastDist = Mathf.Infinity;
            for (int i = 0; i < playerInfos.Count; i++)
            {          
                if (playerInfos[i].PlayerId != myPlayerInfos.PlayerId && playerInfos[i].IsActive)
                {
                    if (modeTeam && whiteList.Contains(playerInfos[i].Name))
                    {
                        continue;
                    }
                    dist = Vector3.Distance(playerInfos[i].Transform.Position, myPlayerInfos.Transform.Position);
                    if (dist < lastDist)
                    {
                        lastDist = dist;
                        idTarget = i;
                    }                      
                }
            }
            if (idLastTarget != idTarget)
            {
                lastPosition = idTarget == -1 ? Vector3.zero : playerInfos[idTarget].Transform.Position;
                lastTime = Time.time;
                idLastTarget = idTarget;
            }
            return idTarget != -1 ? State.Success : State.Failure;            
        }

        Vector3 PredictTargetPosition(Vector3 positionTarget, Vector3 nonNormalizeDirectionTarget, float timeElapsed)
        {
            float bulletSpeed = 40.0f * Mathf.Pow(2,myPlayerInfos.BonusOnPlayer[EBonusType.BulletSpeed]);
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

        public State Shoot()
        {
            if (idTarget == -1)
            {
                if (myPlayerInfos.SalvoRemainingAmount < 10)
                {
                    actionList.Add(new AIActionReload());
                }
                return State.Success;
            }                  
            PlayerInformations target = GetPlayerInfos(idTarget, playerInfos);
            Vector3 finalPosition = target.Transform.Position;
            Vector3 direction = -(lastPosition - target.Transform.Position);
            finalPosition = PredictTargetPosition(target.Transform.Position, direction, Time.time - lastTime);
            lastPosition = target.Transform.Position;
            lastTime = Time.time;
            actionList.Add(new AIActionLookAtPosition(finalPosition + new Vector3(0.0f, 0.1f, 0.0f)));

            bool seeTarget = false;
            RaycastHit hit;
            Vector3 dirTarget = (target.Transform.Position - myPlayerInfos.Transform.Position).normalized;
            if (Physics.Raycast(myPlayerInfos.Transform.Position, dirTarget, out hit, 100.0f))
            {
                if (AIGameWorldUtils.PlayerLayerMask == (AIGameWorldUtils.PlayerLayerMask | (1 << hit.transform.gameObject.layer)))
                {
                    seeTarget = true;
                }
            }
            if (seeTarget)
            {
                float cos = Mathf.Cos(15 * Mathf.Deg2Rad);  // 15 degrés en radians
                if (Vector3.Dot((myPlayerInfos.Transform.Rotation*Vector3.forward).normalized, (finalPosition - myPlayerInfos.Transform.Position).normalized) > cos)
                {
                    actionList.Add(new AIActionFire());
                }                
            }
            else if(myPlayerInfos.SalvoRemainingAmount < 10)
            {
                actionList.Add(new AIActionReload());
            }
            return State.Success;
        }

        public State FindBonus()
        {
            bool seeTarget = false;
            if (idTarget != -1)
            {
                PlayerInformations target = GetPlayerInfos(idTarget, playerInfos);
                if (target.IsActive)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(myPlayerInfos.Transform.Position, (target.Transform.Position - myPlayerInfos.Transform.Position).normalized, out hit))
                    {
                        if (AIGameWorldUtils.PlayerLayerMask == (AIGameWorldUtils.PlayerLayerMask | (1 << hit.transform.gameObject.layer)))
                        {
                            seeTarget = true;
                        }
                    }                    
                }
            }
            if(!seeTarget)
            {
                List<BonusInformations> bonus = AIGameWorldUtils.GetBonusInfosList();
                if (bonus.Count >= 1)
                {
                    int index = 0;
                    float dist = Mathf.Infinity;
                    for (int i = 1; i < bonus.Count; i++)
                    {
                        float nd = Vector3.Distance(myPlayerInfos.Transform.Position, bonus[i].Position);
                        if (nd < dist || bonus[i].Type != EBonusType.Health)
                        {
                            dist = nd;
                            index = i;
                            if (bonus[i].Type != EBonusType.Health)
                            {
                                break;
                            }
                        }
                    }
                    actionList.Add(new AIActionMoveToDestination(bonus[index].Position));
                    if (myPlayerInfos.IsDashAvailable && (Time.time- dashTime) > 6.0f)
                    {
                        Vector3 ndir = (bonus[index].Position - myPlayerInfos.Transform.Position);
                        ndir.y = 0;
                        actionList.Add(new AIActionDash(ndir));
                        dashTime = Time.time;
                    }
                }
            }
            return State.Success;
        }

        public State MoveTarget()
        {
            PlayerInformations target = GetPlayerInfos(idTarget, playerInfos);
            actionList.Add(new AIActionMoveToDestination(target.Transform.Position));
            return State.Success;
        }

        public State Bonus()
        {
            List<BonusInformations> bonus = AIGameWorldUtils.GetBonusInfosList();
            if (bonus.Count >= 1)
            {
                int index = 0;
                float dist = Mathf.Infinity;
                for (int i = 1; i < bonus.Count; i++)
                {
                    float nd = Vector3.Distance(myPlayerInfos.Transform.Position, bonus[i].Position);
                    if (nd < dist || bonus[i].Type != EBonusType.Health)
                    {
                        dist = nd;
                        index = i;
                        if (bonus[i].Type != EBonusType.Health)
                        {
                            break;
                        }
                    }
                }
                actionList.Add(new AIActionMoveToDestination(bonus[index].Position));
                if (myPlayerInfos.IsDashAvailable)
                {
                    Vector3 ndir = (bonus[index].Position - myPlayerInfos.Transform.Position);
                    ndir.y = 0;
                    if(agent.desiredVelocity.magnitude > 0.1f)
                    {
                        ndir = agent.desiredVelocity;
                    }
                    actionList.Add(new AIActionDash(ndir));
                }
            }
            else
            {
                return State.Failure;
            }
            return State.Success;
        }

        public List<AIAction> ComputeAIDecision()
        {
            playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            projectiles = AIGameWorldUtils.GetProjectileInfosList();
            if (nodeManager == null)
            {
                lastTime = Time.time;
                for(int i = 0; i < playerInfos.Count && !modeTeam; i++)
                {
                    if (!whiteList.Contains(playerInfos[i].Name))
                    {
                        modeTeam = true;
                    }
                }
                //autoriser par le prof de lire les donnes du navMesh
                NavMeshAgent[] array = GameObject.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
                for(int i = 0; i < array.Length && agent == null; i++)
                {
                    if(array[i].gameObject.transform.position == myPlayerInfos.Transform.Position)
                    {
                        agent = array[i];
                    }
                }
                Sequence baseS = new Sequence();
                Sequence attaque = new Sequence();
                attaque.AddChild(new Node(FindTarget));
                attaque.AddChild(new Node(Shoot));
                Selector bonus = new Selector();
                bonus.AddChild(new Node(Bonus));
                bonus.AddChild(new Node(SickDodge));
                baseS.AddChild(bonus);
                baseS.AddChild(attaque);              
                nodeManager = new NodeManager(baseS);
            }
            actionList.Clear();
            nodeManager.update();            
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

        int countDodge = 0;
        int idTarget = -1;
        int idLastTarget = -1;
        public List<string> whiteList = new List<string> { "GodSlayer","GOD","KS","Risitas"};
        public bool modeTeam = false;
        private Vector3 lastPosition;
        private Vector3 lastDirection = new Vector3(0,0,0);
        private float lastTime;
        public NodeManager nodeManager = null;
        public List<AIAction> actionList = new List<AIAction>();
        public List<PlayerInformations> playerInfos;
        public PlayerInformations myPlayerInfos;
        public List<ProjectileInformations> projectiles;
        NavMeshAgent agent = null;
        private float dashTime = 0.0f;
    }
}