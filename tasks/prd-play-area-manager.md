# PRD: PlayAreaManager

## 1. 소개/개요
플레이 영역(PlayArea)과 그 주변 요소(벽, 플레이어 위치, 블록 스폰, 파괴 존)를 수동으로 배치하는 현재 워크플로우를 자동화한다.  
개발자가 PlayAreaManager의 width/height만 변경하면, 에디터에서 즉시 모든 관련 오브젝트가 올바른 위치로 재배치되도록 한다.

## 2. 목표
- PlayArea의 크기 변경 시, 관련 오브젝트가 자동으로 재정렬된다.
- PlayArea 중심은 월드 (0,0)을 기준으로 고정된다.
- 벽/파괴존의 **위치만** 자동 조정되고, 크기는 수동 유지된다.
- 플레이어/블록 스폰 위치는 지정된 오프셋에 따라 자동 배치된다.

## 3. 사용자 스토리
- 개발자로서 PlayArea의 크기를 바꾸면, 벽/플레이어/스폰 위치를 다시 계산하지 않고 즉시 반영되길 원한다.
- 레벨 디자이너로서 Play 단계 진입 전에 배치가 정확한지 에디터에서 바로 확인하고 싶다.

## 4. 기능 요구사항
1) PlayAreaManager는 `width`, `height`(월드 유닛) 값을 가진다.  
2) PlayAreaManager는 PlayArea의 `SpriteRenderer.size`를 `width/height`로 설정한다.  
3) PlayArea의 중심 좌표는 **항상 월드 (0,0)** 이 되도록 강제한다.  
4) 벽(상/하/좌/우)은 **기존 오브젝트를 참조**하여 위치를 자동 조정한다.  
   - 벽의 크기는 PlayAreaManager에서 지정 가능해야 한다.  
   - 벽 크기에 맞게 Collider2D/SpriteRenderer 크기를 자동 설정한다.  
5) 플레이어는 PlayArea 하단에 배치되며, `playerYOffset`(월드 유닛)를 적용한다.  
6) 블록 스폰 위치는 PlayArea 상단에 배치되며, `blockSpawnYOffset`(월드 유닛)를 적용한다.  
   - **y 좌표만 변경**하며, x/z는 유지한다.  
7) BlockDestroyZone은 PlayArea 하단에 배치되며, `blockDestroyYOffset`(월드 유닛)를 적용한다.  
   - x 위치는 0으로 강제한다.  
   - 크기는 수동 유지한다.  
8) ProjectileDestroyZone은 상/하/좌/우 4개 오브젝트를 사용한다.  
   - PlayArea 경계에서 `projectileDestroyOffset`(월드 유닛)만큼 떨어진 위치에 배치한다.  
   - 크기는 수동 유지한다.  
9) **에디터 즉시 반영**: `OnValidate`/`ExecuteAlways`로 변경 사항이 에디터에서 즉시 반영된다.  
10) **런타임 보정**: `Start()`에서 1회 재배치하여 런타임도 동일하게 보장한다.  
11) 레퍼런스가 누락되면 경고 로그를 출력하되, 에러로 중단하지 않는다.

## 5. 비목표 (범위 밖)
- 파괴존의 크기(스케일) 자동 조정은 하지 않는다.
- PlayArea 외 시스템(플레이어 이동 로직, 블록 스폰 로직)의 기능 변경은 하지 않는다.
- 자동 배치 UI/버튼 추가는 하지 않는다.

## 6. 디자인 고려사항
- PlayArea 중심 고정(0,0)을 유지한다.
- 오프셋 값은 **월드 유닛 기준**으로 관리한다.

## 7. 기술 고려사항
- `ExecuteAlways` + `OnValidate`로 에디터에서 즉시 동작하도록 한다.
- 벽의 크기는 PlayAreaManager에서 지정한 값을 사용해 자동 설정한다.  
  - Collider2D와 SpriteRenderer가 존재하면 함께 반영한다.
- 자동 배치 메서드(`ApplyLayout` 등)를 1곳에 모아 재사용한다.

## 8. 성공 지표
- PlayArea 크기 변경 후 수동 배치 작업이 필요하지 않다.
- Play 단계 진입 시 벽/파괴존/스폰 위치가 항상 일관되게 맞는다.

## 9. 오픈 질문
- 벽/ProjectileDestroyZone/BlockDestroyZone의 크기는 **공통 Vector2 1개**로 통일한다.
