##Map画面のコントロール  
内容
----
LSではmap画面（Tableに投影されたmap風のもの）を操作するSceneが存在する。その際にMapを移動したりUnit等を選択する操作の内容  

MouseMoveXY
----
`CursorMode = Lock`の際にMouseの移動Deltaを取得するのだがこれがマウスからのナマの値で値が飛び飛びになる時がある  
この時にはローパスフィルターを値にかける  
```c#
// MouseXが生の値
SmoothMouseX = (1 - SmoothMouseConst) * _SmoothMouseX + SmoothMouseConst * MouseX;
if (Mathf.Abs(SmoothMouseX) < 0.00001)
    SmoothMouseX = 0;
_SmoothMouseX = SmoothMouseX;

SmoothMouseY = (1 - SmoothMouseConst) * _SmoothMouseY + SmoothMouseConst * MouseY;
if (Mathf.Abs(SmoothMouseY) < 0.00001)
    SmoothMouseY = 0;
_SmoothMouseY = SmoothMouseY;
```  
のだが結局カーソル移動も使いたいためカーソルのみでのMapの上下は見送り、wasdでマップを移動させることに  
 
wasdによるマップの操作
----
Sequenceを60フレームでアニメーション移動させる。更にEdgeに到達した場合それ以上動けないようにした。
```c#
/*
* Mapと表示領域(table)の各端に空のオブジェクトを仕込みこれ以上進まない範囲を決定した
* e.g. Transform mapLeftUp, Transform tableRightUp
* isControllingはマップ上にraycast等を放つなどの重い動作で移動してなければ更新する必要がない物を動かすためのキー
* Sequenceを使わないと動きが少しガタガタ下感じが出たため使用
*/

void MapMouseControl()
{
    if (mapMoveSequence.IsActive() && mapMoveSequence.IsPlaying())
    {
        // 現在Mapのアニメーション中
        mapMoveSequence.Kill();
        mapMoveSequence = DOTween.Sequence();
        mapMoveSequence.SetEase(Ease.Linear);
    }
    else
    {
        mapMoveSequence = DOTween.Sequence();
        mapMoveSequence.SetEase(Ease.InQuad);
    }

    AddMoveMapHorizontal(UserController.KeyHorizontal);
    AddMoveMapVertical(UserController.KeyVertical);
    mapMoveSequence.Play();
}

bool isOnHorizontalLeftEdge = false;
bool isOnHorizontalRightEdge = false;
/// <summary>
/// Mapの水平方向への移動を行う
/// </summary>
void AddMoveMapHorizontal(float value, float duration=1f/60f)
{
    if (value == 0)
        return;

    var deltaPos = new Vector3(value * keyMoveSencitive, 0, 0);
    var left = mapLeftUp.position + deltaPos;
    var right = mapRightUp.position + deltaPos;

    if (tableLeftUp.position.x > left.x && tableRightUp.position.x < right.x && 
        !isOnHorizontalRightEdge && 
        !isOnHorizontalLeftEdge)
    {
        isOnHorizontalLeftEdge = false;
        isOnHorizontalRightEdge = false;

        mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
        isControlling = true;
    }
    else if (tableLeftUp.position.x <= left.x)
    {
        // Leftの端に位置している
        isOnHorizontalLeftEdge = true;
        isOnHorizontalRightEdge = false;
    }
    else if (tableRightUp.position.x >= right.x)
    {
        // Rightの端に位置している
        isOnHorizontalLeftEdge = false;
        isOnHorizontalRightEdge = true;
    }
    else if (isOnHorizontalLeftEdge && !isOnHorizontalRightEdge)
    {
        if (deltaPos.x < 0)
        {
            // Leftの端から移動中
            isOnHorizontalRightEdge = false;
            isOnHorizontalLeftEdge = false;

            mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
            isControlling = true;
        }
    }
    else if (!isOnHorizontalLeftEdge && isOnHorizontalRightEdge)
    {
        if (deltaPos.x > 0)
        {
            isOnHorizontalRightEdge = false;
            isOnHorizontalLeftEdge = false;
            mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
            isControlling = true;
        }   
    }
}

bool isOnVerticalDownEdge = false;
bool isOnVerticalUpEdge = false;
/// <summary>
/// Mapの水平方向への移動を行う
/// </summary>
void AddMoveMapVertical(float value, float duration=1f/60f)
{
    if (value == 0) return;

    var deltaPos = new Vector3(0, 0, value * keyMoveSencitive);
    var up = mapLeftUp.position + deltaPos;
    var down = mapLeftDown.position + deltaPos;

    if (tableLeftUp.position.z < up.z && tableLeftDown.position.z > down.z &&
        !isOnVerticalUpEdge &&
        !isOnVerticalDownEdge)
    {
        isOnVerticalUpEdge = false;
        isOnVerticalDownEdge = false;

        mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
        isControlling = true;
    }
    else if (tableLeftUp.position.z >= up.z)
    {
        // Upの端に位置している
        isOnVerticalUpEdge = true;
        isOnVerticalDownEdge = false;
    }
    else if (tableLeftDown.position.z <= down.z)
    {
        // Downの端に位置している
        isOnVerticalUpEdge = false;
        isOnVerticalDownEdge = true;
    }
    else if (isOnVerticalUpEdge && !isOnVerticalDownEdge)
    {
        if (deltaPos.z > 0)
        {
            // Upの端から移動中
            isOnVerticalUpEdge = false;
            isOnVerticalDownEdge = false;

            mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
            isControlling = true;
        }
    }
    else if (!isOnVerticalUpEdge && isOnVerticalDownEdge)
    {
        if (deltaPos.z < 0)
        {
            // Downの端から移動中
            isOnVerticalUpEdge = false;
            isOnVerticalDownEdge = false;

            mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
            isControlling = true;
        }
    }
}
```  
今回はwasdでの移動を行ったが、前述のローパスフィルターをかけたマウス移動でMapを動かせる  
その場合すこし動きが渋い感じがしたので要改善  
PCを基本としているため60フレームでアニメーションさせているが30フレームでも良い機体なら買えても良いと思う  


