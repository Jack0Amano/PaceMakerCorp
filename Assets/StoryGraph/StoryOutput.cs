using StoryGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;


namespace StoryGraph.InOut
{
    #region Output
    /// <summary>
    /// �C�x���g�̊��N���X
    /// </summary>
    public class EventOutput
    {
        /// <summary>
        /// ����Execute����ProcessNode�ɂȂ���Node
        /// </summary>
        public Port NextPort;

        public string nodeID;

        public EventOutput()
        {
        }

        public EventOutput(string id, Port outputPort)
        {
            nodeID = id;
            NextPort = outputPort;
        }
    }

    /// <summary>
    /// �X�|�[������Squad�Ƃ��̈ʒu��� ���o���C�x���g
    /// </summary>
    public class SpawnEventOutput : EventOutput
    {
        /// <summary>
        /// Spawn�����镔����ID (spawnData�̒�����I�΂��)
        /// </summary>
        public string squadID;
        /// <summary>
        /// �X�|�[������n�_��ID
        /// </summary>
        public Vector3 location;
        /// <summary>
        /// �o���m�� 1�Ȃ�C�x���g�ŕK�{�̕���
        /// </summary>
        public float spawnRate;

        public SpawnEventOutput(string id, Port outputPort): base(id, outputPort)
        {
        }

        public override string ToString()
        {
            return $"SpawnEvent: {squadID}, {location}";
        }
    }


    /// <summary>
    /// ���b�Z�[�W�{�b�N�X���|�b�v�A�b�v������^�C�v�̃C�x���g
    /// </summary>
    public class MessageEventOutput : EventOutput
    {
        /*
         * {Stc0, Stc1, Cho1, Cho2, Cho3}
         * �Ƃ������ɕ��ׂ�ꂽ�ꍇ Stc0���ŏ��ɏo�� �y�[�W����{�^����Stc1��
         * Stc1���o���� Cho1~Cho3
         * �I�����̏�����Graph�ɕԂ��Č��ʂ�҂�
         */
        /// <summary>
        /// ��b�̗���
        /// </summary>
        public List<Sentence> sentences = new List<Sentence>();

        /// <summary>
        /// �L��������C���[�W
        /// </summary>
        public List<(ImageAlignment alignment, Sprite image)> ShowImages = new List<(ImageAlignment alignment, Sprite image)>();

        /// <summary>
        /// �L��������C���[�W 
        /// </summary>
        public List<ImageAlignment> ActivateImages = new List<ImageAlignment>();

        /// <summary>
        /// ��L��������C���[�W �F�������Ȃ�
        /// </summary>
        public List<ImageAlignment> DisactivateImages = new List<ImageAlignment>();

        /// <summary>
        /// �폜����C���[�W
        /// </summary>
        public List<ImageAlignment> HideImage = new List<ImageAlignment>();

        public MessageEventOutput(string id, Port outputPort): base(id, outputPort)
        {
        }


        public class Sentence
        {
            public string id;
            /// <summary>
            /// ���b�Z�[�W�̖{��
            /// </summary>
            public string text;
            /// <summary>
            /// ���b�Z�[�W���N���瑗��ꂽ��
            /// </summary>
            public string messageFrom;
            /// <summary>
            /// �ԓ��p�̑I����
            /// </summary>
            public bool isChoice;

            public override string ToString()
            {
                if (isChoice)
                    return text;
                else
                    return $"{messageFrom}< {text}";
            }
        }

        public override string ToString()
        {
            var output = "";
            var index = 0;
            var choiceIndex = 0;
            sentences.ForEach(s =>
            {
                if (!s.isChoice)
                {
                    output += $"{index}. {s}\n";
                    index++;
                }
                else
                {
                    output += $"-{choiceIndex}. {s}\n";
                    choiceIndex++;
                }
            });

            return $"MessageEvent\n{output}";
        }
    }

    /// <summary>
    /// Message��Image��\������ۂ̈ʒu
    /// </summary>
    [Serializable]
    public enum ImageAlignment
    {
        Center,
        Right,
        Left
    }
    #endregion

    #region Input
    /// <summary>
    /// �Q�[���v���O�����{�̂���EventGraph�ɕԂ�class
    /// </summary>
    public class EventInput
    {
        /// <summary>
        /// �r������n�߂邽�߂�Node��ID
        /// </summary>
        public string StartAtID = "";

        /// <summary>
        /// �I�������̃g���K�[��ۂꍇ�̂��̑I������Index
        /// </summary>
        public int SelectTriggerIndex;

        /// <summary>
        /// ���ݎ���
        /// </summary>
        public DateTime DateTime;

        /// <summary>
        /// �������Ă���A�C�e����ID
        /// </summary>
        public List<string> ItemsID;
    }
    #endregion


}