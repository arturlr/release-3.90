using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Blogs;

namespace Nop.Data.Mapping.Blogs;

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPost");
        builder.HasKey(bp => bp.Id);
    }
}

public class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> builder)
    {
        builder.ToTable("BlogComment");
        builder.HasKey(bc => bc.Id);
    }
}
