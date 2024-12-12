using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using AIGraph.Editor;
using UnityEditor.UIElements;
using UnityEditor;
using AIGraph.InOut;
using System.Linq;
using System.Collections.Generic;

namespace AIGraph.Nodes
{
    public class MyDataNode : SampleNode
    {
        internal UnitData UnitData;

        public MyDataNode(): base()
        {
            NodePath = "Value/My Data";
            title = "My Data";

            //var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AIGraph/Node/Style/UnitDataNode.uxml");
            //var container = asset.Instantiate();
            //mainContainer.Add(container);

            var unitDataPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(UnitData));
            unitDataPort.portName = "UnitData";
            outputContainer.Add(unitDataPort);

            var hpPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(int));
            hpPort.portName = "HP";
            outputContainer.Add(hpPort);

            var isComdrPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            isComdrPort.portName = "Is Comdr";
            outputContainer.Add(isComdrPort);

            var isFlag = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            isFlag.portName = "Is Flag";
            outputContainer.Add(isFlag);

            var antiPersWeapon = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ItemData));
            antiPersWeapon.portName = "Anti-Pers Weapon";
            outputContainer.Add(antiPersWeapon);

            var antiTankWeapon = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ItemData));
            antiTankWeapon.portName = "Anti-Tank Weapon";
            outputContainer.Add(antiTankWeapon);
        }
    }
}