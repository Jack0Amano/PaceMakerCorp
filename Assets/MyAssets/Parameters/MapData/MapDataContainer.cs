using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parameters.MapData
{
    /// <summary>
    /// MapData(SelectMap���̃f�[�^��ێ�����)�̃��X�g��ێ�����ScriptableObject��Container
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
