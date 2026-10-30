using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Product> Products()
    {
        return
        [
            Product.Create("P-001", "SKU-001", "Цемент М400", "шт", 120),
            Product.Create("P-002", "SKU-002", "Пісок будівельний", "т", 18),
            Product.Create("P-003", "SKU-003", "Цегла червона", "шт", 4200),
            Product.Create("P-004", "SKU-004", "Фарба водоемульсійна", "шт", 36),
            Product.Create("P-005", "SKU-005", "Шпаклівка фінішна", "кг", 250),
            Product.Create("P-006", "SKU-006", "Профіль CD-60", "м", 800),
            Product.Create("P-007", "SKU-007", "Саморізи 3.5x25", "уп", 140),
            Product.Create("P-008", "SKU-008", "Грунтовка", "л", 60),
            Product.Create("P-009", "SKU-009", "Плитка керамічна", "м2", 310),
            Product.Create("P-010", "SKU-010", "Клей плитковий", "кг", 540),
            Product.Create("P-011", "SKU-011", "Гіпсокартон", "лист", 200),
            Product.Create("P-012", "SKU-012", "Стрічка армуюча", "м", 400),
            Product.Create("P-013", "SKU-013", "Мінеральна вата", "м2", 90),
            Product.Create("P-014", "SKU-014", "Монтажна піна", "шт", 75),
            Product.Create("P-015", "SKU-015", "Труба ПВХ", "м", 160)
        ];
    }
}