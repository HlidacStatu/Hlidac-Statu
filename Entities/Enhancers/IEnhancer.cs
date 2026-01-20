using System.Threading.Tasks;

namespace HlidacStatu.Entities.Enhancers
{


    public interface IEnhancer
    {
        string Name { get; }
        string Description { get; }
        Task<bool> UpdateAsync(Smlouva item);

        int Priority { get; }
        void SetInstanceData(object data);
    }


}
