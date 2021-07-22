namespace HlidacStatu.Entities.Enhancers
{


    public interface IEnhancer
    {
        string Name { get; }
        string Description { get; }
        bool Update(ref Smlouva item);

        int Priority { get; }
        void SetInstanceData(object data);
    }


}
