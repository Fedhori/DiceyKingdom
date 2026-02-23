# PROTOTYPE
> 역할: 프로토타입 단계 목표/범위/진행 상태 관리.

- 마지막 갱신: `2026-02-23`

---

## 1) 현재 방향

- 프로젝트명: `Free or Die`
- 컨셉: 군대 전장형이 아니라 투기장 1:1 결투형
- 핵심 단위: `Ability`
- 적 배치 정보: `Intent`
- 미배치 영역: `Bag`
- 후퇴 용어: `Surrender`
- 전투 자원 `Focus`: 사용하지 않음

---

## 2) 완료된 핵심 작업

- Ability 도메인 모델/효과 처리 기본 경로 동작
- Duel 페이즈 오케스트레이션(Reset~ClashResolve)
- GameScene 최소 디버그 패널 연동
- 적 자동 배치 fallback(선호 Clash 실패 시 다른 Clash 탐색)
- EditMode 테스트 53개 통과

---

## 3) 다음 작업 우선순위

1. UI/프리팹 정리
2. Ability 타입별 실제 효과 확장(Attack/Skill/Passive)
3. Encounter/Enemy 스케일 확장(다양한 Clash 구성)
4. 메타 진행(Reward/Maintenance) 최소 루프

---

## 4) 검증 기준

- 컴파일 에러 0
- EditMode 테스트 전부 통과
- GameScene 수동 검증:
  - Start Duel
  - PlayerSetup 배치
  - Roll
  - ClashResolve
  - Surrender

