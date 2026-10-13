using Finora.Api.Data.Entities;

namespace Finora.Api.Data;

// 가입할 때 자동으로 만들어 주는 기본 카테고리 (계획서 8장). 이후 설정 화면에서 사용자가 고칠 수 있다.
public static class DefaultCategories
{
    private static readonly string[] IncomeNames = ["급여", "용돈", "기타수입"];
    private static readonly string[] ExpenseNames =
        ["식비", "카페·간식", "교통", "주거·통신", "생활용품", "쇼핑", "의료·건강", "문화·여가", "경조사", "기타지출"];

    public static List<Category> CreateFor(string userId)
    {
        var now = DateTime.UtcNow;
        // Select((name, index) => ...): 각 항목과 순번(0, 1, 2...)을 함께 받아 SortOrder로 쓴다.
        return IncomeNames.Select((name, index) => New(name, CategoryType.Income, index))
            .Concat(ExpenseNames.Select((name, index) => New(name, CategoryType.Expense, index)))
            .ToList();

        // 지역 함수: 이 메서드 안에서만 쓰는 작은 도우미 함수
        Category New(string name, CategoryType type, int sortOrder) => new()
        {
            UserId = userId,
            Name = name,
            Type = type,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
