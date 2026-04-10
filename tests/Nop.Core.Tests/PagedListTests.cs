using FluentAssertions;
using Nop.Core;
using Xunit;

namespace Nop.Core.Tests;

public class PagedListTests
{
    [Fact]
    public void Constructor_IList_PagesCorrectly()
    {
        var source = Enumerable.Range(1, 25).ToList();
        var paged = new PagedList<int>(source, pageIndex: 1, pageSize: 10);

        paged.Should().HaveCount(10);
        paged.First().Should().Be(11);
        paged.TotalCount.Should().Be(25);
        paged.TotalPages.Should().Be(3);
        paged.PageIndex.Should().Be(1);
        paged.PageSize.Should().Be(10);
        paged.HasPreviousPage.Should().BeTrue();
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void Constructor_IList_FirstPage()
    {
        var source = Enumerable.Range(1, 5).ToList();
        var paged = new PagedList<int>(source, pageIndex: 0, pageSize: 3);

        paged.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        paged.HasPreviousPage.Should().BeFalse();
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void Constructor_IList_LastPage()
    {
        var source = Enumerable.Range(1, 5).ToList();
        var paged = new PagedList<int>(source, pageIndex: 1, pageSize: 3);

        paged.Should().BeEquivalentTo(new[] { 4, 5 });
        paged.HasPreviousPage.Should().BeTrue();
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void Constructor_IQueryable_PagesCorrectly()
    {
        var source = Enumerable.Range(1, 10).AsQueryable();
        var paged = new PagedList<int>(source, pageIndex: 0, pageSize: 5);

        paged.Should().HaveCount(5);
        paged.TotalCount.Should().Be(10);
        paged.TotalPages.Should().Be(2);
    }

    [Fact]
    public void Constructor_PrePaged_UsesProvidedTotalCount()
    {
        var items = new[] { 1, 2, 3 };
        var paged = new PagedList<int>(items, pageIndex: 2, pageSize: 3, totalCount: 100);

        paged.Should().HaveCount(3);
        paged.TotalCount.Should().Be(100);
        paged.TotalPages.Should().Be(34);
        paged.PageIndex.Should().Be(2);
    }
}
