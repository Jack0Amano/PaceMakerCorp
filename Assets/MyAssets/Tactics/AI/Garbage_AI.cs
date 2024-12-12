using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tactics.Character;
using Tactics.Map;
using UnityEngine;
using static Tactics.AI.AIAction_x;
using static Utility;

/*
namespace Tactics.AI
{
    /// <summary>
    /// 昔作ったAIのゴミ
    /// </summary>
    public class GarbageAI
    {
        internal TilesController tilesController;
        internal UnitsController unitsController;
        internal GeneralParameter parameters;
        internal DebugController debugController;

        internal AIAction Run(UnitController unit)
        {
            const float FromCurrentLocScore = 0.5f;
            const float FromCurrentTileScore = 0.25f;

            var isWarningHealthThreshold = IsWarningHealthThreshold(unit);
            var targetFromCurrentLocation = ActionOnCurrentLocation(unit);
            targetFromCurrentLocation.score += FromCurrentLocScore;
            var targetFromCurrentTile = ActionOnCurrentTile(unit);
            targetFromCurrentTile.score += FromCurrentTileScore;
            var targetFromBorderTiles = ActionOnBorderTiles(unit);

            Print($"{unit}: A2M {targetFromCurrentLocation.score}, M2A {targetFromCurrentTile.score}, M2A2 {targetFromBorderTiles.score}");

            var output = new AIAction(unit);

            if (isWarningHealthThreshold)
            {
                output.locationToMove = EscapeToSafePosition(unit);
                // HPが低い状態
                if (targetFromCurrentLocation != null)
                {
                    // 現在の位置から移動無しで攻撃できるため ActionToMove で攻撃してから逃げる
                    output.orderOfAction = OrderOfAction.ActionToMove;
                    output.target = targetFromCurrentLocation.target;
                    output.rate = targetFromCurrentLocation.rate;
                    return output;
                }
                else
                {
                    // 体力が低いため逃げるルーチン
                    output.orderOfAction = OrderOfAction.MoveToSkip;
                    return output;
                }
            }

            if (targetFromCurrentLocation.target != null &&
                targetFromCurrentLocation.score > targetFromCurrentTile.score &&
                targetFromCurrentLocation.score > targetFromBorderTiles.score)
            {
                // 現在地点から攻撃するのが最も良い場合
                output.orderOfAction = OrderOfAction.ActionToMove;
                output.target = targetFromCurrentLocation.target;
                output.rate = targetFromCurrentLocation.rate;

                return output;

            }

            if (targetFromCurrentTile.target != null &&
                targetFromCurrentTile.score > targetFromCurrentLocation.score &&
                targetFromCurrentTile.score > targetFromBorderTiles.score)
            {
                // 現在のタイル内で移動して攻撃するのが一番良い場合
                output.orderOfAction = OrderOfAction.MoveToAction;
                output.target = targetFromCurrentTile.target;
                output.locationToMove = targetFromCurrentTile.location;
                output.rate = targetFromCurrentTile.rate;

                return output;
            }

            if (targetFromBorderTiles.score > targetFromCurrentLocation.score &&
                targetFromBorderTiles.score > targetFromCurrentTile.score)
            {
                // 横のタイルに移動して攻撃するのが一番良い
                output.orderOfAction = OrderOfAction.MoveToAction;
                output.target = targetFromBorderTiles.target;
                output.locationToMove = targetFromBorderTiles.location;
                output.rate = targetFromBorderTiles.rate;

                return output;
            }


            // 最終層
            // 体力も十分で攻撃するべき敵が存在するためそれに向かって移動
            var nextCellAndTarget = MoveToWeakTarget(unit);
            if (nextCellAndTarget.target == null)
            {
                output.orderOfAction = OrderOfAction.Skip;
                return output;
            }
            var nextPosition = GetSafePosition(nextCellAndTarget.nextCell, unitsController.activeUnit);
            output.locationToMove = nextPosition;
            output.orderOfAction = OrderOfAction.MoveToSkip;
            output.rate = 0;
            output.target = null;

            return output;
        }

        #region Main AI Layer
        /// <summary>
        /// 現在の場所から狙えるUnitがある場合のAction (第一層)
        /// </summary>
        private TmpTarget ActionOnCurrentLocation(UnitController unit)
        {
            // 現在の場所から狙えるUnit
            var targets = GetTargetsOnLocation(unit, new LocationAndScore(unit.gameObject.transform.position, 0));
            if (targets.Count == 0) return new TmpTarget(unit);

            var betterTarget = GetBetterScoreOfLocationAndTargets(targets);
            if (betterTarget == null)
                return new TmpTarget(unit);

            return betterTarget;
        }

        /// <summary>
        /// 現在のタイル内で狙えるUnitがある場合のAction (第二層)
        /// </summary>
        private TmpTarget ActionOnCurrentTile(UnitController unit)
        {
            var currentTile = tilesController.GetTileInUnit(unit.gameObject);
            if (currentTile == null)
            {
                debugController.Show((command) => { });
                debugController.AddText($"Unit {unit} isn't in a tile. Skip AI of AIController.ActionOnCurrentTile.");
                return new TmpTarget(unit);
            }

            var betterTargets = new List<TmpTarget>();
            foreach (var gridAndPoint in currentTile.locationsAndScores)
            {
                if (unitsController.unitsList.TryFindFirst(u => u.IsInMyArea(gridAndPoint.location), out var _))
                    continue;

                var tmpTargets = GetTargetsOnLocation(unit, gridAndPoint);
                if (tmpTargets.Count == 0)
                    continue;
                var betterTarget = GetBetterScoreOfLocationAndTargets(tmpTargets);
                if (betterTarget == null)
                    continue;
                betterTargets.Add(betterTarget);
            }

            if (betterTargets.Count == 0)
                return new TmpTarget(unit);

            return betterTargets.FindMax(t => t.score);
        }

        /// <summary>
        /// 横のタイルに移動して狙える敵がある場合のAction (第三層)
        /// </summary>
        private TmpTarget ActionOnBorderTiles(UnitController unit)
        {
            var currentTile = tilesController.GetTileInUnit(unit.gameObject);
            if (currentTile == null)
            {
                debugController.Show((command) => { });
                debugController.AddText($"Unit {unit} isn't in a tile. Skip AI of AIController.ActionOnBorderTiles.");
                return new TmpTarget(unit);
            }

            var betterTargets = new List<TmpTarget>();
            foreach (var tile in currentTile.borderOnTiles)
            {
                foreach (var gridAndPoint in tile.locationsAndScores)
                {
                    if (unitsController.unitsList.TryFindFirst(u => u.IsInMyArea(gridAndPoint.location), out var _))
                        continue;

                    var tmpTargets = GetTargetsOnLocation(unit, gridAndPoint);
                    if (tmpTargets.Count == 0)
                        continue;
                    var betterTarget = GetBetterScoreOfLocationAndTargets(tmpTargets);
                    if (betterTarget == null)
                        continue;

                    betterTargets.Add(betterTarget);
                }
            }

            if (betterTargets.Count == 0)
                return new TmpTarget(unit);

            return betterTargets.FindMax(t => t.score);
        }

        /// <summary>
        /// 今のターンで狙える敵が居ないため発見されている最も弱い敵に移動する (最終層))
        /// </summary>
        private (TileCell nextCell, UnitController target) MoveToWeakTarget(UnitController unit)
        {
            var activeTile = tilesController.GetTileInUnit(unit.gameObject);
            var targets = unitsController.unitsList.FindAll(u => u.IsEnemyFromMe(unit));
            var orderOfAction = unitsController.GetOrderOfAction();

            var unitCellDict = new Dictionary<UnitController, TileCell>();
            var cellScoreDict = new Dictionary<TileCell, (float score, TileCell next)>();
            targets.ForEach(t =>
            {
                var cell = tilesController.GetTileInUnit(t.gameObject);
                cellScoreDict[cell] = (-1f, null);
                unitCellDict[t] = cell;
            });

            // ActiveCell -> TargetCellへ距離が近いほど優先的
            foreach (var pair in cellScoreDict.ToList())
            {
                var way = tilesController.GetShortestWay(activeTile, pair.Key);
                var wayScore = 10 / (way.Count + 4);
                cellScoreDict[pair.Key] = (wayScore, way[1]);
            }

            // WayScoreとHealthScoreを加算して合計スコア
            var unitScoreList = new List<((TileCell next, UnitController target) cellAndTarget, float score)>();
            unitCellDict.ToList().ForEach(pair =>
            {
                var healthScore = unit.CalcHealthScore(pair.Key, orderOfAction);
                var cellAndScore = cellScoreDict[pair.Value];
                var wayScore = cellAndScore.score;
                var elem = ((cellAndScore.next, pair.Key), healthScore + wayScore);
                unitScoreList.Add(elem);
            });


            if (unitScoreList.Count == 0)
                return (activeTile, null);
            var result = unitScoreList.FindMax(a => a.score);

            return result.cellAndTarget;
        }

        /// <summary>
        /// 安全そうな場所に逃げる
        /// </summary>
        private Vector3 EscapeToSafePosition(UnitController unit)
        {
            var activeTile = tilesController.GetTileInUnit(unit.gameObject);
            if (activeTile == null)
            {
                debugController.Show((command) => { });
                debugController.AddText($"Unit {unit} isn't in a tile. Skip AI of AIController.EscapeToSafePostion.");
                return unit.transform.position;
            }

            var safeTile = activeTile.borderOnTiles.FindMin(t =>
            {
                return unitsController.GetEnemiesFrom(unit).ConvertAll(e =>
                {
                    return Vector3.Distance(t.transform.position, e.transform.position);
                }).Min();
            });

            return GetSafePosition(safeTile, unit);
        }

        /// <summary>
        /// <c>tile</c>内の最も安全な場所を取得する
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="active"></param>
        private Vector3 GetSafePosition(TileCell tile, UnitController active)
        {
            float MAX_DISTANCE_LIMIT = 25;
            var positionAndRiskScore = new Dictionary<Vector3, float>();

            foreach (var locAndScore in tile.locationsAndScores)
            {
                var position = locAndScore.location;
                if (unitsController.unitsList.TryFindFirst(u => u.IsInMyArea(locAndScore.location), out var _))
                    continue;

                var enemyCount = 0;
                positionAndRiskScore[position] = locAndScore.score;
                foreach (var u in unitsController.unitsList)
                {
                    if (!active.IsEnemyFromMe(u))
                        continue;

                    // positionから見えているEnemyの危険度を計測
                    var distance = Vector3.Distance(position, u.gameObject.transform.position);
                    if (distance > MAX_DISTANCE_LIMIT) continue;
                    var rayDistance = active.GetRayDistanceTo(u, position);
                    positionAndRiskScore[position] += MAX_DISTANCE_LIMIT / rayDistance;

                    // 敵の予想移動位置から射線が通る場合の危険度
                    var (forcastHit, distanceToForcastlyHit) = ForcastHit(position, u.actionForcast.locationToMove);
                    positionAndRiskScore[position] += MAX_DISTANCE_LIMIT / distanceToForcastlyHit;

                    // 敵が遠く危険度が低い場合
                    if (rayDistance > MAX_DISTANCE_LIMIT && distanceToForcastlyHit > MAX_DISTANCE_LIMIT)
                        continue;

                    // 敵ユニットとの射線が通りなおかつMAX_DISTANCE_LIMIT以下で狙われやすい状態
                    positionAndRiskScore[position] += rayDistance / MAX_DISTANCE_LIMIT;
                    enemyCount += forcastHit ? 2 : 1;

                    var isCoveredFromForcastingEnemy = IsCoveredFromEnemy(locAndScore, u.actionForcast.locationToMove);
                    var isCovered = IsCoveredFromEnemy(locAndScore, u.gameObject.transform.position);
                    if (isCovered || isCoveredFromForcastingEnemy)
                    {
                        // 現在のGridはCoverのSafePositionであり、かつEnemyがCoverで防御される位置にいる
                        // Cover越しの敵が2以下ならばscoreは優先される
                        // 一方で3以上ならscoreは引かれる
                        if (enemyCount <= 2)
                            positionAndRiskScore[position] -= 0.4f;
                        else
                            positionAndRiskScore[position] += 1f;
                    }
                    else
                    {
                        // EnemyがCoverで隠れれない位置にいる
                        // 位置としての優先順位は低くなる
                        positionAndRiskScore[position] += 1;
                    }
                }

            }

            if (positionAndRiskScore.Count == 0)
            {
                return active.gameObject.transform.position;
            }
            else
            {
                var bestPositionAndScore = positionAndRiskScore.FindMin(a => a.Value);
                return bestPositionAndScore.Key;
            }
        }

        #endregion

        #region Sub functions
        /// <summary>
        /// 攻撃するのに最も適した場所とターゲットのスコアを取得する 3層のすべてで使われる評価用
        /// </summary>
        /// <param name="tmpTargets">移動する位置とそこから狙える敵Units (すべて同じ場所であること)</param>
        // NOTE 特に位置関係のスコアの計算と評価
        private TmpTarget GetBetterScoreOfLocationAndTargets(List<TmpTarget> tmpTargets)
        {
            var active = unitsController.activeUnit;

            var targetedScore = (tmpTargets.Count - 1) * -0.5f;
            if (targetedScore == 0)
                targetedScore += 0.2f;

            // 恐らく敵ターンと味方ターンを明確に分けるタイプ (戦ヴァル風）にするため削除
            //// 4のターン以内の敵が撃破可能な場合それを優先
            //const float OnShotKillScore = 1f;
            //var turn0To4 = unitsController.unitsList.slice(0, 4);
            //if (turn0To4.Count != 0)
            //{
            //    var nextEnemy = turn0To4.Find(u => u.IsEnemyFromMe(active));
            //    var output = tmpTargets.Find(t => t.target.Equals(nextEnemy));
            //    if (output != null)
            //    {
            //        var damage = active.GetAIAttackDamage(nextEnemy);
            //        if (nextEnemy.info.healthPoint <= damage)
            //        {
            //            output.score = CalcBaseScore(damage, output.rate, output.target);
            //            output.score += targetedScore;
            //            output.score += output.target.info.menace / 5f;

            //            output.score += OnShotKillScore;

            //            return output;
            //        }
            //    }
            //}

            List<TmpTarget> _targets = tmpTargets.ConvertAll(t =>
            {
                var damage = active.GetAIAttackDamage(t.target);
                t.score = CalcBaseDamageScore(damage, t.rate, t.target);
                t.score += targetedScore;
                t.score += t.target.info.menace / 5f;
                t.score += t.gridAndScore.score;

                return t;
            });

            var maxTargetAndScore = _targets.FindMax(t => t.score);
            var betterTargeAndScores = _targets.FindAll(t => t.score == maxTargetAndScore.score);
            if (betterTargeAndScores.ChooseRandom(out var bestTargetAndScore))
                return bestTargetAndScore;

            return null;
        }

        /// <summary>
        /// <c>active</c>が<c>location</c>から狙えるすべてのTargetを取得する 敵Unit系の評価を行う
        /// </summary>
        /// <param name="active">行動するUnit</param>
        /// <param name="location">行動を開始する位置</param>
        private List<TmpTarget> GetTargetsOnLocation(UnitController active, LocationAndScore locAndPoint)
        {
            var enemies = unitsController.GetEnemiesFrom(active);
            var output = new List<TmpTarget>();
            foreach (var e in enemies)
            {
                var distance = active.GetRayDistanceTo(e, locAndPoint.location);
                var (hitForcast, distanceForcast) = ForcastHit(locAndPoint.location, e.actionForcast.locationToMove);
                if (distance == 0)
                    continue;

                var rate = active.GetAIAttackRate(e, locAndPoint.location) * 100;

                if (rate != 0)
                    output.Add(new TmpTarget(e, locAndPoint, rate));
            }
            return output;
        }

        /// <summary>
        /// gridの位置が<c>enemyPos</c>からカバーされる位置にいるかどうか
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="enemyPos"></param>
        /// <returns></returns>
        private bool IsCoveredFromEnemy(LocationAndScore grid, Vector3 enemyPos)
        {
            if (grid.coverObject == null)
                return false;

            var posC = new Vector2(grid.coverObject.transform.position.x,
                       grid.coverObject.transform.position.z);
            var posE = new Vector2(enemyPos.x, enemyPos.z);
            var posG = new Vector2(grid.location.x, grid.location.z);
            var rad = Utility.RadianOfTwoVector(posE - posC, posG - posC);
            var deg = rad / (Mathf.PI / 180);

            return deg > 100;
        }

        /// <summary>
        /// ダメージ量と命中確率から基本評価値を計算する (0~1)
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="rate">命中確率 0~1</param>
        /// <param name="target"></param>
        /// <returns></returns>
        private float CalcBaseDamageScore(int damage, float rate, UnitController target)
        {
            var score = (float)target.info.healthPoint / (float)damage;
            if (score >= 1)
                score = 1;
            score *= rate;

            return score;
        }

        /// <summary>
        /// HPがActiveな行動を取れるかの最低値
        /// </summary>
        /// <returns></returns>
        private bool IsWarningHealthThreshold(UnitController unit)
        {
            if (unitsController.GetEnemiesFrom(unit).Count == 1)
                return false;

            float totalHealth = unit.info.original.healthPoint + unit.info.original.extensionHealthPoint;
            if ((float)unit.info.healthPoint / totalHealth > parameters.ThresholdOfWarningHealth)
                return false;

            return true;
        }

        /// <summary>
        /// Unitがlocationに移動した際にどの方向を向くのが自然か判別
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        private Vector3 GetDirection(UnitController unit, Vector3 location)
        {
            var enemies = unitsController.GetEnemiesFrom(unit);
            enemies.ForEach(e =>
            {
                if (e.actionForcast == null)
                    e.actionForcast = Run(e);
            });

            var directionPos = Vector3.zero;
            var distanceToEnemy = 0f;
            var hitToEnemy = false;
            foreach (var e in enemies)
            {
                // Objectにhitするのだが
                // (1) hitしたObjectまでの距離が Distance(location, enemyLocationForcast) より短い場合は Objectに遮られている
                // (2) HItしたobjectまでの距離がDistance(location, enemyLocationForcast)　より長い場合は予測移動地点から射線が通る
                // (3) もしすべてのhitが Distanceより短い場合はObjectまでの距離が長い方向に向くのが適当
                // (4) 射線が通る地点が複数ある場合は最も短い場合を採用
                var (hit, distanceToHit) = ForcastHit(location, e.actionForcast.locationToMove);
                if (hitToEnemy)
                {
                    if (hit)
                    {
                        if (distanceToHit < distanceToEnemy)
                        {
                            // 上記の(4)
                            directionPos = e.actionForcast.locationToMove;
                            distanceToEnemy = distanceToHit;
                        }
                    }
                }
                else
                {
                    if (hit)
                    {
                        // 新たに射線が通る場所を見つけている
                        hitToEnemy = true;
                        directionPos = e.actionForcast.locationToMove;
                    }
                    else
                    {
                        if (distanceToEnemy < distanceToHit)
                        {
                            // (3)の状態
                            directionPos = e.actionForcast.locationToMove;
                            distanceToEnemy = distanceToHit;
                        }
                    }
                }
            }
            return directionPos;
        }

        /// <summary>
        /// TargetにObjectがない場合でもhitを計測する
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private (bool hit, float distance) ForcastHit(Vector3 origin, Vector3 target)
        {
            var ray = new Ray(origin, target - origin);
            var mask = LayerMask.GetMask(new string[] { "Object" });

            var distance = Vector3.Distance(target, origin);
            if (Physics.Raycast(ray, out var hit, 100, mask, QueryTriggerInteraction.UseGlobal))
            {
                if (hit.distance > distance)
                {
                    // Hit
                    return (true, hit.distance);
                }
                else
                {
                    // Covered by Object
                    return (false, hit.distance);
                }
            }
            return (false, 0);
        }

        #endregion

        /// <summary>
        /// EnemyUnitと攻撃位置、レートのまとめたClass
        /// </summary>
        private class TmpTarget
        {
            internal UnitController target;
            internal LocationAndScore gridAndScore;
            /// <summary>
            /// 命中確率 0~1
            /// </summary>
            internal float rate;
            /// <summary>
            /// 評価値
            /// </summary>
            internal float score = 0;

            internal Vector3 location
            {
                get => gridAndScore.location;
            }

            internal TmpTarget(UnitController target, LocationAndScore gridAndPoint, float rate)
            {
                this.target = target;
                this.gridAndScore = gridAndPoint;
                this.rate = rate;
            }

            internal TmpTarget(UnitController active)
            {
                gridAndScore = new LocationAndScore(active.gameObject.transform.position, 0);
            }

        }
    }

}
*/