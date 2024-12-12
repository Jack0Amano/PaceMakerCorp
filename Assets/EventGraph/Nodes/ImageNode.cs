using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using EventGraph.Editor;
using UnityEditor.UIElements;
using UnityEditor;
using EventGraph.InOut;
using System.Linq;
using System.Collections.Generic;

namespace EventGraph.Nodes
{
    public class ImageNode : SampleNode
    {
        public Sprite Image { get { return (SetImageType)TypeEnumField.value == SetImageType.Show ? (Sprite)ImageField.value : null; } }
        /// <summary>
        /// 処理を行うImageの位置
        /// </summary>
        public ImageAlignment Alignment { get { return (ImageAlignment)AlignmentField.value; } }
        /// <summary>
        /// Imageをどのように処理するかの指定
        /// </summary>
        public SetImageType SetType { get { return (SetImageType)TypeEnumField.value; } }

        private readonly Image ImagePanel;
        private readonly GroupBox ImagePanelBox;

        private readonly ObjectField ImageField;
        private readonly GroupBox ImageFieldBox;

        private readonly EnumField AlignmentField;
        private readonly EnumField TypeEnumField;

        private readonly string ImageAccessKey = "ImageKey";
        private readonly string ImageAlignmentKey = "ImageAlignmentKey";
        private readonly string SetImageTypeKey = "SetImageTypeKey";

        public ImageNode() : base()
        {
            NodePath = "Value/Image";
            title = "Image";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/ImageNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            ImageFieldBox = container.Q<GroupBox>("ImageFieldBox");
            ImageField = container.Q<ObjectField>();
            ImageField.objectType = typeof(Sprite);

            AlignmentField = container.Q<EnumField>("AlignEnum");
            AlignmentField.Init(ImageAlignment.Center);

            TypeEnumField = container.Q<EnumField>("TypeEnum");
            TypeEnumField.Init(SetImageType.Active);

            ImagePanelBox = container.Q<GroupBox>("ImageBox");
            ImagePanel = new Image();
            ImagePanel.style.height = 200;
            ImagePanel.scaleMode = ScaleMode.ScaleToFit;

            ImagePanelBox.Add(ImagePanel);

            var outputPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof((Sprite, ImageAlignment)));
            outputPort.portName = "Align Image";
            outputContainer.Add(outputPort);

            ImageField.RegisterValueChangedCallback((c) =>
            {
                ImagePanel.sprite = (Sprite)ImageField.value;
            });

            TypeEnumField.RegisterValueChangedCallback(c => 
            {
                if ((SetImageType)TypeEnumField.value == SetImageType.Show)
                {
                    ImagePanelBox.Add(ImagePanel);
                    ImageFieldBox.Add(ImageField);
                }
                else if (ImagePanelBox.Contains(ImagePanel))
                {
                    ImagePanelBox.Remove(ImagePanel);
                    ImageFieldBox.Remove(ImageField);
                }
                    
            });
        }


        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(ImageAccessKey, out Sprite image))
                ImageField.value = image;

            data.raw.GetFromPairs(ImageAlignmentKey, out string alignmentSrt);
            var alignType = ((ImageAlignment[])Enum.GetValues(typeof(ImageAlignment))).ToList().FirstOrDefault(t => t.ToString() == alignmentSrt);
            AlignmentField.Init(alignType);

            if (ImageField.value != null)
                ImagePanel.sprite = (Sprite) ImageField.value;

            data.raw.GetFromPairs(SetImageTypeKey, out string strType);
            var imageType = ((SetImageType[])Enum.GetValues(typeof(SetImageType))).ToList().FirstOrDefault(t => t.ToString() == strType);
            TypeEnumField.Init(imageType);

            if ((SetImageType)TypeEnumField.value == SetImageType.Show)
            {
                ImagePanelBox.Add(ImagePanel);
                ImageFieldBox.Add(ImageField);
            }
            else if (ImagePanelBox.Contains(ImagePanel))
            {
                ImagePanelBox.Remove(ImagePanel);
                ImageFieldBox.Remove(ImageField);
            }

            ImageField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
            AlignmentField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
            TypeEnumField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));

        }

        public override NodeData Save()
        {
            var node = base.Save();
            node.raw.SetToPairs(ImageAccessKey, ImageField.value);
            node.raw.SetToPairs(ImageAlignmentKey, AlignmentField.value.ToString());
            node.raw.SetToPairs(SetImageTypeKey, TypeEnumField.value.ToString());
            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            ImageField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
            AlignmentField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
            TypeEnumField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
        }

        /// <summary>
        /// Imageの表示タイプを設定する
        /// </summary>
        public enum SetImageType
        {
            Active,
            Disactive,
            Show,
            Hide,
        }
    }
}