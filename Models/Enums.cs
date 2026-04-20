using System.ComponentModel;

namespace ServiserOnline.Models;

public enum CaseStatus
{
    [Description("Primljen")]
    Yellow = 1,
    [Description("Prihvacen")]
    Orange,
    [Description("Neuspesno zavrsen")]
    LightGreen,
    [Description("Uspesno zavrsen")]
    Green,
    [Description("Odbijen")]
    Black,
    [Description("Fakturisan")]
    LightBlue,
    [Description("U konfliktu")]
    Purple
}

public enum PayWhen
{
    [Description("Naplati Odmah")]
    PayNow,
    [Description("Naplati kasnije")]
    PayLater,
    [Description("Bez naplate")]
    NoPay
}
