using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tactics.Character;
using Tactics.Map;
using UnityEngine;
using static Tactics.AI.AIAction_x;
using static Utility;
using static Tactics.AI.AICore.Situation;

namespace Tactics.AI
{

    /*
  * No1. 敵が(locAndScoreの)位置から射線の通る位置であり倒しきれる
  * No2. 敵が位置から射線の通る位置であり倒しきれない、そして予想されるTotalDamageは耐えれる
  * No3. 敵が位置から射線の通る位置であり倒しきれない、そして予想されるTotalDamageは耐えれない
  * No4. 敵の予想移動地点から射線の通る位置である、そして予想されるTotalDamageは耐えれる
  * No5. 敵の予想移動地点から射線の通る位置である、そして予想されるTotalDamageは耐えられない
  * 
  * TotalDamageとカバー系付加
  * No1. TotalDamageが体力より低く敵の現在位置からカバーされている
  * No2. TotalDamageが体力より低く敵の現在位置からカバーされていない
  * No3. TotalDamgeが体力より高く敵の現在位置からカバーされている
  * No4. Totaldamageが体力より高く敵の現在位置からカバーされていない
  * 
  *  状況と優先度
  *  - 単体の倒しきれる敵、被ダメージを耐えられる、forcastの射線が通らない
  *  - 単体の倒しきれる敵、被ダメージは耐えられない、forcastの射線は通らない
  *  - 単体の倒しきれる敵、被ダメージは耐えられる、forcastからの合計被ダメージは耐えられる
  *  - 単体の倒しきれる敵、被ダメージは耐えられない、forcastからの射線が通る
  *  - 単体の倒しきれない敵、隣接味方の与えるダメージで倒せる、被ダメージは耐えれる、forcast X
  *  - 単体の倒しきれない敵、隣接味方の与えるダメージで倒せる、夜ダメージは耐えられない、forcast X
  *  - 単体の倒しきれない敵、隣接味方の与えるダメージで倒せる、夜ダメージは耐えられ、forcastからの合計被ダメージは耐えられる
  *  - 単体の倒しきれない敵、隣接味方の与えるダメージで倒せる、夜ダメージは耐えられない、forcastからのダメージも耐えられない
  *  - 単体の倒しきれない敵、ダメージは耐えられる、forcast X
  *  - 単体の倒しきれない敵、ダメージは耐えられない
  *  - 単体の倒しきれない敵、ダメージは耐えられる、forcastからの合計被ダメージは無理
  *  - 複数の敵
  */

    /// <summary>
    /// アルファ型のTacticsAI (2022/08/05~
    /// </summary>
    public class AlphaAI: AICore
    {
        [Tooltip("カバーに隠れているときの命中カーブ\nX軸 = 0(必ず命中) ~ 1(命中しない)")]
        [SerializeField] AnimationCurve CoveredCurve;
        [Tooltip("なるべく適正距離でAimingするためのカーブ \nX軸 = 命中率  Y軸 = 評価スコア")]
        [SerializeField] AnimationCurve NormalAimCurve;
        [Tooltip("なるべく遠くでエイミングするためのカーブ \nNormalより低い値を通る \nX軸 = 命中率  Y軸 = 評価スコア")]
        [SerializeField] AnimationCurve HardAimCurve;
        [Tooltip("どの程度のダメージを許容して積極的に動くか x=1に近づくほど大ダメージ 左肩上がり")]
        [SerializeField] AnimationCurve DamageCurve;
        [Tooltip("SafePositionを算出するときに許容するダメージ量\n" +
            "0がScore=0.9 で25%くらいから急降下してScore=0になる形のグラフ")]
        [SerializeField] AnimationCurve SafePositionDamageCurve;

        [Tooltip("移動Tileの増加に反比例して減少するScore" +
            "X軸は10倍され  (0.1, 1) ~ (0.5, 0)")]
        [SerializeField] AnimationCurve wayCountCurve;

        bool DebugAIRoutine = true;
        int DebugAIRoutineIndex = 0;

        /// <summary>
        /// AIActionの一段目
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public override IEnumerator GetAIAction()
        {
            debugTexts.Add(new AIDebugMsg("GetAIAction", $"start"));
            var debug = debugTexts.Last();

            yield return StartCoroutine(base.GetAIAction());

            //DebugAIRoutine = null;
            //if (enableForcast)
            //    DebugAIRoutine = "";

            DebugAIRoutineIndex++;

            var currentTile = unit.tileCell;

            var borderTile = currentTile.borderOnTiles.FindAll(t =>
            {
                return t.CanEnterUnitInThis(unit);
            });

            debug.message += $", findedEnemies {aiController.FindedEnemies.Count}, WarningHP {IsWarningHealthThreshold(unit)}";

            if (aiController.FindedEnemies.Count == 0)
            {
                // UnitのEnemy非発見状態
                yield return StartCoroutine( WaitRoutineAI(unit));
            }
            else if (IsWarningHealthThreshold(unit))
            {
                // HPが減り現在逃走中
                yield return StartCoroutine( EscapeAI(unit, currentTile));
            }
            else
            {
                //  AttackAI
                yield return StartCoroutine(AttackAI(unit, currentTile));
            }

            if (Result == null)
            {
                debug.message += $"\nResult returns null";
                // UnitのEnemy非発見状態のルーチンを実行
                yield return StartCoroutine(WaitRoutineAI(unit));
            }
        }

        /// <summary>
        /// OrderOfActionがActionTo_の場合どの様に動くか
        /// </summary>
        /// <param name="removedUnits">Actionによって既に排除されたUnit</param>
        /// <returns></returns>
        public override IEnumerator AfterActionAIAction(List<UnitController> removedUnits)
        {
            debugTexts.Add(new AIDebugMsg("AfterActionAIAction", "MoveAI"));
            yield return StartCoroutine(MoveAI(unit, unit.tileCell, removedUnits));

            yield return true;
        }


        public override IEnumerator FindEnemyAction()
        {
            var debugTxt = new AIDebugMsg("FindEnemyAction", "");
            debugTexts.Add(debugTxt);

            // AIUnitが
            tilesController.ForceUpdateUnitsInTile();

            var currentTile = unit.tileCell;
            var situations = currentTile.pointsInTile.ConvertAll(p => GetPositionAction(unit, p, currentTile));

            //if (tilesController.UnitIsMoved)
            //{
            //    // 既にUnitが別のCellに移動しており現在のCellからは移動できない
            //}
            //else
            //{
            //    // Unitは隣接するTileに移動できるためこれを考慮
            //    foreach(var tile in currentTile.borderOnTiles)
            //    {
            //        if (!tile.CanEnterUnitInThis(unit)) continue;
            //        situations.AddRange(tile.pointsInTile.ConvertAll(p => GetPositionAction(unit, p, tile)));
            //    }
            //}

            var (situation, enemy, _) = situations.FindMax(e => e.point);
            debugTxt.message = $"Better situation: {situation}, {enemy}";

            var action = new AIAction_x(unit);

            if (enemy != null)
            {
                action.orderOfAction = OrderOfAction_x.ActionToSkip;
                action.target = enemy.unit;
                action.rate = enemy.HitRateToThis;
            }
            else
            {
                action.orderOfAction = OrderOfAction_x.Skip;
            }
            action.locationToMove = situation.pointInTile.location;

            Result = action;

            yield return null;
        }

        #region 周辺環境を取得する

        /// <summary>
        /// 地点が<c>unit</c>にとってどのような状況になっているか判断する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="locAndScore">評価する地点</param>
        /// <param name="removed"></param>
        /// <param name="tileCell"><c>locAndScore</c>の位置しているtile</param>
        /// <returns>0_1でより評価の高い地点が安全</returns>
        private Situation GetSituation(UnitController unit, PointInTile locAndScore, TileCell tileCell, List<UnitController> removed = null)
        {
            // List<RatingInfo> => 特定位置で通る射線

            float MAX_DISTANCE_LIMIT = 100;
            var pos = locAndScore.location;

            Enemy _GetEnemyInfo(UnitController enemy, bool isForcast)
            {
                if (removed != null && removed.Contains(enemy))
                    return null;

                var enemyPos = enemy.gameObject.transform.position;
                //if (isForcast)
                //    enemyPos = enemy.aiController.actionForcast.locationToMove;

                var distance = Vector3.Distance(pos, enemyPos);
                if (distance > MAX_DISTANCE_LIMIT) return null;

                var rayDist = 0f;
                if (isForcast)
                {
                    var forcastHit = false;
                    (forcastHit, rayDist) = ForcastHit(pos, enemyPos);
                    if (!forcastHit) return null;
                }
                else
                {
                    rayDist = unit.GetRayDistanceTo(enemy, pos);
                    if (rayDist == 0) return null;
                }
                var enemyInfo = new Situation.Enemy(enemy, unit, locAndScore)
                {
                    isForcastInfo = isForcast,
                    distance = rayDist,
                    canKillThis = (enemy.CurrentParameter.HealthPoint - unit.GetAIAttackDamage(enemy)) <= 0,
                    canDefenceFromThis = (unit.CurrentParameter.HealthPoint - enemy.GetAIAttackDamage(unit)) > 0,
                    isCoveredFromThis = IsCoveredFromEnemy(locAndScore, enemyPos)
                };
                // 複数の敵から射線の通る位置かどうか
                enemyInfo.CalcHitRate();

                return enemyInfo;
            }

            // 敵の現在位置からsituationを計算
            var situation = new Situation(unit, tileCell, locAndScore) ;
            foreach (var detected in aiController.FindedEnemies)
            {
                var info = _GetEnemyInfo(detected.Enemy, false);
                if (info != null)
                    situation.enemies.Add(info);
            }

            // 敵の予想移動地点からsituationを計算
            var forcastEnemies = aiController.FindedEnemies.ConvertAll(e => e.Enemy);
            foreach(var enemy in forcastEnemies)
            {
                // 高速化のために既にsituationに登録されているenemyのforcastは計算しない
                // 処理能力が充分ある場合は計算してもいいが
                //if (situation.enemies.Exists(e => e.unit.Equals(enemy)))
                //    continue;
                var info = _GetEnemyInfo(enemy, true);
                if (info != null)
                    situation.enemies.Add(info);
            }

            return situation;
        }
        
        /// <summary>
        /// Unitの現在位置のSituationを取得する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private Situation GetCurrentSituation(UnitController unit)
        {
            var loc = new PointInTile(unit);
            return GetSituation(unit, loc, unit.tileCell);
        }

        #endregion

        #region 攻撃的なAI
        /// <summary>
        /// 攻撃的なAI
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private IEnumerator AttackAI(UnitController unit, TileCell current)
        {
            debugTexts.Add(new AIDebugMsg("AttackAI", ""));
            var debug = debugTexts.Last();

            var nearTileAndSituations = new Dictionary<TileCell, List<Situation>>();

            // CurrentのSituationを取得
            (Situation situation, Enemy enemy, float point) currentTileSituation = (null, null, 0);
            IEnumerator _CurrentTile()
            {
                var tmp = new List<(Situation situation, Enemy enemy, float point)>();
                foreach (var pos in current.pointsInTile)
                {
                    tmp.Add(GetPositionAction(unit, pos, current));
                }
                currentTileSituation = tmp.FindMax(t => t.point);
                nearTileAndSituations[current] = tmp.ConvertAll(t => t.situation);
                yield return true;
            }

            var currentCoroutine = _CurrentTile();
            StartCoroutine(currentCoroutine) ;

            // BorderのSituationを取得
            var borderCoroutines = new List<IEnumerator>();
            var borderTileSituations = new Dictionary<TileCell, (Situation situation, Enemy enemy, float point)>();
            IEnumerator _BorderTile(TileCell tile)
            {
                var tmp = new List<(Situation situation, Enemy enemy, float point)>();
                foreach(var pos in tile.pointsInTile)
                {
                    tmp.Add(GetPositionAction(unit, pos, tile));
                }

                borderTileSituations[tile] = tmp.FindMax(t => t.point);
                nearTileAndSituations[tile] = tmp.ConvertAll(t => t.situation);
                yield return true;
            }

            foreach(var border in current.borderOnTiles)
            {
                if (!border.CanEnterUnitInThis(unit))
                    continue;
                var coroutine = _BorderTile(border);
                borderCoroutines.Add(coroutine);
                StartCoroutine(coroutine);
            }

            // 現在位置のSituation
            var currentLocAndScore = new PointInTile(unit);
            var currentPosSituation = GetPositionAction(unit, currentLocAndScore, current);

            // DEBUG Debug show score
            var _currentPos = current.pointsInTile.FindMin(l => Vector3.Distance(l.location, unit.transform.position));
            _currentPos.DebugScore = currentPosSituation.point;
            

            // 現在位置からActionToMoveできる場合はそれを優先するように
            if (currentPosSituation.enemy != null)
            {
                var addPos = currentPosSituation.point + 0.2f;
                if (addPos > 1)
                    addPos = 1;
                currentPosSituation.point += CalcRate(addPos, currentPosSituation.point, 0.3f, 0.7f);
            }

            // CurrentCoroutineを待つ
            while (currentCoroutine.IsNotCompleted(true))
                yield return null;

            // BorderCoroutineを待つ
            while (borderCoroutines.AreNotCompleted(true))
                yield return null;

            if(currentPosSituation.enemy == null &&
               currentTileSituation.enemy == null &&
               borderTileSituations.ToList().Find(b => b.Value.enemy != null).Value.enemy == null)
            {
                debug.message = $"Route of MoveToWeakTarget SituTiles: {string.Join(",", nearTileAndSituations.Keys)}";

                // Targetがいない場合はWeakTargetに向かう
                yield return StartCoroutine(MoveToWeakTarget(unit, nearTileAndSituations));
            }
            // 各Situationを比較
            else if (currentPosSituation.point > borderTileSituations.FindMax(e => e.Value.point).Value.point &&
                     currentPosSituation.point > currentTileSituation.point)
            {
                debug.message = "Route of Action on this position";

                // 現在位置から攻撃するのが最も良い場合 ActionToMove or ActionToSkip
                var action = new AIAction_x(unit);
                action.orderOfAction = OrderOfAction_x.ActionTo_ ;
                action.target = currentPosSituation.enemy.unit;
                action.rate = currentPosSituation.enemy.HitRateToThis;
                Result = action;

            }
            else if (currentTileSituation.point > borderTileSituations.FindMax(e => e.Value.point).Value.point)
            {
                debug.message = "Route of Action on current tile";

                // 現在のTile内で移動してから攻撃するのが最も良い場合 MoveToAction
                var action = new AIAction_x(unit);
                action.orderOfAction = OrderOfAction_x.MoveToAction;
                action.locationToMove = currentTileSituation.situation.pointInTile.location;
                action.target = currentTileSituation.enemy.unit;
                action.rate = currentTileSituation.enemy.HitRateToThis;
                Result = action;
            }
            else if (borderTileSituations.FindMax(e => e.Value.point).Value.enemy != null)
            {
                debug.message = "Route of Action on border tile";

                // 横のTil内で移動してから攻撃するのが最も良い場合 MoveToAction
                var betterTileSituation = borderTileSituations.FindMax(e => e.Value.point);
                var action = new AIAction_x(unit);
                action.orderOfAction = OrderOfAction_x.MoveToAction;
                action.locationToMove = betterTileSituation.Value.situation.pointInTile.location;
                action.target = betterTileSituation.Value.enemy.unit;
                action.rate = betterTileSituation.Value.enemy.HitRateToThis;
                Result = action;
            }
        }

        #region GetPositionAction
        /// <summary>
        /// Positionが行動するのに良い地点か評価する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="pos"></param>
        /// <param name="tile">指定<c>pos</c>の位置しているTile</param>
        /// <returns></returns>
        private (Situation situation, Situation.Enemy enemy, float point) GetPositionAction(UnitController unit, PointInTile pos, TileCell tile)
        {
            var situation = GetSituation(unit, pos, tile);

            var currentHits = situation.enemies.FindAll(s => s.hit && !s.isForcastInfo);
            var forcastHits = situation.enemies.FindAll(s => !s.hit && s.isForcastInfo);
            // pointは高いほど移動するに良い地点
            (Situation situation, Situation.Enemy enemy, float point) output = (new Situation(unit, tile, pos), null, 0);

            if (currentHits.Count == 1 && forcastHits.Count == 0)
            {
                var point = SingleEnemyNoForcast(situation);
                output.situation = situation;
                output.enemy = currentHits[0];
                output.point = point;
            }
            else if (currentHits.Count == 1 && forcastHits.Count != 0)
            {
                var point = SingleEnemyAndForcast(situation);
                output.situation = situation;
                output.enemy = currentHits[0];
                output.point = point;
            }
            else if (currentHits.Count != 0 && forcastHits.Count != 0)
            {
                var (target, point) = MultiEnemies(situation);
                output.situation = situation;
                output.enemy = target;
                output.point = point;
            }
            else
            {
            }

            // Enemyが移動先のTileにいる場合反撃を受けるためスコアを減らす
            var forcastDamageWhenEnter = situation.forcastDamageToCounterattack;
            if (forcastDamageWhenEnter != 0)
            {
                var enemyScore =  forcastDamageWhenEnter / situation.active.CurrentParameter.HealthPoint;
                if (enemyScore > 1) enemyScore = 1;
                enemyScore = 1 - enemyScore;
                if (enemyScore < output.point)
                {
                    output.point = CalcRate(output.point, enemyScore, 0.4f, 0.6f);
                }
            }

            // DEBUG Show debug score
            pos.DebugScore = output.point;

            return output;
        }

        /// <summary>
        /// 単体の敵、Forcastがない場合
        /// </summary>
        /// <param name="input"></param>
        private float SingleEnemyNoForcast(Situation input)
        {
            if (!input.enemies.IndexAt(0, out var enemy))
                return 0;

            var debugText = $"{DebugAIRoutineIndex} SingleEnemyNoForcast ";
            debugText += $": Enemy distance {enemy.distance} {enemy.HitRateToThis}\n";

            var output = 0f;

            if (enemy.DamageRateToThis == 1)
            {
                // 倒し切れる場合
                // カバー分のポイント、（近いほど軽減されなくなる）
                var coverPoint = 0f;
                if (enemy.isCoveredFromThis)
                    coverPoint = CoveredCurve.Evaluate(1 - enemy.HitRateToThis);
                var aimPoint = NormalAimCurve.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.85f, 0.15f);

                debugText += $"Damage 100% {enemy}";

            }
            else if (enemy.DamageRateToThis >= 0.5)
            {
                // 50%以上体力を削れる場合
                var coverPoint = 0f;
                if (enemy.isCoveredFromThis)
                    coverPoint = CoveredCurve.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = input.tileCell.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(input.active));
                var firendPoint = nearFriends.Count >= 2 ? 0.9f : 0.5f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = NormalAimCurve.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.9f, 0.1f);

                debugText += $"Damage 50% {enemy} A{aimPoint} C{coverPoint}";
            }
            else if (enemy.DamageRateToThis >= 0.3)
            {
                // 30%以上体力を削れる場合
                var coverPoint = 0f;
                if (enemy.isCoveredFromThis)
                    coverPoint = CoveredCurve.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = input.tileCell.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(input.active));
                var firendPoint = nearFriends.Count >= 3 ? 0.8f : 0.4f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = HardAimCurve.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.9f, 0.1f);


                debugText += $"Damage 30% {enemy} P{aimPoint} C{coverPoint}";
            }
            else
            {
                // 30%以下でしか体力を削れない場合
                var coverPoint = 0f;
                if (enemy.isCoveredFromThis)
                    coverPoint = CoveredCurve.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = input.tileCell.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(input.active));
                var firendPoint = nearFriends.Count >= 4 ? 0.6f : 0.1f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = HardAimCurve.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.4f, 0.6f);

                debugText += $"Damage under 30% {enemy} P{aimPoint} c{coverPoint}";
            }

            return output;
        }

        /// <summary>
        /// 単体の敵、Forcastがある場合
        /// </summary>
        /// <param name="situation"></param>
        /// <param name="forcasts"></param>
        /// <returns></returns>
        private float SingleEnemyAndForcast(Situation situation)
        {
            var debugText = "";

            var forcasts = situation.enemies.FindAll(e => e.isForcastInfo);

            var output = 0f;
            var currentPoint = SingleEnemyNoForcast(situation);

            var total = forcasts.Sum(f =>
            {
                float damage = f.DamageFromThis;
                if (f.DamageRateFromThis < 0.6)
                    damage *= f.HitRateToThis;
                return (int)damage;
            });

            var forcastDamageRate = situation.active.CurrentParameter.HealthPoint <= total ? 1f : total / situation.active.CurrentParameter.HealthPoint;
            var forcastDamagePoint = DamageCurve.Evaluate(forcastDamageRate);

            output = CalcRate(currentPoint, forcastDamageRate, 0.6f, 0.4f);

            var forcastCount = situation.enemies.FindAll(e => e.isForcastInfo).Count;

            debugText = $"SingleEnemyAndForcast forcast:{forcastCount}  enemy:{situation.enemies.Count - forcastCount}";

            return output;
        }

        /// <summary>
        /// 複数の敵
        /// </summary>
        /// <param name="enemies"></param>
        /// <param name="forcasts"></param>
        /// <returns></returns>
        private (Situation.Enemy target, float score) MultiEnemies(Situation situation)
        {
            var debugText = "MultiEnemies";

            var active = situation.active;
            var canKillEnemies = situation.enemies.FindAll(e => !e.isForcastInfo && e.canKillThis);
            var enemies = situation.enemies.FindAll(e => !e.isForcastInfo);
            enemies.Sort((a, b) => (int)((a.DamageFromThis - b.DamageFromThis) * 100));

            var totalDamageFromEnemies = situation.enemies.Sum(e => e.DamageFromThis);
            // 敵の攻撃で受けるダメージの割合
            var damageRateFromEnemies = totalDamageFromEnemies >= active.CurrentParameter.HealthPoint ? 1 : totalDamageFromEnemies / active.CurrentParameter.HealthPoint;

            // RatingInfoのtargetからの反撃に対してカバーできる箇所にいるか計算
            float CalcCoverPoint(Situation.Enemy target, float hitRate)
            {
                var coverPoint = 0f;
                if (target.isCoveredFromThis)
                    coverPoint = CoveredCurve.Evaluate(1 - hitRate);

                var nearFriends = situation.tileCell.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(active));
                var firendPoint = nearFriends.Count >= 2 ? 0.9f : 0.5f;
                return CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);
            }

            // 次のターンで自身が撃破されないように 6割以下のダメージに抑えられる場合
            if (damageRateFromEnemies < 0.6f)
            {
                // 自身に高いダメージを与え尚且つこのターンの攻撃で撃破可能な敵ユニット
                var hightDamageAndCanKill = enemies.Find(e => canKillEnemies.Contains(e));
                if (hightDamageAndCanKill != null)
                {
                    var aimPoint = NormalAimCurve.Evaluate(hightDamageAndCanKill.HitRateToThis);

                    var coverPoint = CalcCoverPoint(hightDamageAndCanKill, hightDamageAndCanKill.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.8f, 0.2f);

                    return (hightDamageAndCanKill, totalPoint);
                }

                enemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));

                if (enemies.IndexAt(0, out var betterDamageTarget))
                {
                    // 敵のHPを60%以上削れる場合
                    var aimPoint = CalcRate(betterDamageTarget.HitRateToThis, betterDamageTarget.DamageRateToThis, 0.6f, 0.4f);
                    aimPoint = NormalAimCurve.Evaluate(aimPoint);

                    var coverPoint = CalcCoverPoint(betterDamageTarget, betterDamageTarget.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.7f, 0.3f);

                    return (betterDamageTarget, totalPoint);
                }

            }
            else
            {
                // 上記以外　被撃破の可能性があり　あまり推奨しない

                enemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));
                if (enemies.IndexAt(0, out var betterDamageTarget))
                {
                    var aimPoint = CalcRate(betterDamageTarget.HitRateToThis, betterDamageTarget.DamageRateToThis, 0.6f, 0.4f);
                    aimPoint = HardAimCurve.Evaluate(aimPoint);

                    var coverPoint = CalcCoverPoint(betterDamageTarget, betterDamageTarget.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.6f, 0.4f);

                    return (betterDamageTarget, totalPoint);
                }

            }

            return (null, 0);
        }

        /// <summary>
        /// 現在とターン終了後の未来において的に遭遇しない位置
        /// </summary>
        /// <returns></returns>
        private float NoEnemies(UnitController unit)
        {
            var enemies = unitsController.GetEnemiesFrom(unit);
            var distanceFromEnemies = enemies.ConvertAll((e) =>
            {
                return Vector3.Distance(e.gameObject.transform.position, unit.gameObject.transform.position);
            });
            var avg = distanceFromEnemies.Sum() / distanceFromEnemies.Count;
            return distanceFromEnemies.Min() / avg;
        }
        #endregion

        #endregion

        #region 逃走中なAI
        /// <summary>
        /// 逃走をするためのAI FirendCountは2以上
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="current"></param>
        /// <param name="removedUnits">驚異査定から除外されるUnit</param>
        /// <param name="canAttack">攻撃してから逃げる</param>
        /// <returns></returns>
        private IEnumerator EscapeAI(UnitController unit, TileCell current, List<UnitController> removedUnits = null, bool canAttack = true)
        {
            debugTexts.Add(new AIDebugMsg("EscapeAI", ""));
            var debug = debugTexts.Last();

            // TODO 隣接Cellに敵がいないかつ当該セルに敵がいない条件を付加
            var cells = current.borderOnTiles.FindAll(t => t.CanEnterUnitInThis(unit));
            cells.Add(current);

            // 隣接Tileに敵がいないかつ当該セルに敵がいない
            cells.RemoveAll(t =>
            {
                if (t.UnitsInCell.Find(u => u.IsEnemyFromMe(unit)))
                    return true;
                if (t.borderOnTiles.Find(bt => bt.UnitsInCell.Find(u => u.IsEnemyFromMe(unit))))
                    return true;
                return false;
            });
            if (cells.Count == 0)
            {
                // 囲まれているため現時点のTileから移動しない
                cells.Add(current);
            }

            var cellAndSituation = new List<(TileCell tile, (Situation situation, float point) value)>();
            IEnumerator _GetSituations(TileCell c)
            {
                var situations = c.pointsInTile.ConvertAll(l =>
                {
                    return GetSituation(unit, l, c, removedUnits);
                });
                cellAndSituation.Add((c, GetSafePoint(unit, situations)));
                yield return true;
            }
            var situationsCoroutines = cells.ConvertAll(c => _GetSituations(c));
            situationsCoroutines.ForEach(c => StartCoroutine(c));

            // 各Friendsまでの最短距離を取得
            var friends = unitsController.GetFriendsOf(unit);
            var wayToFriends = new List<(UnitController friend, List<TileCell> way)>();
            IEnumerator _ShrotestWaies()
            {
                wayToFriends = friends.ConvertAll(f =>
                {
                    return (f, tilesController.GetShortestWay(current, f.tileCell));
                });
                yield return true;
            }
            var shortestWaiesCoroutine = _ShrotestWaies();
            StartCoroutine(shortestWaiesCoroutine);

            // 各Coroutineを待つ
            while (shortestWaiesCoroutine.IsNotCompleted(true))
                yield return null;

            while (situationsCoroutines.AreNotCompleted(true))
                yield return null;

            Vector3 nextPoint = unit.transform.position ;
            if (wayToFriends.Count != 0)
            {
                // Friendのいる方向に逃げる
                // 道順までのスコアが同じならHPの多い方に逃げる
                var max = wayToFriends.Max(a => a.way.Count * 100 + ((a.friend.CurrentParameter.HealthPoint * 100) / (a.friend.CurrentParameter.Data.HealthPoint * 100)));
                var _max = (float)max / 100;

                wayToFriends.Sort((a, b) =>
                {
                    var wayA = a.way.Count * 100 + ((a.friend.CurrentParameter.HealthPoint * 100) / (a.friend.CurrentParameter.Data.HealthPoint * 100));
                    var rateA = ((float)wayA / 100) / _max;
                    var wayB = b.way.Count * 100 + ((b.friend.CurrentParameter.HealthPoint * 100) / (b.friend.CurrentParameter.Data.HealthPoint * 100));
                    var rateB = ((float)wayB / 100) / _max;

                    var situA = cellAndSituation.Find(x => x.tile == a.way[1]);
                    var situB = cellAndSituation.Find(x => x.tile == b.way[1]);

                    var A = CalcRate(rateA, situA.value.point, 0.3f, 0.7f);
                    var B = CalcRate(rateB, situB.value.point, 0.3f, 0.7f);

                    var _A = (int)(A * 100);
                    var _B = (int)(B * 100);

                    return _A - _B;
                });

                var nextTile = wayToFriends[0].way[1];
                nextPoint = cellAndSituation.Find(cs => cs.tile == nextTile).value.situation.pointInTile.location;
            }
            else
            {
                // 仲間への道が存在しない場合
                // 隣接するCellで最も安全な箇所
                if (cellAndSituation.Count != 0)
                {
                    var safeSituation = cellAndSituation.FindMin(cs => cs.value.point).value.situation;
                     nextPoint = safeSituation.pointInTile.location;
                }
                else
                {
                    PrintError("EscapeAI: No situation");
                }
            }

            var action = new AIAction_x(unit);
            action.locationToMove = nextPoint;
                
            // 現在位置からActionで狙える敵が存在する場合に攻撃しておいてから逃げる
            if (canAttack)
            {
                var currentSitu = GetCurrentSituation(unit);
                var currentEnemies = currentSitu.enemies.FindAll(e => !e.isForcastInfo);
                if (currentEnemies.Count != 0)
                {
                    currentEnemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));
                    action.target = currentEnemies[0].unit;
                    action.orderOfAction = OrderOfAction_x.ActionToMove;
                }
                else
                {
                    action.orderOfAction = OrderOfAction_x.MoveToSkip;
                }
            }
            else
            {
                action.orderOfAction = OrderOfAction_x.MoveToSkip;
            }

            Result = action;

            yield return true;
        }
        #endregion

        #region 狙える位置に移動するAI
        /// <summary>
        /// 今のターンで狙える敵が居ないため発見されている最も弱い敵に移動する detectedEnemies > 0
        /// </summary>
        /// <param name="tileAndSituations"></param>
        private IEnumerator MoveToWeakTarget(UnitController unit, Dictionary<TileCell, List<Situation>> tileAndSituations)
        {
            debugTexts.Add(new AIDebugMsg("MoveToWeakTarget", ""));
            var DebugMsg = debugTexts.Last();

            DebugMsg.message = $"MoveToWeakTarget: active({unit}), ";

            var activeTile = unit.tileCell;
            //var targets = unitsController.unitsList.FindAll(u => u.IsEnemyFromMe(unit));
            DebugMsg.message += $"current({activeTile})\n";

            // 既に見つけているEnemyのScoreを計算
            var enemies = aiController.FindedEnemies.ConvertAll(d =>
            {
                return (d.Enemy, CalcHealthScore(d.Enemy));
            });
            enemies.Sort((a, b) => (int)((b.Item2 - a.Item2) * 1000));
            var targets = enemies.Slice(0, 3).ToDictionary(a => a.Enemy, a => a.Item2);
            DebugMsg.message += $"{targets.Count} Enemies, ";

            // Enemyへの道のりを計算
            var unitCellDict = new Dictionary<UnitController, TileCell>();
            // cellScoreDict.key = 目的地TileCell,  cellScoreDict.value.next = 目的地に向かうための次のTile
            var cellScoreDict = new Dictionary<TileCell, (float score, TileCell next)>();

            var targetScoreAndWaysDict = new Dictionary<TileCell, List<(float score, TileCell next)>>();
            targets.Keys.ToList().ForEach(t =>
            {
                targetScoreAndWaysDict[t.tileCell] = new List<(float score, TileCell next)>();
                unitCellDict[t] = t.tileCell;
            });

            // 最大3つの経路を探索する
            List<List<TileCell>> SearchWays(TileCell end)
            {
                var output = new List<List<TileCell>>();

                var way1 = tilesController.GetShortestWay(activeTile, end);
                output.Add(way1);
                // 隣接Cellの場合 Way探索は1ルートのみ
                if (way1.Count <= 2)
                    return output;

                var exceptTiles = new List<TileCell>();

                exceptTiles.Add(way1[1]);
                var way2 = tilesController.GetShortestWay(activeTile, end, exceptTiles);

                if (way2.Count == 0)
                {
                    // way1[1]を通らないway2が存在しないということ
                    if (way1.Count > 3)
                    {
                        exceptTiles.Clear();
                        exceptTiles.Add(way1[2]);
                        var way3 = tilesController.GetShortestWay(activeTile, end, exceptTiles);
                        if (way3.Count != 0)
                            output.Add(way3);
                    }
                    else
                    {
                        // way1.Count == 3 ということは start - way[1] - end　以外のルートが存在しない
                        return output;
                    }
                }
                else
                {
                    // way1[1]を通らないway2が存在する
                    output.Add(way2);

                    if (way2.Count > 3)
                    {
                        exceptTiles.Add(way2[1]);
                        var way3 = tilesController.GetShortestWay(activeTile, end, exceptTiles);
                        if (way3.Count != 0)
                            output.Add(way3);
                    }

                }

                return output;
            }

            // TODO 現在地点のTileにEnemyが存在する場合にway.count == 1となってerror
            // ActiveCell -> TargetCellへ距離が近いほど優先的
            // Goalに向けてMoveするための初期経路を取得
            IEnumerator GetWaysForEachTargets(TileCell goalTile)
            {
                if (goalTile == activeTile)
                {
                    // 敵が現在のcellにいる
                    targetScoreAndWaysDict[goalTile] = new List<(float, TileCell)> { (1, goalTile) };
                    yield break;
                }

                var ways = SearchWays(goalTile);
                DebugMsg.message += $"{ways.Count} ways ";
                foreach (var way in ways)
                {
                    DebugMsg.message += string.Join(".", way) + "\n";
                    var wayScore = wayCountCurve.Evaluate((float)ways.Count / 10f);

                    if (way.Count > 1)
                    {
                        if (targetScoreAndWaysDict[goalTile] != null)
                            targetScoreAndWaysDict[goalTile].Add((wayScore, way[1]));
                        else
                            targetScoreAndWaysDict[goalTile] = new List<(float, TileCell)> { (wayScore, way[1]) };
                    }
                    
                }

                yield return true;
            }

            var coroutines = targetScoreAndWaysDict.ToList().ConvertAll(pair =>
            {
                return StartCoroutine(GetWaysForEachTargets(pair.Key));
            });

            // Coroutineが終了するまで待つ
            foreach(var c in coroutines)
            {
                yield return c;
            }

            // すべてのスコアを合算したもの
            var totalScoreAndSituation = new List<(Situation situ, float point)>();
            var tileAndSafeSituationBuffer = new Dictionary<TileCell, (Situation situ, float point)>();
            // WayScoreとHealthScoreを加算
            unitCellDict.ToList().ForEach(pair =>
            {
                // 攻撃に適したEnemyを表すPoint
                // 攻撃に適したEnemyを表すPoint
                // 0~1で高いほど適したUnit
                var enemyScore = targets[pair.Key];

                // pair.Value (TileCell) に行くための道順をGetSafePointする
                targetScoreAndWaysDict[pair.Value].ForEach(scoreAndNext =>
                {
                    // 移動の短くて済む 高いほうが良い経路 
                    var wayScore = scoreAndNext.score;

                    if (!tileAndSafeSituationBuffer.TryGetValue(scoreAndNext.next, out var safe))
                    {
                        // Bufferにない場合は作成
                        var situations = tileAndSituations[scoreAndNext.next];
                        safe = GetSafePoint(unit, situations);
                        tileAndSafeSituationBuffer[scoreAndNext.next] = safe;
                    }
                    // safe.pointは移動地点として安全な場所
                    // safe.pointは 0~1で高いほど安全
                    var safePoint = safe.point;

                    totalScoreAndSituation.Add((safe.situ, safePoint + wayScore + enemyScore));
                });
            });
            
            // DEBUG Error when run forcast
            var betterSitu = totalScoreAndSituation.FindMax(t => t.point);

            var output = new AIAction_x(unit);
            output.locationToMove = betterSitu.situ.pointInTile.location;
            output.orderOfAction = OrderOfAction_x.MoveToSkip;
            output.rate = 0;
            output.target = null;
            Result = output;

            yield break;

            // WayScoreとHealthScoreを加算して合計スコア
            var unitScoreList = new List<((TileCell next, UnitController target) cellAndTarget, float score)>();
            unitCellDict.ToList().ForEach(pair =>
            {
                var healthScore = CalcHealthScore(pair.Key);
                var cellAndScore = cellScoreDict[pair.Value];
                var wayScore = cellAndScore.score;
                var elem = ((cellAndScore.next, pair.Key), healthScore + wayScore);
                unitScoreList.Add(elem);
            });
           

            if (unitScoreList.Count == 0)
            {
                output.orderOfAction = OrderOfAction_x.Skip;
                Result = output;
                yield break;
            }
            var result = unitScoreList.FindMax(a => a.score);

            var nextCellAndTarget = result.cellAndTarget;

            if (nextCellAndTarget.target == null)
            {
                output.orderOfAction = OrderOfAction_x.Skip;

                Result = output;
                yield break;
            }

            var situations = tileAndSituations[nextCellAndTarget.next];
            var safeSituation = GetSafePoint(unit, situations);

            output.locationToMove = safeSituation.situation.pointInTile.location;
            output.orderOfAction = OrderOfAction_x.MoveToSkip;
            output.rate = 0;
            output.target = null;

            Result = output;
        }

        // TODO menaceとかsmartとかの古いパラメーターを使用しているため改善する
        /// <summary>
        /// 体力面での敵ユニットの評価値
        /// </summary>
        /// <param name="target">標的となる敵Unit</param>
        /// <param name="orderOfAction">ActiveUnitのIndexを0にした行動順リスト</param>
        /// <returns>0~1で値が高いほど攻撃するに適したTarget</returns>
        /// 味方Unitの連撃を考慮
        public float CalcHealthScore(UnitController target)
        {
            var score = (unit.itemController.CalcDamage(target) / unit.CurrentParameter.TotalHealthPoint) * 0.7f;
            score += 0.3f * unit.CurrentParameter.menace;

            var rand = UnityEngine.Random.Range(0, 1.0001f);

            score += rand;

            return score;
        }

        // Escape, ToWeakTarget, SafeHP系のスコアを出す Attack系のGetPositionActionの逆的な
        /// <summary>
        /// <c>situations</c>の中から安全な場所とそのスコアを算出する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="situations"></param>
        /// <param name="removeUnit">計算から外すUnit</param>
        /// <returns>(安全なsituation, 0~1で低いほど安全)</returns>
        private (Situation situation, float point) GetSafePoint(UnitController unit, List<Situation> situations)
        {
            // TODO Forcastのenemyを考慮していない
            // Situation.Enemy.positionを利用して

            // Scoreは0~1で高いほど有力地
            var situationAndRiskScore = new Dictionary<Situation, float>();
            foreach(var situation in situations)
            {
                if (unitsController.UnitsList.FindAll(u => u.IsInMyArea(situation.pointInTile.location)).Count != 0)
                    continue;
                situationAndRiskScore[situation] = 1f;

                if (situation.enemies.Count == 0)
                {
                    var allEnemies = unitsController.UnitsList.FindAll(u => unit.IsEnemyFromMe(u));
                    allEnemies.Sort((a, b) => {
                        var distA = Vector3.Distance(unit.transform.position, a.transform.position);
                        var distB = Vector3.Distance(unit.transform.position, b.transform.position);
                        return (int)((distA - distB) * 100);
                    });

                    var isCovered = IsCoveredFromEnemy(situation.pointInTile, allEnemies[0].transform.position);
                    situationAndRiskScore[situation] = isCovered ? 1f : 0.96f;
                }
                else
                {
                    foreach (var enemy in situation.enemies)
                    {
                        var damageScore = CalcRate(enemy.DamageRateFromThis, enemy.HitRateToThis, 0.95f, 0.1f);
                        damageScore = SafePositionDamageCurve.Evaluate(damageScore);

                        var coverScore = enemy.isCoveredFromThis ? 0.05f : 0f;

                        situationAndRiskScore[situation] += damageScore + coverScore;
                        situationAndRiskScore[situation] /= 2;
                    }
                }

                // DEBUG Show debug score
                situation.pointInTile.DebugScore = situationAndRiskScore[situation];
            }

            var _list = situationAndRiskScore.ToList().Shuffle();
            var max = _list.FindMax(s => s.Value);


            DebugPositions.Add(max.Key.pointInTile.location);

            return (max.Key, max.Value);
        }

        #endregion

        #region 移動のみするAI
        /// <summary>
        /// 移動のみで行動しないAI
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="current"></param>
        /// <param name="removedUnits"></param>
        /// <returns></returns>
        private IEnumerator MoveAI(UnitController unit, TileCell current, List<UnitController> removedUnits)
        {
            debugTexts.Add(new AIDebugMsg("MoveAI", ""));
            // HPが危険値ならSafePosition
            // HPが通常値でかつ現在CellとBorderから狙える敵がいない場合はWeakTarget
            // HPが通常値でかつ狙える敵がいる場合はSafePosition

            if (IsWarningHealthThreshold(unit))
                yield return StartCoroutine(EscapeAI(unit, current, removedUnits, false));
            else
                yield return StartCoroutine(SafeHPMoveAI(unit, current, removedUnits));
            
        }

        /// <summary>
        /// HPに余裕があるとき次の行動のために移動するAI　基本遠方に敵がいる場合のAI
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="current"></param>
        /// <param name="removedUnits"></param>
        /// <param name="excludedTile">除外する</param>
        /// <returns></returns>
        private IEnumerator SafeHPMoveAI(UnitController unit, TileCell current, List<UnitController> removedUnits)
        {
            debugTexts.Add(new AIDebugMsg("SafeHPMoveAI", $""));
            var debug = debugTexts.Last();

            // CurrentTileのSituationを取得する
            var currentSituation = new List<Situation>();
            IEnumerator _GetCurrentSituation()
            {
                currentSituation = current.pointsInTile.ConvertAll(p =>
                {
                    return GetSituation(unit, p, current, removedUnits);
                });
                yield return true;
            }
            var currentCoroutine = _GetCurrentSituation();
            StartCoroutine(currentCoroutine);

            // 横のSituationを取得する
            var tilesSituation = new Dictionary<TileCell, List<Situation>>();
            var tilesCoroutines = new List<IEnumerator>();
            IEnumerator _GetBorderSituation(TileCell tile)
            {
                var tmp = tile.pointsInTile.ConvertAll(p => GetSituation(unit, p, tile));
                tilesSituation[tile] = tmp;
                yield return true;
            }

            var safeTiles = current.borderOnTiles.FindAll(t => 
            {
                return !t.UnitsInCell.Exists(u => aiController.FindedEnemies.Exists(e => e.Enemy == u));
            });

            tilesCoroutines = safeTiles.ConvertAll(t =>
            {
                var coroutine = _GetBorderSituation(t);
                StartCoroutine(coroutine);
                return coroutine;
            });

            // Coroutineを待つ
            while (currentCoroutine.IsNotCompleted(true))
                yield return null;

            while (tilesCoroutines.AreNotCompleted(true))
                yield return null;

            void SafeAction()
            {
                // HPが通常値でかつ狙える敵がいる場合はSafePosition
                var situations = new List<Situation>(currentSituation);
                tilesSituation.ToList().ForEach(t =>
                {
                    if (t.Key.CanEnterUnitInThis(unit))
                        situations.AddRange(t.Value);
                });

                var (betterSituation, point) = GetSafePoint(unit, situations);

                debug.message += $"\nGetSafePoint selects ({betterSituation}) in {situations.Count} situations, Score {point}";

                var action = new AIAction_x(unit);
                action.orderOfAction = OrderOfAction_x.MoveToSkip;
                action.locationToMove = betterSituation.pointInTile.location;

                Result = action;
            }

            // HPが危険値ならSafePosition
            // HPが通常値でかつ現在CellとBorderから狙える敵がいない場合はWeakTarget
            // HPが通常値でかつ狙える敵がいる場合はSafePosition
            var existNearEnemy = tilesSituation.ToList().Exists((tAndS) =>
            {
                return tAndS.Value.Exists(s => s.enemies.Exists(e => e.HitRateToThis > 0.3f));
            });

            if (existNearEnemy)
            {
                // 近い敵が存在する場合
                SafeAction();
            }
            else
            {
                // 近い敵が存在しない場合
                tilesSituation[current] = currentSituation;
                yield return StartCoroutine(MoveToWeakTarget(unit, tilesSituation));
            }
        }
        #endregion

        #region 待機AI
        /// <summary>
        /// 待機もしくは巡回ルートに沿った行動を行う
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        private IEnumerator WaitRoutineAI(UnitController unit)
        {
            debugTexts.Add(new AIDebugMsg("WaitRoutineAI", ""));

            if (WayPassPoints != null && WayPassPoints.Count != 0)
            {

                var currentTile = unit.tileCell;
                var currentPassPointIndex = WayPassPoints.FindIndex(w => w.tile == currentTile);

                if (currentPassPointIndex == -1)
                {
                    // 現在UnitはwayPassから外れたTileに存在する
                    // 最寄りのTileに戻る
                    Result = MoveToNearWayPassTile(currentTile, null);
                    
                }

                if (!WayPassPoints.IndexAt(currentPassPointIndex + 1, out var nextPass))
                    nextPass = WayPassPoints.First();
                
                if (currentTile.borderOnTiles.Contains(nextPass.tile))
                {
                    // nextPassのtileがcurrentTileにつながっている
                    var action = new AIAction_x(unit);
                    action.orderOfAction = OrderOfAction_x.MoveAndFind;
                    action.locationToMove = nextPass.point.position;
                    action.rotation = nextPass.point.rotation;
                    Result = action;
                }
                else
                {
                    // nextPasのtileがcurrentTileから離れている
                    Result = MoveToNearWayPassTile(currentTile, nextPass.tile);
                }
            }
            else
            {
                var action = new AIAction_x(unit);
                action.orderOfAction = OrderOfAction_x.Skip;

                yield return null;
                Result = action;
            }
        }

        /// <summary>
        /// 現在UnitがWayPass上に存在しないため最寄りのwaypassTileに移動する
        /// </summary>
        /// <returns></returns>
        private AIAction_x MoveToNearWayPassTile(TileCell current, TileCell to)
        {
            var action = new AIAction_x(unit);
            action.orderOfAction = OrderOfAction_x.Skip;

            if (to == null)
            {
                var _nearTile = WayPassPoints.FindMin(pp => tilesController.GetShortestWay(current, pp.tile).Count);
                if (_nearTile == default((Transform, TileCell)))
                    return action;
                to = _nearTile.tile;
            }

            var way = tilesController.GetShortestWay(current, to);
            if (way.Count <= 2)
                return action;

            var nextPos = way[1].pointsInTile.Find(p => p.isNormalPosition);
            action.orderOfAction = OrderOfAction_x.MoveAndFind;
            action.locationToMove = nextPos.location;

            return action;
        }
        #endregion

        #region Sub functions

        /// <summary>
        /// gridの位置が<c>enemyPos</c>からカバーされる位置にいるかどうか
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="enemyPos"></param>
        /// <returns></returns>
        private bool IsCoveredFromEnemy(PointInTile grid, Vector3 enemyPos)
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

        /// <summary>
        /// HPがActiveな行動を取れるかの最低値
        /// </summary>
        /// <returns></returns>
        private bool IsWarningHealthThreshold(UnitController unit)
        {
            float totalHealth = unit.CurrentParameter.Data.HealthPoint + unit.CurrentParameter.Data.AdditionalHealthPoint;
            if ((float)unit.CurrentParameter.HealthPoint / totalHealth > parameters.ThresholdOfWarningHealth)
                return false;
            
            return true;
        }

        #endregion

    }

}