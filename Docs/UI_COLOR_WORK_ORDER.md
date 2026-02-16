# UI Color Work Order

- 마지막 갱신: **2026-02-16**
- 기준 팔레트: `Assets/Scripts/Data/Colors.cs`
- 원칙: 하드코딩 색상 금지, `Colors.Semantic` alias 우선 사용
- 프로토타입 규칙: 정적 기본색은 에디터에서 설정, 코드에서는 상태 기반 동적색만 처리

## 1) 매칭표 (Component -> Semantic Alias)

| 영역 | 컴포넌트 | Alias |
|---|---|---|
| HUD | 상단 수치 텍스트 | `HudStatText` |
| 월드 임무 카드 | 카드 배경 | `MissionWorldCardBg` |
| 월드 임무 카드 | 선택 하이라이트(채움/윤곽) | `MissionWorldCardSelectedFill`, `MissionWorldCardSelectedBorder` |
| 월드 임무 카드 | 임무명 | `MissionWorldCardTitleText` |
| 월드 임무 카드 | 메타 텍스트(기한/배치 인원) | `MissionWorldCardMetaText` |
| 월드 테스트 타일 | 난이도 숫자 | `MissionWorldTestValueText` |
| 월드 테스트 타일 | 클리어 체크 | `MissionWorldTestClearedText` |
| 임무 오버레이 | 타이틀 | `MissionOverlayTitleText` |
| 임무 오버레이 | 기한/보조 텍스트 | `MissionOverlayMetaText` |
| 임무 오버레이 | 태그 텍스트 | `MissionOverlayTagText` |
| 임무 오버레이 | 테스트 숫자 | `MissionOverlayTestValueText` |
| 임무 오버레이 | 테스트 클리어 체크 | `MissionOverlayTestClearedText` |
| 임무 오버레이 | 파티합 숫자 | `MissionOverlayPartyStatValueText` |
| 임무 오버레이 | 슬롯 프레임(사용 가능/불가) | `MissionOverlaySlotFrameUsable`, `MissionOverlaySlotFrameLocked` |
| 임무 오버레이 | 슬롯 `+` / 잠금 오버레이 | `MissionOverlaySlotPlus`, `MissionOverlaySlotLockedOverlay` |
| 임무 오버레이 | 보상/실패 텍스트 | `MissionOverlayRewardText`, `MissionOverlayFailureText` |
| 임무 오버레이 | 원정 시작 버튼 | `MissionOverlayStartButtonBg`, `MissionOverlayStartButtonBgDisabled`, `MissionOverlayStartButtonFg` |
| 모달 | 딤 배경 | `ModalDimBackground` |
| 모달 | 패널 배경/테두리 | `ModalPanelBg`, `ModalPanelBorder` |
| 모달 | 제목/본문 | `ModalTitleText`, `ModalBodyText` |
| 모달 | 확인/취소 버튼 | `ModalConfirmButtonBg/Fg`, `ModalCancelButtonBg/Fg` |

## 2) 적용 순서

1. `Colors.cs` alias 확정
- 신규/기존 UI 매핑용 alias를 `Semantic`에 추가한다.
- Primitive 값 변경 없이 alias만 추가한다.

2. 에디터 기본색 적용
- `Image`, `TMP_Text`, `Button ColorBlock`의 정적 기본색은 프리팹/씬 인스펙터에서 설정한다.
- 런타임 코드에서 정적 기본색을 덮어쓰지 않는다.

3. 코드 동적색 적용
- 상태 기반 색 변경(선택/잠금/버튼 disabled)만 코드 분기에서 alias를 사용한다.

4. Scene/Prefab 잔여 하드코딩 제거
- 코드에서 다루지 않는 `Image`, `TMP_Text`, `Button ColorBlock`를 순서대로 스캔한다.
- 하드코딩된 색이 남아 있으면 대응 alias를 추가하거나 기존 alias로 치환한다.

5. 모달/툴팁 공통 UI 확장 적용
- `ConfirmationModal`, `InfoModal`, Tooltip 계열에 alias를 연결한다.
- 에디터 직렬화 참조 누락 시 자동 복구 없이 에러 로그 후 중단 원칙을 유지한다.

6. 검증
- GameScene 진입 시 HUD/월드카드/오버레이 색이 매칭표와 일치하는지 확인한다.
- 버튼 상태(normal/hover/pressed/disabled) 색이 의도대로 전환되는지 확인한다.
- 컴파일 오류 0 확인.

## 3) 체크리스트

- [ ] 코드 내 `new Color(...)` / 임의 RGBA 하드코딩 제거(예외: 이펙트/실험 코드)
- [ ] Mission UI 동적 상태색에서 alias 기반 색상 사용
- [ ] 모달 계열 alias 적용
- [ ] 문서(`GENERAL_RULES`)와 코드 일치 확인
- [ ] 컴파일 오류 0
