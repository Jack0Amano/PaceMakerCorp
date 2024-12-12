using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Linq;
using Tactics;

/// <summary>
/// デバッグ用のコンソール画面を表示
/// </summary>
public class DebugController : MonoBehaviour
{
    [SerializeField] Canvas debugCanvas;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI textField;

    private bool previousEnableCoursor;
    private bool previousUpdateKeyInputs;
    private Action debugUIOnCompleted;
    /// <summary>
    /// <c>Hide()</c>が既に実行されている状態であるか (Hideは消えるのにタイマーがついてるため)
    /// </summary>
    private bool isWaitingToHideDebugUI = false;
    /// <summary>
    /// Enterしてコマンドを実行する
    /// </summary>
    private Action<List<string>> enterCommand;

    public Action<List<string>> disableTileWalls;
    public Action<List<string>> getSquareInUnit;
    public Action<List<string>> endTactics;
    public Action<List<string>> showPrepare;
    public Action<List<string>> unitInfo;
    public Action<List<string>> rotation;

    public Action<UnitAttribute, bool> tacticsAI;

    /// <summary>
    /// DebugUIが表示中かどうか
    /// </summary>
    public bool IsActive { private set; get; } = false;

    // Update is called once per frame
    void Update()
    {
        if (IsActive)
        {
            // リターンキーを押したとき
            if (Input.GetKeyDown(KeyCode.Return))
            {
                inputField.OnSelect(null);
                // Enterキーの入力
                if (inputField.text.Length != 0)
                {
                    AddText($"> {inputField.text}");
                    var lowerInput = inputField.text.ToLower();
                    var commands = lowerInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (commands.Count == 0)
                        return;
                    RunCommand(commands, inputField.text) ;
                    enterCommand?.Invoke(commands);
                }
                inputField.text = "";
                return;
            }
            // DebugUIを表示中で隠すとき
            if (UserController.DebugKeyDown)
            {
                StartCoroutine(Hide());
                return;
            }
        }
        else
        {
            if (UserController.DebugKeyDown)
            {
                Show(null);
            }
        }

    }

    /// <summary>
    /// Debugコンソールを表示する
    /// </summary>
    /// <param name="enterCommand"></param>
    public void Show(Action<List<string>> enterCommand = null)
    {

        if (IsActive)
        {
            // 既にDebugUIを表示中のためEenterCommandのActionだけ登録して終わり
            this.enterCommand = enterCommand;
            return;
        }
        
        IsActive = true;

        previousEnableCoursor = UserController.enableCursor;
        previousUpdateKeyInputs = UserController.updateKeyInput;

        UserController.enableCursor = true;
        UserController.updateKeyInput = false;

        debugCanvas.gameObject.SetActive(true);
        this.enterCommand = enterCommand;
        inputField.text = "";
        inputField.OnSelect(null);
    }

    /// <summary>
    /// Debugコンソールを非表示にする
    /// </summary>
    public IEnumerator Hide(float waitTime = 0, Action completed = null)
    {
        if (isWaitingToHideDebugUI)
            yield break ;

        isWaitingToHideDebugUI = true;
        UserController.enableCursor = previousEnableCoursor;
        UserController.updateKeyInput = previousUpdateKeyInputs;

        yield return new WaitForSeconds(waitTime);

        debugCanvas.gameObject.SetActive(false);
        debugUIOnCompleted?.Invoke();
        debugUIOnCompleted = null;
        completed?.Invoke();
        IsActive = false;
        isWaitingToHideDebugUI = false;
    }

    /// <summary>
    /// コンソールに文字を表示
    /// </summary>
    /// <param name="text"></param>
    public void AddText(string text)
    {
        if (textField.text.Length == 0)
            textField.text += $"{text}";
        else
            textField.text += $"\n{text}";
    }

    #region Commands
    private void RunCommand(List<string> commands, string rawCommand)
    {
        if (commands.Count == 1)
        {
            switch (commands[0])
            {
                case "getsquare":
                    getSquareInUnit?.Invoke(commands);
                    return;
                case "showprepare":
                    showPrepare?.Invoke(commands);
                    return;
                case "showinfo":
                    unitInfo?.Invoke(commands);
                    return;
            }
        }
        if (commands.Count == 2)
        {
            switch (commands[0])
            {
                case "tiles":
                    disableTileWalls?.Invoke(commands);
                    return;
                case "end":
                    endTactics?.Invoke(commands);
                    return;
                case "rotation":
                    rotation?.Invoke(commands);
                    return;

            }
        }
        if (commands.Count == 3)
        {
            switch (commands[0])
            {
                case "ai":
                    DebugAiControlling(commands);
                    return;
            }
        }
        if (commands.Count == 4)
        {

        }

        AddText($"Invalid command: {rawCommand}");
    }

    /// <summary>
    /// TacticsSceneでのAI操作の有効化
    /// </summary>
    /// <param name="command"></param>
    private void DebugAiControlling(List<string> command)
    {
        bool isControlled;
        if (command[1] == "enable")
            isControlled = true;
        else if (command[1] == "disable")
            isControlled = false;
        else
        {
            AddText($"Invalid second argument: {command[1]}  |  ai [enable/disable] [player/enemy]");
            return;
        }

        UnitAttribute attribute;
        if (command[2] == "player")
        {
            attribute = UnitAttribute.PLAYER;
            GameManager.Instance.GeneralParameter.playerAiEnable = isControlled;
        }
        else if (command[2] == "enemy")
        {
            attribute = UnitAttribute.ENEMY;
            GameManager.Instance.GeneralParameter.enemyAiEnable = isControlled;
        }
        else
        {
            AddText($"Invalid third argument: {command[2]}  |  ai [player/enemy] [enable/disable]");
            return;
        }

        tacticsAI?.Invoke(attribute, isControlled);
        AddText($"AI of {attribute} is {(isControlled ? "Activated" : "Disactivated")}.");
    }
    #endregion
}
