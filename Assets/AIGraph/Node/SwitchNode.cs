using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using AIGraph.Nodes.Parts;
using AIGraph.Editor;
using AIGraph.InOut;
using UnityEditor;
using UnityEditor.UIElements;
using Tactics.Character;
using Tactics.Map;
using Unity.Logging;
using static Utility;

namespace AIGraph.Nodes
{

    public class SwitchNode : ProcessNode
    {
        private readonly List<string> SwitchChoices = new List<string>
        {
            "Find any enemy",
            "Is low HP mode",
            "Did kill target?",
            "Can counter attack",
            "Can friends kill target",
            "Is enemy in target tile",
            "Is Already done acion",
            "Is Already moved",
            "Enemy on same tile"
        };
        readonly DropdownField DropdownField;
        private readonly string SwitchChoiceIndexKey = "SwitchChoiceIndexKey";

        public readonly CustomPort TruePort;
        public readonly CustomPort FalsePort;

        public SwitchNode() : base()
        {
            title = "Switch";
            NodePath = "Logic/Switch";

            DropdownField = new DropdownField();
            DropdownField.choices = SwitchChoices;
            mainContainer.Add(DropdownField);

            OutputPort.RemoveFromHierarchy();
            TruePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            TruePort.portName = "True";
            outputContainer.Add(TruePort);
            FalsePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            FalsePort.portName = "False";
            outputContainer.Add(FalsePort);
        }
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(SwitchChoiceIndexKey, out int id))
                DropdownField.index = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(SwitchChoiceIndexKey, DropdownField.index);
            return data;
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);

            if (DropdownField.index == 0)
            {
                // FindAnyEnemy
                if (input.MyUnitController.aiController.FindedEnemies.Count == 0)
                    input.OutPort = FalsePort;
                else
                    input.OutPort = TruePort;
            }
            else if (DropdownField.index == 1)
            {
                // Low hp mode
                input.OutPort = input.MyUnitController.CurrentParameter.IsLowHPMode ? TruePort : FalsePort;
                DebugLabel.text = $"{input.MyUnitController.CurrentParameter.Data.Name} HP: {input.MyUnitController.CurrentParameter.HealthPoint}/{input.MyUnitController.CurrentParameter.TotalHealthPoint}";
            }
            else if (DropdownField.index == 2)
            {
                // Did kill target
                if (input.TargetedUnitController != null)
                {
                    input.OutPort = input.TargetedUnitController.IsDead ? TruePort : FalsePort;
                    DebugLabel.text = $"Target {input.TargetedUnitController.CurrentParameter.Data.Name} is dead ({input.TargetedUnitController.IsDead})";
                }
                else
                {
                    PrintWarning("EnvironmentData.TargetedUnitController is null. Always return false");
                    input.OutPort = FalsePort;
                }
            }
            else if (DropdownField.index == 3)
            {
                // Can counterattack
                input.OutPort = MyUnitController.CurrentParameter.Data.CanCounterAttack ? TruePort : FalsePort;
                DebugLabel.text = $"Counterattack: {MyUnitController.CurrentParameter.Data.CanCounterAttack}";
            }
            else if (DropdownField.index == 4)
            {
                // Can friends kill target
                if (input.TargetedUnitController != null)
                {
                    // [active, next, ....]
                    List<UnitController> friendTurnList;
                    if (UnitsController.TurnOfAttribute == MyUnitController.Attribute)
                        friendTurnList = UnitsController.TurnList.Slice(UnitsController.TurnCount);
                    else
                        friendTurnList = UnitsController.UnitsList.FindAll(u => u.Attribute != MyUnitController.Attribute);
                    friendTurnList.Remove(MyUnitController);
                    // 行動をまだ行っていなくて、かつ自分の近くにいるfriendのlist
                    var nearFriends = friendTurnList.FindAll(u => MyUnitController.tileCell.borderOnTiles.Contains(u.tileCell) || u.tileCell == MyUnitController.tileCell);
                    var damage = nearFriends.Sum(u => u.GetAIAttackDamage(input.TargetedUnitController));
                    input.OutPort = input.TargetedUnitController.CurrentParameter.HealthPoint <= damage ? TruePort : FalsePort;
                    DebugLabel.text = $"Friends can kill {input.TargetedUnitController.CurrentParameter.Data.Name}: ({input.TargetedUnitController.CurrentParameter.HealthPoint < damage})";
                }
                else
                {
                    PrintWarning("EnvironmentData.TargetedUnitController is null. Always return false");
                    input.OutPort = FalsePort;
                }
            }
            else if (DropdownField.index == 5)
            {
                // Is enemy in target tile
                if (input.TargetedUnitController != null)
                {
                    var enemyInTile = AIController.FindedEnemies.Find(e => e.Enemy.tileCell == input.TargetedUnitController.tileCell);
                    input.OutPort = enemyInTile == null ? TruePort : FalsePort;
                }
                else
                {
                    PrintWarning("EnvironmentData.TargetedUnitController is null. Always return false");
                    input.OutPort = FalsePort;
                }
            }
            else if (DropdownField.index == 6)
            {
                // Is Already done acion
                input.OutPort = MyUnitController.isAlreadyActioned ? TruePort : FalsePort; 
            }
            else if (DropdownField.index == 7)
            {
                // Is Already moved
                input.OutPort = MyUnitController.isAlreadyMoved ? TruePort : FalsePort;
            }
            else if (DropdownField.index == 8)
            {
                var enemies = UnitsController.GetEnemiesFrom(MyUnitController);
                input.OutPort = enemies.Find(e => e.tileCell == MyUnitController.tileCell) ? TruePort : FalsePort;
            }
            else
            {
                PrintWarning("Dropdown of SwitchNode is not selected. Always return flase");
                input.OutPort = FalsePort;
            }

            return input;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            DropdownField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }
    }
}