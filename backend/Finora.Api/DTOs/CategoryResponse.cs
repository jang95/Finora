using Finora.Api.Data.Entities;

namespace Finora.Api.DTOs;

// 카테고리 응답. Frontend의 Category 타입(types/category.ts)과 같은 모양이다.
// UserId는 내보내지 않는다. (자기 데이터만 받으므로 필요 없고, 다른 사용자 Id 형식을 알려줄 이유도 없다)
// ParentId: 대분류는 null. 목록은 평평한 배열로 보내고, 화면이 ParentId로 트리를 만든다. (결정 D23)
public record CategoryResponse(int Id, string Name, CategoryType Type, int? ParentId);
