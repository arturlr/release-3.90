namespace Nop.Core.Domain.Security
{

    public interface IAclSupported
    {

        bool SubjectToAcl { get; set; }
    }
}
