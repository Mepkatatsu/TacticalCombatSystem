# Tactical Combat System

클라이언트의 전투 결과를 신뢰하지 않고, **서버가 동일한 공용 로직으로 전투를 재현·검증**하는 클라이언트/서버 전투 시스템입니다. 전투는 클라이언트에서 실시간으로 진행하되, 결과의 신뢰성은 서버 재현으로 확보하는 구조를 목표로 했습니다.

플레이 경험(실시간 연출)과 결과 신뢰성(서버 검증)을 동시에 확보하기 위해, 클라이언트와 서버가 **같은 전투 로직을 공유**하고, 모든 계산을 **정수 기반**으로 설계해 플랫폼에 상관없이 항상 같은 결과가 나오도록 했습니다.

---

## 핵심 포인트

### 1) 클라이언트/서버 공용 로직 기반 전투 재현·검증
전투 자체는 클라이언트에서 실시간으로 진행하고, 서버는 실시간으로 개입하지 않습니다.

- **클라이언트**: 공용 로직으로 전투를 시뮬레이션 + 연출 + 프레임 간격(`updateInterval`) 기록
- **서버**: 클라이언트가 보낸 프레임 간격을 **동일한 공용 시뮬레이터로 재실행**
- **검증**: 승패 / 생존 캐릭터 체력 상태를 비교해 일치 여부 판정

→ 실시간 서버 개입 비용 없이 **사후 검증(post-verification)** 만으로 결과 신뢰성을 확보합니다.

### 2) 결정성(Determinism)을 위한 정수 기반 설계
- 좌표·거리 계산을 모두 정수(고정소수점, `FixedPos` / `FixedDir`)로 처리
- 부동소수점 누적 오차를 배제 → **동일 입력이면 항상 동일 결과**
- 플랫폼/환경이 달라도 클라이언트와 서버 재현 결과가 일치

### 3) 인터페이스 기반 의존성 분리 (시각 표현 ↔ 전투 판정)
- 직접 참조 대신 인터페이스(`IBattleMapContext`, `IBattleMapEventHandler`)로 요청/이벤트만 전달
- **클라이언트는 시각화만**(`EntityView` 등), **전투 판정은 공용 로직**에서 처리
- 동일 구조를 서버에서도 그대로 재사용 → 클라이언트와 서버가 같은 전투 로직 공유

### 4) 상태 기반 전투 구조
- 캐릭터 행동을 상태 단위(`IState`: Enter / Update / Exit)로 분리해 책임 경계를 명확화
- 상태 전이 판단을 `EntityBrain` 한 곳에 모아 일관되게 관리
- 프레임 간격(ms) 기반 갱신으로 **동일 입력 → 동일 상태 전이** 보장

### 5) 정수 기반 경로 탐색 (데이터 주도 맵)
- 정적 장애물 환경의 효율적인 경로 탐색을 위해 **Visibility Graph + A\*** 적용
- 경로 충돌 판정 안정화를 위해 **Bresenham 확장(super-cover) 셀 샘플링** 적용
- 맵 형태·장애물은 **데이터(에디터 툴)로 구성** → 기반 코드 변경 없이 새 맵/장애물 추가
- 다양한 이동 시나리오를 단위 테스트로 검증

---

## 서버 (요약)

서버(`MiniServerProject`)는 두 가지 역할을 담당합니다.

1. **전투 재현·검증**: 클라이언트 기록을 공용 로직으로 재실행하고 결과를 검증 (`Domain/Battle`, `Application/Stages/BattleHistory`)
2. **게임 컨텐츠 API**: 스테이지 Enter / Clear / GiveUp을 **서버 중심 상태 전이**로 처리하고, **멱등성(Redis + DB Log + UNIQUE)**, 정적 테이블 기반 컨텐츠 로직, 전역 예외 미들웨어 등 실서비스 패턴을 최소 범위로 구현

> 서버 설계의 상세(멱등성 처리 흐름, 상태 전이 API, 정적 테이블, 예외 처리, 실행 방법 등)는 **[Server/README.md](Server/README.md)** 를 참조해 주세요.

---

## 기술 스택

- **클라이언트**: Unity 2022.3 LTS (C#)
- **서버**: .NET 8 / ASP.NET Core Web API
- EF Core + MySQL, Redis, xUnit, Swagger

---

## 프로젝트 구조(요약)

**[Assets/Script] — Unity 클라이언트 / 공용 로직**
- `CommonLib/` : 클라이언트·서버가 공유하는 코드
  - `Battle/` : 상태 기반 전투(`IState`, `AttackState`/`IdleState`/`DieState`, `EntityStateType`)
  - `Map/` : 전투 시뮬레이터·맵 데이터(`BattleMapSimulator`, `BattleMapData`, `IBattleMapContext`)
    - `Path/` : 정수 기반 경로 탐색(`FixedPos`, `GridPos`, `BresenhamSuperCoverNodeVisitor`)
  - `Tables/`, `Requests/`, `Responses/`, `Tests/`
- `ClientLib/` : 클라이언트 전용(`ClientBattleMapSimulator`, `EntityView`, 네트워크 `ApiClient`, 카메라)
- `EditorLib/` : 맵/장애물 편집 도구(`BattleMapEditor`, `ObstacleEditor`)

**[Server] — 게임 서버**
- `MiniServerProject` : 전투 검증 + 컨텐츠 API (상세는 `Server/README.md`)
- `MiniServerProject.Tests` : 서비스 레이어 단위 테스트(xUnit)
- `MiniServerProject.TestClient` : 콘솔 기반 테스트 클라이언트

---

## 검증 결과

클라이언트 기록 기반 서버 재현 검증 결과입니다. (검증 기준: 승패, 생존 캐릭터 체력 상태)

| 항목 | 값 |
| --- | --- |
| 테스트 횟수 | 105회 |
| 결과 일치율 | 100% |
| 평균 재현 시간 | 31.13ms |
| 평균 전투 시간 | 316.05s |
| 평균 프레임 수 | 19,510.98 프레임 |

→ 실시간 서버 개입 없이도 전투 결과를 검증할 수 있음을 확인했습니다.

---

## 실행 / 코드 검토 안내

**클라이언트는 클론한 상태로는 정상 실행되지 않습니다.**
클라이언트는 유료 서드파티 에셋(캐릭터 모델 등)을 사용하며, 라이선스상 공개 저장소에 포함할 수 없어 `.gitignore`로 제외하고 별도로 관리하고 있습니다. 따라서 Unity에서 그대로 열어 실행하면 필수 리소스(모델 등)가 로드되지 않습니다.

코드는 다음을 중심으로 봐주시면 됩니다.

- **공용 전투 로직**: `Assets/Script/CommonLib` — 클라이언트와 서버가 공유하는 핵심 로직
- **클라이언트 연동·시각화**: `Assets/Script/ClientLib`
- **서버**: `Server/` — 빌드·마이그레이션·실행·테스트 절차는 **[Server/README.md](Server/README.md)** 참조

---

## 확장 방향

현재는 프레임 간격 기록 기반 결과 검증 단계이며, 다음 순서로 확장 가능하도록 설계했습니다.

1. (현재) 프레임 간격 기록 기반 결과 검증
2. (다음) Random Seed Log 검증 추가 (크리티컬 등 랜덤 요소)
3. (확장) Active Skill / Player Input Log 검증 확장
