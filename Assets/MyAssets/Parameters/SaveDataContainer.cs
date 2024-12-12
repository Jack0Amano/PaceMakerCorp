using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveDataContainer : ScriptableObject
{
    public SaveData SaveData;

    public SaveDataContainer(SaveData data)
    {
        SaveData = data;
    }

}
