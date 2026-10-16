namespace ProjectFlow.Core.Interfaces;

public interface IActor
{
    string Id
    {
        get;
    }
    bool IsAdmin
    {
        get;
    }
    bool IsManager
    {
        get;
    }
}
