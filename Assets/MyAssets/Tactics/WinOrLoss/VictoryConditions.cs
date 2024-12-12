using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using static Utility;

/*
// SRPGの勝利条件は
// 1. 敵の全滅
// 2. ターゲットの殺害
// 3. 敵陣地の制圧
// 4. Xターン持ちこたえる（自陣の防衛）
// そのために必要な要素は
UnitsControllClass のUnitsList -> Player . Enemy tag
ターン経過　数
ターゲットとなるUnitの取得　UnitConにKillTargetTagを設定してもいいかも
敵 味方陣地のGameObject
*/
namespace Tactics
{
    public class VictoryConditions : MonoBehaviour
    {
        #region Properties

        public enum Mode
        {
            NONE,
            KILL_ALL,
            KILL_TARGET,
            CONTROL_ENEMY_AREA,
            DEFENCE_X_TURN,
            KILL_X
        }
        
        /// <summary>
        /// Tacticsの結果
        /// </summary>
        public enum GameResult
        {
            None,
            Win,
            Lose,
            Playing,
            ForceEnd
        }

        /// <summary>
        /// 勝利条件
        /// </summary>
        [SerializeField] internal Mode mode = Mode.NONE;
        [SerializeField] private Character.UnitsController unitsController;
        // Optional
        [SerializeField] private List<GameObject> targetEnemyAreas;
        [SerializeField] private List<GameObject> defenceAreas;
        [SerializeField] internal int defenceTurn = 5;

        internal int currentTurn;
        internal List<Camp> campList;

        public Action<GameResult> endGameAction;
        public GameResult sceneState { private set; get; }
        #endregion

        /// <summary>
        /// 勝敗をModeに従って確認する
        /// </summary>
        public void CheckGameState()
        {
            switch (mode)
            {
                case Mode.KILL_TARGET:
                    sceneState = KillTargets();
                    break;

                case Mode.CONTROL_ENEMY_AREA:
                    sceneState = ControlArea();
                    break;

                case Mode.DEFENCE_X_TURN:
                    sceneState = DefenceArea(defenceTurn);
                    break;
            }

            sceneState = KillAll();

            if (!sceneState.Equals(GameResult.Playing))
                endGameAction?.Invoke(sceneState);
        }

        #region State Checker

        private GameResult KillAll()
        {
            int enemyCount = 0;
            int playerCount = 0;
            foreach (var unit in unitsController.UnitsList)
            {
                var attribute = unit.Attribute;
                if (attribute == UnitAttribute.ENEMY)
                    enemyCount++;
                else if (attribute == UnitAttribute.PLAYER)
                    playerCount++;
            }

            if (playerCount == 0)
                return GameResult.Lose;
            else if (enemyCount == 0)
                return GameResult.Win;
            else
                return GameResult.Playing;
        }

        /// <summary>
        /// すべてのTargetをkillする
        /// </summary>
        /// <returns></returns>
        private GameResult KillTargets()
        {
            int targetCount = 0;
            int playerCount = 0;
            foreach (var unit in unitsController.UnitsList)
            {
                var attribute = unit.Attribute;
                if (attribute == UnitAttribute.ENEMY)
                {
                    if (unit.CurrentParameter.Data.IsFlag)
                        targetCount++;
                }
                else if (attribute == UnitAttribute.PLAYER)
                {
                    playerCount++;
                }
            }

            if (playerCount == 0)
                return GameResult.Lose;
            else if (targetCount == 0)
                return GameResult.Win;
            else
                return GameResult.Playing;

        }

        private GameResult ControlArea()
        {
            var count = 0;
            foreach (var camp in campList)
                if (!camp.ownArea && camp.flagArea)
                    count++;

            var playerCount = 0;
            foreach (var unit in unitsController.UnitsList)
                if (unit.Attribute == UnitAttribute.PLAYER)
                    playerCount++;

            if (playerCount == 0)
                return GameResult.Lose;

            if (count == 0)
                return GameResult.Win;
            else
                return GameResult.Playing;
        }

        private GameResult DefenceArea(int limitTurn)
        {
            var count = 0;
            foreach (var camp in campList)
                if (camp.ownArea && camp.flagArea)
                    count++;

            var playerCount = 0;
            foreach (var unit in unitsController.UnitsList)
                if (unit.Attribute == UnitAttribute.PLAYER)
                    playerCount++;

            if (playerCount == 0)
                return GameResult.Lose;

            if (count == 0)
                return GameResult.Lose;

            if (currentTurn > limitTurn)
                return GameResult.Win;

            return GameResult.Playing;
        }
        #endregion



    }
}