namespace HlidacStatu.Lib.Analytics
{
    public interface IAddable<T>
    {
        T Add(T other);
        T Subtract(T other);
    }
}
