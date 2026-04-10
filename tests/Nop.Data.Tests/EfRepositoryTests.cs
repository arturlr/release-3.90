using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Configuration;
using Nop.Data;
using Xunit;

namespace Nop.Data.Tests;

public class EfRepositoryTests : IDisposable
{
    private readonly NopDbContext _context;
    private readonly EfRepository<Setting> _repo;

    public EfRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NopDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new NopDbContext(options);
        _repo = new EfRepository<Setting>(_context);
    }

    [Fact]
    public void Insert_And_GetById()
    {
        var setting = new Setting("test.key", "test.value");
        _repo.Insert(setting);

        setting.Id.Should().BeGreaterThan(0);
        _repo.GetById(setting.Id).Should().NotBeNull();
        _repo.GetById(setting.Id)!.Value.Should().Be("test.value");
    }

    [Fact]
    public void Insert_Batch()
    {
        var settings = new[]
        {
            new Setting("key1", "val1"),
            new Setting("key2", "val2")
        };
        _repo.Insert(settings);

        _repo.Table.Count().Should().Be(2);
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var setting = new Setting("key", "old");
        _repo.Insert(setting);

        setting.Value = "new";
        _repo.Update(setting);

        _repo.GetById(setting.Id)!.Value.Should().Be("new");
    }

    [Fact]
    public void Delete_RemovesEntity()
    {
        var setting = new Setting("key", "val");
        _repo.Insert(setting);
        var id = setting.Id;

        _repo.Delete(setting);

        _repo.GetById(id).Should().BeNull();
    }

    [Fact]
    public void Delete_Batch()
    {
        var settings = new[]
        {
            new Setting("k1", "v1"),
            new Setting("k2", "v2"),
            new Setting("k3", "v3")
        };
        _repo.Insert(settings);

        _repo.Delete(settings.Take(2));

        _repo.Table.Count().Should().Be(1);
    }

    [Fact]
    public void TableNoTracking_ReturnsData()
    {
        _repo.Insert(new Setting("key", "val"));

        _repo.TableNoTracking.Count().Should().Be(1);
    }

    [Fact]
    public void Insert_NullEntity_Throws()
    {
        var act = () => _repo.Insert((Setting)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
