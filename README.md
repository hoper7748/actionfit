# Block Editor System

## 작성된 코드
### BardController
- BoardController+CreateBoard => BoardController에서 보드의 크기에 따라 동적으로 새로운 보드를 만들어주는 기능을 분리. 
- BoardController+CreatePlayingBlock => BoardController에서 사용자가 조작할 수 있는 블럭을 생성 기능을 분리.
- BoardController+CreateWall => BoardCOntroller에서 블럭과 상호작용이 가능한 벽을 만드는 기능을 분리.

### DragHandler
- BlockInputHandler => BlockDragHandler에서 블럭을 조작하는 기능을 분리.
- CollisionHandler => BlockDragHandler에서 블럭에 충돌이 가해지면 실행되는 기능을 분리.
- DragMovementHandler => BlockDragHandler에서 사용자의 조작에 따라 블럭을 움직이는 기능을 분리

## MapEditor
- 맵 에디터

## 사용법
1. Unity 메뉴에서 [Tools > Map Editor] 클릭
2. Asset에 ScriptableObject 추가
3. 생성된 보드의 타일을 누르면 왼쪽 하단에 타일 정보가 뜸
4. 정보 수정이 진행되면 자동 저장
