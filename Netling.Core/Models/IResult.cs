namespace Netling.Core.Models
{
    public interface IResult
    {
        long Bytes { get; }
        bool IsError { get; }
    }
}