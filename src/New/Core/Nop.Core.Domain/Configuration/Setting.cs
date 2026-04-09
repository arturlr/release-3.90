using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Configuration;

public class Setting : BaseEntity, ILocalizedEntity
{
    public Setting() { }

    public Setting(string name, string value, int storeId = 0)
    {
        Name = name;
        Value = value;
        StoreId = storeId;
    }

    public string? Name { get; set; }
    public string? Value { get; set; }
    public int StoreId { get; set; }

    public override string? ToString() => Name;
}
