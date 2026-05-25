using System;
using System.Collections.Generic;

namespace Ayapos.Api.Data.Entities;

public partial class AppSetting
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}
