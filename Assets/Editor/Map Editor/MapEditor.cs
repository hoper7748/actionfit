using Project.Scripts.Data_Script;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ObjectPropertiesEnum;
using TextAsset = UnityEngine.TextAsset;

public class MapEditor : EditorWindow
{
    private Object levelDbObj;
    private TextAsset currentLevelDataJson;
    private StageData currentLevelDataSO;
    private int tileSize = 40;
    private Vector2 scrollPos;
    private int row, column;
    private BoardBlockData temp;
    private int selectedColorIndex = 0;
    private int selectedWallIndex = 0;

    private EditTileData currentData;
    private WallData currentWallData;

    public class EditTileData
    {
        public int row, col;
        public ColorType color;
        public List<ColorType> colorTypes;
        public List<GimmickData> gimmicks;
        public int dataType;

        // wall일 경우 사용할 데이터
        public WallDirection wallDirection;
        // 컬러 
        public WallGimmickType wallGimmickType;

    }

    private readonly string[] colorOptions = { "None", "Red", "Orange", "Yellow", "Gray", "Purple", "Begic", "Blue", "Green", };
    private readonly string[] gimmickOptions = { "None", };
    private readonly string[] wallGimmickOptions = { "None", "Star" };
    private static bool[] gimmickSet = { false };
    private static bool[] colorSet = { false, false, false, false, false, false, false, false, false };
    private readonly Color[] colors = {
        Color.white,
        Color.red,
        new Color(1f, 0.5f, 0f), // Orange
        Color.yellow,
        new Color(0.5f, 0.5f, 0.5f),     // Gray
        new Color(0.6f, 0f, 0.6f),       // Purple
        new Color(0.96f, 0.96f, 0.86f),  // Begic
        Color.blue,
        Color.green,
    };

    [MenuItem("Tools/Map Editor", false, 0)]
    private static void Init()
    {
        var window = GetWindow(typeof(MapEditor));
        window.titleContent = new GUIContent("Map Editor");
    }

    private void OnGUI()
    {
        Draw();
    }

    private void ColorSetting()
    {
        GUILayout.Label("사용할 색", EditorStyles.boldLabel);
        GUILayout.Space(10);

        for (int i = 1; i < colorOptions.Length; i++)
        {
            GUILayout.BeginHorizontal();

            // 색 미리보기 박스
            Color originalColor = GUI.color;
            GUI.color = colors[i];
            GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.color = originalColor;

            // 색 이름과 선택 버튼
            GUILayout.Label(colorOptions[i], GUILayout.Width(100));

            if (colorSet[i])
            {
                if (GUILayout.Button("< 선택됨", EditorStyles.boldLabel, GUILayout.Width(80)))
                {
                    colorSet[i] = false;
                }
            }
            else if (GUILayout.Button("선택", GUILayout.Width(60)))
            {
                colorSet[i] = true;
            }

            GUILayout.EndHorizontal();
        }
    }
    private void InitData()
    {
        LoadSOData();
    }

    private void Draw()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        var oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 90;

        // 맵 데이터 입력

        InitData();

        GUILayout.Space(20);

        // 버튼을 지정된 좌표에 배치 (x, y, width, height)
        Rect buttonRect = new Rect(350, 0, 150, 40);
        if (currentLevelDataSO != null)
        {
            DrawEditor();
        }
        else
        {
            if (GUI.Button(buttonRect, "Stage Data 생성"))
            {
                CreateMyDataAsset();
            }
        }
        buttonRect = new Rect(550, 0, 75, 40);
        if (GUI.Button(buttonRect, "시작"))
        {
            EditorApplication.isPlaying = !EditorApplication.isPlaying;
        }
        EditorGUILayout.EndScrollView();
    }

    private void CreateMyDataAsset()
    {

        StageData asset = ScriptableObject.CreateInstance<StageData>();

        // 경로 설정
        string path = "Assets/Project/Resource/Data/StageData So";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/StageData.asset");

        // 에셋 생성 및 저장
        AssetDatabase.CreateAsset(asset, uniquePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 생성된 파일을 선택
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log("ScriptableObject 생성 완료: " + uniquePath);
        levelDbObj = EditorGUILayout.ObjectField("Asset", asset, typeof(StageData), false, GUILayout.Width(340));
        currentLevelDataSO = (StageData)levelDbObj;
    }

    private void DrawEditor()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(700));

        var style = new GUIStyle
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        EditorGUILayout.LabelField("Map Edit", style);
        GUILayout.Space(10);


        GUILayout.BeginHorizontal(GUILayout.Width(300));
        EditorGUILayout.HelpBox(
            "The general settings of this level.",
            MessageType.Info);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 보드 전에 세팅 할 수 있는 공간이 있으면 좋음

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("column"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        column = EditorGUILayout.IntField(column, GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("row"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        row = EditorGUILayout.IntField(row, GUILayout.Width(30));

        GUILayout.EndHorizontal();

        ColorSetting();

        GUILayout.Space(10f);

        if (GUILayout.Button("Create Map", GUILayout.Width(250), GUILayout.Height(tileSize)))
        {
            CreateLevel();
        }

        NewDrawBoard();

        GUILayout.Space(10f);

        GUILayout.Label("선택");

        if (currentData != null && (currentData.row > 0 && currentData.col > 0 && currentData.row < row + 2 && currentData.col < column + 2))
        {
            DrawTileInfos();
        }
        else if (currentData != null)
        {
            DrawWallInfos();
        }

        GUILayout.EndVertical();
    }

    private void DrawTileInfos()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("column"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(currentData.col.ToString());
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("row"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(currentData.row.ToString());

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("color"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(colorOptions[(int)currentData.color]);

        GUILayout.EndHorizontal();

        GimmickSet();

        // 원하는 위치에 Rect 생성 (x, y, width, height)
        Rect popupRect = new Rect(190, 495, 75, 20);
        // 팝업 직접 그리기
        selectedColorIndex = EditorGUI.Popup(popupRect, selectedColorIndex, colorOptions);
        selectedBoardPosition = new Vector2Int(currentData.col - 1, currentData.row - 1);

        if (selectedColorIndex > 0 && (ColorType)selectedColorIndex != currentData.color && CheckColorLRTB(currentData.col, currentData.row))
        {
            tempShapeData = new ShapeData();

            // 현재 위치에 해당하는 기존 블록 그룹 제거 시도
            foreach (var block in playerBlocks)
            {
                for (int i = 0; i < block.shapes.Count; i++)
                {
                    var worldPos = block.center + block.shapes[i].offset;
                    if (worldPos == selectedBoardPosition)
                    {
                        block.shapes.RemoveAt(i);
                        if (block.shapes.Count <= 0)
                        {
                            // shape이 없어요!
                            currentLevelDataSO.playingBlocks.Remove(block);
                            Debug.Log("1");
                        }
                        break;
                    }
                }
            }

            // 새로운 블록 추가
            var targetBlock = playerBlocks.FirstOrDefault(p => p.colorType == (ColorType)selectedColorIndex);
            PlayingBlockData target = null;
            foreach (var t in playerBlocks)
            {
                foreach (var shape in t.shapes)
                {
                    if (CheckColorLRTB(currentData.col, currentData.row))
                    {
                        target = t;
                        break;
                    }
                }
            }

            if (target == null)
            {
                target = new PlayingBlockData
                {
                    colorType = (ColorType)selectedColorIndex,
                    center = selectedBoardPosition,
                    shapes = new List<ShapeData>()
                };
                playerBlocks.Add(target);
            }

            tempShapeData.offset = selectedBoardPosition - target.center;

            if (!target.shapes.Any(s => s.offset == tempShapeData.offset))
            {
                target.shapes.Add(tempShapeData);
            }

            currentData.color = (ColorType)selectedColorIndex;
        }
        else if (selectedColorIndex == 0)
        {
            // 색상 제거: 기존 블록에서 제거
            foreach (var block in playerBlocks)
            {
                for (int i = 0; i < block.shapes.Count; i++)
                {
                    var worldPos = block.center + block.shapes[i].offset;
                    if (worldPos == selectedBoardPosition)
                    {
                        block.shapes.RemoveAt(i);
                        if (block.shapes.Count <= 0)
                        {
                            // shape이 없어요!
                            currentLevelDataSO.playingBlocks.Remove(block);
                            Debug.Log("0");
                        }
                        break;
                    }
                }
            }

            currentData.color = ColorType.None;
        }
        else if (selectedColorIndex > 0 && (ColorType)selectedColorIndex != currentData.color)
        {
            Debug.Log("Empty Color");
            //selectedBoardPosition = new Vector2Int(currentData.col - 1, currentData.row - 1);

            // 이제 색을 칠해주고 새로운 PlayerBlocks를 업데이트 함 .
            PlayingBlockData newBlock = new PlayingBlockData
            {
                colorType = (ColorType)selectedColorIndex,
                center = selectedBoardPosition,
                shapes = new List<ShapeData> { new ShapeData { offset = Vector2Int.zero } },
                gimmicks = new List<GimmickData> { new GimmickData { gimmickType = "None" } }
            };

            playerBlocks.Add(newBlock);
            currentLevelDataSO.playingBlocks.Add(newBlock);
            currentData.color = (ColorType)selectedColorIndex;
        }
    }

    // 벽을 누르면 생성되어야 할 데이터 
    //             WallDirection 
    //             lengh
    //              gimmick
    private void DrawWallInfos()
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("column"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(currentData.col.ToString());
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("row"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(currentData.row.ToString());

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("color"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(colorOptions[(int)currentData.color]);

        GUILayout.EndHorizontal();

        // 원하는 위치에 Rect 생성 (x, y, width, height)
        Rect popupRect = new Rect(190, 495, 75, 20);
        // 팝업 직접 그리기
        selectedColorIndex = EditorGUI.Popup(popupRect, selectedColorIndex, colorOptions);
        //selectedBoardPosition = new Vector2Int(currentData.col - 1, currentData.row - 1);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Gimmick"),
            GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.LabelField(wallGimmickOptions[(int)currentData.wallGimmickType]);

        GUILayout.EndHorizontal();

        popupRect = new Rect(190, 515, 75, 20);
        // 팝업 직접 그리기
        selectedWallIndex = EditorGUI.Popup(popupRect, selectedWallIndex, wallGimmickOptions);

        // 데이터 업데이트
        //selectedBoardPosition = new Vector2Int(currentData.col - 1, currentData.row - 1);

        //WallColorUpdate();
        NewWallColorUpdate();
        WallGimmickUpdate();
    }

    private void NewWallColorUpdate()
    {
        if ((ColorType)selectedColorIndex != currentData.color)
        {
            // 색을 칠해
            currentData.color = (ColorType)selectedColorIndex;

            NewWallUpdate();
        }
    }

    private void NewWallUpdate()
    {
        WallContainer.Clear();

        WallData tempWall = null;
        // 4방향 문을 체크

        // 아래부터

        tempWall = new WallData();
        tempWall.x = 1 - 1;
        tempWall.y = 0;
        tempWall.wallColor = tileData[0][1].color;
        tempWall.WallDirection = tileData[0][1].wallDirection;
        tempWall.length = 0;

        for (int x = 1; x <= column + 2; x++)
        {
            if (tempWall.wallColor == tileData[0][x].color)
            {
                tempWall.length++;
            }
            else
            {
                WallContainer.Add(tempWall);
                tempWall = new WallData();
                tempWall.x = x - 1;
                tempWall.y = 0;
                tempWall.wallColor = tileData[0][x].color;
                tempWall.WallDirection = tileData[0][x].wallDirection;
                tempWall.length = 1;
            }
        }
        WallContainer.Add(tempWall);
        // 위

        tempWall = new WallData();
        tempWall.x = 1 - 1;
        tempWall.y = row;
        tempWall.wallColor = tileData[row + 2][1].color;
        tempWall.WallDirection = tileData[row + 2][1].wallDirection;
        tempWall.length = 0;


        for (int x = 1; x <= column + 2; x++)
        {
            if (tempWall.wallColor == tileData[row + 2][x].color)
            {
                tempWall.length++;
            }
            else
            {
                WallContainer.Add(tempWall);
                tempWall = new WallData();
                tempWall.x = x - 1;
                tempWall.y = row;
                tempWall.wallColor = tileData[row + 2][x].color;
                tempWall.WallDirection = tileData[row + 2][x].wallDirection;
                tempWall.length = 1;
            }
        }
        WallContainer.Add(tempWall);

        // 좌
        tempWall = new WallData();
        tempWall.x = 0;
        tempWall.y = 1 - 1;
        tempWall.wallColor = tileData[1][0].color;
        tempWall.WallDirection = tileData[1][0].wallDirection;
        tempWall.length = 0;


        for (int y = 1; y <= row + 2; y++)
        {
            if (tempWall.wallColor == tileData[y][0].color)
            {
                tempWall.length++;
            }
            else
            {
                WallContainer.Add(tempWall);
                tempWall = new WallData();
                tempWall.x = 0;
                tempWall.y = y - 1;
                tempWall.wallColor = tileData[y][0].color;
                tempWall.WallDirection = tileData[y][0].wallDirection;
                tempWall.length = 1;
            }
        }
        WallContainer.Add(tempWall);

        // 우
        tempWall = new WallData();
        tempWall.x = column;
        tempWall.y = 1 - 1;
        tempWall.wallColor = tileData[1][column + 2].color;
        tempWall.WallDirection = tileData[1][column + 2].wallDirection;
        tempWall.length = 0;


        for (int y = 1; y <= row + 2; y++)
        {
            if (tempWall.wallColor == tileData[y][column + 2].color)
            {
                tempWall.length++;
            }
            else
            {
                WallContainer.Add(tempWall);
                tempWall = new WallData();
                tempWall.x = column;
                tempWall.y = y - 1;
                tempWall.wallColor = tileData[y][column + 2].color;
                tempWall.WallDirection = tileData[y][column + 2].wallDirection;
                tempWall.length = 1;
            }
        }
        WallContainer.Add(tempWall);
        currentLevelDataSO.Walls = new List<WallData>(WallContainer);
        //Debug.Log("Call");
    }

    private bool NewFindGroup()
    {
        int cur = 0;
        foreach (var wall in WallContainer)
        {
            if (wall.WallDirection == currentData.wallDirection)
            {
                switch (currentData.wallDirection)
                {
                    case WallDirection.None:
                        break;
                    case WallDirection.Single_Up:
                    case WallDirection.Single_Down:
                        for (int i = 0; i < wall.length; i++)
                        {
                            int targetX = wall.x + i;
                            if ((targetX == currentData.col && wall.y == currentData.row) ||
                                (targetX == currentData.col && wall.y == currentData.row - 2))
                            {
                                Debug.Log("Find");
                                return true;
                            }
                        }
                        break;
                    case WallDirection.Single_Left:
                        break;
                    case WallDirection.Single_Right:
                        break;
                    case WallDirection.Left_Up:
                        break;
                    case WallDirection.Left_Down:
                        break;
                    case WallDirection.Right_Up:
                        break;
                    case WallDirection.Right_Down:
                        break;
                    case WallDirection.Open_Up:
                        break;
                    case WallDirection.Open_Down:
                        break;
                    case WallDirection.Open_Left:
                        break;
                    case WallDirection.Open_Right:
                        break;
                    default:
                        break;
                }
            }
        }
        return false;
    }

    private void FindGroup(EditTileData tile)
    {
        bool isMatch = false;
        foreach (var wall in WallContainer)
        {
            if (!isMatch)
            {
                switch (wall.WallDirection)
                {
                    case WallDirection.Single_Up:
                    case WallDirection.Single_Down:
                        // 수평 벽 (가로줄)
                        if (tile.row == wall.y || tile.row - 2 == wall.y)
                        {
                            for (int i = 0; i < wall.length; i++)
                            {
                                int targetX = wall.x + i;
                                if ((targetX == tile.col - 1 && wall.y == tile.row) ||
                                    (targetX == tile.col - 1 && wall.y == tile.row - 2))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                        }
                        break;

                    case WallDirection.Single_Left:
                    case WallDirection.Single_Right:
                        // 수직 벽 (세로줄)
                        if (tile.col == wall.x || tile.col - 2 == wall.x)
                        {
                            for (int i = 0; i < wall.length; i++)
                            {
                                int targetY = wall.y + i;
                                if ((wall.x == tile.col && targetY == tile.row - 1) ||
                                    (wall.x == tile.col - 2 && targetY == tile.row - 1))
                                {
                                    isMatch = true;
                                    break;
                                }
                            }
                        }
                        break;
                    case WallDirection.Left_Up:
                    case WallDirection.Left_Down:
                    case WallDirection.Right_Up:
                    case WallDirection.Right_Down:
                    case WallDirection.Open_Up:
                    case WallDirection.Open_Down:
                    case WallDirection.Open_Left:
                    case WallDirection.Open_Right:
                    case WallDirection.None:
                    default:
                        break;
                }

                if (isMatch)
                {
                    Debug.Log($"Wall Match Found: {wall.x}, {wall.y} - Direction: {wall.WallDirection}");
                    currentData.color = (ColorType)selectedColorIndex;
                    wall.length += 1;
                    //// TODO: 겹치는 벽 제거 로직이 여기 추가되어야 함
                    // 이제 지워주는 것도 만들어야함...
                    // 어떻게 지워줄까...
                }
            }
        }

        if (isMatch)
        {
            foreach (var wall in WallContainer)
            {
                bool isMatchOriginPos = false;
                int cur = 0;
                if (currentData.color == wall.wallColor)
                    continue;
                // 매치가 된 벽을 찾는 내용.
                switch (wall.WallDirection)
                {
                    case WallDirection.Single_Up:
                    case WallDirection.Single_Down:
                        // 수평 벽 (가로줄)
                        if (currentData.row == wall.y || currentData.row - 2 == wall.y)
                        {
                            for (int i = 0; i < wall.length; i++)
                            {
                                int targetX = wall.x + i;
                                if ((targetX == currentData.col - 1 && wall.y == currentData.row) ||
                                    (targetX == currentData.col - 1 && wall.y == currentData.row - 2))
                                {
                                    Debug.Log($"hold {wall.x} / {wall.y}");
                                    isMatchOriginPos = true;
                                    cur = i;
                                    break;
                                }
                            }
                        }
                        break;

                    case WallDirection.Single_Left:
                    case WallDirection.Single_Right:
                        // 수직 벽 (세로줄)
                        if (tile.col == wall.x || tile.col - 2 == wall.x)
                        {
                            for (int i = 0; i < wall.length; i++)
                            {
                                int targetY = wall.y + i;
                                if ((wall.x == currentData.col && targetY == currentData.row - 1) ||
                                    (wall.x == currentData.col - 2 && targetY == currentData.row - 1))
                                {
                                    Debug.Log($"hold {wall.x} / {wall.y}");
                                    isMatchOriginPos = true;
                                    cur = i;
                                    break;
                                }
                            }
                        }
                        break;
                    case WallDirection.Left_Up:
                    case WallDirection.Left_Down:
                    case WallDirection.Right_Up:
                    case WallDirection.Right_Down:
                    case WallDirection.Open_Up:
                    case WallDirection.Open_Down:
                    case WallDirection.Open_Left:
                    case WallDirection.Open_Right:
                    case WallDirection.None:
                    default:
                        break;
                }
                if (isMatchOriginPos)
                {
                    switch (wall.WallDirection)
                    {
                        case WallDirection.None:
                        case WallDirection.Single_Up:
                            if (wall.y == tile.row - 2)
                            {
                                Debug.Log("A");
                                if (cur == 0)
                                {
                                    wall.x += 1;
                                }
                                else
                                {
                                    // 가르거나 wall.lengh - 1 == cur일 경우 lengh 만 줄여줌.
                                }

                            }
                            break;

                        case WallDirection.Single_Down:
                            if (wall.y == tile.row)
                            {
                                Debug.Log("B");
                                if (cur == 0)
                                {
                                    wall.x += 1;
                                }
                            }
                            break;
                        case WallDirection.Single_Left:
                            if (wall.x == tile.col)
                            {
                                Debug.Log("C");
                                if (cur == 0)
                                {
                                    wall.y += 1;
                                }
                            }
                            break;
                        case WallDirection.Single_Right:
                            if (wall.x == tile.row - 2)
                            {
                                Debug.Log("D");
                                if (cur == 0)
                                {
                                    wall.y += 1;
                                }
                            }
                            break;
                        case WallDirection.Left_Up:
                        case WallDirection.Left_Down:
                        case WallDirection.Right_Up:
                        case WallDirection.Right_Down:
                        case WallDirection.Open_Up:
                        case WallDirection.Open_Down:
                        case WallDirection.Open_Left:
                        case WallDirection.Open_Right:
                        default:
                            break;
                    }
                    wall.length -= 1;
                }
            }
        }
    }

    private void WallColorUpdate()
    {
        // 컬러 인덱스가 1이상이면서 현재 컬러와 다르고, 좌 우를 탐색했을 때 같은 색이 있을 경우
        if (selectedColorIndex > 0 && (ColorType)selectedColorIndex != currentData.color)
        {
            // 좌 또는 우에 존재한다는 것을 체크 
            if (currentData.col == 0 || currentData.col == column + 1)
            {
                if (tileData[currentData.row + 1][currentData.col].color == (ColorType)selectedColorIndex)
                {
                    Debug.Log("AA");
                    FindGroup(tileData[currentData.row + 1][currentData.col]);
                }
                else if (tileData[currentData.row - 1][currentData.col].color == (ColorType)selectedColorIndex)
                {
                    Debug.Log("BB");
                    FindGroup(tileData[currentData.row - 1][currentData.col]);
                }
                else
                {
                    Debug.Log("아예 없는 경우");
                }
            }
            else if (currentData.row == 0 || currentData.row == row + 1)
            {
                if (tileData[currentData.row][currentData.col + 1].color == (ColorType)selectedColorIndex)
                {
                    Debug.Log("CC");
                    FindGroup(tileData[currentData.row][currentData.col + 1]);
                }
                else if (tileData[currentData.row][currentData.col - 1].color == (ColorType)selectedColorIndex)
                {
                    Debug.Log("DD");
                    FindGroup(tileData[currentData.row][currentData.col - 1]);
                }
                else
                {
                    Debug.Log("아예 없는 경우");
                    // wall container를 분류할 필요가 있음.
                }
            }
        }
        //else if (selectedColorIndex > 0 && (ColorType)selectedColorIndex != currentData.color)
        //{
        //    Debug.Log("Empty Color");
        //    //selectedBoardPosition = new Vector2Int(currentData.col - 1, currentData.row - 1);

        //    // 이제 색을 칠해주고 새로운 PlayerBlocks를 업데이트 함 .
        //    PlayingBlockData newBlock = new PlayingBlockData
        //    {
        //        colorType = (ColorType)selectedColorIndex,
        //        center = selectedBoardPosition,
        //        shapes = new List<ShapeData> { new ShapeData { offset = Vector2Int.zero } },
        //        gimmicks = new List<GimmickData> { new GimmickData { gimmickType = "None" } }
        //    };

        //    playerBlocks.Add(newBlock);
        //    currentLevelDataSO.playingBlocks.Add(newBlock);
        //    currentData.color = (ColorType)selectedColorIndex;
        //}
    }

    private void EmptyWallCheck()
    {

    }

    private void WallGimmickUpdate()
    {
        if(currentData.wallGimmickType != (WallGimmickType)selectedWallIndex)
        {
            currentData.wallGimmickType = (WallGimmickType)selectedWallIndex;
            NewWallUpdate();
        }
    }

    private void GimmickSet()
    {
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Gimmick")/*, GUILayout.Width(50)*/);
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();
        bool prev = false;
        bool cur = false;
        for (int i = 0; i < gimmickSet.Length; i++)
        {
            GUILayout.BeginHorizontal();

            prev = gimmickSet[i];
            // 체크박스로 선택 여부 표시
            cur = GUILayout.Toggle(gimmickSet[i], "선택", GUILayout.Width(80));

            if (prev != cur)
            {
                gimmickSet[i] = cur;
                if (cur)
                {
                    // 기믹 추가 

                }
                else
                {
                    // 기믹 제거

                }
            }
            GUILayout.Label(gimmickOptions[i], GUILayout.Width(100));

            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }



    private void CreateLevel()
    {
        currentLevelDataSO.boardBlocks.Clear();
        for (int y = 0; y < row; y++)
        {
            for (int x = 0; x < column; x++)
            {
                temp = new BoardBlockData();
                temp.x = x;
                temp.y = y;
                currentLevelDataSO.boardBlocks.Add(temp);
            }
        }
    }
    private void DrawBoard()
    {
        if (currentLevelDataSO.boardBlocks.Count < 0)
            return;

        // 일단 띄우는 것부터 해봐 뭐부터? 바닥부터 그려봐
        GUILayout.Space(tileSize * 0.5f);
        GUILayout.BeginHorizontal();
        GUILayout.Space(tileSize * 0.5f);

        GUILayout.EndHorizontal();
        GUILayout.Space(tileSize * 0.5f);
    }

    private void NewDrawBoard()
    {
        if (tileData.Count <= 0)
            return;

        int boardPixelWidth = (column + 4) * tileSize;

        // 버튼 UI 영역 시작 (왼쪽 상단 고정)
        GUILayout.BeginArea(new Rect(300, 100, boardPixelWidth, 9999)); // x=10으로 왼쪽 정렬
        for (int y = row + 2; y >= 0; y--)
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            for (int x = 0; x <= column + 2; x++)
            {
                CreateButton(tileData[y][x]);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
    }

    ShapeData tempShapeData;
    private Vector2Int selectedBoardPosition;
    private void CreateButton(EditTileData boardData)
    {
        GUI.color = SetGUIColor(boardData.color);

        if (boardData.row > 0 && boardData.col > 0 && boardData.col < column + 2 && boardData.row < row + 2)
        {
            if (GUILayout.Button("타일", GUILayout.Width(tileSize), GUILayout.Height(tileSize)))
            {
                Debug.Log($"좌표 {boardData.col - 1} / {boardData.row - 1}");
                currentData = boardData;
                if (boardData.gimmicks != null)
                {
                    foreach (var item in boardData.gimmicks)
                    {
                        switch (item.gimmickType)
                        {
                            case "None":
                                gimmickSet[0] = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
                selectedColorIndex = (int)currentData.color;
            }
        }
        else if (!(boardData.row == 0 && boardData.col == 0) && !(boardData.row == 0 && boardData.col == column + 2) &&
                 !(boardData.row == row + 2 && boardData.col == 0) && !(boardData.row == row + 2 && boardData.col == column + 2))
        {
            if (GUILayout.Button("벽", GUILayout.Width(tileSize), GUILayout.Height(tileSize)))
            {
                Debug.Log($"벽 : {boardData.col} / {boardData.row}");
                currentData = boardData;
                selectedColorIndex = (int)currentData.color;
            }
        }
        else
        {
            GUILayout.Space(tileSize + tileSize * 0.15f);
        }
    }

    private bool CheckColorLRTB(int x, int y)
    {
        // 4방향 중 같은 색상과 연결이 된다면?
        // 또는 연결된 색상이 없다면?
        if (x > 0 && x < column + 2 && tileData[y][x].color == (ColorType)selectedColorIndex)
        {
            return true;
        }
        else if (x > 0 && x < column + 2 && tileData[y][x + 1].color == (ColorType)selectedColorIndex)
        {
            return true;
        }
        else if (x > 0 && x < column + 2 && tileData[y][x - 1].color == (ColorType)selectedColorIndex)
        {
            return true;
        }
        else if (y > 0 && y < row + 2 && tileData[y + 1][x].color == (ColorType)selectedColorIndex)
        {
            return true;
        }
        else if (y > 0 && y < row + 2 && tileData[y - 1][x].color == (ColorType)selectedColorIndex)
        {
            return true;
        }
        return false;
    }

    private bool CheckNearColor(int x, int y)
    {
        if (x == 0 || x == column)
        {
            if (tileData[y + 1][x].color == (ColorType)selectedColorIndex)
                return true;
            if (tileData[y - 1][x].color == (ColorType)selectedColorIndex)
                return true;
        }
        else if (y == 0 || y == row)
        {
            if (tileData[y][x + 1].color == (ColorType)selectedColorIndex)
                return true;
            if (tileData[y][x - 1].color == (ColorType)selectedColorIndex)
                return true;
        }

        return false;

    }

    private Color SetGUIColor(ColorType color)
    {
        switch (color)
        {
            case ColorType.None:
                break;
            case ColorType.Red:
                return Color.red;
            case ColorType.Orange:
                return new Color(1f, 0.5f, 0f); // Orange
            case ColorType.Yellow:
                return Color.yellow;
            case ColorType.Gray:
                return Color.gray;
            case ColorType.Purple:
                return new Color(0.6f, 0f, 0.6f); // Pink
            case ColorType.Beige:
                return new Color(0.96f, 0.96f, 0.86f);
            case ColorType.Blue:
                return Color.blue;
            case ColorType.Green:
                return Color.green;
            default:
                break;
        }
        return Color.white;
    }

    public List<List<EditTileData>> tileData = new List<List<EditTileData>>();

    private void LoadSOData()
    {
        var oldDb = levelDbObj;

        levelDbObj = EditorGUILayout.ObjectField("Asset", levelDbObj, typeof(StageData), false, GUILayout.Width(340));
        if (levelDbObj != oldDb)
        {
            currentLevelDataSO = (StageData)levelDbObj;
            row = currentLevelDataSO.boardBlocks[currentLevelDataSO.boardBlocks.Count - 1].y;
            column = currentLevelDataSO.boardBlocks[currentLevelDataSO.boardBlocks.Count - 1].x;
            tileData.Clear();

            EditTileData tempTile = null;

            if (currentLevelDataSO != null)
            {
                int count = 0;
                for (int y = 0; y <= row + 2; y++)
                {
                    List<EditTileData> temp = new List<EditTileData>();
                    for (int x = 0; x <= column + 2; x++)
                    {
                        tempTile = new EditTileData();
                        // 일반 타일
                        if (y != 0 && x != 0 && x != column + 2 && y != row + 2)
                        {
                            tempTile.col = x;
                            tempTile.row = y;
                            tempTile.colorTypes = currentLevelDataSO.boardBlocks[count].colorType;
                            count++;
                        }
                        // 문 
                        else
                        {
                            tempTile.col = x;
                            tempTile.row = y;
                            if (y == 0)
                                tempTile.wallDirection = WallDirection.Single_Down;
                            if (y == row + 2)
                                tempTile.wallDirection = WallDirection.Single_Up;
                            if (x == 0)
                                tempTile.wallDirection = WallDirection.Single_Left;
                            if (x == column + 2)
                                tempTile.wallDirection = WallDirection.Single_Right;
                        }
                        temp.Add(tempTile);
                    }
                    tileData.Add(temp);
                }

                WallContainer.Clear();
                for (int y = 1; y < row + 2; y++)
                {
                    InitWall(0, y);
                    InitWall(column, y);
                }
                for (int x = 1; x < column + 2; x++)
                {
                    InitWall(x, 0);
                    InitWall(x, row);
                }

                playerBlocks.Clear();

                for (int i = 0; i < colorSet.Length; i++)
                {
                    colorSet[i] = false;
                }

                foreach (var pBlocks in currentLevelDataSO.playingBlocks)
                {
                    playerBlocks.Add(pBlocks);

                    // 색이 등장한 것으로 체크
                    colorSet[(int)pBlocks.colorType - 1] = true;

                    foreach (var shapes in pBlocks.shapes)
                    {
                        int y = pBlocks.center.y + shapes.offset.y + 1;
                        int x = pBlocks.center.x + shapes.offset.x + 1;

                        tileData[y][x].color = pBlocks.colorType;
                        tileData[y][x].gimmicks = pBlocks.gimmicks;
                    }
                }
            }
        }
    }

    //Dictionary<ColorType, PlayingBlockData> PlayerBlocks = new Dictionary<ColorType, PlayingBlockData>();
    List<PlayingBlockData> playerBlocks = new List<PlayingBlockData>();


    List<WallData> WallContainer = new List<WallData>();

    private void InitWall(int x, int y)
    {
        for (int i = 0; i < currentLevelDataSO.Walls.Count; i++)
        {
            if (x == 0 || x == column)
            {
                int iy = y - 1;
                if (!WallContainer.Contains(currentLevelDataSO.Walls[i]) && SetWall(currentLevelDataSO.Walls[i], x, iy)) return;
            }
            else if (y == 0 || y == row)
            {
                int ix = x - 1;
                if (!WallContainer.Contains(currentLevelDataSO.Walls[i]) && SetWall(currentLevelDataSO.Walls[i], ix, y)) return;
            }
        }
    }

    private bool SetWall(WallData curWall, int x, int y)
    {
        if (x == curWall.x && y == curWall.y)
        {
            switch (curWall.WallDirection)
            {
                case ObjectPropertiesEnum.WallDirection.None:
                    break;
                case ObjectPropertiesEnum.WallDirection.Single_Up:
                case ObjectPropertiesEnum.WallDirection.Single_Down:
                    if (curWall.y == row)
                        y += 2;
                    x += 1;
                    for (int i = 0; i < curWall.length; i++)
                    {
                        tileData[y][x + i].color = curWall.wallColor;
                        //tileData[y][x + i].wallDirection = curWall.WallDirection;
                        //tileData[y][x + i].gimmicks = curWall.gimm;

                    }
                    break;
                case ObjectPropertiesEnum.WallDirection.Single_Left:
                case ObjectPropertiesEnum.WallDirection.Single_Right:

                    if (curWall.x == column)
                        x += 2;
                    y += 1;
                    for (int i = 0; i < curWall.length; i++)
                    {
                        tileData[y + i][x].color = curWall.wallColor;
                        //tileData[y + i][x].wallDirection = curWall.WallDirection;
                    }
                    break;
                case ObjectPropertiesEnum.WallDirection.Left_Up:
                    break;
                case ObjectPropertiesEnum.WallDirection.Left_Down:
                    break;
                case ObjectPropertiesEnum.WallDirection.Right_Up:
                    break;
                case ObjectPropertiesEnum.WallDirection.Right_Down:
                    break;
                case ObjectPropertiesEnum.WallDirection.Open_Up:
                    break;
                case ObjectPropertiesEnum.WallDirection.Open_Down:
                    break;
                case ObjectPropertiesEnum.WallDirection.Open_Left:
                    break;
                case ObjectPropertiesEnum.WallDirection.Open_Right:
                    break;
                default:
                    break;
            }
            WallContainer.Add(curWall);
            return true;
        }
        return false;
    }


    private void DrawJsonData()
    {
        var oldDb = levelDbObj;
        levelDbObj = EditorGUILayout.ObjectField("Asset", levelDbObj, typeof(TextAsset), false, GUILayout.Width(340));
        if (levelDbObj != oldDb)
        {
            currentLevelDataJson = (TextAsset)levelDbObj;
            try
            {
                var check = JsonUtility.FromJson<StageData>(currentLevelDataJson.text);
                Debug.Log("유효한 데이터");
            }
            catch
            {
                Debug.LogError("유효하지 않은 데이터");
                currentLevelDataJson = null;
            }
        }
    }
}
