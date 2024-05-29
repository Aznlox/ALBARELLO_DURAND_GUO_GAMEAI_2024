using AI_BehaviorTree_AIGameUtility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Windows.UI.Xaml.Controls.Primitives;

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

        public List<AIAction> actionList;
        PlayerInformations target;
        NodeManager nodeManager;
        Vector3 oldPos;

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "GodSlayer"; }

        public void OnMyAIDeath() {}

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)

        public List<AIAction> ComputeAIDecision()
        {
            if (actionList == null)
            {
                actionList = new List<AIAction>();
                Selector selectorOriginel = new Selector();

                // Sequence 1
                //Sequence Run = new Sequence();
                //Run.AddChild(new Node(DetectLowLife));
                //Run.AddChild(new Node(RunFromEnemy));
                Sequence ShootEnemy = new Sequence();

                Sequence TowardsBonus = new Sequence();
                TowardsBonus.AddChild(new Node(WalkTowardsBonus));
                TowardsBonus.AddChild(new Node(DashTowardsBonus));
                TowardsBonus.AddChild(ShootEnemy);

                ShootEnemy.AddChild(new Node(LookTowardEnemy));
                ShootEnemy.AddChild(new Node(Shoot));
                ShootEnemy.AddChild(new Node(WalkTowardsEnemy));
                //ShootEnemy.AddChild(new Node(DashRandomly));
                


                //ShootEnemy.AddChild(TowardsBonus);

                //selectorOriginel.AddChild(Run);
                selectorOriginel.AddChild(TowardsBonus);
                selectorOriginel.AddChild(ShootEnemy);
                nodeManager = new NodeManager(selectorOriginel);
            }

            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);

            actionList.Clear();

            foreach (PlayerInformations playerInfo in playerInfos)
            {
                if (!playerInfo.IsActive)
                    continue;

                if (playerInfo.PlayerId == myPlayerInfos.PlayerId)
                    continue;

                target = playerInfo;
                break;
            }

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

        private State LookTowardEnemy()
        {
            if (target == null)
            {
                return State.Failure;
            }
            actionList.Add(new AIActionLookAtPosition(target.Transform.Position));
            return State.Success;
        }

        private State Shoot()
        {
            actionList.Add(new AIActionFire());
            return State.Success;
        }

        private State JustWalk()
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);

            actionList.Add(new AIActionMoveToDestination(new Vector3(0, 0, 0)));
            return State.Success;
        }

        private State WalkTowardsEnemy()
        {
            actionList.Add(new AIActionMoveToDestination(new Vector3(target.Transform.Position.x, target.Transform.Position.y, target.Transform.Position.z)));
            return State.Success;
        }

        private State WalkTowardsBonus()
        {
            if (AIGameWorldUtils.GetBonusInfosList().Count > 0)
            {
                var nextBonus = AIGameWorldUtils.GetBonusInfosList()[0];
                actionList.Add(new AIActionMoveToDestination(new Vector3(nextBonus.Position.x, nextBonus.Position.y, nextBonus.Position.z)));
                return State.Success;
            }
            return State.Failure;
        }

        private State DashTowardsBonus()
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            if (AIGameWorldUtils.GetBonusInfosList().Count > 0 && myPlayerInfos.IsDashAvailable)
            {
                actionList.Add(new AIActionDash(AIGameWorldUtils.GetBonusInfosList()[0].Position - myPlayerInfos.Transform.Position));
                return State.Success;
            }
            return State.Failure;
        }

        private State DetectLowLife()
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            if (myPlayerInfos.CurrentHealth < 5)
            {
                return State.Success;
            }
            return State.Failure;
        }

        private State RunFromEnemy()
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            if (target == null)
            {
                actionList.Add(new AIActionDash(myPlayerInfos.Transform.Rotation * Vector3.back));
                return State.Failure;
            }
            return State.Success;
        }

        private State DashRandomly()
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);

            if (target != null)
            {
                actionList.Add(new AIActionDash(new Vector3(Random.Range(0, 1), 0, Random.Range(0, 1))));
                return State.Success;
            }
            return State.Failure;
        }
    }
}
