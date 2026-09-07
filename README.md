# Tactical Combat System

클라이언트의 전투 결과를 그대로 신뢰하지 않고, **서버가 동일한 공용 로직으로 전투를 재현해 결과를 비교**하는 Unity/C# 전투 시스템입니다. 전투는 클라이언트에서 실시간으로 진행하고, 종료 후 기록을 서버에 전달해 일치 여부를 확인합니다.

화면 연출과 전투 판정을 분리해 자연스러운 플레이와 서버 재현을 함께 구현하는 것이 목표입니다.

1인 개발 / 2025.04 ~ 현재 / 주요 기술: Unity, C#

**[시연 영상](https://youtu.be/VCDOGYQ65sg)**

---

## 핵심 포인트

### 1) 클라이언트/서버 공용 로직 기반 전투 재현·검증

- **클라이언트**: 전투 진행과 연출, 시뮬레이션에 적용한 프레임 간격 기록
- **서버**: 서버가 보유한 맵 데이터로 초기화하고, 전달받은 프레임 간격을 순서대로 공용 시뮬레이터에 적용
- **검증**: 승패와 생존 캐릭터 수를 비교하고, 요청에 담긴 각 캐릭터 ID의 HP를 서버 재현 결과와 대조

검증 API는 결과 일치 여부를 반환하며, 보상 승인과는 아직 연결되지 않았습니다.

### 2) 동일 입력의 재현을 위한 수치와 갱신 순서 관리

- 좌표와 방향은 고정소수점(`FixedPos` / `FixedDir`), 전투 갱신 시간은 정수 밀리초로 처리
- Entity와 Projectile은 Dictionary의 순회 순서에 의존하지 않고 별도 목록에서 ID 순서로 갱신
- 동일 입력의 재현을 위해 갱신 순서와 동률 선택 기준을 명시하고 테스트 작성

### 3) 인터페이스 기반 의존성 분리 (시각 표현 ↔ 전투 판정)

- 전투 객체는 `IBattleMapContext`로 주변 상태를 조회하고 행동을 요청하며, 시뮬레이터는 `IBattleMapEventHandler`로 전투 이벤트 전달
- 공용 로직은 이동과 공격, 피해를 판정하고, 클라이언트 전용 코드는 View와 UI 갱신 및 서버 통신 담당

### 4) 상태 기반 전투 구조

- 캐릭터 행동을 상태 단위(`IState`: Enter / Update / Exit)로 분리해 책임 경계를 명확화
- `EntityBrain`은 다음 상태를 판단하고, `Entity`는 전환 대기와 실제 상태 교체를 관리

### 5) 정수 기반 경로 탐색 (데이터 주도 맵)

- 정적 장애물 환경의 효율적인 경로 탐색을 위해 **Visibility Graph + A\*** 적용
- 경로 충돌 판정 안정화를 위해 **Bresenham 확장(super-cover) 셀 샘플링** 적용
- 맵 형태·장애물은 **데이터(에디터 툴)로 구성** → 기반 코드 변경 없이 새 맵/장애물 추가
- 미리 지정된 지점 외에도 현재 위치와 새 목적지 사이의 경로 탐색 지원

### 6) 교전 위치 예측과 캐릭터별 전투 배치

- 최초 적 발견 시 양 팀 전열을 복제하고 공용 전투 로직으로 선행 시뮬레이션해 예상 교전 위치 계산
- 캐릭터별 공격 사거리와 아군 간격, 기존 좌우 배치와 이동 경로를 고려해 전투 위치 선정
- 사거리 진입 전에 이동을 시작해 교전 직전의 급격한 위치 조정을 줄이고, 예측에 실패하면 기존 목적지 유지

관련 코드: [FrontlineEncounterPredictor](Assets/Script/CommonLib/Map/FrontlineEncounterPredictor.cs), [InitialTacticalFormationPlanner](Assets/Script/CommonLib/Map/InitialTacticalFormationPlanner.cs)

---

## 서버 (요약)

1. **전투 재현·검증**: 클라이언트 기록 재현, 결과 비교 및 측정 로그 기록
2. **게임 컨텐츠 API**: Enter / Clear / GiveUp에 Redis 캐시, DB 요청 로그, UNIQUE 제약과 조건부 상태 갱신을 적용해 재시도와 중복 처리 제어

> 서버 구성과 설정에 대한 설명은 **[Server/README.md](Server/README.md)** 를 참조해 주세요.

---

## 기술 스택

- **클라이언트**: Unity 2022.3.58f1 (C#), Editor에서 실행 및 검증
- **서버**: .NET 8 / ASP.NET Core Web API
- EF Core + MySQL, Redis, xUnit, Swagger

---

## 프로젝트 구조(요약)

**[Assets/Script] — Unity 클라이언트 / 공용 로직**

- `CommonLib/` : 클라이언트·서버가 공유하는 코드
  - `Entity.cs`, `EntityBrain.cs`, `MoveState.cs` : 캐릭터 상태 관리, 다음 상태 판단, 이동
  - `Battle/` : 상태 인터페이스와 공격·대기·퇴각 상태
  - `Map/` : 전투 시뮬레이터, 맵 데이터, 경로 탐색과 최초 교전 배치
    - `Path/` : 고정소수점과 격자 좌표, 셀 방문 도구
  - `Tables/`, `Requests/`, `Responses/`, `Tests/`
- `ClientLib/` : 클라이언트 전용(`ClientBattleMapSimulator`, `EntityView`, 네트워크 `ApiClient`, 카메라)
- `EditorLib/` : 맵/장애물 편집 도구(`BattleMapEditor`, `ObstacleEditor`)

**[Server] — 게임 서버**

- `MiniServerProject` : 전투 검증 + 컨텐츠 API (상세는 `Server/README.md`)
- `MiniServerProject.Tests` : MySQL 임시 DB를 사용하는 서비스 통합 테스트(xUnit)
- `MiniServerProject.TestClient` : 콘솔 기반 테스트 클라이언트

---

## 검증 결과

클라이언트 기록을 서버에서 재현한 뒤 승패, 생존 캐릭터 수와 요청의 ID별 HP를 비교한 측정값입니다.

| 테스트 횟수 | 결과 일치 | 평균 전투 시간 | 평균 재현 시간 | P95 재현 시간 | 평균 프레임 수 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 210회 | 210/210 | 302.4초 | 80.7ms | 97.5ms | 18,620.6 |

2026-09-05, 동일한 1번 맵에서 양 팀의 HP와 공격 스탯을 조정해 약 300초 분량의 전투를 반복 측정했습니다. 서버는 Debug 구성으로 실행했으며, Blue 73승, Red 122승, 무승부 15회 모두 클라이언트와 서버의 결과가 일치했습니다.

첫 실행을 포함한 전체 210회를 집계했습니다. P95는 재현 시간을 오름차순으로 정렬했을 때 200번째 값(97.542ms)입니다. 재현 시간에는 서버 시뮬레이터 생성과 초기화, 기록된 프레임 간격 재현, 최종 결과 비교가 포함되며 HTTP 통신, DB 조회와 맵 데이터 로딩은 포함되지 않습니다.

동일 맵과 편성에서의 측정 결과이며, 플랫폼 간 일치와 더 다양한 전투 조건은 추가 검증이 필요합니다.

---

## 실행 / 코드 검토 안내

**공개 저장소를 클론한 상태만으로는 클라이언트와 서버를 바로 실행할 수 없습니다.**

- **클라이언트**: Unity Editor 실행을 기준으로 개발했습니다. 필수 유료 에셋(캐릭터 모델 등)은 라이선스상 공개 저장소에서 제외해 별도 준비가 필요합니다.
- **서버**: Git에 포함되지 않은 Unity 생성 파일 `CommonLib.csproj`와 로컬 Unity 참조 경로가 필요합니다. DB와 Redis 설정은 [Server/README.md](Server/README.md)를 참고해 주세요.

코드는 다음을 중심으로 봐주시면 됩니다.

- **공용 전투 로직**: [BattleMapSimulator](Assets/Script/CommonLib/Map/BattleMapSimulator.cs), [Entity](Assets/Script/CommonLib/Entity.cs), [EntityBrain](Assets/Script/CommonLib/EntityBrain.cs)
- **경로 탐색과 전투 배치**: [BattleMapPathFinder](Assets/Script/CommonLib/Map/BattleMapPathFinder.cs), [InitialTacticalFormationPlanner](Assets/Script/CommonLib/Map/InitialTacticalFormationPlanner.cs)
- **클라이언트 연동과 시각화**: [ClientBattleMapSimulator](Assets/Script/ClientLib/ClientBattleMapSimulator.cs), [EntityView](Assets/Script/ClientLib/EntityView.cs)
- **서버 검증 진입점**: [StageService.VerifyBattleAsync](Server/MiniServerProject/Application/Stages/StageService.cs)
- **공용 로직 테스트**: [CommonLib/Tests](Assets/Script/CommonLib/Tests)

---

## AI 활용과 코드 검토

코드 분석부터 구현, 테스트 및 리뷰까지 AI를 활용했습니다. AI를 활용한 변경은 해당 커밋에 사용 내역을 명시했습니다.

- 변경된 코드와 실제 플레이를 직접 확인하고, 필요한 기능인지와 유지보수할 수 있는 구조인지를 기준으로 검토
- 필요성이 낮은 경로 보정 로직은 제거하고, 배치와 경로 탐색의 책임을 분리하며 핵심 동작 중심으로 테스트 정리
- 공통 개발 기준과 [프로젝트별 규칙](ENGINEERING_GUIDE.md)을 문서로 관리하고, 검토 피드백을 누적해 이후 작업에도 활용

---

## 현재 범위와 확장 방향

현재는 1번 맵에서 클라이언트가 전달한 프레임 간격으로 전투를 재현하고 결과를 비교합니다. 이를 바탕으로 다음 방향의 확장을 고려하고 있습니다.

- 검증 결과를 보상 승인 조건으로 연결
- 다양한 맵과 캐릭터 편성, 플랫폼에서 결과 일치 여부 검증
- 서버 기준 경과 시간과 누적 전투 시간을 비교해, 지나치게 이른 결과 제출을 점검하는 방안 검토
- Random Seed Log로 랜덤 요소를 재현하고, Active Skill 및 Player Input Log까지 검증 대상 확장
