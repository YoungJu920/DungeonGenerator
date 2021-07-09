using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGeneratorByBinarySpacePartitioning
{
    public class TreeNode
    {
        public TreeNode leftTree;
        public TreeNode rightTree;
        public TreeNode parentTree;
        public RectInt treeSize;
        public RectInt dungeonSize;

        public TreeNode(int x, int y, int width, int height)
        {
            treeSize.x = x;
            treeSize.y = y;
            treeSize.width = width;
            treeSize.height = height;
        }
    }

    public enum TileType
    {
        GROUND = 0,
        DYNAMIC = 1,
        STATIC = 2,
        FLOOR = 3,
    }

    public class DungeonGenerator : MonoBehaviour
    {
        [SerializeField] private Vector2Int mapSize;

        [SerializeField] private int maxNode;
        [SerializeField] private float minDivideSize;
        [SerializeField] private float maxDivideSize;
        [SerializeField] private int minRoomSize;
        [SerializeField] private int bridgeSize;

        [SerializeField] private GameObject line;
        [SerializeField] private Transform lineHolder;
        [SerializeField] private GameObject rectangle;

        [SerializeField] private Tile dynamicTile;
        [SerializeField] private Tile staticTile;
        [SerializeField] private RuleTile ruleTile;

        [SerializeField] private Tile leftBottomTile;
        [SerializeField] private Tile centerBottomTile;
        [SerializeField] private Tile rightBottomTile;
        [SerializeField] private Tilemap tilemap;

        TileType[,] map;
       

        private void Awake()
        {
            map = new TileType[mapSize.x, mapSize.y];

            OnDrawRectangle(0, 0, mapSize.x, mapSize.y); //던전 사이즈에 맞게 벽을 그림
            TreeNode rootNode = new TreeNode(0, 0, mapSize.x, mapSize.y); //루트가 될 트리 생성
            DivideTree(rootNode, 0); //트리 분할
            GenerateDungeon(rootNode, 0); //방 생성
            GenerateRoad(rootNode, 0); //길 연결

            DrawMap();
        }

        private void DivideTree(TreeNode treeNode, int n) //재귀 함수
        {
            if (n < maxNode) //0 부터 시작해서 노드의 최댓값에 이를 때 까지 반복
            {
                RectInt size = treeNode.treeSize; //이전 트리의 범위 값 저장, 사각형의 범위를 담기 위해 Rect 사용
                int length = size.width >= size.height ? size.width : size.height; //사각형의 가로와 세로 중 길이가 긴 축을, 트리를 반으로 나누는 기준선으로 사용
                int split = Mathf.RoundToInt(Random.Range(length * minDivideSize, length * maxDivideSize)); //기준선 위에서 최소 범위와 최대 범위 사이의 값을 무작위로 선택
                if (size.width >= size.height) //가로
                {
                    TreeNode node1 = new TreeNode(size.x, size.y, split, size.height); //기준선을 반으로 나눈 값인 split을 가로 길이로, 이전 트리의 height값을 세로 길이로 사용
                    TreeNode node2 = new TreeNode(size.x + split, size.y, size.width - split, size.height); //x값에 split값을 더해 좌표 설정, 이전 트리의 width값에 split값을 빼 가로 길이 설정

                    // 방향 정하기
                    int rand = Random.Range(0, 2);
                    treeNode.leftTree = rand == 0 ? node2 : node1;
                    treeNode.rightTree = rand == 0 ? node1 : node2;
                    
                    OnDrawLine(new Vector2(size.x + split, size.y), new Vector2(size.x + split, size.y + size.height)); //기준선 렌더링
                }
                else //세로
                {
                    TreeNode node1 = new TreeNode(size.x, size.y, size.width, split);
                    TreeNode node2 = new TreeNode(size.x, size.y + split, size.width, size.height - split);

                    // 방향 정하기
                    int rand = Random.Range(0, 2);
                    treeNode.leftTree = rand == 0 ? node1 : node2;
                    treeNode.rightTree = rand == 0 ? node2 : node1;

                    OnDrawLine(new Vector2(size.x, size.y + split), new Vector2(size.x + size.width, size.y + split));
                }
                treeNode.leftTree.parentTree = treeNode; //분할한 트리의 부모 트리를 매개변수로 받은 트리로 할당
                treeNode.rightTree.parentTree = treeNode;
                DivideTree(treeNode.leftTree, n + 1); //재귀 함수, 자식 트리를 매개변수로 넘기고 노드 값 1 증가 시킴
                DivideTree(treeNode.rightTree, n + 1);
            }
        }

        private RectInt GenerateDungeon(TreeNode treeNode, int n) //방 생성
        {
            if (n == maxNode) //노드가 최하위일 때만 조건문 실행
            {
                RectInt size = treeNode.treeSize;
                //int width = Mathf.Max(Random.Range(size.width / 2, size.width - 1)); //트리 범위 내에서 무작위 크기 선택, 최소 크기 : width / 2
                //int height = Mathf.Max(Random.Range(size.height / 2, size.height - 1));
                int width = (int)Mathf.Max(Random.Range(size.width * 0.4f, size.width * 0.5f)); //트리 범위 내에서 무작위 크기 선택, 최소 크기 : width / 2
                int height = (int)Mathf.Max(Random.Range(size.height * 0.4f, size.height * 0.5f));

                int x = treeNode.treeSize.x + (int)((size.width - width) * 0.5f); //최대 크기 : width / 2
                int y = treeNode.treeSize.y + (int)((size.height - height) * 0.5f);

                SpreadToRoom(x, y, width, height, 1.8f);
                OnDrawDungeon(x, y, width, height); //던전 렌더링
                return new RectInt(x, y, width, height); //리턴 값은 던전의 크기로 길을 생성할 때 크기 정보로 활용
            }
            treeNode.leftTree.dungeonSize = GenerateDungeon(treeNode.leftTree, n + 1); //리턴 값 = 던전 크기
            treeNode.rightTree.dungeonSize = GenerateDungeon(treeNode.rightTree, n + 1);
            return treeNode.leftTree.dungeonSize; //부모 트리의 던전 크기는 자식 트리의 던전 크기 그대로 사용
        }

        private void GenerateRoad(TreeNode treeNode, int n) //길 연결
        {
            if (n == maxNode) return; //노드가 최하위일 때는 길을 연결하지 않음, 최하위 노드는 자식 트리가 없기 때문
            int x1 = GetCenterX(treeNode.leftTree.dungeonSize); //자식 트리의 던전 중앙 위치를 가져옴
            int x2 = GetCenterX(treeNode.rightTree.dungeonSize);
            int y1 = GetCenterY(treeNode.leftTree.dungeonSize);
            int y2 = GetCenterY(treeNode.rightTree.dungeonSize);

            
            // 가로 브릿지
            for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++) //x1과 x2중 값이 작은 곳부터 값이 큰 곳까지 타일 생성
            {
                if (y1 + 3 < mapSize.y) map[x, y1 + 3] = GetRandomTile();
                if (y1 + 2 < mapSize.y) map[x, y1 + 2] = GetRandomTile();
                if (y1 + 1 < mapSize.y) map[x, y1 + 1] = GetRandomTile();

                map[x, y1] = TileType.STATIC;

                if (y1 - 1 >= 0)        map[x, y1 - 1] = GetRandomTile();
                if (y1 - 2 >= 0)        map[x, y1 - 2] = GetRandomTile();
                if (y1 - 3 >= 0)        map[x, y1 - 3] = GetRandomTile();
            }

            // 세로 브릿지
            for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
            {
                if (x2 - 3 >= 0)        map[x2 - 3, y] = GetRandomTile();
                if (x2 - 2 >= 0)        map[x2 - 2, y] = GetRandomTile();
                if (x2 - 1 >= 0)        map[x2 - 1, y] = GetRandomTile();

                map[x2, y] = TileType.STATIC;

                if (x2 + 1 < mapSize.x) map[x2 + 1, y] = GetRandomTile();
                if (x2 + 2 < mapSize.x) map[x2 + 2, y] = GetRandomTile();
                if (x2 + 3 < mapSize.x) map[x2 + 3, y] = GetRandomTile();
            }
                
            GenerateRoad(treeNode.leftTree, n + 1);
            GenerateRoad(treeNode.rightTree, n + 1);
        }

        private void OnDrawLine(Vector2 from, Vector2 to) //라인 렌더러를 이용해 라인을 그리는 메소드
        {
            LineRenderer lineRenderer = Instantiate(line, lineHolder).GetComponent<LineRenderer>();
            Vector2 halfSize = new Vector2(mapSize.x / 2, mapSize.y / 2);
            lineRenderer.SetPosition(0, from - halfSize);
            lineRenderer.SetPosition(1, to - halfSize);
        }

        private void OnDrawDungeon(int x, int y, int width, int height) //크기에 맞춰 타일을 생성하는 메소드
        {
            for (int i = x; i < x + width; i++)
                for (int j = y; j < y + height; j++)
                {
                    //tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, j - mapSize.y / 2, 0), tile);
                    map[i, j] = TileType.STATIC;
                }   
        }

        private void SpreadToRoom(int x, int y, int width, int height, float ratio)   // 방 크기의 ratio 만큼 확장한 영역에 0,1 뿌려준다.
        {
            int _width = (int)(width * ratio);
            int _height = (int)(height * ratio);
            int _x = x - ((int)((_width - width) * 0.5f));
            int _y = y - ((int)((_height - height) * 0.5f));

            for (int i = _x; i < _x + _width; i++)
                for (int j = _y; j < _y + _height; j++)
                {
                    map[i, j] = GetRandomTile();
                }
        }

        private TileType GetRandomTile()
        {
            return Random.Range(0, 100) >= 45 ? TileType.DYNAMIC : TileType.GROUND;
        }

        // DYNAMIC -> STATIC 벽에 붙이기
        private void Absorption()
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int k = 0; k < map.GetLength(1); k++)
                {
                    if (map[i,k] == TileType.DYNAMIC && (GetNeighborCount(i, k, TileType.DYNAMIC) + GetNeighborCount(i, k, TileType.STATIC) >= 4))
                        map[i,k] = TileType.DYNAMIC;

                    else if (map[i,k] == TileType.GROUND && (GetNeighborCount(i, k, TileType.DYNAMIC) + GetNeighborCount(i, k, TileType.STATIC) >= 5))
                        map[i,k] = TileType.DYNAMIC;

                    else if (map[i,k] == TileType.GROUND || map[i,k] == TileType.DYNAMIC)
                        map[i,k] = TileType.GROUND;
                }
            }
        }

        // 보정 작업
        private void Correction()
        {
            // 보정 작업1 (상하좌우에 GROUND가 3개 이상이면 걍 없애버림)
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int k = 0; k < map.GetLength(1); k++)
                {
                    if (map[i,k] == TileType.DYNAMIC && GetCrossCount(i, k) >= 3)
                        map[i,k] = TileType.GROUND;
                }
            }

            // 보정 작업2 (2층 타일 처리)
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int k = 0; k < map.GetLength(1); k++)
                {
                    if (map[i,k] != TileType.GROUND)
                        continue;

                    if (k + 1 < mapSize.y && (map[i, k+1] == TileType.DYNAMIC || map[i, k+1] == TileType.STATIC)
                    && k - 1 >= 0 && map[i, k-1] == TileType.GROUND)
                    {
                        map[i,k] = TileType.FLOOR;
                    }
                }
            }
        }

        private int GetNeighborCount(int x, int y, TileType type)
        {
            int count = 0;

            if (x - 1 >= 0 && y + 1 < mapSize.y && map[x - 1, y + 1] == type)
                count++;

            if (y + 1 < mapSize.y && map[x, y + 1] == type)
                count++;

            if (x + 1 < mapSize.x && y + 1 < mapSize.y && map[x + 1, y + 1] == type)
                count++;

            if (x - 1 >= 0 && map[x - 1, y] == type)
                count++;

            if (x + 1 < mapSize.x && map[x + 1, y] == type) 
                count++;

            if (x - 1 >= 0 && y - 1 >= 0 && map[x - 1, y - 1] == type)
                count++;

            if (y - 1 >= 0 && map[x, y - 1] == type)
                count++;

            if (x + 1 < mapSize.x && y - 1 >= 0 && map[x + 1, y - 1] == type)
                count++;

            return count;
        }

        private int GetCrossCount(int x, int y)
        {
            int count = 0;

            if (y + 1 < mapSize.y && map[x, y + 1] == TileType.GROUND)
                count++;

            if (x - 1 >= 0 && map[x - 1, y] == TileType.GROUND)
                count++;

            if (x + 1 < mapSize.x && map[x + 1, y] == TileType.GROUND) 
                count++;

            if (y - 1 >= 0 && map[x, y - 1] == TileType.GROUND)
                count++;

            return count;
        }

        private void OnDrawRectangle(int x, int y, int width, int height) //라인 렌더러를 이용해 사각형을 그리는 메소드
        {
            LineRenderer lineRenderer = Instantiate(rectangle, lineHolder).GetComponent<LineRenderer>();
            Vector2 halfSize = new Vector2(mapSize.x / 2, mapSize.y / 2);
            lineRenderer.SetPosition(0, new Vector2(x, y) - halfSize); //위치를 화면 중앙에 맞춤
            lineRenderer.SetPosition(1, new Vector2(x + width, y) - halfSize);
            lineRenderer.SetPosition(2, new Vector2(x + width, y + height) - halfSize);
            lineRenderer.SetPosition(3, new Vector2(x, y + height) - halfSize);
            lineRenderer.SetPosition(4, new Vector2(x, y) - halfSize);
        }

        private int GetCenterX(RectInt size)
        {
            return size.x + size.width / 2;
        }

        private int GetCenterY(RectInt size)
        {
            return size.y + size.height / 2;
        }

        
        private void DrawMap()
        {
            Absorption();
            Correction();

            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int k = 0; k < map.GetLength(1); k++)
                {
                    if (map[i,k] == TileType.STATIC)
                        tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), ruleTile);

                    if (map[i,k] == TileType.DYNAMIC)
                        tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), ruleTile);

                    if (map[i,k] == TileType.FLOOR)
                    {
                        if (i - 1 >= 0 && map[i - 1, k] == TileType.GROUND)
                            tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), leftBottomTile);

                        else if (i + 1 < mapSize.x && map[i + 1, k] == TileType.GROUND)
                            tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), rightBottomTile);

                        else
                            tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), centerBottomTile);
                    }
                }
            }

            // // 보정 작업2 (2층 타일 처리)
            // for (int i = 0; i < map.GetLength(0); i++)
            // {
            //     for (int k = 0; k < map.GetLength(1); k++)
            //     {
            //         if (map[i,k] != TileType.GROUND)
            //             continue;

            //         if (k + 1 < mapSize.y && map[i, k+1] == TileType.DYNAMIC
            //         && k - 1 >= 0 && map[i, k-1] == TileType.GROUND)
            //         {
            //             tilemap.SetTile(new Vector3Int(i - mapSize.x / 2, k - mapSize.y / 2, 0), centerBottomTile);
            //         }
            //     }
            // }


        }

        public void Reset()
        {
            // LineRenderes 클리어
            LineRenderer[] renderers = this.gameObject.GetComponentsInChildren<LineRenderer>();
            for (int i = 0; i < renderers.Length; i++)
                Destroy(renderers[i].gameObject);
            renderers = null;

            // 타일맵 클리어
            tilemap.ClearAllTiles();

            map = new TileType[mapSize.x, mapSize.y];

            OnDrawRectangle(0, 0, mapSize.x, mapSize.y); //던전 사이즈에 맞게 벽을 그림
            TreeNode rootNode = new TreeNode(0, 0, mapSize.x, mapSize.y); //루트가 될 트리 생성
            DivideTree(rootNode, 0); //트리 분할
            GenerateDungeon(rootNode, 0); //방 생성
            GenerateRoad(rootNode, 0); //길 연결

            DrawMap();
        }
    }
}