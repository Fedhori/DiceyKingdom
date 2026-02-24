# 일반 규칙
> 역할: 프로젝트 전반에 공통 적용되는 개발 규칙/원칙 기준 문서.

- 마지막 갱신: `2026-02-24`

---

## 문서 범위

- 범용 개발 규칙은 이 문서에 기록한다.
- 게임 기획/구조/루프는 `Docs/GAME_STRUCTURE.md`를 단일 기준으로 사용한다.
- 데이터 스키마는 `Docs/DATA_SCHEMA.md`를 단일 기준으로 사용한다.
- 용어 기준은 `Docs/GLOSSARY.md`를 단일 기준으로 사용한다.

## Unity/코드 규칙

- Unity 버전: `Unity 6.2 (6000.2.x)`
- C# 들여쓰기: 4 spaces
- 중괄호: Allman braces
- 신규 코드는 `Assets/Scripts/Game` 하위에만 추가한다.
- 신규 네임스페이스는 `Game.*`로 시작한다.

## JSON/데이터 규칙

- `JsonUtility` 사용 금지
- JSON 직렬화/역직렬화는 `Newtonsoft.Json` 사용
- JSON `id` 및 참조 ID(`abilityId`, `enemyId` 등)는 점(`.`) 표기법을 사용한다.
- 예시: `ability.slash.sword`, `ability.shield.up`, `enemy.northern.footman`, `duel.config`
- 언더스코어(`_`) 기반 신규 ID는 사용하지 않는다.

## 난수 규칙

- `UnityEngine.Random` 사용 금지
- 난수는 `System.Random` 사용
- 테스트/디버깅 재현을 위해 seed 주입 가능한 구조를 우선한다.

## 데이터 보정/검증 규칙

- 조용한 수정(silent fix) 금지
- 자동 보정이 필요한 경우 최소 `Debug.LogWarning`을 남긴다.
- 검증 실패 데이터는 숨기지 않고 실패로 처리한다.

## 런타임/UI 규칙

- 런타임 오브젝트 생성은 Prefab 기반으로 관리한다.
- UI 배치(위치/크기/정렬)는 가능한 한 에디터에서 설정한다.
- 코드에서의 UI 배치 조정은 불가피한 경우에만 사용한다.

## 문서/커뮤니케이션 규칙

- 문서와 피드백은 이해하기 쉬운 표현을 우선한다.
- 어려운 용어를 쓰면 바로 쉬운 말로 풀어쓴다.
- 모호한 표현 대신, 동작/필드/조건을 구체적으로 적는다.

## 문서 인코딩 규칙

- `Docs/*.md` 파일은 반드시 `UTF-8 (without BOM)`으로 저장한다.

## 와이어프레임 제시 규칙

- 와이어프레임은 PNG 파일로 만들어 제시한다.

## 문서 갱신 규칙

- 코드/설계 변경 시 관련 문서를 함께 갱신한다.
- 의사결정 기록은 사용자가 명시적으로 지시한 경우에만 반영한다.

## 문서 맵

- 게임 구조: `Docs/GAME_STRUCTURE.md`
- 데이터 스키마: `Docs/DATA_SCHEMA.md`
- 용어 사전: `Docs/GLOSSARY.md`
- 프로토타입 진행: `Docs/PROTOTYPE.md`
