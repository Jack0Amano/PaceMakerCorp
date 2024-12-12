using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Utility;

namespace Tactics.UI
{
    public class UnitInResult : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameLabel;
        [SerializeField] CounterLabel levelLabel;
        [SerializeField] CounterLabel currentExpLabel;
        [SerializeField] CounterLabel rootExpLabel;
        [SerializeField] Image faceImage;

        public UnitData info { private set; get; }

        private readonly float counterTime = 2;

        public bool isCounting { private set; get; } = false;

        public void Set(UnitData unitParameter)
        {
            info = unitParameter;
            nameLabel.SetText(info.Name);
            levelLabel.textLabel.SetText(info.Level);
            currentExpLabel.textLabel.SetText(info.Exp);
            rootExpLabel.textLabel.SetText(info.RequiredExp(info.Level));;
        }

        /// <summary>
        /// 経験値を追加する
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public IEnumerator AddExp(int exp)
        {
            isCounting = true;

            var l = exp;
            var upLevel = 0;
            var exps = new List<int>();
            // 最初に与えられたexpから各レベルの必要exp毎に割り振ってexpsにlist化しておく
            for(var i=0; l>0; i++)
            {
                var l2 = l - info.RequiredExp(info.Level + i);
                if (l2 < 0)
                {
                    // 与えられた経験値の中で残りのexpが次のレベルアップまでない場合
                    exps.Add(l);
                }
                else
                {
                    // レベルアップの必要経験値分与えられたexpがある場合は次のレベルに
                    exps.Add(info.RequiredExp(info.Level + i));
                }
                l = l2;
                upLevel = i;
            }

            var delta = counterTime / (upLevel + 1);
            for(var i=0; i<exps.Count; i++)
            {
                yield return StartCoroutine(currentExpLabel.SetCount(info.Exp, exps[i], delta));

                if (exps.Count == (i + 1))
                {
                    info.Exp += exps[i];
                    
                    if (info.RequiredExp(info.Level) == info.Exp)
                    {
                        info.Exp = 0;
                        info.Level += 1;
                    }
                }
                else
                {
                    info.Exp = 0;
                    info.Level += 1;
                }
                currentExpLabel.textLabel.SetText(info.Exp);
                rootExpLabel.textLabel.SetText(info.RequiredExp(info.Level));
                levelLabel.textLabel.SetText(info.Level);
            }

            isCounting = false;
        }
    }
}