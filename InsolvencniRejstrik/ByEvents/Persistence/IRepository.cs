using HlidacStatu.Entities.Insolvence;
using System;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	interface IRepository
	{
		Task<Rizeni> GetInsolvencyProceedingAsync(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding);
		Task SetInsolvencyProceedingAsync(Rizeni item);
	}
}
