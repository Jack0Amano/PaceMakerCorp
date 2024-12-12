using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "DeviceParameters", menuName = "Device")]
[Serializable]
public class DeviceParameters : ScriptableObject
{
    public int sandbagsAdditionalHideRate = 20;
}
