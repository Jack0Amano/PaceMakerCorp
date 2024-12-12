using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Utils;
using System;
using Tactics.Character;
using static Utility;

namespace Tactics.Object
{
    public class GimmickObject : MonoBehaviour
    {

        //
        // 概要:
        //     The shape of the obstacle.
        internal Shape shape = Shape.Cylinder;
        //
        // 概要:
        //     Height of the obstacle's cylinder shape.
        public float height;
        //
        // 概要:
        //     Radius of the obstacle's capsule shape.
        public float radius;
        //
        // 概要:
        //     The center of the obstacle, measured in the object's local space.
        public Vector3 center;
        //
        // 概要:
        //     The size of the obstacle, measured in the object's local space.
        public Vector3 size;

        [Tooltip("Gimmickの名前")]
        [SerializeField] public string GimmickName;

        [Tooltip("Focusを行うときにどのようなModeで行うか")]
        [SerializeField] public FocusModeType FocusModeType = FocusModeType.None;

        [Tooltip("Gimmickの使用回数のMax")]
        [SerializeField] public int MaxActionCount;
        /// <summary>
        /// このGImmickを使用できる残りの回数
        /// </summary>
        [Tooltip("Gimmickの使用回数の残り")]
        [SerializeField] public int RemainingActionCount;

        [Tooltip("ギミックに接近時にMesssageを出すたぐいのものか")]
        public bool ShowMessage  = false;


        [Tooltip("このGimmickを同時使用できるUnitの数")]
        [SerializeField] int CountOfMemberCanUse;

        [Header("接近時の処理")]
        [Tooltip("どれだけGimmickに近づけば動作をさせられるか")]
        [SerializeField] public float DistanceActivateGimmick = 1;
        [Tooltip("GImmickに接近したときの表示文字のコード")]
        [SerializeField] string MessageCodeWhenCloseGimmick;
        [Tooltip("Gimmickに接近したときのDetailの表示文字")]
        [SerializeField] string DetailMessageCodeWhenCloseGimmick;

        [Header("使用中の処理")]
        [Tooltip("使用中に表示するMessage")]
        [SerializeField] string MessageCodeWhileUsing;
        [Tooltip("使用中に表示するDetailMessage")]
        [SerializeField] string DetailMessageCodeWhileUsing;

        [Header("破壊された際の処理")]
        [Tooltip("爆発系の小型武器によって破壊可能なGimmickか")]
        [SerializeField] public bool IsSoftyDestructible = true;
        [Tooltip("爆発系の大型武器によって破壊可能なGimmickか")]
        [SerializeField] public bool IsHardyDestructible = true;
        [Tooltip("既に破壊されているか")]
        [SerializeField] public bool IsDestroyed = false;
        [Tooltip("破壊されたときに表示するMessage")]
        [SerializeField] public string MessageCodeWhenDestroyed;
        [Tooltip("破壊されたときに表示するDetailMessage")]
        [SerializeField] public string DetailMessageCodeWhenDestroyed;

        [Header("Gimmickの使用に関する処理")]
        [Tooltip("Gimmickの使用回数が0の時に表示するMessage")]
        [SerializeField] string messageCodeWhenActionCountIsZero;
        [Tooltip("Gimmickを使用中にAlreadyMovedとならないためNotMoveCircleのPosを追従させるか")]
        [SerializeField] public bool UpdateNotMoveCirclePos = false;


        /// <summary>
        /// Gimmickに接近したときのメッセージ
        /// </summary>
        public string MessageWhenCloseGimmick
        {
            get
            {
                messageWhenCloseGimmick ??= commonUITranslation.ReadValue("GimmickMessage", MessageCodeWhenCloseGimmick, MessageCodeWhenCloseGimmick);
                return messageWhenCloseGimmick;
            }
        }
        string messageWhenCloseGimmick;

        /// <summary>
        /// Gimmickに接近したときのDetailMessage
        /// </summary>
        public string DetailMessageWhenCloseGimmick
        {
            get
            {
                detailMessageWhenCloseGimmick ??= objectTranslation.ReadValue("GimmickMessage", DetailMessageCodeWhenCloseGimmick, DetailMessageCodeWhenCloseGimmick);
                return detailMessageWhenCloseGimmick;
            }
        }
        string detailMessageWhenCloseGimmick;

        /// <summary>
        /// 使用中に表示するメッセージ
        /// </summary>
        public string MessageWhileUsing
        {
            get
            {
                messageWhileUsing ??= commonUITranslation.ReadValue("GimmickMessage", MessageCodeWhileUsing, MessageCodeWhileUsing);
                return messageWhileUsing;
            }
        }
        string messageWhileUsing;

        /// <summary>
        /// 使用中に表示するDetailMessage
        /// </summary>
        public string DetailMessageWhileUsing
        {
            get
            {
                detailMessageWhileUsing ??= commonUITranslation.ReadValue("GimmickMessage", DetailMessageCodeWhileUsing, DetailMessageCodeWhileUsing);
                return detailMessageWhileUsing;
            }
        }
        string detailMessageWhileUsing;

        /// <summary>
        /// 破壊された時に表示するメッセージ
        /// </summary>
        public string MessageWhenDestroyed
        {
            get
            {
                messageWhenDestroyed ??= commonUITranslation.ReadValue("GimmickMessage", MessageCodeWhenDestroyed, MessageCodeWhenDestroyed);
                return messageWhenDestroyed;
            }
        }
        string messageWhenDestroyed;

        /// <summary>
        /// 破壊された時に表示するDetailMessage
        /// </summary>  
        public string DetailMessageWhenDestroyed
        {
            get
            {
                detailMessageWhenDestroyed ??= objectTranslation.ReadValue("GimmickMessage", DetailMessageCodeWhenDestroyed, DetailMessageCodeWhenDestroyed);
                return detailMessageWhenDestroyed;
            }
        }
        string detailMessageWhenDestroyed;

        /// <summary>
        /// Gimmickの使用回数が0の時に表示するメッセージ
        /// </summary>
        public string MessageWhenActionCountIsZero
        {
            get
            {
                messageWhenActionCountIsZero ??= objectTranslation.ReadValue("GimmickMessage", messageCodeWhenActionCountIsZero, messageCodeWhenActionCountIsZero);
                return messageWhenActionCountIsZero;
            }
        }
        string messageWhenActionCountIsZero;


        INIParser commonUITranslation;
        private INIParser objectTranslation;

        /// <summary>
        /// Gimmickを現在使用中のUnitのリスト
        /// </summary>
        private readonly List<UnitController> unitsOfUseingList = new List<UnitController>();

        /// <summary>
        /// このギミックを使用可能か？
        /// </summary>
        public bool CanUseIt
        {
            get
            {
                if (MaxActionCount > 0)
                {
                    return RemainingActionCount > 0 && CountOfMemberCanUse > unitsOfUseingList.Count && !IsDestroyed;
                }
                return CountOfMemberCanUse > unitsOfUseingList.Count && !IsDestroyed;
            }
        }

        private void Awake()
        {
            RemainingActionCount = MaxActionCount;
            commonUITranslation = GameManager.Instance.Translation.CommonUserInterfaceIni;
            objectTranslation = GameManager.Instance.Translation.SceneObjectsIni;
        }

        /// <summary>
        /// TileControllerのTileのCollider判定でPositionがどのCellに位置しているか判定
        /// </summary>
        [Serializable] public class LocationAndTilePair
        {

            public Vector3 position;
            public string tileID;

            [NonSerialized]
            public Tactics.Map.TileCell tile;

            public LocationAndTilePair(Vector3 pos, Map.TileCell tile)
            {
                position = pos;
                this.tile = tile;
                tileID = tile.id;
            }
        }

        /// <summary>
        /// Gimmickを使用する
        /// </summary>
        public void UseGimmick()
        {
            if (MaxActionCount > 0)
            {
                RemainingActionCount--;
            }
        }

        /// <summary>
        /// UnitがGimmickを使用中にする
        /// </summary>
        /// <param name="unit">使用するUnit</param>
        /// <returns>使用可能か</returns>
        public bool AddUnitToUse(UnitController unit)
        {
            if (!CanUseIt)
                return false;

            unitsOfUseingList.Add(unit);
            return true;
        }

        /// <summary>
        /// UnitのGimmickの使用を終了する
        /// </summary>
        /// <param name="unit"></param>
        public void RemoveUnitToUse(UnitController unit)
        {
            unitsOfUseingList.Remove(unit);
        }

        /// <summary>
        /// unitがGimmickを使用中であるか
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool IsUsingGimmick(UnitController unit)
        {
            return unitsOfUseingList.Contains(unit);
        }

        /// <summary>
        /// PositionがInstallation内に含まれているかどうかの判定
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Contain(Vector3 position)
        {
            if (shape == Shape.Cylinder)
            {
                var shapeCenter = transform.position + center;
                shapeCenter.y = position.y;
                return Vector3.Distance(shapeCenter, position) <= radius;
            }
            return false;

        }

        /// <summary>
        /// 爆発等による破壊アニメーション
        /// </summary>
        virtual public IEnumerator DestroyAnimation()
        {
            Print("Gimmick is destroyed", this);
            IsDestroyed = true;
            unitsOfUseingList.ForEach(unit => unit.UsingGimmickIsDestroied(this));
            unitsOfUseingList.Clear();
            yield break;
        }

        protected private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (shape == Shape.Box)
            {
                Gizmos.DrawWireCube(transform.position + center, size);
            }
            else if (shape == Shape.Cylinder)
            {
                GizmosExtensions.DrawWireCylinder(transform.position + center, radius, transform.localScale.y - 0.05f);
            }

        }

        [Serializable]
        public enum Shape
        {
            Box,
            Cylinder
        }
    }
}