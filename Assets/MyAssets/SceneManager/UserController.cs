using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utility;

/// <summary>
/// ユーザーのキーパディングをコントロール
/// </summary>
public class UserController : MonoBehaviour
{
    static public float MouseWheel { private set; get; }
    static public bool MouseClickDown { private set; get; }
    static public bool MouseClickUp { private set; get; }
    static public bool MouseGrab = false;
    static public Vector3 MousePosition;
    static public bool MouseMoving { private set; get; }
    static public bool KeyShowInfo { private set; get; } = false;
    static public bool KeyReturnToBase { private set; get; } = false;

    static public bool KeyForcusDown { private set; get; } = false;
    static public bool KeyForcus { private set; get; } = false;

    static public bool KeyUseItem { private set; get; } = false;

    public static bool KeyCodeC { protected set; get; } = false;
    public static bool KeyCodeM { protected set; get; } = false;
    public static bool KeyCodeI { protected set; get; } = false;
    public static bool KeyCodeSpace { protected set; get; } = false;
    public static bool KeyCodeLeftShift { protected set; get; } = false;
    public static bool KeyCodeJump { protected set; get; } = false;
    public static bool KeyCodeDash { protected set; get; } = false;

    public static bool KeyCodeSetting { protected set; get; } = false;

    public static List<bool> NumberCodeDown { protected set; get; } = new List<bool>();

    public static bool MouseLeft { protected set; get; } = false;
    public static bool MouseRight { protected set; get; } = false;
    public static float MouseDeltaX { protected set; get; } = 0f;
    public static float MouseDeltaY { protected set; get; } = 0f;
    public static float KeyHorizontal { protected set; get; } = 0f;
    public static float KeyVertical { protected set; get; } = 0f;
    public static float KeyHorizontalRaw { protected set; get; } = 0;
    public static float KeyVerticalRaw { protected set; get; } = 0;
    public static bool MouseRightDown { protected set; get; } = false;
    public static bool MouseRightUp { protected set; get; } = false;
    public static bool MouseLeftDown { protected set; get; } = false;
    public static bool DebugKeyDown { protected set; get; } = false;
    /// <summary>
    /// FollowCameraで左右の場所の変更を行うボタン
    /// </summary>
    public static bool ChangeFollowCameraPositionRightOrLeft { protected set; get; } = false; 

    private Vector3 mousePreviousPosition = Vector3.zero;

    /// <summary>
    /// 入力コントロールの更新を停止する
    /// </summary>
    public static bool updateInputs = true;
    /// <summary>
    /// マウスコントロールの更新を停止する
    /// </summary>
    public static bool updateMouseInput = true;
    /// <summary>
    /// キーボード入力の更新を停止する
    /// </summary>
    public static bool updateKeyInput = true;

    public static float SmoothMouseX = 0;
    public static float SmoothMouseY = 0;
    private float _SmoothMouseX = 0;
    private float _SmoothMouseY = 0;
    const float SmoothMouseConst = 0.5f;


    private void Awake()
    {
        NumberCodeDown.AddRange(Enumerable.Repeat(false, 10));
        Application.targetFrameRate = 60;
    }


    // Update is called once per frame
    protected private void Update()
    {
        if (!updateInputs)
            return;

        if (updateMouseInput)
        {
            MouseWheel = Input.GetAxis("Mouse ScrollWheel");
            MouseClickDown = Input.GetMouseButtonDown(0);
            if (MouseClickDown && !MouseGrab)
                MouseGrab = true;
            MouseClickUp = Input.GetMouseButtonUp(0);

            if (MouseClickUp && MouseGrab)
                MouseGrab = false;

            MousePosition = Input.mousePosition;
            MouseMoving = mousePreviousPosition.Equals(MousePosition) ? false : true;
            mousePreviousPosition = MousePosition;

            MouseDeltaX = Input.GetAxis("Mouse X");
            MouseDeltaY = Input.GetAxis("Mouse Y");

            // ローパスフィルターをかけたマウスの移動量
            SmoothMouseX = (1 - SmoothMouseConst) * _SmoothMouseX + SmoothMouseConst * MouseDeltaX;
            if (Mathf.Abs(SmoothMouseX) < 0.00001)
                SmoothMouseX = 0;
            _SmoothMouseX = SmoothMouseX;

            SmoothMouseY = (1 - SmoothMouseConst) * _SmoothMouseY + SmoothMouseConst * MouseDeltaY;
            if (Mathf.Abs(SmoothMouseY) < 0.00001)
                SmoothMouseY = 0;
            _SmoothMouseY = SmoothMouseY;
        }

        if (updateKeyInput)
        {
            KeyCodeLeftShift = Input.GetKeyDown(KeyCode.LeftShift);
            KeyCodeDash = Input.GetKey(KeyCode.LeftShift);
            KeyShowInfo = Input.GetButtonDown("Infomation");
            KeyReturnToBase = Input.GetButtonDown("Return");
            KeyForcusDown = Input.GetKeyDown(KeyCode.F);
            KeyCodeSetting = Input.GetKeyDown(KeyCode.F1);
            KeyForcus = Input.GetKey(KeyCode.F);
            KeyCodeM = Input.GetKeyDown(KeyCode.M);
            KeyCodeI = Input.GetKeyDown(KeyCode.I);
            KeyCodeC = Input.GetKeyDown(KeyCode.C);
            KeyUseItem = Input.GetButtonDown("UseItem");

            ChangeFollowCameraPositionRightOrLeft = Input.GetMouseButtonUp(2);

            NumberCodeDown[0] = Input.GetKeyDown(KeyCode.Alpha0);
            NumberCodeDown[1] = Input.GetKeyDown(KeyCode.Alpha1);
            NumberCodeDown[2] = Input.GetKeyDown(KeyCode.Alpha2);
            NumberCodeDown[3] = Input.GetKeyDown(KeyCode.Alpha3);
            NumberCodeDown[4] = Input.GetKeyDown(KeyCode.Alpha4);
            NumberCodeDown[5] = Input.GetKeyDown(KeyCode.Alpha5);
            NumberCodeDown[6] = Input.GetKeyDown(KeyCode.Alpha6);
            NumberCodeDown[7] = Input.GetKeyDown(KeyCode.Alpha7);
            NumberCodeDown[8] = Input.GetKeyDown(KeyCode.Alpha8);
            NumberCodeDown[9] = Input.GetKeyDown(KeyCode.Alpha9);

            KeyVertical = Input.GetAxis("Vertical");
            KeyHorizontal = Input.GetAxis("Horizontal");
            KeyVerticalRaw = Input.GetAxisRaw("Vertical");
            KeyHorizontalRaw = Input.GetAxisRaw("Horizontal");
            KeyCodeSpace = Input.GetKeyDown(KeyCode.Space);
        }

        DebugKeyDown = Input.GetKeyDown(KeyCode.F12);
    }

    /// <summary>
    /// マウスカーソルの表示状態を管理する
    /// </summary>
    static public bool enableCursor
    {
        get => _enableCoursor;
        set
        {
            if (value)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            _enableCoursor = value;
        }
    }
    static private bool _enableCoursor = true;
}
