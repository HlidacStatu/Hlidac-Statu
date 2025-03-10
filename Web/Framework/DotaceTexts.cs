using HlidacStatu.Repositories;

namespace HlidacStatu.Web.Framework;

public static class DotaceTexts
{
    public static string NekompletniDataAlert => $"Data za rok {DotaceRepo.LastCompleteYear + 1} nejsou kompletní. Data postupně doplňujeme podle toho, jak je získáváme od státu.";
}