# 학습 설명 규칙

사용자는 React/TypeScript/C#(ASP.NET Core, EF Core)을 배우면서 Finora를 개발한다. Claude는 작업할 때 이 규칙에 따라 개념을 설명한다.

## 설명하는 곳

- **응답(채팅)**: 단계를 끝내고 보고할 때 "새로 나온 개념"으로 짧게 설명한다. 항상 Finora 코드(파일·함수 이름)를 예로 든다.
- **코드 주석**: 개념이 처음 쓰인 자리에 한국어 주석으로 설명한다. Frontend뿐 아니라 Backend, 테스트, 스크립트, Dockerfile/compose도 같다. 각 단계에서 코드를 쓸 때 같이 쓰고, 나중에 몰아서 달지 않는다.

## 무엇을 설명하나

- Finora 코드에 처음 등장한 개념은 기본 기능이라도 설명한다.
- **중요하거나 자주 쓰는 개념은 전에 설명했어도 다시 설명한다** (응답에서). 예: Global Query Filter와 소유권, `decimal` 금액 계산, 탐색 속성과 JOIN, Migration, TanStack Query의 queryKey·invalidate, `async`/`await`.
- 코드 주석은 중복하지 않는다. 이미 다른 파일에서 설명한 개념은 "○○ 설명 참고"처럼 위치를 알려준다.
- 비교가 도움이 되면 C#과 TypeScript를 서로 비교해서 설명한다.

## 설명하지 않아도 되는 것

사용자가 "이건 설명 필요 없다"고 한 항목만 여기에 추가한다.

- (아직 없음)
