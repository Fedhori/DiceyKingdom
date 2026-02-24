# PROTOTYPE
> 역할: 프로토타입 단계 목표/범위/진행 상태 관리.

- 마지막 갱신: `2026-02-24`

---

## 1) 현재 방향

- 프로젝트명: `Free or Die`
- 컨셉: 투기장 1:1 결투
- 핵심 단위: `Ability`
- 전투 공간: `Combat` 3개 고정
- 미배치 영역: `Loadout`
- 후퇴 용어: `Surrender`
- 전투 중 소모 자원: 없음

---

## 2) 완료된 핵심 작업

- 기본 전투 루프(Reset~Resolve) 동작
- GameScene 디버그 패널 연동
- Ability 데이터 `slash.sword`, `shield.up` 기준으로 교체
- EditMode 테스트 통과(53개)
- Docs 깨짐 복구 및 최신 기획 기준 재정렬 진행

---

## 3) 다음 작업 우선순위

1. 코드/데이터 마이그레이션
2. `Clash -> Combat` 타입/필드명 정리
3. `Pattern` 및 `Encounter` 제거
4. `Enemy` 단일 데이터 구조로 로더/검증기 전환
5. 적 Ability 무작위 Combat 배치 고정
6. UI 텍스트/로그 용어 `Combat`로 통일
7. 테스트 케이스 재정비

---

## 4) 검증 기준

- 컴파일 에러 0
- EditMode 테스트 전부 통과
- GameScene 수동 검증:
  - Start Duel
  - OpponentSetup(적 랜덤 배치 확인)
  - PlayerSetup 배치
  - Roll
  - Resolve
  - Surrender
