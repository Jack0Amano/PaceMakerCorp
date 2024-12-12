using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parameters.MapData
{
    /// <summary>
    /// MapData(SelectMap等のデータを保持する)のリストを保持するScriptableObjectのContainer
    /// </summary>
    public class MapDataContainer : ScriptableObject
    {
        public List<MapData> MapDataList;

        public MapDataContainer()
        {
            MapDataList = new List<MapData>();
        }

    }
}
