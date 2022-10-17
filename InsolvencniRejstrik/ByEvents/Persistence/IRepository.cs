using HlidacStatu.Entities.Insolvence;
using System;

namespace InsolvencniRejstrik.ByEvents
{
	interface IRepository
	{
		Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding);
		void SetInsolvencyProceeding(Rizeni item);
	}
}
