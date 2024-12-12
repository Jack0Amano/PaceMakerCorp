using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace MainMap.UI.InfoPanel
{
    /// <summary>
    /// 選択した物に対する情報を色々と表示する小窓
    /// </summary>
    public class InfoPanel : MonoBehaviour
    {
        //[SerializeField] public SquadInfoCard squadInfoWindow;
        //[SerializeField] LocationInfoWindow locationInfoWindow;

        //// Squad
        //public IEnumerator Show(Squad squad)
        //{
        //    if (locationInfoWindow.isShown || squadInfoWindow.isShown)
        //    {
        //        yield return StartCoroutine( Hide());
        //        squadInfoWindow.SetInfomation(squad);
        //        yield return StartCoroutine(squadInfoWindow.Show());
        //    }
        //    else
        //    {
        //        squadInfoWindow.SetInfomation(squad);
        //        yield return StartCoroutine( squadInfoWindow.Show());
        //    } 
        //}

        //// Location
        //public IEnumerator Show(LocationParamter location)
        //{
        //    if (locationInfoWindow.isShown || squadInfoWindow.isShown)
        //    {
        //        yield return StartCoroutine( Hide() );
        //        locationInfoWindow.SetInfomation(location);
        //        yield return StartCoroutine( locationInfoWindow.Show() );
        //    }
        //    else
        //    {
        //        locationInfoWindow.SetInfomation(location);
        //        yield return StartCoroutine( locationInfoWindow.Show());
        //    }
        //}


        ///// <summary>
        ///// 現在表示中のInfoWindowを非表示にする
        ///// </summary>
        //public IEnumerator Hide()
        //{
        //    if (squadInfoWindow.isShown)
        //        yield return StartCoroutine( squadInfoWindow.Hide());
        //    else if (locationInfoWindow.isShown)
        //        yield return StartCoroutine( locationInfoWindow.Hide());
        //}

        ///// <summary>
        ///// 現在選択中のSquadの情報がアップデートされたときに呼び出される
        ///// </summary>
        ///// <param name="squad"></param>
        //public void UpdateSquadInfo(Squad squad)
        //{
        //    if (squad.Equals(squadInfoWindow.squad))
        //    {
        //        squadInfoWindow.UpdateSupplyLevel();
        //    }
        //}
    }
}