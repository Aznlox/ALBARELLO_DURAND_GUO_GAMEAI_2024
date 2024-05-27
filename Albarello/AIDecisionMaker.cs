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
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            List<ProjectileInformations> projectiles = AIGameWorldUtils.GetProjectileInfosList();
           
            Vector3 movePosition = myPlayerInfos.Transform.Position;
            RaycastHit hit;

            /*foreach (ProjectileInformations projectile in projectiles)
            {
               if (Physics.Raycast(projectile.Transform.Position, projectile.Transform.Rotation * Vector3.forward,out hit,Mathf.Infinity))
               {                    
                    if(hit.transform.gameObject.transform.position == myPlayerInfos.Transform.Position)
                    {                        
                        //movePosition += CalculateTangent((myPlayerInfos.Transform.Position- hit.transform.position).normalized) *20.0f;
                        movePosition += new Vector3(1, 0, 0);
                    }
               }
            }*/
            string aaa = "";
            aaa += movePosition;
            movePosition += new Vector3(1, 0, 0);
            aaa += " " + movePosition;
            NavMeshHit nhit;
            if (NavMesh.SamplePosition(movePosition, out nhit, Mathf.Infinity, NavMesh.AllAreas))
            {
                movePosition = nhit.position;
            }
            aaa += " " + movePosition;
            Debug.LogError(aaa);

            actionList.Add(new AIActionFire());            
            return State.Success;
        }


        public List<AIAction> ComputeAIDecision()
        {
            if(nodeManager == null)
            {
                Selector selector = new Selector();
                selector.AddChild(new Node(SickDodge));
                nodeManager = new NodeManager(selector); 
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

        public NodeManager nodeManager = null;
        public List<AIAction> actionList = new List<AIAction>();
    }
}
