# Tactical Combat System 작업 안내

사용자의 최신 지시를 우선하고, 현재 요청에 필요한 문서와 코드만 읽는다.

1. 프로젝트 목적과 공개 구조는 [README.md](README.md)를 확인한다.
2. 코드·Prefab·data를 변경할 때는 [ENGINEERING_GUIDE.md](ENGINEERING_GUIDE.md)의 관련 절과 수정 대상의 인접 코드·test를 읽는다.
3. CommonLib 변경은 Client와 Server 공유·정수 결정성 계약을 함께 검토한다.
4. 기존 worktree의 사용자 변경을 보존하고 관련 없는 파일을 수정·정리하지 않는다.
5. 사용자의 변경 요청은 범위 내 로컬 편집과 비파괴적 build/test를 승인한 것으로 본다. 외부 행동·파괴적 변경·material scope 확대만 다시 확인한다.
6. 완료 시 변경 파일, test/build 근거, 남은 Play 확인을 간결하게 보고한다.

일반 개발론이나 외부 공통 guide를 이 저장소에 복제하지 않는다. 이 저장소의 프로젝트 계약은 `ENGINEERING_GUIDE.md`를 단일 기준으로 삼는다.
